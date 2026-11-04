using System.Collections.Generic;

namespace chats
{
	public class ConcurrentDictionary<TKey, TValue>
	{
		private readonly object m_lock = new object();

		private readonly Dictionary<TKey, TValue> m_dictionary = new Dictionary<TKey, TValue>();

		public bool TryGetValue(TKey key, out TValue value)
		{
			lock (m_lock)
			{
				return m_dictionary.TryGetValue(key, out value);
			}
		}

		public void Remove(TKey key)
		{
			lock (m_lock)
			{
				m_dictionary.Remove(key);
			}
		}

		public bool TryRemove(TKey key, out TValue value)
		{
			lock (m_lock)
			{
				bool result = m_dictionary.TryGetValue(key, out value);
				m_dictionary.Remove(key);
				return result;
			}
		}

		public void TryAdd(TKey key, TValue value)
		{
			lock (m_lock)
			{
				m_dictionary.Add(key, value);
			}
		}
	}
}
