using System.Collections;
using Chat.Overlay.UI;

namespace Chat.Overlay
{
	public class ContextInitializer
	{
		private ChatService m_service;

		private GameContextObserver m_contextObserver;

		private ContextLayoutProcessor m_layoutProcessor;

		private UIChatOverlayContext m_contextInstance;

		private bool m_isContextPreloadStarted;

		private bool m_isContextInitialized;

		private bool m_isContextReady;

		public void Setup(ChatService service, GameContextObserver contextObserver, ContextLayoutProcessor layoutProcessor)
		{
			m_service = service;
			m_contextObserver = contextObserver;
			m_layoutProcessor = layoutProcessor;
			m_contextObserver.onStateChanged += OnStateChangedHandler;
			m_contextObserver.onActiveContextChanged += OnContextChangedHandler;
			m_service.onInitialized += OnServiceInitializedHandler;
		}

		public void Release()
		{
			m_contextObserver.onStateChanged -= OnStateChangedHandler;
			m_contextObserver.onActiveContextChanged -= OnContextChangedHandler;
			m_service.onInitialized -= OnServiceInitializedHandler;
			OnServiceReleasedHandler();
			m_contextObserver = null;
			m_service = null;
		}

		private void OnStateChangedHandler()
		{
			m_contextObserver.GetCurrentContextOwner()?.Preload<UIChatOverlayContext>();
		}

		private void OnContextChangedHandler()
		{
			m_contextObserver.onActiveContextChanged -= OnContextChangedHandler;
			if (!m_isContextPreloadStarted && !m_isContextInitialized)
			{
				m_isContextPreloadStarted = true;
				TimeController.StartNow(PreloadContextCoroutine());
			}
		}

		private IEnumerator PreloadContextCoroutine()
		{
			ContextOwner currentContextOwner = m_contextObserver.GetCurrentContextOwner();
			m_contextInstance = currentContextOwner.Get<UIChatOverlayContext>();
			if (m_contextInstance == null)
			{
				m_contextInstance = currentContextOwner.Preload<UIChatOverlayContext>();
			}
			if (m_contextInstance == null)
			{
				m_isContextPreloadStarted = false;
				yield break;
			}
			m_contextInstance.Preload();
			yield return null;
			m_isContextPreloadStarted = false;
			m_contextInstance.BindLayout(m_layoutProcessor);
			m_isContextInitialized = true;
		}

		private void OnServiceInitializedHandler()
		{
			m_service.onInitialized -= OnServiceInitializedHandler;
			TimeController.StartNow(SetupContextCoroutine());
		}

		private IEnumerator SetupContextCoroutine()
		{
			while (!m_isContextInitialized)
			{
				yield return null;
			}
			m_contextInstance.Setup(m_service);
			m_isContextReady = true;
			m_service.onReleased += OnServiceReleasedHandler;
		}

		private void OnServiceReleasedHandler()
		{
			m_service.onReleased -= OnServiceReleasedHandler;
			if (m_isContextReady)
			{
				m_isContextReady = false;
				m_contextInstance.Release();
			}
			if (m_isContextInitialized)
			{
				m_isContextInitialized = false;
				m_contextInstance = null;
			}
		}
	}
}
