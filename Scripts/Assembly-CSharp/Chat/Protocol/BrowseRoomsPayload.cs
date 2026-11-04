using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class BrowseRoomsPayload
	{
		[JsonProperty("limit")]
		public int limit;

		[JsonProperty("offset")]
		public int offset;
	}
}
