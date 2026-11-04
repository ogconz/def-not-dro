using System;
using DI;

namespace Chat.Overlay
{
	public class ContextVisibilityScheduler
	{
		private bool m_prevOverlayState;

		private GameContextActivityValidator m_contextActivityValidator;

		private GameContextObserver m_contextObserver;

		private GamePrefs m_prefs;

		public event Action<ContextOwner> onOverlayShow;

		public event Action<ContextOwner> onOverlayHide;

		public void Setup(GameContextActivityValidator contextActivityValidator, GameContextObserver contextObserver)
		{
			m_contextActivityValidator = contextActivityValidator;
			m_contextObserver = contextObserver;
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_contextObserver.onStateChanged += UpdateOverlayVisibility;
			m_contextObserver.onActiveContextChanged += UpdateOverlayVisibility;
			KeepAliveManager.onUIVisibleChanged += UpdateOverlayVisibility;
			m_prefs.globals.showChatChanged += UpdateOverlayVisibility;
		}

		public void Release()
		{
			m_contextObserver.onStateChanged -= UpdateOverlayVisibility;
			m_contextObserver.onActiveContextChanged -= UpdateOverlayVisibility;
			KeepAliveManager.onUIVisibleChanged -= UpdateOverlayVisibility;
			m_prefs.globals.showChatChanged -= UpdateOverlayVisibility;
			m_contextActivityValidator = null;
			m_contextObserver = null;
			m_prefs = null;
		}

		public void UpdateOverlayVisibility()
		{
			TryReleaseMainMenuContext();
			TryHookMainMenuContext();
			bool flag = CanActivateOverlay();
			if (m_prevOverlayState != flag)
			{
				m_prevOverlayState = flag;
				ContextOwner currentContextOwner = m_contextObserver.GetCurrentContextOwner();
				if (flag)
				{
					this.onOverlayShow.SafeCall(currentContextOwner);
				}
				else
				{
					this.onOverlayHide.SafeCall(currentContextOwner);
				}
			}
		}

		private bool CanActivateOverlay()
		{
			if (KeepAliveManager.isUIHidden)
			{
				return false;
			}
			if (!m_prefs.globals.chatSingleplayer)
			{
				return false;
			}
			return m_contextActivityValidator.GetActivityState();
		}

		private void TryHookMainMenuContext()
		{
			if (m_contextObserver.GetCurrentState() is GarageGUIState garageGUIState)
			{
				UIMainMenuContext uIMainMenuContext = garageGUIState.contextOwner.Get<UIMainMenuContext>();
				if (uIMainMenuContext != null)
				{
					uIMainMenuContext.indexCategory.OnValueChanged -= UpdateOverlayVisibility;
					uIMainMenuContext.indexCategory.OnValueChanged += UpdateOverlayVisibility;
				}
			}
		}

		private void TryReleaseMainMenuContext()
		{
			ContextOwner currentContextOwner = m_contextObserver.GetCurrentContextOwner();
			if (currentContextOwner != null)
			{
				UIMainMenuContext uIMainMenuContext = currentContextOwner.Get<UIMainMenuContext>();
				if (uIMainMenuContext != null)
				{
					uIMainMenuContext.indexCategory.OnValueChanged -= UpdateOverlayVisibility;
				}
			}
		}
	}
}
