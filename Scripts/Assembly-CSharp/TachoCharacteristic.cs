using UnityEngine;

public class TachoCharacteristic : CarCharacteristic
{
	private const float MAX_RPM = 0.36f;

	private float m_maxFill;

	private float? m_maxRevLimiterValue;

	public override float maxFillVal => m_maxFill;

	public TachoCharacteristic(float? maxRevLimiterValue = null)
	{
		m_maxRevLimiterValue = maxRevLimiterValue;
	}

	public override void Init(RaceCar raceCar)
	{
		base.Init(raceCar);
		if (!(m_raceCar == null) && !(m_raceCar.carX == null))
		{
			m_maxFill = 0.36f * m_raceCar.carX.engineRevLimiter / 10000f;
		}
	}

	public override float GetValue()
	{
		if (!m_raceCar.carX.isEngineWorks)
		{
			return 0f;
		}
		return Mathf.Max(0f, m_raceCar.carX.rpm);
	}

	public override string GetStringValue()
	{
		return Mathf.RoundToInt(GetValue()).ToString("N0");
	}

	public override float GetFillValue()
	{
		return Mathf.Clamp01(GetValue() / (m_maxRevLimiterValue.HasValue ? m_maxRevLimiterValue.Value : m_raceCar.carX.engineRevLimiter));
	}
}
