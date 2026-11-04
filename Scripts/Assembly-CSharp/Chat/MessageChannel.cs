using System;
using System.Collections.Generic;
using Chat.Protocol;

namespace Chat
{
	public class MessageChannel
	{
		private const int BufferSize = 100;

		private MessagePipeline m_messagePipeline;

		private readonly TargetInfo m_target;

		private string m_caption;

		private List<Message> m_messages = new List<Message>();

		public event Action<Message> onMessageAdded;

		public event Action<Message> onMessageRemoved;

		public event Action<string> onCaptionUpdated;

		public MessageChannel(TargetInfo target)
		{
			m_target = target;
		}

		public TargetInfo GetTargetInfo()
		{
			return m_target;
		}

		public string GetCaption()
		{
			return m_caption;
		}

		public void SetCaption(string caption)
		{
			m_caption = caption;
			this.onCaptionUpdated?.Invoke(m_caption);
		}

		public void SetMessagePipeline(MessagePipeline messagePipeline)
		{
			m_messagePipeline = messagePipeline;
		}

		public IReadOnlyList<Message> GetMessages()
		{
			return m_messages;
		}

		public void AddMessage(Message message)
		{
			m_messagePipeline.Process(message, OnPipelineResultHandler);
		}

		private void OnPipelineResultHandler(Message message)
		{
			if (!message.isLocalOwner || !message.isMessageBlocked)
			{
				RemoveLastMessageIfNeed();
				m_messages.Add(message);
				this.onMessageAdded.SafeCall(message);
			}
		}

		private void RemoveLastMessageIfNeed()
		{
			while (m_messages.Count >= 100)
			{
				this.onMessageRemoved.SafeCall(m_messages[0]);
				m_messages.RemoveAt(0);
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is MessageChannel messageChannel)
			{
				return object.Equals(m_target, messageChannel.m_target);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (m_target == null)
			{
				return 0;
			}
			return m_target.GetHashCode();
		}
	}
}
