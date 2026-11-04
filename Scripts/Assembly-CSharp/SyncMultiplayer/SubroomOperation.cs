namespace SyncMultiplayer
{
	public enum SubroomOperation : byte
	{
		Spawn = 1,
		Join = 2,
		Leave = 3,
		Destroy = 4,
		RequestJoin = 5,
		RequestJoinNegative = 6,
		StartRace = 7,
		RaceResult = 8,
		LocalFinished = 11,
		WaitRatingRace = 12,
		ChangeRoomID = 13,
		ConfirmReadyToStart = 14,
		OtherFinished = 15
	}
}
