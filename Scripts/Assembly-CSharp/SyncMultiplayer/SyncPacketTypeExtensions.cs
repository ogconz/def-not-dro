namespace SyncMultiplayer
{
	public static class SyncPacketTypeExtensions
	{
		public static bool IsId(this SyncPacketType type)
		{
			return type == SyncPacketType.Id;
		}

		public static bool IsInit(this SyncPacketType type)
		{
			return type == SyncPacketType.Init;
		}

		public static bool IsState(this SyncPacketType type)
		{
			return type == SyncPacketType.State;
		}
	}
}
