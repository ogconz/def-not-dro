using CarX;
using UnityEngine;

public class DynoSteeringModel
{
	private RaceCar m_raceCar;

	private float m_smoothVelo;

	private float m_targetSteer;

	private float m_currentSteer;

	public float currentSteer => m_currentSteer;

	public void InitCar(RaceCar raceCar)
	{
		m_raceCar = raceCar;
	}

	public void ReleaseCar()
	{
		m_raceCar = null;
	}

	public bool IsCarValid()
	{
		return !BaseCar.IsNull(m_raceCar);
	}

	public void UpdateTargetSteer()
	{
		if (!BaseCar.IsNull(m_raceCar))
		{
			Car carX = m_raceCar.carX;
			m_currentSteer = Mathf.SmoothDamp(carX.GetSteer(), m_targetSteer, ref m_smoothVelo, 0.2f);
			carX.SetSteer(m_currentSteer, useAssistent: false, Time.deltaTime);
		}
	}

	public void ManualSteerUpdate(float sliderValue)
	{
		m_targetSteer = 0f - sliderValue;
	}

	public float GetFrontToe()
	{
		float y = m_raceCar.carModel.carWheels[0].hub.parent.localRotation.eulerAngles.y;
		float y2 = m_raceCar.carModel.carWheels[1].hub.parent.localRotation.eulerAngles.y;
		return Mathf.DeltaAngle(0f, y - y2) / 2f;
	}

	public float GetRearToe()
	{
		return Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[2].hub.parent.localRotation.eulerAngles.y);
	}

	public float GetOuterWheelCamber()
	{
		return Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[GetOuterWheelIndex()].hub.parent.localRotation.eulerAngles.z);
	}

	public float GetLeftWheelCamber()
	{
		return Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[0].hub.parent.localRotation.eulerAngles.z);
	}

	public float GetRightWheelCamber()
	{
		return Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[1].hub.parent.localRotation.eulerAngles.z);
	}

	public float GetRearCamber()
	{
		return Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[2].hub.parent.localRotation.eulerAngles.z);
	}

	private int GetOuterWheelIndex()
	{
		if (m_currentSteer > 0f)
		{
			return 1;
		}
		return 0;
	}

	public (float, float) GetFrontWheelAnglesTuple()
	{
		float item = Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[0].hub.parent.localRotation.eulerAngles.y);
		float item2 = Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[1].hub.parent.localRotation.eulerAngles.y);
		return (item, item2);
	}

	public (float, float) GetFrontWheelsCambersTuple()
	{
		float item = Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[0].hub.parent.localRotation.eulerAngles.z);
		float item2 = Mathf.DeltaAngle(0f, m_raceCar.carModel.carWheels[1].hub.parent.localRotation.eulerAngles.z);
		return (item, item2);
	}
}
