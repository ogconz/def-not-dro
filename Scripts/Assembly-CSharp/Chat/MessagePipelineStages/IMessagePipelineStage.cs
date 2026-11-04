using Cysharp.Threading.Tasks;

namespace Chat.MessagePipelineStages
{
	public interface IMessagePipelineStage
	{
		UniTask ProcessAsync(Message message);
	}
}
