using Cysharp.Threading.Tasks;

namespace Chat.MessagePipelineStages
{
	public class FilterSpamStage : IMessagePipelineStage
	{
		private string m_previousContent;

		public UniTask ProcessAsync(Message message)
		{
			if (!message.isLocalOwner || message.isMessageBlocked)
			{
				return UniTask.CompletedTask;
			}
			if (string.CompareOrdinal(m_previousContent, message.content) == 0)
			{
				message.isMessageBlocked = true;
			}
			m_previousContent = message.content;
			return UniTask.CompletedTask;
		}
	}
}
