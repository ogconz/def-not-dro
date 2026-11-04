using System.Collections.Generic;

namespace SyncMultiplayer
{
	public delegate void RoomOptionsEvent(SmartfoxRoomClient client, SmartfoxRoomOptions options, HashSet<string> changedProps);
}
