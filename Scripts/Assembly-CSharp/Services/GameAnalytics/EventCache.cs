using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Services.GameAnalytics
{
	public class EventCache
	{
		private struct EventState
		{
			public byte[] data;
		}

		private Queue<string> m_eventQueue;

		private Dictionary<string, Queue<EventState>> m_stateQueueByEventMap;

		public void Enqueue(string eventName, Dictionary<string, object> parameters)
		{
			if (m_eventQueue == null)
			{
				m_eventQueue = new Queue<string>();
			}
			if (!m_eventQueue.Contains(eventName))
			{
				m_eventQueue.Enqueue(eventName);
			}
			if (ConstructStateFromData(parameters, out var state))
			{
				if (m_stateQueueByEventMap == null)
				{
					m_stateQueueByEventMap = new Dictionary<string, Queue<EventState>>();
				}
				if (!m_stateQueueByEventMap.ContainsKey(eventName))
				{
					m_stateQueueByEventMap.Add(eventName, new Queue<EventState>());
				}
				m_stateQueueByEventMap[eventName].Enqueue(state);
			}
		}

		public bool Dequeue(out string eventName, out Dictionary<string, object> parameters)
		{
			eventName = null;
			parameters = null;
			if (m_eventQueue == null || !m_eventQueue.TryPeek(out eventName))
			{
				return false;
			}
			if (m_stateQueueByEventMap == null || !m_stateQueueByEventMap.TryGetValue(eventName, out var value))
			{
				m_eventQueue.Dequeue();
				if (m_eventQueue.Count == 0)
				{
					m_eventQueue = null;
				}
				return true;
			}
			EventState state = value.Dequeue();
			if (value.Count == 0)
			{
				m_stateQueueByEventMap.Remove(eventName);
				m_eventQueue.Dequeue();
			}
			if (m_eventQueue.Count == 0 && m_stateQueueByEventMap.Count == 0)
			{
				m_eventQueue = null;
				m_stateQueueByEventMap = null;
			}
			parameters = ConstructDataFromState(in state);
			return parameters != null;
		}

		private bool ConstructStateFromData(Dictionary<string, object> parameters, out EventState state)
		{
			state = default(EventState);
			state.data = Serialize(parameters);
			return state.data != null;
		}

		private Dictionary<string, object> ConstructDataFromState(in EventState state)
		{
			return Deserialize<Dictionary<string, object>>(state.data);
		}

		private byte[] Serialize<T>(T obj)
		{
			if (obj == null)
			{
				return null;
			}
			try
			{
				using MemoryStream memoryStream = new MemoryStream();
				new BinaryFormatter().Serialize(memoryStream, obj);
				return memoryStream.ToArray();
			}
			catch (IOException)
			{
				return null;
			}
		}

		private T Deserialize<T>(byte[] data)
		{
			try
			{
				using MemoryStream serializationStream = new MemoryStream(data);
				return (T)new BinaryFormatter().Deserialize(serializationStream);
			}
			catch (IOException)
			{
				return default(T);
			}
		}
	}
}
