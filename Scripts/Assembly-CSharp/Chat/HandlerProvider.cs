using System;
using System.Collections.Generic;
using Chat.EventHandlers;

namespace Chat
{
	public class HandlerProvider
	{
		private Dictionary<Type, IEventHandler> m_handlers = new Dictionary<Type, IEventHandler>();

		public void Register(IEventHandler handler)
		{
			m_handlers[handler.GetType()] = handler;
		}

		public T Get<T>()
		{
			return (T)m_handlers[typeof(T)];
		}

		public void AddListeners(EventDispatcher dispatcher)
		{
			foreach (IEventHandler value in m_handlers.Values)
			{
				value.AddListeners(dispatcher);
			}
		}

		public void RemoveListeners(EventDispatcher dispatcher)
		{
			foreach (IEventHandler value in m_handlers.Values)
			{
				value.RemoveListeners(dispatcher);
			}
		}
	}
}
