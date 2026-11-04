using System.Collections.Generic;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings
{
	public class RoomPackageThreadScheduleSettings
	{
		public const int DefaultDelayMs = 100;

		protected Dictionary<PacketId, int> m_delayPerPacket = new Dictionary<PacketId, int> { 
		{
			PacketId.Controls,
			25
		} };

		public int GetDelay(PacketId packetId)
		{
			if (m_delayPerPacket.TryGetValue(packetId, out var value))
			{
				return value;
			}
			return 100;
		}
	}
}
