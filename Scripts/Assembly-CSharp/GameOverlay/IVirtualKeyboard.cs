using System;
using Cysharp.Threading.Tasks;

namespace GameOverlay
{
	public interface IVirtualKeyboard
	{
		void ReceiveText(string title, string defaultText, int maxLength, Action<string> callback);

		UniTask<string> ReceiveText(string title, string defaultText, int maxLength);
	}
}
