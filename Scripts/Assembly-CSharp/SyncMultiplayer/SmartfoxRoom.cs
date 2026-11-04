using Sfs2X.Entities;

namespace SyncMultiplayer
{
	public class SmartfoxRoom
	{
		public Room Room { get; private set; }

		public SmartfoxRoomOptions RoomOptions { get; private set; }

		public SmartfoxRoom(Room room, SmartfoxRoomOptions options = null)
		{
			Room = room;
			RoomOptions = options ?? new SmartfoxRoomOptions(room);
		}
	}
}
