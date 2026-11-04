using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.BlackList
{
	public class IgnorePanelHintVisibilityHandler : BaseBindingTarget
	{
		private BoolProperty m_ignorePanelHint = new BoolProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveHandler;
		}

		public void Release()
		{
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveHandler;
			m_state = null;
		}

		private void OnNavigationLayerActiveHandler(bool isActive)
		{
			m_ignorePanelHint.value = isActive;
		}
	}
}
