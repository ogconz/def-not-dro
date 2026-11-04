using SyncMultiplayer;

namespace HUD
{
	public class ChallengeView : SubroomView
	{
		public ChallengeView(UIRaceNetworkModeContext context, SubroomInfo subroom)
			: base(context, subroom)
		{
			context.isDriftMode.value = true;
			context.showInclinometer.value = true;
		}

		public override void Update()
		{
			base.Update();
			base.context.UpdateWrongWay();
			base.context.RefreshDriftPointAction();
			base.context.RefreshPlayerList(base.subroom);
		}
	}
}
