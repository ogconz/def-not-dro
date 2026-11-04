using System;
using CarX;
using DI;
using Unity.Mathematics;
using UnityEngine;

public class WheelsParticles : BaseWheelSmoke
{
	private class WheelData
	{
		public CARXWheelAccessor wheelAccessor;

		public Vector3 lastEmitPosition;

		public Vector3 lastEmitVelocity;

		public float3 activeEmitPosition;

		public float elapsedAccumulation;

		public bool wasStopped;

		public void Reset()
		{
			lastEmitPosition = Vector3.zero;
			lastEmitVelocity = Vector3.zero;
			elapsedAccumulation = 0f;
			wasStopped = true;
		}
	}

	private CARXWheelsParticlesData m_data;

	private ParticleSystem m_emitter;

	private WheelData[] m_wheelData;

	private RaceCar m_car;

	private GraphicsQuality m_graphicsQuality;

	private bool m_qualitySubscribed;

	private bool m_requireUpdateMaxParticleSize;

	private bool m_lastActiveState;

	private float m_maxParticleSize;

	private float m_emissionRateScale;

	private int m_surfaceIndex = -1;

	private void SubscribeToQualitySettings()
	{
		if (!m_qualitySubscribed)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Combine(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
			m_qualitySubscribed = true;
		}
	}

	public void Init(CARXWheelsParticlesData data, int surfaceIndex, RaceCar car)
	{
		m_data = data;
		m_car = car;
		m_surfaceIndex = surfaceIndex;
		m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		SubscribeToQualitySettings();
		base.enabled = false;
		base.enabled = true;
	}

	public override void ClearSmoke()
	{
		if (!(m_emitter != null) || m_emitter.particleCount <= 0)
		{
			return;
		}
		if (m_wheelData != null)
		{
			for (int i = 0; i < m_wheelData.Length; i++)
			{
				m_wheelData[i].Reset();
			}
		}
		m_emitter.Clear(withChildren: true);
	}

