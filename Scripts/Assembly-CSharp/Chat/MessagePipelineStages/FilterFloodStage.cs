using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Chat.MessagePipelineStages
{
	public class FilterFloodStage : IMessagePipelineStage
	{
		private const int Timeout = 10;

		private const int MaxMessagePerTime = 5;

		private List<Message> m_buffer = new List<Message>();

		private List<Message> m_clearBuffer = new List<Message>();

		public UniTask ProcessAsync(Message message)
		{
			if (!message.isLocalOwner || message.isMessageBlocked)
			{
				return UniTask.CompletedTask;
			}
			m_buffer.Add(message);
			ClearTimeoutMessages();
			if (m_buffer.Count >= 5)
			{
				message.isMessageBlocked = true;
			}
			return UniTask.CompletedTask;
		}

		private void ClearTimeoutMessages()
		{
			DateTime dateTime = DateTime.Now - TimeSpan.FromSeconds(10.0);
			foreach (Message item in m_buffer)
			{
				if (item.timestamp < dateTime)
				{
					m_clearBuffer.Add(item);
				}
			}
			foreach (Message item2 in m_clearBuffer)
			{
				m_buffer.Remove(item2);
			}
			m_clearBuffer.Clear();
		}
	}
}
