namespace SyncMultiplayer
{
	public static class DisqualifyReasonExtension
	{
		public static bool IsChaserGuilty(this DisqualifyReason reason)
		{
			if (reason != DisqualifyReason.Overtaking && reason != DisqualifyReason.ChaserCarCollide)
			{
				return reason == DisqualifyReason.RaceStopped;
			}
			return true;
		}

		public static bool IsLeaderGuilty(this DisqualifyReason reason)
		{
			if (reason != DisqualifyReason.LeaderCarCollide)
			{
				return reason == DisqualifyReason.CriticalBraking;
			}
			return true;
		}

		public static bool IsEmpty(this DisqualifyReason reason)
		{
			return reason == DisqualifyReason.Unknown;
		}
	}
}
