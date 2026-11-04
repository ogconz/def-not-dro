using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Utlis;

public class SafetyAwaiterSet<T>
{
	private CancellationTokenSet<T> m_tokenSet = new CancellationTokenSet<T>();

	private readonly HashSet<T> m_requests = new HashSet<T>();

	public async UniTask<bool> Wait(T key, CancellationToken cancellationToken = default(CancellationToken), int timeout = 30)
	{
		CancellationTokenSource tokenSource = m_tokenSet.Create(key, timeout);
		bool success = false;
		while (Predicate(tokenSource.Token, cancellationToken))
		{
			await UniTask.Yield();
		}
		m_tokenSet.Dispose(key);
		return success;
		bool Predicate(CancellationToken localCancelToken, CancellationToken outerCancelToken)
		{
			if (IsReady(key))
			{
				Reset(key);
				success = true;
				return false;
			}
			if (!localCancelToken.IsCancellationRequested)
			{
				return !outerCancelToken.IsCancellationRequested;
			}
			return false;
		}
	}

	public bool IsReady(T key)
	{
		return m_requests.Contains(key);
	}

	public void SetReady(T key)
	{
		m_requests.Add(key);
	}

	public void Reset(T key)
	{
		m_tokenSet.Dispose(key);
		m_requests.Remove(key);
	}

	public void ResetAll()
	{
		m_tokenSet.DisposeAll();
		m_requests.Clear();
	}
}
