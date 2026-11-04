using UnityEngine;

public class SpeedCharacteristic : CarCharacteristic
{
	private GamePrefs m_prefs;

	public SpeedCharacteristic(GamePrefs prefs)
	{
		m_prefs = prefs;
	}

	private float GetMaxSpeed()
	{
		return m_raceCar.carX.CalcTopSpeed() * 3.6f * (m_prefs.globals.speedKmh ? 1f : 0.6213f);
	}

	public override float GetValue()
	{
		return Mathf.FloorToInt(m_prefs.globals.speedKmh ? m_raceCar.carX.speedKMH : m_raceCar.carX.speedMPH);
	}

	public override string GetStringValue()
	{
		return Mathf.RoundToInt(GetValue()).ToString("N0");
	}

	public override float GetFillValue()
	{
		return Mathf.Clamp01((m_prefs.globals.speedKmh ? m_raceCar.carX.speedKMH : m_raceCar.carX.speedMPH) / GetMaxSpeed());
	}
}
