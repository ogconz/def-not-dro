using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CarX;
using UnityEngine;

[RequireComponent(typeof(Car))]
[AddComponentMenu("")]
public class BaseCar : MonoBehaviour
{
	[Serializable]
	public class Desc
	{
		public CarDesc carXDesc = new CarDesc();

		public float baseY;

		public float frontBaseWidth;

		public float frontBaseZ;

		public float rearBaseWidth;

		public float rearBaseZ;

		[XmlIgnore]
		public float? frontWheelDiam;

		[XmlIgnore]
		public float? rearWheelDiam;

		public void CopyFrom(Desc o)
		{
			carXDesc.CopyFrom(o.carXDesc);
			baseY = o.baseY;
			frontBaseWidth = o.frontBaseWidth;
			frontBaseZ = o.frontBaseZ;
			rearBaseWidth = o.rearBaseWidth;
			rearBaseZ = o.rearBaseZ;
			frontWheelDiam = o.frontWheelDiam;
			rearWheelDiam = o.rearWheelDiam;
		}

		public Desc Clone()
		{
			Desc desc = new Desc();
			desc.CopyFrom(this);
			return desc;
		}

		public void CalcWheelsDiam()
		{
			frontWheelDiam = carXDesc.frontTyre.discDiam * 25.4f - carXDesc.frontTyre.profile * carXDesc.frontTyre.width / 50f;
			rearWheelDiam = carXDesc.rearTyre.discDiam * 25.4f - carXDesc.rearTyre.profile * carXDesc.rearTyre.width / 50f;
		}
	}

	public class DynoParams
	{
		public float dragAccelEngine;

		public float dragAccelTyre;

		public float brakeAccelBrake;

		public float brakeAccelTyre;

		public float sideAccel;

		public float sideAccelMax;

		public float extAccelFront;

		public float vMax;
	}

	[NonSerialized]
	public float frontBaseWidth;

	[NonSerialized]
	public float rearBaseWidth;

	[NonSerialized]
	public float frontBaseOffset;

	[NonSerialized]
	public float rearBaseOffset;

	[SerializeField]
	private float m_lengthFront;

	[SerializeField]
	private float m_lengthRear;

	[SerializeField]
	private float m_width;

	private Car m_carX;

	private DynoParams m_dynoParams;

	private TiresConfig m_tiresConfig;

	private float m_spawnHeight = 0.2f;

	public TiresConfig tiresConfig
	{
		get
		{
			if (m_tiresConfig == null)
			{
				m_tiresConfig = ScriptableSingleton<TiresConfig>.instance;
			}
			return m_tiresConfig;
		}
	}

	public float lengthFront => m_lengthFront;

	public float lengthRear => m_lengthRear;

	public float width => m_width;

	public Transform getTransform => m_carX.getTransform;

	public Rigidbody getRigidbody => m_carX.getRigidbody;

	public Car carX => m_carX;

	public WheelIndex[] activeWheels { get; private set; }

	public bool storedTC { get; private set; }

	public bool useTC
	{
		get
		{
			return carX.useTC;
		}
		set
		{
			if (carX.useTC != value)
			{
				carX.useTC = value;
				GameEvents.CarTCChanged(new GameEvents.CarTCInfo(this as RaceCar, value));
			}
		}
	}

	public DynoParams dynoParams
	{
		get
		{
			if (m_dynoParams == null)
			{
				CalcDynoParams();
			}
			return m_dynoParams;
		}
	}

	public bool wheelsSteerNoInput
	{
		get
		{
			GetCar().GetIntegrationParams(out var result);
			return result;
		}
		set
		{
			GetCar().SetIntegrationParams(value);
		}
	}

	public event Action<BaseCar> OnSetDescEvent;

	public static bool IsNull(BaseCar car)
	{
		if (!(car == null))
		{
			return car.carX == null;
		}
		return true;
	}

	public bool IsPhysicsBroken()
	{
		return !carX.enabled;
	}

	public void ResetPhysicsAfterBroke()
	{
		carX.enabled = true;
		carX.getRigidbody.isKinematic = false;
		carX.getRigidbody.detectCollisions = true;
	}

	public void SetDesc(Desc desc, bool skipWheelBase = false)
	{
		if (!desc.frontWheelDiam.HasValue || !desc.rearWheelDiam.HasValue)
		{
			desc.CalcWheelsDiam();
		}
		if (!skipWheelBase)
		{
			frontBaseWidth = desc.frontBaseWidth;
			rearBaseWidth = desc.rearBaseWidth;
		}
		SetWheelBase(WheelIndex.FrontLeft, desc.frontBaseWidth + frontBaseOffset, desc.baseY, desc.frontBaseZ);
		SetWheelBase(WheelIndex.FrontRight, desc.frontBaseWidth + frontBaseOffset, desc.baseY, desc.frontBaseZ);
		SetWheelBase(WheelIndex.RearLeft, desc.rearBaseWidth + rearBaseOffset, desc.baseY, desc.rearBaseZ);
		SetWheelBase(WheelIndex.RearRight, desc.rearBaseWidth + rearBaseOffset, desc.baseY, desc.rearBaseZ);
		bool isPlaying = Application.isPlaying;
		carX.SetCarDesc(desc.carXDesc, isPlaying);
		if (isPlaying)
		{
			CalcDynoParams();
			if (this.OnSetDescEvent != null)
			{
				this.OnSetDescEvent(this);
			}
		}
		storedTC = desc.carXDesc.engine.useTC;
	}

