using System;
using Chat.Protocol;
using GameOverlay;

namespace Chat
{
	public class Message
	{
		public TargetInfo targetInfo;

		public string content;

		public string username;

		public Guid messageId;

		public Guid userId;

		public string userExternalId;

		public PlayerId playerId;

		public PlatformAuthType platformAuthType;

		public DateTime timestamp;

		public DateTime syncTime;

		public bool synced;

		public int syncAttempt;

		public bool skipSyncAnimation;

		public bool isLocalOwner;

		public bool isMessageBlocked;

		public bool isSamePlatform;

		public event Action onSyncedEvent;

		public Message(TargetInfo targetInfo, string content)
		{
			this.targetInfo = targetInfo;
			this.content = content;
			messageId = Guid.NewGuid();
			synced = false;
			isLocalOwner = true;
			TryConstructPlayerId();
		}

		public Message(RecvMessagePayload payload)
		{
			content = payload.message;
			username = payload.username;
			messageId = payload.messageId;
			userId = payload.id;
			userExternalId = payload.externalId;
			platformAuthType = payload.platformAuthType;
			timestamp = payload.timestamp.ToLocalTime();
			syncTime = payload.timestamp.ToLocalTime();
			synced = true;
			isLocalOwner = false;
			TryConstructPlayerId();
		}

		public void ConfirmSync()
		{
			synced = true;
			this.onSyncedEvent.SafeCall();
		}

		private void TryConstructPlayerId()
		{
			int num = (int)PlatformMapping.AuthToPlatform(platformAuthType);
			PlayerId.TryParse($"{userExternalId}|{num}", out playerId);
		}
	}
}
