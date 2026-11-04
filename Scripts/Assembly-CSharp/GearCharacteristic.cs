using UnityEngine;

public class GearCharacteristic : CarCharacteristic
{
	private const string GEAR_N = "N";

	private const string GEAR_R = "R";

	private string m_gearStr;

	private int m_currentGear;

	public override string GetStringValue()
	{
		if (m_raceCar != null && m_raceCar.carX != null)
		{
			m_currentGear = m_raceCar.carX.gear;
		}
		if (m_currentGear > 0)
		{
			m_gearStr = m_currentGear.ToString();
		}
		else
		{
			m_gearStr = ((m_currentGear < 0) ? "R" : "N");
		}
		return m_gearStr;
	}

	public override float GetFillValue()
	{
		return Mathf.Clamp01((float)m_raceCar.carX.gear / (float)m_raceCar.carX.numGears);
	}
}
