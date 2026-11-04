using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Services.GameAnalytics.Sentry.EventHandlers;

namespace Services.GameAnalytics.Sentry
{
	public class SentryAnalyticsService : ICommonGameAnalytics
	{
		private SentrySdkInitializer m_sdkInitializer;

		private SentryConfigHandler m_config;

		private SentryRawRequestWorker m_rawRequestWorker;

		private SentryEventFormatter m_eventFormatter;

		private HandlerMapping m_handlerMapping;

		private string m_userId;

		public async UniTask InitAsync(string currentUserId)
		{
			m_userId = currentUserId;
			m_sdkInitializer = new SentrySdkInitializer();
			m_config = new SentryConfigHandler();
			await m_config.InitConfigIfNeedAsync();
			m_sdkInitializer.Initialize(m_config);
			m_rawRequestWorker = new SentryRawRequestWorker(m_config);
			m_eventFormatter = new SentryEventFormatter();
			m_handlerMapping = new HandlerMapping();
			m_handlerMapping.RegisterHandler(new PingEventHandler(m_rawRequestWorker, m_userId));
			m_handlerMapping.RegisterHandler(new LoadPrefsErrorEventHandler());
			m_handlerMapping.BindEventToHandler<PingEventHandler>("SfsPingTcp");
			m_handlerMapping.BindEventToHandler<PingEventHandler>("SfsPingUdp");
			m_handlerMapping.BindEventToHandler<LoadPrefsErrorEventHandler>("LoadPrefsError");
		}

		public void SendEvent(string eventName)
		{
			SendEvent(eventName, null);
		}

		public void SendEvent(string eventName, Dictionary<string, object> parameters)
		{
			if (m_config.isInitialized && m_config.isServiceEnabled)
			{
				IEventHandler handler = m_handlerMapping.GetHandler(eventName);
				eventName = m_eventFormatter.Format(eventName);
				handler?.Process(eventName, parameters);
			}
		}
	}
}
