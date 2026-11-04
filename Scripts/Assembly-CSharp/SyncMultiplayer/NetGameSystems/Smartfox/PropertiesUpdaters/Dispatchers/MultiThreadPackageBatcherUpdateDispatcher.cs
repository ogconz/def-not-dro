using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers
{
	public class MultiThreadPackageBatcherUpdateDispatcher<T, K, V> : PackageUpdateDispatcher<T, V>, IDisposable where K : IValueHolder<V>, new()
	{
		protected Batcher<T, K, V> batcher = new Batcher<T, K, V>();

		protected int m_batchDelayMs;

		private ConcurrentQueue<(T, V)> m_batchQueue = new ConcurrentQueue<(T, V)>();

		private CancellationTokenSource ctsHandleTask;

		public override void Process(T subject, V changes)
		{
			m_batchQueue.Enqueue((subject, changes));
		}

		public override void Init()
		{
			StartTaskOnThreadPool();
		}

		public override void Dispose()
		{
			TryCancelTask();
			ctsHandleTask = null;
			m_batchQueue.Clear();
		}

		public virtual void SetDelay(int delayMs)
		{
			m_batchDelayMs = delayMs;
		}

		private void StartTaskOnThreadPool()
		{
			TryCancelTask();
			if (ctsHandleTask == null)
			{
				ctsHandleTask = new CancellationTokenSource();
			}
			UniTask.RunOnThreadPool(() => HandleBatchQueue(), configureAwait: true, ctsHandleTask.Token).Forget();
		}

		private void TryCancelTask()
		{
			if (ctsHandleTask != null && !ctsHandleTask.IsCancellationRequested)
			{
				ctsHandleTask.Cancel();
			}
		}

		protected async UniTask HandleBatchQueue()
		{
			while (ctsHandleTask != null && !ctsHandleTask.IsCancellationRequested)
			{
				(T, V) result;
				while (m_batchQueue.TryPeek(out result) && batcher.TryAdd(result.Item1, result.Item2))
				{
					m_batchQueue.TryDequeue(out var _);
				}
				if (!batcher.isEmpty)
				{
					foreach (KeyValuePair<T, K> item in batcher)
					{
						m_updater.Apply(item.Key, item.Value.value);
					}
					batcher.Clear();
				}
				await Task.Delay(m_batchDelayMs);
			}
		}
	}
}
