using Chat.Overlay.UI;

namespace Chat.Overlay
{
	public class ContextNavigationPriorityHolder
	{
		private GameContextObserver m_contextObserver;

		public void Setup(GameContextObserver contextObserver)
		{
			m_contextObserver = contextObserver;
			m_contextObserver.onActiveContextChanged += OnActiveContextChangedHandler;
		}

		public void Release()
		{
			m_contextObserver.onActiveContextChanged -= OnActiveContextChangedHandler;
			m_contextObserver = null;
		}

		private void OnActiveContextChangedHandler()
		{
			ContextOwner currentContextOwner = m_contextObserver.GetCurrentContextOwner();
			if (currentContextOwner.IsActive<UIChatOverlayContext>())
			{
				currentContextOwner.Get<UIChatOverlayContext>().UpdateNavigationPriority();
			}
		}
	}
}
