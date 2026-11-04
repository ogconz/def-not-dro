using System.Collections.Generic;
using System.Threading;

namespace Utlis
{
	public class CancellationTokenSet<T>
	{
		private Dictionary<T, CancellationTokenSource> m_tokenSources = new Dictionary<T, CancellationTokenSource>();

		public CancellationTokenSource Create(T key, int timeout = 30)
		{
			Dispose(key);
			return m_tokenSources[key] = new CancellationTokenSource(timeout * 1000);
		}

		public void Dispose(T key)
		{
			if (m_tokenSources.TryGetValue(key, out var value))
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Remove(key);
		}

		public void DisposeAll()
		{
			foreach (CancellationTokenSource value in m_tokenSources.Values)
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Clear();
		}
	}
}
