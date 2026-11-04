using CarX.Gameplay.ClippingZone;
using UnityEngine;

public class DriftClippingPointAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	public DriftClippingPointAchievement()
	{
		Reset();
	}

	public DriftClippingPointAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		dataTable = data;
		base.AchieveType = AchievementType.ClippingPoint;
	}

	public override void Update(float dt)
	{
		if (!m_driftController || m_driftController.queueClippingPoints.Count <= 0)
		{
			return;
		}
		ClippingPointInfo clippingPointInfo = m_driftController.DequeueClippingPointInfo();
		if (!m_driftController.isDrift && !clippingPointInfo.touched)
		{
			return;
		}
		float num = Mathf.Min(clippingPointInfo.minDistanceFront, clippingPointInfo.minDistanceRear);
		int num2 = ((!(clippingPointInfo.minDistanceFront < clippingPointInfo.minDistanceRear)) ? 1 : 0);
		int num3;
		if (!clippingPointInfo.touched)
		{
			num3 = ((num <= clippingPointInfo.coloredStripesLengths[0]) ? 2 : ((num <= clippingPointInfo.coloredStripesLengths[1]) ? 1 : 0));
		}
		else
		{
			num2 += 2;
			num3 = 2;
		}
		if (num3 <= -1)
		{
			return;
		}
		onAchievementReceive(base.AchieveType, num2, (!clippingPointInfo.touched) ? dataTable[num3].y : 0f);
		int num4 = 250;
		if (clippingPointInfo.minSpeed >= 80f && !clippingPointInfo.touched)
		{
			if (clippingPointInfo.minSpeed >= 95f)
			{
				num4 *= 2;
			}
			else if (clippingPointInfo.minSpeed >= 110f)
			{
				num4 *= 3;
			}
			UIDriftEventsContext.ShowAddition(base.AchieveType, LocalizationKeys.driftevent_clippingspeed, num2, Mathf.FloorToInt(num4));
			onAchievementReceive(AchievementType.Addition, num2, num4);
		}
	}
}
