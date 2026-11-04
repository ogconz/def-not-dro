using System;
using Chat.Protocol;

namespace Chat
{
	public class ClientInfo
	{
		public int apiVersion;

		public string networkVersion;

		public string nickName;

		public string token;

		public PlatformAuthType authType;

		public string externalId;

		public Guid internalId;

		public string temporalToken;
	}
}
