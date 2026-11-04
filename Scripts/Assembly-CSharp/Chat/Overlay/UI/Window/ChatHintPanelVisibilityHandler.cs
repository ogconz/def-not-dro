using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.Window
{
	public class ChatHintPanelVisibilityHandler : BaseBindingTarget
	{
		private BoolProperty m_chatHintPanelVisibility = new BoolProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.windowActive.onChanged += OnWindowActiveHandler;
		}

		public void Release()
		{
			m_state.windowActive.onChanged -= OnWindowActiveHandler;
			m_state = null;
		}

		private void OnWindowActiveHandler(bool isActive)
		{
			m_chatHintPanelVisibility.value = isActive;
		}
	}
}