	private void DestroyEmitter()
	{
		if ((bool)Singletone<Simulator>.instance)
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent -= HandleOnUpdateWheelsEvent;
		}
		if (m_emitter != null)
		{
			ParticlesManager.DestroyParticleSystem(ref m_emitter);
		}
	}

	private void CreateEmitter()
	{
		DestroyEmitter();
		if (!(m_car == null) && !(m_data == null))
		{
			CARXWheelAccessor[] componentsInChildren = m_car.GetComponentsInChildren<CARXWheelAccessor>(includeInactive: true);
			m_wheelData = new WheelData[componentsInChildren.Length];
			int i = 0;
			for (int num = componentsInChildren.Length; i < num; i++)
			{
				m_wheelData[i] = new WheelData
				{
					wheelAccessor = componentsInChildren[i],
					lastEmitPosition = Vector3.zero,
					lastEmitVelocity = Vector3.zero,
					elapsedAccumulation = 0f,
					wasStopped = true
				};
			}
			m_emitter = ParticlesManager.InstantiateParticleSystem(m_data.particles, needUniqueInstance: false, initCollision: true);
			InitCollisionModule(m_emitter);
			if ((bool)Singletone<Simulator>.instance)
			{
				Singletone<Simulator>.instance.OnUpdateWheelsEvent += HandleOnUpdateWheelsEvent;
			}
		}
	}

	protected override bool OnEnableHandler()
	{
		if (m_graphicsQuality == null)
		{
			m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		}
		if (m_graphicsQuality == null || !m_graphicsQuality.smokeEnabled)
		{
			base.enabled = false;
			return false;
		}
		CreateEmitter();
		if (m_emitter == null)
		{
			return false;
		}
		OnSmokeLevelChange(m_graphicsQuality.smokeLevel);
		return true;
	}

	protected override void OnDisableHandler()
	{
		DestroyEmitter();
	}

	private void HandleOnUpdateWheelsEvent()
	{
		UpdateParticles();
	}

	private void UpdateParticles()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime == 0f)
		{
			return;
		}
		bool flag = base.isEnabled != m_lastActiveState;
		if (flag)
		{
			m_lastActiveState = base.isEnabled;
			if (!base.isEnabled)
			{
				int i = 0;
				for (int num = m_wheelData.Length; i < num; i++)
				{
					m_wheelData[i].elapsedAccumulation = 0f;
					m_wheelData[i].wasStopped = true;
				}
			}
		}
		if (!flag && !base.isEnabled)
		{
			return;
		}
		if (m_requireUpdateMaxParticleSize)
		{
			ApplyParticleSize(m_maxParticleSize);
			m_requireUpdateMaxParticleSize = false;
		}
		Color32 color = (m_data.colorCustomization ? (m_data.color * base.smokeColor) : m_data.color);
		float num2 = m_data.emissionRate * m_emissionRateScale;
		float num3 = 1f / num2;
		float num4 = Mathf.Clamp01(m_car.carX.speedKMH * 0.01f);
		float num5 = (m_data.colorCustomization ? 1 : 0);
		float num6 = 2.5f * num4;
		int j = 0;
		for (int num7 = m_wheelData.Length; j < num7; j++)
		{
			WheelData wheelData = m_wheelData[j];
			Wheel wheel = wheelData.wheelAccessor.wheel;
			Vector3 left;
			Vector3 right;
			Vector3 cnt;
			Vector3 n;
			bool patch = wheel.GetPatch(out left, out right, out cnt, out n);
			int userData = -1;
			if (patch && wheel.GetUserData(out userData) && userData == m_surfaceIndex && wheel.GetSmoke())
			{
				wheelData.elapsedAccumulation += deltaTime;
				if (!(wheelData.elapsedAccumulation >= num3))
				{
					continue;
				}
				float num8 = wheelData.elapsedAccumulation * m_data.emissionRate;
				int num9 = Mathf.FloorToInt(num8);
				float num10 = num3 * (float)num9;
				float num11 = wheelData.elapsedAccumulation;
				float num12 = num8;
				float num13 = 1f / num12;
				float num14 = num3;
				int num15 = num9;
				float num16 = Mathf.Abs(wheel.w - wheel.w0);
				float num17 = Mathf.Clamp01(Mathf.Abs(wheel.w) * num16 * 0.0002f);
				float scale = 1f + 0.2f * num17 + 0.1f * num4 * num5;
				Vector3 smokeVelocity = wheel.smokeVelocity;
				Vector3 vector = cnt;
				vector += n * (0.15f + 0.1f * num17 + 0.1f * num4 * num5);
				Vector3 vector2 = smokeVelocity * m_data.velocityScale;
				vector2 += n * (m_data.velocityNormalFactor * num17);
				vector2 += m_car.getRigidbody.GetPointVelocity(vector) * m_data.inheritVelocity;
				if (wheelData.wasStopped)
				{
					wheelData.lastEmitPosition = vector;
					wheelData.lastEmitVelocity = vector2;
					wheelData.wasStopped = false;
				}
				Vector3 lastEmitPosition = wheelData.lastEmitPosition;
				Vector3 lastEmitVelocity = wheelData.lastEmitVelocity;
				Vector3 vector3 = vector - wheelData.lastEmitPosition;
				Vector3 vector4 = vector3 * num13;
				float maxDistance = m_data.maxDistance;
				if (maxDistance > 0f && vector4.sqrMagnitude > maxDistance * maxDistance)
				{
					float magnitude = vector3.magnitude;
					num12 = magnitude / maxDistance;
					num15 = Mathf.FloorToInt(num12);
					if (num15 < num9 * 10)
					{
						vector4 = vector3 / magnitude * maxDistance;
						num14 = (float)num9 * num3 / (float)num15;
						num13 = 1f / num12;
					}
					else
					{
						num15 = num9 * 10;
						num12 = num15;
						num13 = 1f / num12;
						maxDistance = magnitude * num13;
						vector4 = vector3 / magnitude * maxDistance;
						num14 = (float)num9 * num3 * num13;
					}
				}
				Vector3 vector5 = (vector2 - wheelData.lastEmitVelocity) * num13;
				for (int k = 0; k < num15; k++)
				{
					lastEmitPosition += vector4;
					lastEmitVelocity += vector5;
					Vector3 vector6 = UnityEngine.Random.insideUnitSphere * num6;
					EmitParticle(lastEmitPosition, lastEmitVelocity + vector6, scale, num11, color);
					num11 -= num14;
				}
				wheelData.lastEmitPosition = lastEmitPosition;
				wheelData.lastEmitVelocity = lastEmitVelocity;
				wheelData.elapsedAccumulation -= num10;
			}
			else
			{
				wheelData.elapsedAccumulation = 0f;
				wheelData.wasStopped = true;
			}
		}
	}

	private void EmitParticle(Vector3 pos, Vector3 vel, float scale, float age, Color32 color)
	{
		ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
		{
			position = pos,
			velocity = vel,
			startLifetime = UnityEngine.Random.Range(m_data.minLifeTime, m_data.maxLifeTime) - age,
			startColor = color,
			startSize = UnityEngine.Random.Range(m_data.minSize, m_data.maxSize) * scale,
			rotation = UnityEngine.Random.Range(0f, m_data.maxStartRotationAngle),
			angularVelocity = UnityEngine.Random.Range(m_data.minAngularVelocity, m_data.maxAngularVelocity),
			randomSeed = 5u
		};
		m_emitter.Emit(emitParams, 1);
	}

	private static void InitCollisionModule(ParticleSystem particleSystem)
	{
		ParticleSystem.CollisionModule collision = particleSystem.collision;
		collision.enabled = true;
		collision.enableDynamicColliders = true;
		collision.maxCollisionShapes = 4;
		collision.mode = ParticleSystemCollisionMode.Collision3D;
		collision.quality = ParticleSystemCollisionQuality.Low;
		collision.type = ParticleSystemCollisionType.World;
		collision.collidesWith = 8388608;
	}

	private void ApplyParticleSize(float size)
	{
		if ((bool)m_emitter)
		{
			m_emitter.GetComponent<ParticleSystemRenderer>().maxParticleSize = m_data.particles.GetComponent<ParticleSystemRenderer>().maxParticleSize * size;
		}
	}

	private void OnDestroy()
	{
		if (m_graphicsQuality != null)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Remove(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
			m_qualitySubscribed = false;
		}
	}

	private void OnSmokeLevelChange(int qualityLevel)
	{
		switch (qualityLevel)
		{
		case 3:
			base.enabled = true;
			m_requireUpdateMaxParticleSize = true;
			m_maxParticleSize = 1f;
			m_emissionRateScale = 1f;
			break;
		case 2:
			base.enabled = true;
			m_requireUpdateMaxParticleSize = true;
			m_maxParticleSize = 0.65f;
			m_emissionRateScale = 0.65f;
			break;
		case 1:
			base.enabled = true;
			m_requireUpdateMaxParticleSize = true;
			m_maxParticleSize = 0.4f;
			m_emissionRateScale = 0.4f;
			break;
		case 0:
			base.enabled = false;
			break;
		}
	}
}
