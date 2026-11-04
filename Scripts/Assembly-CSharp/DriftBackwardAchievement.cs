using UnityEngine;

public class DriftBackwardAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private DriftController.DriftState m_prevState;

	private float m_timer;

	private bool m_isAfterBackward;

	private DriftController.MovementType m_prevMovement;

	public DriftBackwardAchievement()
	{
	}

	public DriftBackwardAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		if ((bool)m_driftController)
		{
			m_prevState = m_driftController.driftState;
		}
		dataTable = data;
		base.AchieveType = AchievementType.Backward;
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
		if (m_isTracking && m_driftController.isDrift && !m_isAfterBackward)
		{
			DriftController.MovementType typeMovement = m_driftController.TypeMovement;
			switch (typeMovement)
			{
			case DriftController.MovementType.Forward:
				if (m_prevMovement == DriftController.MovementType.Backward)
				{
					m_isAfterBackward = true;
					int num = FindIndex(m_timer, dataTable);
					if (num >= 0)
					{
						onAchievementReceive(base.AchieveType, num, dataTable[num].y);
						Reset();
					}
				}
				break;
			case DriftController.MovementType.Backward:
				m_timer += dt;
				break;
			default:
				m_timer = 0f;
				break;
			}
			m_prevMovement = typeMovement;
		}
		m_prevState = driftState;
	}

	public override void Reset()
	{
		m_timer = 0f;
		m_isAfterBackward = false;
		m_prevMovement = DriftController.MovementType.No;
		base.Reset();
	}
}
