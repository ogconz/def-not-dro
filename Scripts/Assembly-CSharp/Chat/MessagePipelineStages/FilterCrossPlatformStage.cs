using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Chat.MessagePipelineStages
{
	public class FilterCrossPlatformStage : IMessagePipelineStage
	{
		private ClientInfo m_clientInfo;

		public FilterCrossPlatformStage(ClientInfo clientInfo)
		{
			m_clientInfo = clientInfo;
		}

		public UniTask ProcessAsync(Message message)
		{
			message.isSamePlatform = message.platformAuthType == m_clientInfo.authType;
			if (HasCommunicationRestrictions(message))
			{
				message.isMessageBlocked = true;
			}
			return UniTask.CompletedTask;
		}

		private bool HasCommunicationRestrictions(Message message)
		{
			IGameOverlay instance = GameOverlay.Overlay.instance;
			if (instance == null)
			{
				return false;
			}
			if (!message.isSamePlatform && instance.IsCrossTextCommunicationRestricted)
			{
				return true;
			}
			return false;
		}
	}
}
