using System;
using Chat.Overlay;
using GameOverlay;

namespace Chat
{
	public class ChatOverlay
	{
		private static ChatOverlay s_instance;

		private bool m_initialized;

		private ChatService m_service;

		private OverlayContextProcessor m_contextProcessor;

		public static bool IsInitialized()
		{
			if (s_instance != null)
			{
				return s_instance.m_initialized;
			}
			return false;
		}

		public static void Initialize()
		{
			if (s_instance == null)
			{
				s_instance = new ChatOverlay();
				if (s_instance.CanInitialize())
				{
					GameOverlay.Overlay.OnReady = (Action)Delegate.Combine(GameOverlay.Overlay.OnReady, new Action(s_instance.InternalInitialize));
				}
			}
		}

		public static void Release()
		{
			if (s_instance != null)
			{
				s_instance.InternalRelease();
				s_instance = null;
			}
		}

		private void InternalInitialize()
		{
			if (!m_initialized)
			{
				m_service = new ChatService();
				m_service.Initialize();
				m_contextProcessor = new OverlayContextProcessor();
				m_contextProcessor.Setup(m_service);
				m_initialized = true;
			}
		}

		private void InternalRelease()
		{
			if (m_initialized)
			{
				m_initialized = false;
				m_contextProcessor.Release();
				m_contextProcessor = null;
				m_service.Release();
				m_service = null;
			}
		}

		private bool CanInitialize()
		{
			return true;
		}
	}
}
