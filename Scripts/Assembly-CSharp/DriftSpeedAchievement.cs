using UnityEngine;

public class DriftSpeedAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private DriftController.DriftState m_prevState;

	public DriftSpeedAchievement()
	{
		Reset();
	}

	public DriftSpeedAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		if ((bool)m_driftController)
		{
			m_prevState = m_driftController.driftState;
		}
		dataTable = data;
		base.AchieveType = AchievementType.Speed;
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
			if (m_prevState == DriftController.DriftState.No)
			{
				m_isTracking = true;
			}
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
}
