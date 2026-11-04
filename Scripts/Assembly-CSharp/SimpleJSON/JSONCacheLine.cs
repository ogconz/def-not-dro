using System.Collections.Generic;

namespace SimpleJSON
{
	internal class JSONCacheLine<T> where T : JSONNode, new()
	{
		private List<T> m_pool;

		private int m_nextFree;

		private int m_preAllocate;

		public int poolSize => m_pool.Count;

		public int usedNow => m_nextFree;

		public int allocationSaved { get; private set; }

		public JSONCacheLine(int capacity, int preAllocate = 8)
		{
			m_pool = new List<T>(capacity);
			m_nextFree = 0;
			m_preAllocate = preAllocate;
		}

		public void Init()
		{
			allocationSaved = 0;
		}

		public T New()
		{
			if (m_nextFree < m_pool.Count)
			{
				allocationSaved++;
				return m_pool[m_nextFree++];
			}
			allocationSaved -= m_preAllocate;
			for (int i = 0; i < m_preAllocate; i++)
			{
				m_pool.Add(new T());
			}
			return New();
		}

		public void Free()
		{
			for (int i = 0; i < m_nextFree; i++)
			{
				m_pool[i].Free();
			}
			m_nextFree = 0;
		}
	}
}
