using System;

namespace Custom
{
	public class AttemptWorker<T1, T2>
	{
		private readonly int m_total;

		private int m_current;

		private Action m_logic;

		private Action<T1, T2> m_result;

		private bool m_done;

		public AttemptWorker(int totalAttempts)
		{
			m_total = totalAttempts;
		}

		public void Setup(Action logic, Action<T1, T2> result)
		{
			m_logic = logic;
			m_result = result;
		}

		public void NotifyResult(bool isError, T1 value1, T2 value2)
		{
			if (isError && m_current > 0)
			{
				m_current--;
				m_logic.SafeCall();
			}
			else if (!m_done)
			{
				m_done = true;
				m_result.Call(value1, value2);
			}
		}

		public void Start()
		{
			m_done = false;
			m_current = m_total;
			m_logic.SafeCall();
		}
	}
}
