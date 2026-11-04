using System;
using System.Collections.Generic;

namespace Services.GameAnalytics
{
	public class HandlerMapping
	{
		private Dictionary<Type, IEventHandler> m_handlers = new Dictionary<Type, IEventHandler>();

		private Dictionary<string, IEventHandler> m_mapping = new Dictionary<string, IEventHandler>();

		public void RegisterHandler(IEventHandler handler)
		{
			m_handlers[handler.GetType()] = handler;
		}

		public void BindEventToHandler<T>(string eventName) where T : IEventHandler
		{
			m_mapping[eventName] = m_handlers.GetValueOrDefault(typeof(T));
		}

		public IEventHandler GetHandler(string eventName)
		{
			return m_mapping.GetValueOrDefault(eventName, null);
		}
	}
}
