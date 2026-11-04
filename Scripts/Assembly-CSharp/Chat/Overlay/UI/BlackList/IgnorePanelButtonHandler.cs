using Binding;
using Binding.Base;

namespace Chat.Overlay.UI.BlackList
{
	public class IgnorePanelButtonHandler : BaseBindingTarget
	{
		private ButtonProperty m_ignorePanelButton = new ButtonProperty();

		private ActionElementBindingProperty m_ignorePanelAction = new ActionElementBindingProperty();

		private UIChatIgnoreListPanelContext m_ignorePanel;

		public void Setup(UIChatIgnoreListPanelContext ignorePanel)
		{
			m_ignorePanel = ignorePanel;
			m_ignorePanelButton.value.onClick.AddListener(m_ignorePanel.Open);
			m_ignorePanelAction.BindAction(m_ignorePanel.Open);
		}

		public void Release()
		{
			m_ignorePanelButton.value.onClick.RemoveListener(m_ignorePanel.Open);
			m_ignorePanelAction.ReleaseAction();
			m_ignorePanel = null;
		}
	}
}
