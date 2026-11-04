using UnityEngine;

public class DriftFailedAchievement : BaseDriftAchievement
{
	private DriftController.DriftState m_prevState = DriftController.DriftState.Drift;

	public DriftFailedAchievement()
	{
		Reset();
	}

	public DriftFailedAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		if ((bool)m_driftController)
		{
			m_prevState = m_driftController.driftState;
		}
		base.AchieveType = AchievementType.None;
	}

	public override void Update(float dt)
	{
		if (!m_driftController || !UIDriftEventsContext.HaveShowing())
		{
			return;
		}
		DriftController.DriftState driftState = m_driftController.driftState;
		if (driftState == DriftController.DriftState.No)
		{
			if (m_prevState == DriftController.DriftState.Drift)
			{
				m_isTracking = false;
			}
		}
		else
		{
			m_isTracking = true;
		}
		if (!m_isTracking && !m_driftController.isDrift)
		{
			onAchievementReceive(base.AchieveType, 0, 0f);
			m_isTracking = true;
		}
		m_prevState = driftState;
	}
}
