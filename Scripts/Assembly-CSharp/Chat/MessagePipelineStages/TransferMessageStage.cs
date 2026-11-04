using Cysharp.Threading.Tasks;

namespace Chat.MessagePipelineStages
{
	public class TransferMessageStage : IMessagePipelineStage
	{
		private MessageTransferScheduler m_messageTransferScheduler;

		public TransferMessageStage(MessageTransferScheduler messageTransferScheduler)
		{
			m_messageTransferScheduler = messageTransferScheduler;
		}

		public UniTask ProcessAsync(Message message)
		{
			if (!message.isMessageBlocked)
			{
				m_messageTransferScheduler.AddToQueue(message);
			}
			return UniTask.CompletedTask;
		}
	}
}
