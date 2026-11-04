using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class RoomRemovedPayload
	{
		[JsonProperty("roomId")]
		public Guid roomId;
	}
}
