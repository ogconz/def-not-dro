using System;
using Binding.Base;
using Chat.Overlay.UI.MessageList.Scroll;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList.Jump
{
	public class JumpProcessor : BaseBindingTarget
	{
		private StateContext m_state;

		[NonSerialized]
		private ScrollProcessor m_scrollProcessor;

		private JumpVisibilityHandler m_visibilityHandler = new JumpVisibilityHandler();

		private ButtonJumpHandler m_buttonJumpHandler = new ButtonJumpHandler();

		private AxisJumpHandler m_axisJumpHandler = new AxisJumpHandler();

		public void Setup(StateContext state, ScrollProcessor scrollProcessor)
		{
			m_state = state;
			m_scrollProcessor = scrollProcessor;
			m_visibilityHandler.Setup(m_state);
			m_buttonJumpHandler.Setup(m_scrollProcessor);
			m_axisJumpHandler.Setup(m_scrollProcessor);
		}

		public void Release()
		{
			m_axisJumpHandler.Release();
			m_buttonJumpHandler.Release();
			m_visibilityHandler.Release();
			m_state = null;
			m_scrollProcessor = null;
		}
	}
}
