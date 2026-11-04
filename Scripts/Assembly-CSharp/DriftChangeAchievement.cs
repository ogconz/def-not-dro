using UnityEngine;

public class DriftChangeAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private const float PREV_DRIFT_TIME = 3f;

	private DriftController.DriftState m_prevState;

	private float m_timer;

	public DriftChangeAchievement()
	{
		Reset();
	}

	public DriftChangeAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		if ((bool)m_driftController)
		{
			m_prevState = m_driftController.driftState;
		}
		dataTable = data;
		base.AchieveType = AchievementType.SideChange;
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
			if (m_prevState == DriftController.DriftState.Change)
			{
				m_isTracking = m_timer >= 3f;
			}
			if (m_prevState != driftState)
			{
				m_timer = 0f;
			}
			m_timer += dt;
		}
		else
		{
			m_isTracking = false;
		}
		if (m_isTracking && m_driftController.isDrift)
		{
			int num = FindIndex(m_driftController.driftSpeed, dataTable);
			if (num != -1)
			{
				onAchievementReceive(base.AchieveType, num, dataTable[num].y);
			}
			m_isTracking = false;
		}
		m_prevState = driftState;
	}

	public override void Reset()
	{
		m_timer = 0f;
		base.Reset();
	}
}
