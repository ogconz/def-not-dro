using UnityEngine;

public class DriftJumpAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private float m_timer;

	private bool m_oneShot;

	public DriftJumpAchievement()
	{
		Reset();
	}

	public DriftJumpAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		dataTable = data;
		base.AchieveType = AchievementType.Jump;
	}

	public override void Update(float dt)
	{
		if (!m_driftController)
		{
			return;
		}
		if (m_isTracking && m_driftController.isDrift)
		{
			int num = FindIndex(m_timer, dataTable);
			if (num != -1)
			{
				onAchievementReceive(base.AchieveType, num, dataTable[num].y);
				m_isTracking = false;
				m_oneShot = false;
			}
		}
		if (!m_driftController.raceCar.flipped && !m_driftController.raceCar.onGround)
		{
			if (m_oneShot)
			{
				if (!m_isTracking)
				{
					m_timer = 0f;
					m_isTracking = true;
				}
				m_timer += dt;
			}
		}
		else
		{
			Reset();
		}
	}

	public override void Reset()
	{
		m_oneShot = true;
		m_timer = 0f;
		base.Reset();
	}
}
