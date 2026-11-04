using UnityEngine;

public class DriftQualityAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private const float QUALITY_TIME = 2.5f;

	private DriftController.DriftState m_prevState;

	private float m_timer1;

	private float m_timer2;

	public DriftQualityAchievement()
	{
		Reset();
	}

	public DriftQualityAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		dataTable = data;
		base.AchieveType = AchievementType.Quality;
	}

	public override void Update(float dt)
	{
		if (!m_driftController)
		{
			return;
		}
		DriftController.DriftState driftState = m_driftController.driftState;
		if (driftState == DriftController.DriftState.Drift)
		{
			if (m_prevState != DriftController.DriftState.Drift)
			{
				m_isTracking = true;
			}
		}
		else
		{
			Reset();
		}
		if (m_isTracking)
		{
			int num = FindIndex(m_driftController.pointsEarnSpeed, dataTable);
			switch (num)
			{
			case 0:
				m_timer1 += dt;
				if (m_timer1 > 2.5f)
				{
					onAchievementReceive(base.AchieveType, num, dataTable[num].y);
					Reset();
				}
				break;
			case 1:
				m_timer1 += dt;
				m_timer2 += dt;
				if (m_timer2 > 2.5f)
				{
					onAchievementReceive(base.AchieveType, num, dataTable[num].y);
					Reset();
				}
				break;
			default:
				m_timer1 = (m_timer2 = 0f);
				break;
			}
		}
		m_prevState = driftState;
	}

	public override void Reset()
	{
		m_timer1 = 0f;
		m_timer2 = 0f;
		base.Reset();
	}
}
