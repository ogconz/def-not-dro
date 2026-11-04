using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class RoomsInfoPayload
	{
		[JsonProperty("joinedRooms")]
		public RoomInfo[] joinedRooms;
	}
}
