namespace SyncMultiplayer
{
	public class TimeAttackSubroomController : SubroomController
	{
		public TimeAttackSubroomController(SubroomInfo roomInfo, NetGameSubroomsSystem system, NetworkGame game)
			: base(roomInfo, system, game)
		{
		}

		protected override SubroomInfoRaceResult CalcSelfRaceResult()
		{
			float time = 0f;
			if ((bool)base.raceState)
			{
				time = (base.raceState.currentPenaltyTime.HasValue ? base.raceState.currentPenaltyTime.Value : 0f);
				time += (base.raceState.currentLapTime.HasValue ? base.raceState.currentLapTime.Value : 0f);
			}
			return new SubroomInfoRaceResult(time, base.room.disqualifyReason);
		}
	}
}
