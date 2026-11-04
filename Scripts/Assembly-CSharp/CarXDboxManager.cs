using System.Numerics;
using CarX;
using UnityEngine;

public class CarXDboxManager
{
	private const float HpTpWatts = 735.499f;

	private static CarXDboxManager s_instance;

	private CarXDboxStructs.CarXFrameUpdate m_carXFrameUpdate;

	private CarXDboxStructs.CarXConfigUpdate m_carXConfigUpdate;

	private RaceCar m_raceCar;

	private Car m_carX;

	public static CarXDboxManager instance => s_instance ?? (s_instance = new CarXDboxManager());

	private CarXDboxManager()
	{
	}

	public void SetCar(RaceCar raceCar)
	{
		m_raceCar = raceCar;
		m_carX = m_raceCar.carX;
	}

	public void Initialize()
	{
		CarXDboxModel.Initialize();
	}

	public void Start()
	{
		CarXDboxModel.StartWithReset();
	}

	public void UpdateConfig()
	{
		if (!(m_carX == null))
		{
			SetCarXConfigUpdate();
			CarXDboxModel.UpdateConfig(m_carXConfigUpdate);
		}
	}

	public void UpdateDboxFrame()
	{
		if (!(m_carX == null))
		{
			SetCarXFrameUpdate();
			CarXDboxModel.UpdateFrame(m_carXFrameUpdate);
		}
	}

	public void Stop()
	{
		CarXDboxModel.Stop();
	}

	public void Terminate()
	{
		CarXDboxModel.Terminate();
	}

	private void SetCarXConfigUpdate()
	{
		m_carXConfigUpdate.engineMaxRPM = m_carX.engineCutRPM;
		m_carXConfigUpdate.engineRedRPM = m_carX.engineRevLimiter;
		m_carXConfigUpdate.engineIdleRPM = m_carX.engineIdleRPM;
		m_carXConfigUpdate.engineCylindersCount = 4;
		m_carXConfigUpdate.engineOffsetXYZ = ToNumericsVector3(m_raceCar.swap.engineRootLocalPosition);
		m_carX.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var max_tq, out var _);
		m_carXConfigUpdate.engineMaxPower = max_hp * 735.499f;
		m_carXConfigUpdate.engineMaxPower = max_tq;
		float maxSpringLen = m_carX.GetWheel(WheelIndex.FrontLeft).maxSpringLen;
		float maxSpringLen2 = m_carX.GetWheel(WheelIndex.FrontRight).maxSpringLen;
		float maxSpringLen3 = m_carX.GetWheel(WheelIndex.RearLeft).maxSpringLen;
		float maxSpringLen4 = m_carX.GetWheel(WheelIndex.RearRight).maxSpringLen;
		System.Numerics.Vector4 suspensionMaxTravelQuad = new System.Numerics.Vector4(maxSpringLen, maxSpringLen2, maxSpringLen3, maxSpringLen4);
		m_carXConfigUpdate.suspensionMaxTravelQuad = suspensionMaxTravelQuad;
	}

	private void SetCarXFrameUpdate()
	{
		m_carXFrameUpdate.engineRPM = m_carX.rpm;
		m_carXFrameUpdate.engineCurrentPower = m_carX.engineCurPower * 735.499f;
		m_carXFrameUpdate.engineTorque = m_carX.engineCurTorque;
		m_carXFrameUpdate.currentGear = m_carX.gear;
		m_carXFrameUpdate.gearboxCurrentRPM = 0f;
		Wheel wheel = m_carX.GetWheel(WheelIndex.FrontLeft);
		Wheel wheel2 = m_carX.GetWheel(WheelIndex.FrontRight);
		Wheel wheel3 = m_carX.GetWheel(WheelIndex.RearLeft);
		Wheel wheel4 = m_carX.GetWheel(WheelIndex.RearRight);
		float maxSpringLen = wheel.maxSpringLen;
		float maxSpringLen2 = wheel2.maxSpringLen;
		float maxSpringLen3 = wheel3.maxSpringLen;
		float maxSpringLen4 = wheel3.maxSpringLen;
		float num = Mathf.Abs(wheel.topPos.y) - Mathf.Abs(m_carX.GetWheelTransform(WheelIndex.FrontLeft).localPosition.y);
		float num2 = Mathf.Abs(wheel2.topPos.y) - Mathf.Abs(m_carX.GetWheelTransform(WheelIndex.FrontRight).localPosition.y);
		float num3 = Mathf.Abs(wheel3.topPos.y) - Mathf.Abs(m_carX.GetWheelTransform(WheelIndex.RearLeft).localPosition.y);
		float num4 = Mathf.Abs(wheel4.topPos.y) - Mathf.Abs(m_carX.GetWheelTransform(WheelIndex.RearRight).localPosition.y);
		m_carXFrameUpdate.suspensionCurrentTravelQuad = new System.Numerics.Vector4(num, num2, num3, num4);
		m_carXFrameUpdate.suspensionCurrentTravelNormalizedQuad = new System.Numerics.Vector4(num / maxSpringLen, num2 / maxSpringLen2, num3 / maxSpringLen3, num4 / maxSpringLen4);
		m_carXFrameUpdate.suspensionLoadNormalizedQuad = m_carXFrameUpdate.suspensionCurrentTravelNormalizedQuad;
		m_carXFrameUpdate.suspensionSlipRatioQuad = new System.Numerics.Vector4(wheel.sr, wheel2.sr, wheel3.sr, wheel4.sr);
		m_carXFrameUpdate.suspensionSlipAngleQuad = new System.Numerics.Vector4(wheel.sa, wheel2.sa, wheel3.sa, wheel4.sa);
		m_carXFrameUpdate.suspensionSlipVelocityQuad = new System.Numerics.Vector4(wheel.w * wheel.radius, wheel2.w * wheel2.radius, wheel3.w * wheel3.radius, wheel4.w * wheel4.radius);
		UnityEngine.Vector3 direction = new UnityEngine.Vector3(m_carX.velocity.x, m_carX.velocity.y, m_carX.velocity.z);
		m_carXFrameUpdate.velocity = ToNumericsVector3(m_carX.transform.InverseTransformDirection(direction));
		UnityEngine.Vector3 direction2 = new UnityEngine.Vector3(m_carX.accel.x, m_carX.accel.y, m_carX.accel.z);
		m_carXFrameUpdate.acceleration = ToNumericsVector3(m_carX.transform.InverseTransformDirection(direction2));
	}

	private static System.Numerics.Vector3 ToNumericsVector3(UnityEngine.Vector3 unityVector)
	{
		return new System.Numerics.Vector3(unityVector.x, unityVector.y, unityVector.z);
	}
}
