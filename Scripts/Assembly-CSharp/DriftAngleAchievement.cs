using UnityEngine;

public class DriftAngleAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private const float ANGLE_DELTA = 0.5f;

	private const float DRIFT_TIME = 0.5f;

	private DriftController.DriftState m_prevState;

	private float m_prevAngle;

	private float m_timer1;

	private float m_timer2;

	public DriftAngleAchievement()
	{
		Reset();
	}

	public DriftAngleAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		if ((bool)m_driftController)
		{
			m_prevState = m_driftController.driftState;
		}
		dataTable = data;
		base.AchieveType = AchievementType.Angle;
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
		if (m_isTracking && m_driftController.isDrift)
		{
			float driftAngle = m_driftController.driftAngle;
			int num = FindIndex(driftAngle, dataTable);
			if (num < 0)
			{
				m_timer1 = 0f;
				m_timer2 = 0f;
			}
			else if (num == 0)
			{
				m_timer1 += dt;
			}
			else
			{
				m_timer2 += dt;
				m_timer1 += dt;
			}
			if (driftAngle - m_prevAngle > 0.5f)
			{
				m_prevAngle = driftAngle;
			}
			else
			{
				if (num == 0 && m_timer1 > 0.5f)
				{
					onAchievementReceive(base.AchieveType, num, dataTable[num].y);
					Reset();
				}
				if (num == 1 && m_timer2 > 0.5f)
				{
					onAchievementReceive(base.AchieveType, num, dataTable[num].y);
					Reset();
				}
			}
		}
		m_prevState = driftState;
	}

	public override void Reset()
	{
		m_prevAngle = 0f;
		m_timer1 = 0f;
		m_timer2 = 0f;
		base.Reset();
	}
}
