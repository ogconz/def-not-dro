using System;
using System.Collections.Generic;
using Chat.MessagePipelineStages;

namespace Chat
{
	public class MessagePipelineService
	{
		private ChannelProvider m_channelProvider;

		private List<Func<IMessagePipelineStage>> m_stages;

		public MessagePipelineService(ChannelProvider channelProvider)
		{
			m_channelProvider = channelProvider;
			m_stages = new List<Func<IMessagePipelineStage>>();
			m_channelProvider.onChannelAdded += InjectMessagePipelineInChannel;
		}

		public void Release()
		{
			m_channelProvider.onChannelAdded -= InjectMessagePipelineInChannel;
			m_channelProvider = null;
			m_stages = null;
		}

		public void Add(Func<IMessagePipelineStage> constructor)
		{
			m_stages.Add(constructor);
		}

		private void InjectMessagePipelineInChannel(MessageChannel channel)
		{
			channel.SetMessagePipeline(CreatePipeline());
		}

		private MessagePipeline CreatePipeline()
		{
			MessagePipeline messagePipeline = new MessagePipeline();
			foreach (Func<IMessagePipelineStage> stage in m_stages)
			{
				messagePipeline.Add(stage());
			}
			return messagePipeline;
		}
	}
}
