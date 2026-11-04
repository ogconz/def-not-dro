namespace SyncMultiplayer
{
	public interface ISingleMasterPacketAutoHandler
	{
		PacketId OpCode { get; }

		void Handle(NetworkPlayer player, SmartfoxDataPackage data);
	}
}
