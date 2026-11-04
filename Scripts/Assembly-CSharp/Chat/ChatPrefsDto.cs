using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Chat
{
	[Serializable]
	public class ChatPrefsDto
	{
		[JsonProperty]
		public Dictionary<Guid, UserInfo> usersCache = new Dictionary<Guid, UserInfo>();

		[JsonProperty]
		public HashSet<UserInfo> ignoredPlayers = new HashSet<UserInfo>();

		[JsonProperty]
		public Guid selectedChannel = Guid.Empty;
	}
}
