using Chat.Overlay.UI;

namespace Chat.Overlay
{
	public class ContextVisibilityHandler
	{
		private ContextVisibilityScheduler m_visibilityScheduler;

		public void Setup(ContextVisibilityScheduler visibilityScheduler)
		{
			m_visibilityScheduler = visibilityScheduler;
			m_visibilityScheduler.onOverlayShow += OnOverlayShowHandler;
			m_visibilityScheduler.onOverlayHide += OnOverlayHideHandler;
		}

		public void Release()
		{
			m_visibilityScheduler.onOverlayShow -= OnOverlayShowHandler;
			m_visibilityScheduler.onOverlayHide -= OnOverlayHideHandler;
			m_visibilityScheduler = null;
		}

		private void OnOverlayShowHandler(ContextOwner contextOwner)
		{
			if (!contextOwner.IsActive<UIChatOverlayContext>())
			{
				contextOwner.Add<UIChatOverlayContext>();
			}
		}

		private void OnOverlayHideHandler(ContextOwner contextOwner)
		{
			if (contextOwner.IsActive<UIChatOverlayContext>())
			{
				contextOwner.Hide<UIChatOverlayContext>();
			}
		}
	}
}
