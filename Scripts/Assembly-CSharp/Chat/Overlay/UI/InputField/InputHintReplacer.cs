using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.InputField
{
	public class InputHintReplacer : BaseBindingTarget
	{
		private BoolProperty m_reserveSelectHint = new BoolProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChangedHandler;
			States.onStateActivate += OnStateActivateHandler;
			UpdateHint();
		}

		public void Release()
		{
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChangedHandler;
			States.onStateActivate -= OnStateActivateHandler;
			m_state = null;
		}

		private void OnNavigationLayerActiveChangedHandler(bool isActive)
		{
			UpdateHint();
		}

		private void OnStateActivateHandler(BaseState baseState)
		{
			UpdateHint();
		}

		private void UpdateHint()
		{
			m_reserveSelectHint.value = RequireReplaceSelectHint();
		}

		private bool RequireReplaceSelectHint()
		{
			if (!m_state.navigationLayerActive.Get())
			{
				return States.current is GarageGUIState;
			}
			return true;
		}
	}
}
