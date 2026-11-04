namespace SyncMultiplayer
{
	public enum PacketId : byte
	{
		Controls = 10,
		Matchmaking = 11,
		Subroom = 13,
		Chat = 14,
		Vote = 15,
		Headlights = 16,
		Ignored = 17,
		Object = 18,
		Notify = 19
	}
}
