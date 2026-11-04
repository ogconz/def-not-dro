using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	public class SubroomView
	{
		public SubroomInfo subroom { get; private set; }

		public UIRaceNetworkModeContext context { get; private set; }

		public static SubroomView Create(UIRaceNetworkModeContext context, SubroomInfo subroom)
		{
			return subroom.subroomMode switch
			{
				SubroomMode.TimeAttack => new TimeAttackView(context, subroom), 
				SubroomMode.Tandem => new TandemView(context, subroom), 
				SubroomMode.TandemXDS => new TandemXDSView(context, subroom), 
				SubroomMode.XDS => new XDSView(context, subroom), 
				SubroomMode.ClippingDuel => new ClippingDuelView(context, subroom), 
				SubroomMode.Challenge => new ChallengeView(context, subroom), 
				_ => new SubroomView(context, subroom), 
			};
		}

		protected SubroomView(UIRaceNetworkModeContext context, SubroomInfo subroom)
		{
			this.context = context;
			this.subroom = subroom;
			context.isWaitingOpponent.value = false;
			context.raceState.HideStarter();
		}

		public virtual void Destroy()
		{
			context.showInclinometer.value = true;
			context.isWaitingOpponent.value = false;
			context.raceState.HideStarter();
		}

		public virtual void Update()
		{
			if (subroom.launchTime > Time.unscaledTime)
			{
				context.SetTimer(subroom.raceTime);
			}
			else
			{
				context.SetTimer(subroom.GetLiveTime());
			}
		}
	}
}
