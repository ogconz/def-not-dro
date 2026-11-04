using GameOverlay;

namespace SyncMultiplayer
{
	public class ValidatedRoomResult
	{
		public readonly PlayerId playerId;

		public readonly SubroomInfoRaceResult result;

		public ValidatedRoomResult(PlayerId playerId, SubroomInfoRaceResult result)
		{
			this.result = result;
			this.playerId = playerId;
		}
	}
}
