using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Chat
{
	public class MessageTransferScheduler
	{
		private const float TimeToResendMessage = 1f;

		private const int AttemptLimit = 3;

		private ChatClient m_client;

		private Queue<Message> m_queue = new Queue<Message>();

		private Message m_processedMessage;

		private bool m_work;

		public MessageTransferScheduler(ChatClient client)
		{
			m_client = client;
		}

		public void Start()
		{
			m_work = true;
			m_client.eventHandler.onConnected += ResetSyncTimeForMessages;
			UniTask.RunOnThreadPool((Func<UniTask>)MessageSchedulerTask, true, default(CancellationToken)).Forget();
		}

		public void Stop()
		{
			m_client.eventHandler.onConnected -= ResetSyncTimeForMessages;
			m_queue.Clear();
			m_work = false;
		}

		public void AddToQueue(Message message)
		{
			if (message != null && !message.synced)
			{
				m_queue.Enqueue(message);
			}
		}

		private async UniTask MessageSchedulerTask()
		{
			GameLog.Print(AlertLevel.Warning, "Chat", "Service: Start message scheduler");
			while (m_work)
			{
				if (m_client.IsConnected())
				{
					ProcessQueue();
				}
				await UniTask.Delay(100);
			}
			GameLog.Print(AlertLevel.Warning, "Chat", "Service: Stop message scheduler");
		}

		private void ProcessQueue()
		{
			if (m_processedMessage != null && m_processedMessage.synced)
			{
				m_processedMessage = null;
			}
			if (m_processedMessage == null && m_queue.Count > 0)
			{
				m_processedMessage = m_queue.Dequeue();
			}
			ProcessMessage(m_processedMessage);
		}

		private void ProcessMessage(Message message)
		{
			if (message == null || message.synced)
			{
				return;
			}
			bool num = message.syncAttempt == 0;
			DateTime now = DateTime.Now;
			bool flag = (float)(now - message.syncTime).Seconds > 1f;
			if (num || flag)
			{
				message.syncTime = now;
				message.syncAttempt++;
				if (IsAttemptExpired(message))
				{
					m_client.Disconnect();
				}
				else
				{
					m_client.SendMessage(message.targetInfo, message.messageId, message.content);
				}
			}
		}

		private void ResetSyncTimeForMessages()
		{
			foreach (Message item in m_queue)
			{
				if (!item.synced)
				{
					item.syncTime = DateTime.MinValue;
				}
			}
		}

		private bool IsAttemptExpired(Message message)
		{
			if (message.syncAttempt > 0)
			{
				return message.syncAttempt % 4 == 0;
			}
			return false;
		}
	}
}
