using System;
using System.Collections;
using System.Collections.Generic;

namespace Xbox
{
	public abstract class RequestController<TRequest, TResult> : SimpleRequestController
	{
		private List<TRequest> m_requests = new List<TRequest>();

		private List<TResult> m_results = new List<TResult>();

		private Dictionary<TRequest, TResult> m_cache = new Dictionary<TRequest, TResult>();

		private string m_error = string.Empty;

		public RequestController(int burstLimit, int sustainLimit)
			: base(burstLimit, sustainLimit)
		{
		}

		protected IEnumerator Start()
		{
			m_error = string.Empty;
			m_results.Clear();
			yield return null;
			IncRequest();
		}

		protected override void Finish()
		{
			base.Finish();
			m_requests.Clear();
		}

		public IEnumerator StartRequest(TRequest request, Action<TResult> onSuccess, Action<string> onFail)
		{
			if (m_cache.TryGetValue(request, out var value))
			{
				onSuccess?.Invoke(value);
				yield break;
			}
			if (CanWait())
			{
				yield return DoWait();
			}
			int index = Add(request);
			if (index == 0)
			{
				yield return Start();
				yield return CreateRequest(m_requests, delegate(IReadOnlyList<TResult> results)
				{
					m_results.AddRange(results);
				}, delegate(string error)
				{
					m_error = error;
				});
				if (m_requests.Count == m_results.Count)
				{
					for (int num = 0; num < m_results.Count; num++)
					{
						m_cache[m_requests[num]] = m_results[num];
					}
				}
				Finish();
			}
			yield return Process(index, onSuccess, onFail);
		}

		protected abstract IEnumerator CreateRequest(IReadOnlyList<TRequest> requests, Action<IReadOnlyList<TResult>> onComplete, Action<string> onFail);

		protected int Add(TRequest request)
		{
			m_requests.Add(request);
			return m_requests.Count - 1;
		}

		private IEnumerator Process(int index, Action<TResult> onSuccess, Action<string> onFail)
		{
			while (m_results.Count == 0 && string.IsNullOrEmpty(m_error))
			{
				yield return null;
			}
			if (!string.IsNullOrEmpty(m_error))
			{
				onFail?.Invoke(m_error);
			}
			else if (index >= 0 && index < m_results.Count)
			{
				onSuccess?.Invoke(m_results[index]);
			}
			else
			{
				onFail?.Invoke("not valid index");
			}
		}
	}
}
