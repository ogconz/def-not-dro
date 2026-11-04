using SyncMultiplayer;

namespace HUD
{
	public class XDSView : SubroomView
	{
		public XDSView(UIRaceNetworkModeContext context, SubroomInfo subroom)
			: base(context, subroom)
		{
			context.wrongWay = false;
			context.isDriftMode.value = false;
			context.showInclinometer.value = true;
		}

		public override void Update()
		{
			base.Update();
			base.context.RefreshPlayerList(base.subroom);
		}
	}
}
