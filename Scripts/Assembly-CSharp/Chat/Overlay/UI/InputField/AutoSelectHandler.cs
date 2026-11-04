using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.InputField
{
	public class AutoSelectHandler
	{
		private StateContext m_state;

		private ChatInputFieldElement m_inputField;

		private bool m_hasSelectionBeforeIgnorePanel;

		public void Setup(StateContext state, ChatInputFieldElement inputField)
		{
			m_state = state;
			m_inputField = inputField;
			m_state.actionPanelActive.onChanged += OnActionPanelActiveHandler;
			m_state.ignorePanelActive.onChanged += OnIgnorePanelActiveHandler;
		}

		public void Release()
		{
			m_state.actionPanelActive.onChanged -= OnActionPanelActiveHandler;
			m_state.ignorePanelActive.onChanged -= OnIgnorePanelActiveHandler;
			m_state = null;
			m_inputField = null;
		}

		private void OnActionPanelActiveHandler(bool isActive)
		{
			if (isActive)
			{
				m_inputField.SetSelection(selected: false);
			}
		}

		private void OnIgnorePanelActiveHandler(bool isActive)
		{
			if (isActive)
			{
				m_hasSelectionBeforeIgnorePanel = m_inputField.IsSelected();
				m_inputField.SetSelection(selected: false);
			}
			else
			{
				m_inputField.SetSelection(m_hasSelectionBeforeIgnorePanel);
			}
		}
	}
}
