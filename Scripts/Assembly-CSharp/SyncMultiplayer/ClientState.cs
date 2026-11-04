namespace SyncMultiplayer
{
	public enum ClientState
	{
		Uninitialized = 0,
		StartedConnection = 1,
		ConnectedToGameserver = 2,
		Authenticated = 3,
		Disconnected = 4,
		JoinedLobby = 5,
		RoomCreating = 6,
		Joined = 7
	}
}
