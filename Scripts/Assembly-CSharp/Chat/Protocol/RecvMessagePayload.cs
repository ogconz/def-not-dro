using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class RecvMessagePayload
	{
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public Guid id;

		[JsonProperty("n")]
		public string username;

		[JsonProperty("tm")]
		[JsonConverter(typeof(UnixMillisecondsConverter))]
		public DateTime timestamp;

		[JsonProperty("m")]
		public string message;

		[JsonProperty("mid", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public Guid messageId;

		[JsonProperty("tg")]
		public TargetType targetType;

		[JsonProperty("tg-id", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public Guid targetId;

		[JsonProperty("ex-id")]
		public string externalId;

		[JsonProperty("p")]
		public PlatformAuthType platformAuthType;
	}
}
