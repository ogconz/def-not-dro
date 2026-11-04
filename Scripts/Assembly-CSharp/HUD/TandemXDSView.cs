using SyncMultiplayer;

namespace HUD
{
	public class TandemXDSView : SubroomView
	{
		public TandemXDSView(UIRaceNetworkModeContext context, SubroomInfo subroom)
			: base(context, subroom)
		{
			context.wrongWay = false;
			context.isTandemMode.value = true;
			context.isDriftMode.value = false;
			context.showInclinometer.value = true;
		}

		public override void Destroy()
		{
			base.context.isTandemMode.value = false;
			base.Destroy();
		}

		public override void Update()
		{
			base.Update();
			base.context.RefreshPairPlayersPoints(base.subroom);
		}
	}
}
