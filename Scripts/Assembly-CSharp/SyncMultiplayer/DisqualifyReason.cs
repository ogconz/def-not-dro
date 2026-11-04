namespace SyncMultiplayer
{
	public enum DisqualifyReason
	{
		Unknown = 0,
		StartFailed = 1,
		ReverseDrive = 2,
		LowSpeed = 3,
		BadClipZone = 4,
		LostRoadContact = 5,
		CriticalAngle = 6,
		OppositeDrift = 7,
		SideChange = 8,
		InitiationFailed = 9,
		InitiationFailedByAngle = 10,
		InitiationFailedByWrongSide = 11,
		InitiationFailedByNoDrift = 12,
		CarCollide = 13,
		TimedOut = 14,
		BrakeUse = 15,
		AccelerateUse = 16,
		NoDrift = 17,
		MissedCheckpoint = 18,
		ChaserCarCollide = 19,
		LeaderCarCollide = 20,
		PlayerOut = 21,
		Overtaking = 22,
		BadConnection = 23,
		RaceStopped = 24,
		CriticalBraking = 25
	}
}
