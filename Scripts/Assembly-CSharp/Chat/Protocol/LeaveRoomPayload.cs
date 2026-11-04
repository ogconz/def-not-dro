using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class LeaveRoomPayload
	{
		[JsonProperty("roomId")]
		public Guid roomId;
	}
}
