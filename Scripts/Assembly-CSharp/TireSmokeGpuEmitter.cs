using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using CarX;
using DI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class TireSmokeGpuEmitter : BaseWheelSmoke
{
	private readonly struct ColorOption
	{
		public readonly Color Color;

		public readonly bool Override;

		public ColorOption(Color color)
		{
			Color = color;
			Override = true;
		}

		public ColorOption(bool @override)
		{
			Color = default(Color);
			Override = @override;
		}
	}

	private RaceCar _raceCar;

	private NativeArray<WheelIndex> _wheelAccessors;

	private NativeArray<float3> _wheelPositions;

	private NativeArray<int> _smokeSurfaces;

	private NativeArray<ColorOption> _smokeSurfaceColors;

	private GraphicsQuality _graphicsQuality;

	private NativeArray<float> _temperature;

	private NativeArray<float> _spawnRate;

	private (float activeCar, float opponentCar) _opacitySettings;

	private bool _active;

	private static int _nextCarId = -1;

	private int _carId;

	private RaceCar _activeCar;

	private bool _wantClear;

	private bool IsActiveCar
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (_raceCar != null)
			{
				return _raceCar == _activeCar;
			}
			return false;
		}
	}

	private static event Action<int> FlushCarSmoke;

	public void Init(RaceCar raceCar)
	{
		_raceCar = raceCar;
		if ((bool)_raceCar)
		{
			_carId = FetchNextCarId();
			FlushCarSmoke += OnSmokeFlush;
			CARXWheelAccessor[] componentsInChildren = _raceCar.GetComponentsInChildren<CARXWheelAccessor>(includeInactive: true);
			FillWheels(_raceCar.carX, componentsInChildren, out _wheelAccessors, out _wheelPositions);
			_temperature = new NativeArray<float>(_wheelAccessors.Length, Allocator.Persistent);
			_spawnRate = new NativeArray<float>(_wheelAccessors.Length, Allocator.Persistent);
			_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
			if ((bool)_graphicsQuality)
			{
				GraphicsQuality graphicsQuality = _graphicsQuality;
				graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Combine(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
				OnSmokeLevelChange(_graphicsQuality.smokeLevel);
			}
			CameraSwitch instance = CameraSwitch.instance;
			if ((bool)instance)
			{
				instance.onSwitchCar -= OnSwitchCar;
				instance.onSwitchCar += OnSwitchCar;
			}
			if (States.current is GarageGUIState)
			{
				_activeCar = _raceCar;
			}
			FillSurfaces(CARXSurfaceManager.GetSceneTemplates(), out _smokeSurfaces, out _smokeSurfaceColors);
		}
	}

	protected override bool OnEnableHandler()
	{
		WheelSmokeGpuSimulation.OnGpuEmissionStep -= OnCollectGpuEmissionData;
		WheelSmokeGpuSimulation.OnGpuEmissionStep += OnCollectGpuEmissionData;
		WheelSmokeGpuSimulation.CollectGpuClearRequests -= OnCollectClearRequests;
		WheelSmokeGpuSimulation.CollectGpuClearRequests += OnCollectClearRequests;
		return true;
	}

	protected override void OnDisableHandler()
	{
		WheelSmokeGpuSimulation.OnGpuEmissionStep -= OnCollectGpuEmissionData;
		WheelSmokeGpuSimulation.CollectGpuClearRequests -= OnCollectClearRequests;
	}

	public override void ClearSmoke()
	{
		base.ClearSmoke();
		_wantClear = true;
	}

	private void OnDestroy()
	{
		if (_wheelAccessors.IsCreated)
		{
			_wheelAccessors.Dispose();
		}
		if (_smokeSurfaces.IsCreated)
		{
			_smokeSurfaces.Dispose();
		}
		if (_smokeSurfaceColors.IsCreated)
		{
			_smokeSurfaceColors.Dispose();
		}
		if (_wheelPositions.IsCreated)
		{
			_wheelPositions.Dispose();
		}
		if (_temperature.IsCreated)
		{
			_temperature.Dispose();
		}
		if (_spawnRate.IsCreated)
		{
			_spawnRate.Dispose();
		}
		if ((bool)_graphicsQuality)
		{
			GraphicsQuality graphicsQuality = _graphicsQuality;
			graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Remove(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
		}
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.onSwitchCar -= OnSwitchCar;
		}
		FlushCarSmoke -= OnSmokeFlush;
		FlushCarId(_carId);
	}

	private static void FillWheels(Car car, CARXWheelAccessor[] components, out NativeArray<WheelIndex> wheelAccessors, out NativeArray<float3> wheelPositions)
	{
		wheelAccessors = new NativeArray<WheelIndex>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		for (int i = 0; i < components.Length; i++)
		{
			wheelAccessors[i] = (components[i] ? components[i].wheelIndex : WheelIndex.FrontLeft);
		}
		wheelPositions = new NativeArray<float3>(wheelAccessors.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		for (int j = 0; j < wheelAccessors.Length; j++)
		{
			WheelIndex wheelIndex = wheelAccessors[j];
			Transform wheelTransform = car.GetWheelTransform(wheelIndex);
			wheelPositions[j] = wheelTransform.position;
		}
	}

	private static void FillSurfaces(ReadOnlyCollection<CARXSurfaceTemplate> templates, out NativeArray<int> smokeSurfaces, out NativeArray<ColorOption> smokeSurfaceColors)
	{
		int num = 0;
		for (int i = 0; i < templates.Count; i++)
		{
			if (templates[i] != null && templates[i].emitSmoke)
			{
				num++;
			}
		}
		smokeSurfaces = new NativeArray<int>(num, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		smokeSurfaceColors = new NativeArray<ColorOption>(num, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		for (int j = 0; j < smokeSurfaceColors.Length; j++)
		{
			smokeSurfaceColors[j] = new ColorOption(@override: false);
		}
		num = 0;
		for (int k = 0; k < templates.Count; k++)
		{
			CARXSurfaceTemplate cARXSurfaceTemplate = templates[k];
			if (cARXSurfaceTemplate == null || !cARXSurfaceTemplate.emitGpuSmoke)
			{
				continue;
			}
			smokeSurfaces[num++] = CARXSurfaceManager.IndexOf(cARXSurfaceTemplate);
			if (cARXSurfaceTemplate.wheelVfxData != null)
			{
				CARXWheelsParticlesData particleData = cARXSurfaceTemplate.wheelVfxData.particleData;
				if (particleData != null && !particleData.colorCustomization)
				{
					smokeSurfaceColors[num - 1] = new ColorOption(particleData.color);
				}
			}
		}
	}

	private static int FetchNextCarId()
	{
		return Interlocked.Increment(ref _nextCarId);
	}

	private static void FlushCarId(int id)
	{
		TireSmokeGpuEmitter.FlushCarSmoke?.Invoke(id);
		Interlocked.Decrement(ref _nextCarId);
	}

	private void OnSmokeFlush(int flushId)
	{
		if (_carId > flushId)
		{
			_carId--;
		}
	}

	private void OnSwitchCar(RaceCar car)
	{
		_activeCar = car;
	}

	private bool IsAppropriateSurface(int surface, out ColorOption color)
	{
		color = default(ColorOption);
		if (!_smokeSurfaces.IsCreated || !_smokeSurfaceColors.IsCreated)
		{
			return false;
		}
		for (int i = 0; i < _smokeSurfaces.Length; i++)
		{
			if (_smokeSurfaces[i] == surface)
			{
				color = _smokeSurfaceColors[i];
				return true;
			}
		}
		return false;
	}

	private static uint PackColor(Color color)
	{
		return (uint)((double)math.saturate(color.r) * 255.0) | (((uint)((double)math.saturate(color.g) * 255.0) | (((uint)((double)math.saturate(color.b) * 255.0) | ((uint)((double)math.saturate(color.a) * 255.0) << 8)) << 8)) << 8);
	}

	private static float3 ClamplVector(float3 v, float maxLength)
	{
		float num = math.saturate(maxLength * math.rsqrt(math.dot(v, v)));
		return v * num;
	}

	private static float SpeedRelativeInterpolation(float4 data, float speed)
	{
		return math.lerp(data.x, data.y, math.saturate((speed - data.z) / data.w));
	}

	private void OnCollectGpuEmissionData(WheelGpuEmissionState state)
	{
		if (base.isEnabled && !(_raceCar == null) && !(_raceCar.carX == null) && _wheelAccessors.IsCreated && _active && _carId < state.CarsData.Length)
		{
			CollectGpuEmissionData(state);
		}
	}

	private void CollectGpuEmissionData(WheelGpuEmissionState state)
	{
		Car carX = _raceCar.carX;
		ref SmokeCar baseCarParams = ref state.BaseCarParams;
		baseCarParams.carSpeed = carX.speedKMH;
		baseCarParams.carVelocity = carX.velocity;
		state.CarId = math.max(state.CarId, _carId + 1);
		float3 float5 = carX.getTransform.up;
		int num = math.min(_wheelAccessors.Length, 4);
		for (int i = 0; i < num; i++)
		{
			WheelIndex wheelIndex = _wheelAccessors[i];
			Wheel wheel = carX.GetWheel(wheelIndex);
			Transform wheelTransform = carX.GetWheelTransform(wheelIndex - (wheelIndex & WheelIndex.FrontRight) + (1 - (wheelIndex & WheelIndex.FrontRight)));
			Transform wheelTransform2 = carX.GetWheelTransform(wheelIndex);
			bool num2 = wheelIndex == WheelIndex.FrontLeft || wheelIndex == WheelIndex.RearLeft || wheelIndex == WheelIndex.Rear1Left || wheelIndex == WheelIndex.Rear2Left;
			bool flag = wheelIndex > WheelIndex.FrontRight;
			int num3 = (_carId << 2) + i;
			ref SmokeWheel baseWheelParams = ref state.BaseWheelParams;
			baseWheelParams.radius = wheel.radius;
			baseWheelParams.width = wheel.width;
			baseWheelParams.angularSpeed = wheel.w;
			baseWheelParams.pos = wheelTransform2.position;
			baseWheelParams.velocity = ClamplVector(carX.velocity, SpeedRelativeInterpolation(state.WheelVelocityClamp, baseCarParams.carSpeed));
			baseWheelParams.steered = wheel.addSteerAngle;
			baseWheelParams.isRear = (flag ? 1f : 0f);
			baseWheelParams.up = float5;
			float num4 = (num2 ? (-1f) : 1f);
			baseWheelParams.dir = (flag ? math.normalizesafe(carX.transform.forward) : math.cross(wheelTransform2.right, float5));
			baseWheelParams.outside = math.cross(baseWheelParams.up, baseWheelParams.dir) * num4;
			float3 float6 = wheelTransform.position;
			baseWheelParams.axisCenter = (baseWheelParams.pos + float6) * 0.5f;
			baseWheelParams.axisRadius = math.distance(baseWheelParams.pos, float6) * 0.5f;
			baseWheelParams.dir = math.normalizesafe(baseWheelParams.dir);
			baseWheelParams.outside = math.normalizesafe(baseWheelParams.outside);
			baseWheelParams.up = math.normalizesafe(baseWheelParams.up);
			baseWheelParams.stuckMaxSpeed = SpeedRelativeInterpolation(state.StuckSpeedParams, baseCarParams.carSpeed);
			Vector3 left;
			Vector3 right;
			Vector3 cnt;
			Vector3 n;
			bool patch = wheel.GetPatch(out left, out right, out cnt, out n);
			float num5 = math.distancesq(_wheelPositions[i], baseWheelParams.pos);
			num5 = math.saturate(num5 / state.EmissionCountConfig.MaxEmissionDistance);
			_wheelPositions[i] = baseWheelParams.pos;
			baseWheelParams.pos += math.normalizesafe(baseWheelParams.outside) * state.ContactPointSideOffset;
			state.WheelsData[num3] = baseWheelParams;
			if (flag)
			{
				baseCarParams.bs2 = new float4(baseWheelParams.axisCenter, baseWheelParams.axisRadius * state.AxesScale.z);
			}
			else
			{
				baseCarParams.bs0 = new float4(baseWheelParams.axisCenter, baseWheelParams.axisRadius * state.AxesScale.x);
			}
			float splashMix = SpeedRelativeInterpolation(state.FluffyParams, baseCarParams.carSpeed);
			float dustMix = SpeedRelativeInterpolation(state.ParticleWeightParams, baseCarParams.carSpeed);
			bool smoke = wheel.GetSmoke();
			float4 float7 = (flag ? state.RearWheelTemperature : state.FrontWheelTemperature);
			_temperature[i] += math.select(float7.x, float7.y, smoke) * Time.deltaTime;
			_temperature[i] = math.clamp(_temperature[i], 0f, float7.z);
			if (!patch || !wheel.GetUserData(out var userData) || !IsAppropriateSurface(userData, out var color) || (!smoke && _temperature[i] <= float7.w))
			{
				continue;
			}
			EmissionCountConfig emissionCountConfig = state.EmissionCountConfig;
			float time = math.saturate(_temperature[i] / float7.z);
			float num6 = emissionCountConfig.TemperatureTransparentFactor.Evaluate(time);
			Color color2 = base.smokeColor;
			color2.a *= num6;
			color2.a *= (IsActiveCar ? _opacitySettings.activeCar : _opacitySettings.opponentCar);
			if (math.abs(color2.a) <= float.Epsilon)
			{
				continue;
			}
			baseWheelParams.smokeColor = PackColor(color.Override ? (color.Color * color2) : color2);
			state.WheelsData[num3] = baseWheelParams;
			float num7 = _spawnRate[i];
			num7 += (UnityEngine.Random.Range(emissionCountConfig.TemperatureEmissionVariation.x, emissionCountConfig.TemperatureEmissionVariation.y) * emissionCountConfig.TemperatureEmissionRate.Evaluate(time) + UnityEngine.Random.Range(emissionCountConfig.ParticlesPerDistanceVariation.x, emissionCountConfig.ParticlesPerDistanceVariation.y) * num5) * Time.deltaTime;
			int num8 = (int)num7;
			num7 -= (float)num8;
			_spawnRate[i] = num7;
			if (num8 != 0)
			{
				for (int j = 0; j < num8; j++)
				{
					ref EmittingSpot baseEmitter = ref state.BaseEmitter;
					baseEmitter.contactPoint = cnt;
					baseEmitter.wheelID = (uint)num3;
					baseEmitter.dustMix = dustMix;
					baseEmitter.splashMix = splashMix;
					baseEmitter.groundY = baseEmitter.contactPoint.y;
					baseEmitter.randomSeed = UnityEngine.Random.value;
					state.SpawnData[state.ActiveId++] = baseEmitter;
				}
			}
		}
		baseCarParams.bs1 = new float4((baseCarParams.bs0.xyz + baseCarParams.bs2.xyz) * 0.5f, (baseCarParams.bs0.w + baseCarParams.bs2.w) * 0.5f * state.AxesScale.y);
		baseCarParams.bs0 += new float4(0f, state.AxesYOffset.x, 0f, 0f);
		baseCarParams.bs1 += new float4(0f, state.AxesYOffset.y, 0f, 0f);
		baseCarParams.bs2 += new float4(0f, state.AxesYOffset.z, 0f, 0f);
		state.CarsData[_carId] = baseCarParams;
	}

	private void OnCollectClearRequests(CarSmokeClearState state)
	{
		if (_wantClear)
		{
			state.Requests.Add((uint)_carId);
			_wantClear = false;
		}
	}

	private void OnSmokeLevelChange(int qualityLevel)
	{
		switch (qualityLevel)
		{
		case 3:
			_active = true;
			break;
		case 2:
			_active = true;
			break;
		case 1:
			_active = true;
			break;
		case 0:
			_active = false;
			break;
		}
		_opacitySettings = (activeCar: _graphicsQuality.playerSmokeOpacity, opponentCar: _graphicsQuality.opponentSmokeOpacity);
	}
}
