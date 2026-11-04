using System;
using System.Collections.Generic;
using Chat.MessagePipelineStages;
using Cysharp.Threading.Tasks;

namespace Chat
{
	public class MessagePipeline
	{
		private List<IMessagePipelineStage> m_pipeline = new List<IMessagePipelineStage>();

		public void Add(IMessagePipelineStage stage)
		{
			m_pipeline.Add(stage);
		}

		public async UniTask ProcessAsync(Message message)
		{
			for (int i = 0; i < m_pipeline.Count; i++)
			{
				await m_pipeline[i].ProcessAsync(message);
			}
		}

		public void Process(Message message, Action<Message> callback)
		{
			ProcessAsync(message).ContinueWith((Action)InvokeCallback).Forget();
			void InvokeCallback()
			{
				callback.SafeCall(message);
			}
		}
	}
}
