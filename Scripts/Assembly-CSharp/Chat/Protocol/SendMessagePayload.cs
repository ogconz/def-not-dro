using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class SendMessagePayload
	{
		[JsonProperty("id")]
		public Guid id;

		[JsonProperty("message")]
		public string message;

		[JsonProperty("target")]
		public TargetInfo target;
	}
}
