using CarX.Gameplay.ClippingZone;
using UnityEngine;

public class CustomEventAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private HalloweenEventState state;

	public CustomEventAchievement()
	{
		Reset();
	}

	public CustomEventAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		state = States.current as HalloweenEventState;
		dataTable = data;
		base.AchieveType = AchievementType.CustomForEvent;
	}

	public override void Update(float dt)
	{
		if (!m_driftController || m_driftController.queueCustomClippingPoints.Count <= 0 || !(state != null))
		{
			return;
		}
		ClippingPointInfo clippingPointInfo = m_driftController.queueCustomClippingPoints.Dequeue();
		if (clippingPointInfo != null)
		{
			if (!clippingPointInfo.touched)
			{
				UIDriftEventsContext.ShowAddition(base.AchieveType, LocalizationKeys.driftevent_customClippingEvent_x1, 0, state.eventSettings.incTimerX1);
			}
			else
			{
				UIDriftEventsContext.ShowAddition(base.AchieveType, LocalizationKeys.driftevent_customClippingEvent_x2, 1, state.eventSettings.incTimerX2);
			}
			state.pumpkinCount++;
			state.IncTimer(clippingPointInfo.touched ? state.eventSettings.incTimerX2 : state.eventSettings.incTimerX1);
		}
	}
}
