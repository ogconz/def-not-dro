using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Chat.MessagePipelineStages
{
	public class FilterContentStage : IMessagePipelineStage
	{
		public async UniTask ProcessAsync(Message message)
		{
			if (!message.isMessageBlocked)
			{
				RawTextFilter(message);
			}
			if (!message.isMessageBlocked && !message.isLocalOwner)
			{
				await OverlayTextFilterAsync(message);
			}
		}

		private void RawTextFilter(Message message)
		{
			message.content = UIUtils.FilterText(message.content);
			BlockMessageIfEmpty(message);
		}

		private async UniTask OverlayTextFilterAsync(Message message)
		{
			AutoResetUniTaskCompletionSource tokenResult = AutoResetUniTaskCompletionSource.Create();
			GameOverlay.Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Message, UserPlatform.Id.Unknown, message.content, OnFilterCallback);
			BlockMessageIfEmpty(message);
			await tokenResult.Task;
			void OnFilterCallback(string result)
			{
				message.content = result;
				BlockMessageIfEmpty(message);
				tokenResult.TrySetResult();
			}
		}

		private void BlockMessageIfEmpty(Message message)
		{
			if (string.IsNullOrWhiteSpace(message.content))
			{
				message.isMessageBlocked = true;
			}
		}
	}
}