	public Desc GetDesc(CarDesc carXDesc = null)
	{
		Desc desc = new Desc();
		GetWheelBase(WheelIndex.FrontLeft, out desc.baseY, out desc.frontBaseZ);
		GetWheelBase(WheelIndex.RearLeft, out desc.baseY, out desc.rearBaseZ);
		desc.frontBaseWidth = frontBaseWidth;
		desc.rearBaseWidth = rearBaseWidth;
		if (carXDesc != null)
		{
			desc.carXDesc.CopyFrom(carXDesc);
		}
		else
		{
			GetCar().GetCarDesc(ref desc.carXDesc);
		}
		if (!desc.frontWheelDiam.HasValue || !desc.rearWheelDiam.HasValue)
		{
			desc.CalcWheelsDiam();
		}
		return desc;
	}

	public void SetWheelBase(WheelIndex wheelIndex, float baseWidth, float baseY, float baseZ)
	{
		if (Application.isPlaying)
		{
			Wheel wheel = GetCar().GetWheel(wheelIndex);
			float num = ((wheelIndex != WheelIndex.FrontLeft && wheelIndex != WheelIndex.RearLeft && wheelIndex != WheelIndex.Rear1Left && wheelIndex != WheelIndex.Rear2Left) ? 1 : (-1));
			float num2 = ((wheelIndex == WheelIndex.FrontLeft || wheelIndex == WheelIndex.FrontRight) ? 1 : (-1));
			wheel.topPos = new Vector3(Mathf.Abs(baseWidth / 2f) * num, baseY, Mathf.Abs(baseZ) * num2);
		}
		else
		{
			Transform wheelTransform = GetCar().GetWheelTransform(wheelIndex);
			float num3 = ((wheelIndex != WheelIndex.FrontLeft && wheelIndex != WheelIndex.RearLeft && wheelIndex != WheelIndex.Rear1Left && wheelIndex != WheelIndex.Rear2Left) ? 1 : (-1));
			float num4 = ((wheelIndex == WheelIndex.FrontLeft || wheelIndex == WheelIndex.FrontRight) ? 1 : (-1));
			wheelTransform.localPosition = new Vector3(Mathf.Abs(baseWidth / 2f) * num3, baseY, Mathf.Abs(baseZ) * num4);
		}
	}

	public void GetWheelBase(WheelIndex wheelIndex, out float baseY, out float baseZ)
	{
		baseY = 0f;
		baseZ = 0f;
		Vector3 vector = Vector3.zero;
		if (Application.isPlaying)
		{
			vector = GetCar().GetWheel(wheelIndex).topPos;
		}
		else
		{
			Transform wheelTransform = GetCar().GetWheelTransform(wheelIndex);
			if ((bool)wheelTransform)
			{
				vector = wheelTransform.localPosition;
			}
		}
		baseY = vector.y;
		baseZ = vector.z;
	}

	protected virtual void Reset()
	{
		CalcDimensions();
	}

	protected Car GetCar()
	{
		if (m_carX == null)
		{
			m_carX = GetComponent<Car>();
			InitWheels();
		}
		return m_carX;
	}

	private void InitWheels()
	{
		List<WheelIndex> list = new List<WheelIndex>(Car.maxNumberOfWheels);
		for (int i = 0; i < Car.maxNumberOfWheels; i++)
		{
			if (m_carX.GetWheel((WheelIndex)i).enabled)
			{
				list.Add((WheelIndex)i);
			}
		}
		activeWheels = list.ToArray();
	}

	protected virtual void Awake()
	{
		m_carX = null;
		m_carX = GetCar();
		wheelsSteerNoInput = false;
		SetEnableSteerRack(enable: false);
	}

	public void SetEnableSteerRack(bool enable)
	{
		Car car = GetCar();
		car.GetSteerRackParams(out var _, out var _, out var steerRackRealFFPar);
		car.SetSteerRackParams(enable, enable, steerRackRealFFPar);
	}

	private void CalcDimensions()
	{
		CarDesc desc = null;
		GetCar().GetCarDesc(ref desc);
		m_width = desc.weight.bounds.size.x;
		m_lengthFront = desc.weight.bounds.size.z * 0.5f;
		m_lengthRear = desc.weight.bounds.size.z * 0.5f;
	}

	private void CalcDynoParams()
	{
		GetCar().CalcMaxAccel(1, 0f, Vector3.forward, Vector3.right, Vector3.zero, out var drag_accel_eng, out var drag_accel_tyre, out var brake_accel_br, out var brake_accel_tyre, out var side_accel, out var side_accel_max, out var ext_accel_front, out var Vmax);
		m_dynoParams = new DynoParams
		{
			dragAccelEngine = drag_accel_eng,
			dragAccelTyre = drag_accel_tyre,
			brakeAccelBrake = brake_accel_br,
			brakeAccelTyre = brake_accel_tyre,
			sideAccel = side_accel,
			sideAccelMax = side_accel_max,
			extAccelFront = ext_accel_front,
			vMax = Vmax
		};
	}
}
