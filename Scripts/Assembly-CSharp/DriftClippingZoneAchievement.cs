using CarX.Gameplay.ClippingZone;
using UnityEngine;

public class DriftClippingZoneAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	public DriftClippingZoneAchievement()
	{
		Reset();
	}

	public DriftClippingZoneAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		dataTable = data;
		base.AchieveType = AchievementType.ClippingZone;
	}

	public override void Update(float dt)
	{
		if (!m_driftController || m_driftController.queueClippingZones.Count <= 0)
		{
			return;
		}
		ClippingZoneInfo clippingZoneInfo = m_driftController.DequeueClippingZoneInfo();
		if (!m_driftController.isDrift)
		{
			return;
		}
		int num = 0;
		onAchievementReceive(base.AchieveType, num, dataTable[2].y);
		int num2 = 250;
		if (clippingZoneInfo.minSpeed >= 80f)
		{
			if (clippingZoneInfo.minSpeed >= 95f)
			{
				num2 *= 2;
			}
			else if (clippingZoneInfo.minSpeed >= 110f)
			{
				num2 *= 3;
			}
			UIDriftEventsContext.ShowAddition(base.AchieveType, "driftevent_clippingspeed", num, Mathf.FloorToInt(num2));
			onAchievementReceive(AchievementType.Addition, num, num2);
		}
	}
}
