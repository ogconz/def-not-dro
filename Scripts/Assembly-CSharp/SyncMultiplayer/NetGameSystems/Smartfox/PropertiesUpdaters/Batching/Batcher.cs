using System.Collections;
using System.Collections.Generic;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching
{
	public class Batcher<T, K, V> : IEnumerable<KeyValuePair<T, K>>, IEnumerable where K : IValueHolder<V>, new()
	{
		private Dictionary<T, K> m_batched = new Dictionary<T, K>();

		public bool isEmpty => m_batched.Count == 0;

		public bool TryAdd(T subject, V changes)
		{
			if (m_batched.TryGetValue(subject, out var value))
			{
				return value.Add(changes);
			}
			K value2 = new K();
			if (!value2.Add(changes))
			{
				return false;
			}
			m_batched[subject] = value2;
			return true;
		}

		public void Clear()
		{
			m_batched.Clear();
		}

		public IEnumerator<KeyValuePair<T, K>> GetEnumerator()
		{
			return m_batched.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
