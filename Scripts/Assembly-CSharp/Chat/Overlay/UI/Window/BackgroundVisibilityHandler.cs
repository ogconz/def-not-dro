using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.Window
{
	public class BackgroundVisibilityHandler : BaseBindingTarget
	{
		private BoolProperty m_windowBackground = new BoolProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.controlLayerActive.onChanged += OnStateFlagChangedHandler;
			m_state.xrModeActive.onChanged += OnStateFlagChangedHandler;
		}

		public void Release()
		{
			m_state.controlLayerActive.onChanged -= OnStateFlagChangedHandler;
			m_state.xrModeActive.onChanged -= OnStateFlagChangedHandler;
			m_state = null;
		}

		private void OnStateFlagChangedHandler(bool isActive)
		{
			m_windowBackground.value = CanShowBackground();
		}

		private bool CanShowBackground()
		{
			if (!m_state.controlLayerActive.Get())
			{
				return m_state.xrModeActive.Get();
			}
			return true;
		}
	}
}
