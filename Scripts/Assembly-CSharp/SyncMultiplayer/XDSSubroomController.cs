using System.Collections.Generic;
using GameOverlay;
using UnityEngine;

namespace SyncMultiplayer
{
	public class XDSSubroomController : SubroomController
	{
		public XDSSubroomController(SubroomInfo roomInfo, NetGameSubroomsSystem system, NetworkGame game)
			: base(roomInfo, system, game)
		{
		}

		protected override SubroomInfoRaceResult CalcSelfRaceResult()
		{
			return new SubroomInfoRaceResult(UIXDSInfoContext.instance.controller, base.room?.disqualifyReason ?? DisqualifyReason.Unknown, isLeader: false);
		}

		protected override void OnSelfFinished(SubroomInfoRaceResult result)
		{
			UIXDSInfoContext.instance.HoldResult();
			base.OnSelfFinished(result);
		}

		public override void StartRace(float startTime, List<PlayerId> positions)
		{
			if (iPlayInThisRoom)
			{
				UIXDSInfoContext.instance.PrepareForStart(startTime - Time.unscaledTime);
			}
			base.StartRace(startTime, positions);
		}
	}
}
