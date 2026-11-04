using System;
using Binding;
using Binding.Base;
using Chat.Overlay.UI.MessageList.Scroll;

namespace Chat.Overlay.UI.MessageList.Jump
{
	public class ButtonJumpHandler : BaseBindingTarget
	{
		private ButtonProperty m_jumpButton = new ButtonProperty();

		[NonSerialized]
		private ScrollProcessor m_scrollProcessor;

		public void Setup(ScrollProcessor scrollProcessor)
		{
			m_scrollProcessor = scrollProcessor;
			m_jumpButton.value.onClick.AddListener(OnJumpClickHandler);
		}

		public void Release()
		{
			m_jumpButton.value.onClick.RemoveListener(OnJumpClickHandler);
			m_scrollProcessor = null;
		}

		private void OnJumpClickHandler()
		{
			m_scrollProcessor.FocusToBottom();
		}
	}
}
