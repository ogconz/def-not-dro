using System;
using Chat.EventHandlers;
using Chat.Features;
using Chat.MessagePipelineStages;
using Chat.Protocol;
using GameOverlay;

namespace Chat
{
	public class ChatService
	{
		private const int ChatApiVersion = 2;

		private const string ChatNetworkVersion = "2.27.3";

		private bool m_initialized;

		private ClientInfo m_clientInfo;

		private ChatClient m_client;

		private SessionTicketHolder m_sessionTicketHolder;

		private ConnectionHolder m_connectionHolder;

		private ChannelProvider m_channelProvider;

		private MessagePipelineService m_messagePipelineService;

		private MessageTransferScheduler m_messageTransferScheduler;

		private HandlerProvider m_handlerProvider;

		private ChannelLocalizer m_channelLocalizer;

		private NetworkRoomHolder m_networkRoomHolder;

		private UserInfoProvider m_userInfoProvider;

		private IgnoreUsersProvider m_ignoreUsersProvider;

		private ChannelHolder m_channelHolder;

		public ChannelProvider channelProvider => m_channelProvider;

		public UserInfoProvider userInfoProvider => m_userInfoProvider;

		public IgnoreUsersProvider ignoreUsersProvider => m_ignoreUsersProvider;

		public ChannelHolder channelHolder => m_channelHolder;

		public ChatClient client => m_client;

		public event Action onInitialized;

		public event Action onReleased;

		public bool IsInitialized()
		{
			return m_initialized;
		}

		public void Initialize()
		{
			GameSync.onSuccessAuthorization += Setup;
		}

		private void Setup()
		{
			GameSync.onSuccessAuthorization -= Setup;
			if (!IsInitialized())
			{
				m_clientInfo = BuildClientInfo();
				m_client = new ChatClient(m_clientInfo);
				m_sessionTicketHolder = new SessionTicketHolder(m_clientInfo, m_client);
				m_connectionHolder = new ConnectionHolder(m_client, m_sessionTicketHolder);
				m_channelProvider = new ChannelProvider();
				m_messagePipelineService = new MessagePipelineService(m_channelProvider);
				m_messageTransferScheduler = new MessageTransferScheduler(m_client);
				m_handlerProvider = new HandlerProvider();
				m_channelLocalizer = new ChannelLocalizer(m_channelProvider);
				m_networkRoomHolder = new NetworkRoomHolder(m_client, m_channelProvider);
				m_userInfoProvider = new UserInfoProvider();
				m_ignoreUsersProvider = new IgnoreUsersProvider();
				m_channelHolder = new ChannelHolder();
				m_handlerProvider.Register(new PlayerInfoDataHandler(m_clientInfo));
				m_handlerProvider.Register(new PlayerBanDataHandler());
				m_handlerProvider.Register(new RoomsHandler(m_channelProvider));
				m_handlerProvider.Register(new HandshakeMessageHandler(m_channelProvider, m_clientInfo));
				m_handlerProvider.Register(new ReceiveMessageHandler(m_channelProvider, m_clientInfo));
				m_handlerProvider.AddListeners(m_client.dispatcher);
				m_messagePipelineService.Add(() => new FilterCrossPlatformStage(m_clientInfo));
				m_messagePipelineService.Add(() => new UserInfoRegistryStage(m_userInfoProvider));
				m_messagePipelineService.Add(() => new FilterUsernameStage());
				m_messagePipelineService.Add(() => new FilterContentStage());
				m_messagePipelineService.Add(() => new FilterFloodStage());
				m_messagePipelineService.Add(() => new FilterSpamStage());
				m_messagePipelineService.Add(() => new TransferMessageStage(m_messageTransferScheduler));
				m_connectionHolder.Start();
				m_messageTransferScheduler.Start();
				m_initialized = true;
				this.onInitialized?.Invoke();
			}
		}

		public void Release()
		{
			if (IsInitialized())
			{
				m_initialized = false;
				this.onReleased?.Invoke();
				m_userInfoProvider = null;
				m_ignoreUsersProvider = null;
				m_channelHolder = null;
				m_networkRoomHolder.Release();
				m_networkRoomHolder = null;
				m_channelLocalizer.Release();
				m_channelLocalizer = null;
				m_handlerProvider.RemoveListeners(m_client.dispatcher);
				m_handlerProvider = null;
				m_connectionHolder.Stop();
				m_connectionHolder = null;
				m_sessionTicketHolder.Release();
				m_sessionTicketHolder = null;
				m_messageTransferScheduler.Stop();
				m_messageTransferScheduler = null;
				m_messagePipelineService.Release();
				m_messagePipelineService = null;
				m_channelProvider.Release();
				m_channelProvider = null;
				m_client.Release();
				m_client = null;
			}
		}

		public void SendMessage(MessageChannel channel, string message)
		{
			Message message2 = new Message(channel.GetTargetInfo(), message);
			message2.username = m_clientInfo.nickName;
			message2.userId = m_clientInfo.internalId;
			message2.userExternalId = m_clientInfo.externalId;
			message2.platformAuthType = m_clientInfo.authType;
			message2.timestamp = DateTime.Now;
			channel.AddMessage(message2);
		}

		private ClientInfo BuildClientInfo()
		{
			IPlayer player = GameOverlay.Overlay.instance.player;
			return new ClientInfo
			{
				apiVersion = 2,
				networkVersion = "2.27.3",
				nickName = player.nickName,
				authType = PlatformMapping.PlatformToAuth(GameOverlay.Overlay.GetPlatform()),
				externalId = player.id.accountId.ToString()
			};
		}
	}
}
