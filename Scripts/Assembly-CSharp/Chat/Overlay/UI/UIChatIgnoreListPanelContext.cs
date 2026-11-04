using System;
using Binding;
using Binding.Base;
using Chat.Features;
using Chat.Overlay.UI.BlackList;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;

namespace Chat.Overlay.UI
{
	public class UIChatIgnoreListPanelContext : BaseBindingBehaviourTarget
	{
		private ChatIgnoreListProperty m_users = new ChatIgnoreListProperty();

		private BoolProperty m_isEmpty = new BoolProperty();

		private IgnoreListPanelStateHandler m_stateHandler = new IgnoreListPanelStateHandler();

		private HotkeyProvider m_hotkeyProvider = new HotkeyProvider();

		private NavigationBinder m_navigationBinder = new NavigationBinder();

		private AutoSelectHandler m_autoSelectHandler = new AutoSelectHandler();

		private IgnoreUsersProvider m_ignoreUsersProvider;

		private StateContext m_stateContext;

		private bool m_initialized;

		public event Action onActivityChanged;

		public bool IsActive()
		{
			return base.isActive;
		}

		public void Setup(IgnoreUsersProvider ignoreUsersProvider, StateContext stateContext)
		{
			m_ignoreUsersProvider = ignoreUsersProvider;
			m_stateContext = stateContext;
			m_stateHandler.Setup(this, m_stateContext);
			m_autoSelectHandler.Setup(m_stateContext, m_users);
		}

		public void Release()
		{
			Close();
			m_autoSelectHandler.Release();
			m_stateHandler.Release();
			m_ignoreUsersProvider = null;
			m_stateContext = null;
		}

		public void Open()
		{
			if (!m_initialized)
			{
				m_stateContext.forceActivateControlLayer.Set(value: true);
				m_users.Setup(m_ignoreUsersProvider);
				m_users.onUpdateCanvasEvent += OnUpdateCanvasEventHandler;
				base.owner.Push<UIChatIgnoreListPanelContext>();
				m_navigationBinder.Setup(m_hotkeyProvider, this, m_users);
				m_initialized = true;
				OnUpdateCanvasEventHandler();
				InvokeOnActivityChanged();
				m_stateContext.forceActivateControlLayer.Set(value: false);
			}
		}

		public void Close()
		{
			if (m_initialized)
			{
				m_initialized = false;
				base.owner.Hide<UIChatIgnoreListPanelContext>();
				m_navigationBinder.Release();
				m_users.onUpdateCanvasEvent -= OnUpdateCanvasEventHandler;
				m_users.Release();
				OnUpdateCanvasEventHandler();
				InvokeOnActivityChanged();
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			m_hotkeyProvider.Setup(this);
		}

		protected override void OnDeactivate()
		{
			m_hotkeyProvider.Release();
			Close();
			base.OnDeactivate();
		}

		private void InvokeOnActivityChanged()
		{
			this.onActivityChanged.SafeCall();
		}

		private void OnUpdateCanvasEventHandler()
		{
			m_isEmpty.value = m_initialized && m_users.count == 0;
		}

		public override bool IsTopOrBottomContext(BaseContext context)
		{
			return false;
		}
	}
}
