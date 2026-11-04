using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList.Jump
{
	public class JumpVisibilityHandler : BaseBindingTarget
	{
		private BoolProperty m_jumpVisibility = new BoolProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.messageScrollVisible.onChanged += UpdateJumpVisibilityHandler;
			m_state.messageAutoScrollMode.onChanged += UpdateJumpVisibilityHandler;
		}

		public void Release()
		{
			m_state.messageScrollVisible.onChanged -= UpdateJumpVisibilityHandler;
			m_state.messageAutoScrollMode.onChanged -= UpdateJumpVisibilityHandler;
			m_state = null;
		}

		private void UpdateJumpVisibilityHandler(bool _)
		{
			m_jumpVisibility.value = m_state.messageScrollVisible.Get() && !m_state.messageAutoScrollMode.Get();
		}
	}
}
