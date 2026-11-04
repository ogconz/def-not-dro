using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class TargetInfo
	{
		[JsonProperty("type")]
		public TargetType type;

		[JsonProperty("id")]
		public Guid id;
	}
}
