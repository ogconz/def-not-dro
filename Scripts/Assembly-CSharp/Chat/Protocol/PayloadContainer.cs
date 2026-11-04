using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class PayloadContainer<TPayload>
	{
		[JsonProperty("type")]
		public string type;

		[JsonProperty("payload")]
		public TPayload payload;
	}
}
