using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class JoinRoomPayload
	{
		[JsonProperty("roomId")]
		public Guid roomId;

		[JsonProperty("password")]
		public string password;
	}
}
