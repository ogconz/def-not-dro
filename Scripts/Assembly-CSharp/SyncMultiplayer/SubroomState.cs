namespace SyncMultiplayer
{
	public enum SubroomState : byte
	{
		WaitingPlayers = 0,
		WaitingStart = 1,
		Race = 2,
		WaitingResults = 3,
		Finished = 4,
		WaitingSecondRace = 5
	}
}
