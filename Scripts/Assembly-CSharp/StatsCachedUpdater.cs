using UnityEngine;

public class StatsCachedUpdater
{
	private StatsManager.EStats m_stat;

	private float m_step;

	private float m_curValue;

	private float m_nextStepValue;

	public StatsCachedUpdater(StatsManager.EStats stat, int step = 1)
	{
		Init(stat, step);
	}

	public void Init(StatsManager.EStats stat, int step = 1)
	{
		m_stat = stat;
		m_step = step;
		StatsManager.GetStat(stat, out m_curValue);
		UpdateNextStepValue();
	}

	private void UpdateNextStepValue()
	{
		m_nextStepValue = (float)Mathf.FloorToInt(m_curValue / m_step) * m_step + m_step;
	}

	public void AddDelta(float delta)
	{
		if (!NeedSkipDeltaIncrease())
		{
			m_curValue += delta;
			if (m_curValue >= m_nextStepValue)
			{
				SendCurrentToStat();
				UpdateNextStepValue();
			}
		}
	}

	private bool NeedSkipDeltaIncrease()
	{
		return (States.mainCurrent is XDSRaceModeState) & (m_stat == StatsManager.EStats.DriftScore);
	}

	public void SendCurrentToStat()
	{
		StatsManager.SetStat(m_stat, m_curValue);
	}
}
