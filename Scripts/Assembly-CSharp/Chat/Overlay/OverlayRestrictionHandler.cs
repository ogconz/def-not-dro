using GameOverlay;

namespace Chat.Overlay
{
	public class OverlayRestrictionHandler
	{
		private ChatService m_service;

		private ContextVisibilityScheduler m_visibilityScheduler;

		public void Setup(ChatService service, ContextVisibilityScheduler visibilityScheduler)
		{
			m_service = service;
			m_visibilityScheduler = visibilityScheduler;
			m_service.onInitialized += OnInitializeServiceHandler;
		}

		public void Release()
		{
			m_service.onInitialized -= OnInitializeServiceHandler;
			m_service = null;
			m_visibilityScheduler = null;
		}

		private void OnInitializeServiceHandler()
		{
			m_service.onInitialized -= OnInitializeServiceHandler;
			TimeController.StartNow(GameOverlay.Overlay.instance.CheckCommunicationRestrictions(m_visibilityScheduler.UpdateOverlayVisibility));
		}
	}
}
