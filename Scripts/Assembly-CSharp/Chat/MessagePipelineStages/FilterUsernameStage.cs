using Chat.Protocol;
using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Chat.MessagePipelineStages
{
	public class FilterUsernameStage : IMessagePipelineStage
	{
		public async UniTask ProcessAsync(Message message)
		{
			if (!message.isLocalOwner)
			{
				await OverlayUsernameFilterAsync(message);
			}
		}

		private async UniTask OverlayUsernameFilterAsync(Message message)
		{
			AutoResetUniTaskCompletionSource tokenResult = AutoResetUniTaskCompletionSource.Create();
			GameOverlay.Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Nickname, PlatformMapping.AuthToPlatform(message.platformAuthType), message.username, OnFilterCallback);
			await tokenResult.Task;
			void OnFilterCallback(string result)
			{
				message.username = result;
				tokenResult.TrySetResult();
			}
		}
	}
}
