using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	public class TandemView : SubroomView
	{
		public TandemView(UIRaceNetworkModeContext context, SubroomInfo subroom)
			: base(context, subroom)
		{
			context.isTandemMode.value = true;
			context.isTandemChaser.value = false;
			context.showInclinometer.value = false;
		}

		public override void Destroy()
		{
			base.context.isTandemMode.value = false;
			base.context.isTandemChaser.value = false;
			base.Destroy();
		}

		public override void Update()
		{
			base.Update();
			DriftController driftController = base.context.GetDriftController();
			if (driftController != null)
			{
				base.context.isTandemChaser.value = driftController.tandemLeader != null && driftController.tandemLeader.isNetworkCar && Time.unscaledTime > base.subroom.launchTime;
				if (base.context.isTandemChaser.value)
				{
					base.context.tandemFactor.value = 0.2f * driftController.tandemMultiplier;
				}
			}
			base.context.UpdateWrongWay();
			base.context.SetDriftPoints(base.context.raceState.currentDriftPoints);
			base.context.RefreshPairPlayersPoints(base.subroom);
			base.context.isDriftMode.value = !base.context.isTandemChaser.value && base.subroom.state == SubroomState.Race;
			base.context.showInclinometer.value = !base.context.isTandemChaser.value;
			base.context.RefreshDriftPointAction();
		}
	}
}
