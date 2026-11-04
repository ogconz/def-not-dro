using UnityEngine;

public class TurboCharacteristic : CarCharacteristic
{
	private readonly float m_maxAngle;

	private float m_maxPressure;

	private FMODCarTurbo.TurboBoostNegativer m_boostNegativer = new FMODCarTurbo.TurboBoostNegativer();

	public TurboCharacteristic()
	{
	}

	public TurboCharacteristic(float maxAngle)
	{
		m_maxAngle = maxAngle;
	}

	public override void Init(RaceCar raceCar)
	{
		base.Init(raceCar);
		m_raceCar.carX.GetEngineTurboParams(out var maxTurboPressure, out var _, out var _);
		m_maxPressure = maxTurboPressure;
	}

	public override float GetValue()
	{
		m_raceCar.carX.GetEngineTurboParams(out var _, out var curTurboPressure, out var _);
		float num = m_boostNegativer.UpdateValue(curTurboPressure / m_maxPressure, Time.deltaTime);
		if (num < 0f)
		{
			return num;
		}
		return curTurboPressure;
	}

	public override string GetStringValue()
	{
		return Mathf.RoundToInt(GetValue()).ToString("N0");
	}

	public override float GetFillValue()
	{
		return Mathf.Clamp01(GetValue() / m_maxPressure);
	}

	public override float GetAngleValue()
	{
		return m_maxAngle * (0f - GetValue() / m_maxPressure);
	}
}
