using System;
using Cysharp.Threading.Tasks;

namespace GameOverlay
{
	public class VoidVirtualKeyboard : IVirtualKeyboard
	{
		public void ReceiveText(string title, string defaultText, int maxLength, Action<string> callback)
		{
			callback?.Invoke(null);
		}

		public async UniTask<string> ReceiveText(string title, string defaultText, int maxLength)
		{
			return null;
		}
	}
}
