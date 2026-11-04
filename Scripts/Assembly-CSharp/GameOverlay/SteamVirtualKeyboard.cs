using System;
using Cysharp.Threading.Tasks;

namespace GameOverlay
{
	public class SteamVirtualKeyboard : IVirtualKeyboard
	{
		public void ReceiveText(string title, string defaultText, int maxLength, Action<string> callback)
		{
			IGameOverlay instance = Overlay.instance;
			SteamOverlay overlay = instance as SteamOverlay;
			if (overlay != null && GameManager.IsLinuxOsPlatform())
			{
				SteamOverlay steamOverlay = overlay;
				steamOverlay.onCloseOverlayKeyboardSubmit = (Action<string, bool>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboardSubmit, new Action<string, bool>(ProcessDialog));
				overlay.ShowOverlayKeyboard(title, defaultText, maxLength);
			}
			else
			{
				callback.SafeCall(null);
			}
			void ProcessDialog(string text, bool submit)
			{
				SteamOverlay steamOverlay2 = overlay;
				steamOverlay2.onCloseOverlayKeyboardSubmit = (Action<string, bool>)Delegate.Remove(steamOverlay2.onCloseOverlayKeyboardSubmit, new Action<string, bool>(ProcessDialog));
				callback.SafeCall(submit ? text : null);
			}
		}

		public async UniTask<string> ReceiveText(string title, string defaultText, int maxLength)
		{
			AutoResetUniTaskCompletionSource<string> tokenResult = AutoResetUniTaskCompletionSource<string>.Create();
			ReceiveText(title, defaultText, maxLength, delegate(string text)
			{
				tokenResult.TrySetResult(text);
			});
			return await tokenResult.Task;
		}
	}
}
