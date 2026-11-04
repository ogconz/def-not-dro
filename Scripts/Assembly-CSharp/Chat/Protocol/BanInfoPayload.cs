using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class BanInfoPayload
	{
		[JsonProperty("reason")]
		public string reason;
	}
}
