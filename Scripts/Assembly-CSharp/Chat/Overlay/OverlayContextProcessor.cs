namespace Chat.Overlay
{
	public class OverlayContextProcessor
	{
		private GameContextObserver m_contextObserver;

		private GameContextActivityValidator m_contextActivityValidator;

		private ContextVisibilityScheduler m_visibilityScheduler;

		private ContextVisibilityHandler m_visibilityHandler;

		private ContextLayoutProcessor m_layoutProcessor;

		private ContextInitializer m_contextInitializer;

		private ContextNavigationPriorityHolder m_contextNavigationPriorityHolder;

		private OverlayEulaHandler m_overlayEulaHandler;

		private OverlayRestrictionHandler m_overlayRestrictionHandler;

		public void Setup(ChatService service)
		{
			m_contextObserver = new GameContextObserver();
			m_contextObserver.Setup();
			m_contextActivityValidator = new GameContextActivityValidator();
			m_contextActivityValidator.Setup(m_contextObserver);
			m_visibilityScheduler = new ContextVisibilityScheduler();
			m_visibilityScheduler.Setup(m_contextActivityValidator, m_contextObserver);
			m_visibilityHandler = new ContextVisibilityHandler();
			m_visibilityHandler.Setup(m_visibilityScheduler);
			m_layoutProcessor = new ContextLayoutProcessor();
			m_layoutProcessor.Setup(m_contextObserver);
			m_contextInitializer = new ContextInitializer();
			m_contextInitializer.Setup(service, m_contextObserver, m_layoutProcessor);
			m_contextNavigationPriorityHolder = new ContextNavigationPriorityHolder();
			m_contextNavigationPriorityHolder.Setup(m_contextObserver);
			m_overlayEulaHandler = new OverlayEulaHandler();
			m_overlayEulaHandler.Setup();
			m_overlayRestrictionHandler = new OverlayRestrictionHandler();
			m_overlayRestrictionHandler.Setup(service, m_visibilityScheduler);
		}

		public void Release()
		{
			m_overlayRestrictionHandler.Release();
			m_overlayRestrictionHandler = null;
			m_overlayEulaHandler.Release();
			m_overlayEulaHandler = null;
			m_contextNavigationPriorityHolder.Release();
			m_contextNavigationPriorityHolder = null;
			m_contextInitializer.Release();
			m_contextInitializer = null;
			m_layoutProcessor.Release();
			m_layoutProcessor = null;
			m_visibilityHandler.Release();
			m_visibilityHandler = null;
			m_visibilityScheduler.Release();
			m_visibilityScheduler = null;
			m_contextActivityValidator.Release();
			m_contextActivityValidator = null;
			m_contextObserver.Release();
			m_contextObserver = null;
		}
	}
}
