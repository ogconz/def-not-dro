using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class GetAndJoinMpRoomPayload
	{
		[JsonProperty("mpRoomId")]
		public Guid mpRoomId;

		[JsonProperty("maxPlayersCount")]
		public int maxPlayersCount;

		[JsonProperty("password")]
		public string password;
	}
}
