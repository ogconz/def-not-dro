using System;
using Newtonsoft.Json;

namespace Chat.Protocol
{
	public class RoomInfo
	{
		[JsonProperty("id")]
		public Guid id;

		[JsonProperty("name")]
		public string name;

		[JsonProperty("description")]
		public string description;

		[JsonProperty("crossPlatform")]
		public bool crossPlatform;

		[JsonProperty("crossProject")]
		public bool crossProject;

		[JsonProperty("project")]
		public string project;

		[JsonProperty("platform")]
		public string platform;

		[JsonProperty("type")]
		public TargetType type;

		[JsonProperty("maxPlayers")]
		public int maxPlayers;

		[JsonProperty("joined")]
		public bool joined;
	}
}
