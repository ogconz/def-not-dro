using SyncMultiplayer;

namespace HUD
{
	public class TimeAttackView : SubroomView
	{
		public TimeAttackView(UIRaceNetworkModeContext context, SubroomInfo subroom)
			: base(context, subroom)
		{
			context.isDriftMode.value = false;
			context.showInclinometer.value = false;
		}

		public override void Update()
		{
			base.Update();
			base.context.RefreshTimeAttack();
			base.context.UpdateWrongWay();
			base.context.RefreshPlayerList(base.subroom);
		}
	}
}
