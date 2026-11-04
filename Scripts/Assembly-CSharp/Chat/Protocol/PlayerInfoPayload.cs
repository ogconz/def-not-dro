using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class PlayerInfoPayload
	{
		[JsonProperty("internalId")]
		public Guid internalId;

		[JsonProperty("temporalToken")]
		public string temporalToken;
	}
}
