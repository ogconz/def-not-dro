using Binding.Base;
using Chat.Overlay.UI.Layout;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI
{
	public class UIChatOverlayContext : BaseBindingBehaviourTarget
	{
		private UIChatWindowContext m_window;

		private UIChatActionPanelContext m_actionPanel;

		private UIChatIgnoreListPanelContext m_ignorePanel;

		private LayoutData m_layoutData = new LayoutData();

		private StateContext m_state = new StateContext();

		public void Setup(ChatService service)
		{
			m_state.Setup();
			m_actionPanel.Setup(m_state);
			m_ignorePanel.Setup(service.ignoreUsersProvider, m_state);
			m_window.Setup(service, m_state, m_actionPanel, m_ignorePanel);
		}

		public void Release()
		{
			m_window.Release();
			m_ignorePanel.Release();
			m_actionPanel.Release();
			m_state.Release();
			m_window = null;
			m_actionPanel = null;
			m_ignorePanel = null;
		}

		public void Preload()
		{
			base.owner.Preload<UIChatWindowContext>();
			base.owner.Preload<UIChatActionPanelContext>();
			base.owner.Preload<UIChatIgnoreListPanelContext>();
			m_window = base.owner.Get<UIChatWindowContext>();
			m_actionPanel = base.owner.Get<UIChatActionPanelContext>();
			m_ignorePanel = base.owner.Get<UIChatIgnoreListPanelContext>();
		}

		public void UpdateNavigationPriority()
		{
			NavigationInputModule.instance.RegisterContext(m_window);
		}

		public void BindLayout(ContextLayoutProcessor layoutProcessor)
		{
			m_layoutData.CollectAllElements(base.transform);
			layoutProcessor.BindData(m_layoutData);
			layoutProcessor.BindContext(m_window, TargetType.Window);
			layoutProcessor.BindContext(m_actionPanel, TargetType.ActionPanel);
			layoutProcessor.BindContext(m_ignorePanel, TargetType.IgnorePanel);
			layoutProcessor.UpdateLayout();
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			base.owner.Add<UIChatWindowContext>();
		}

		protected override void OnDeactivate()
		{
			base.owner.Hide<UIChatWindowContext>();
			base.owner.Hide<UIChatActionPanelContext>();
			base.owner.Hide<UIChatIgnoreListPanelContext>();
			base.OnDeactivate();
		}
	}
}
