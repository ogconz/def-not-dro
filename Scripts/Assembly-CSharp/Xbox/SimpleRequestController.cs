using System.Collections;
using UnityEngine;

namespace Xbox
{
	public class SimpleRequestController
	{
		public const float BURST_CALL_PERRIOD = 15f;

		public const float SUSTAIN_CALL_PERRIOD = 300f;

		public const float BURST_DETECTION_WINDOW = 2f;

		public const int BURST_SIZE_TO_DETECT = 3;

		protected int m_sustainedCallLimit = 30;

		protected int m_burstCallLimit = 10;

		protected int m_sustainRequestCount;

		protected int m_burstRequestCount;

		protected float m_sustainRequestTime = float.MinValue;

		protected float m_burstRequestTime = float.MinValue;

		protected float m_lastRequestTime = float.MinValue;

		protected float m_requstInterval = 1f;

		public bool hasRequst { get; private set; }

		public SimpleRequestController(int burstLimit, int sustainLimit)
		{
			m_requstInterval = Mathf.Max(2f / 3f, 15f / (float)burstLimit);
			m_burstCallLimit = burstLimit;
			m_sustainedCallLimit = sustainLimit;
		}

		public float GetRealTime()
		{
			return Time.realtimeSinceStartup;
		}

		public bool IsWaitRequst()
		{
			if (!hasRequst)
			{
				return m_lastRequestTime + m_requstInterval > GetRealTime();
			}
			return true;
		}

		public bool IsLimit()
		{
			if (m_burstRequestCount < m_burstCallLimit)
			{
				return m_sustainRequestCount >= m_sustainedCallLimit;
			}
			return true;
		}

		protected IEnumerator DoWait()
		{
			while (IsWaitRequst())
			{
				yield return null;
			}
			while (IsLimit())
			{
				yield return null;
				RefreshLimits(GetRealTime());
			}
		}

		protected bool CanWait()
		{
			if (!IsWaitRequst())
			{
				return IsLimit();
			}
			return true;
		}

		protected void IncRequest()
		{
			hasRequst = true;
		}

		protected virtual void Finish()
		{
			m_lastRequestTime = GetRealTime();
			RefreshLimits(m_lastRequestTime);
			m_sustainRequestCount++;
			m_burstRequestCount++;
			hasRequst = false;
		}

		private void RefreshLimits(float realTime)
		{
			if (realTime > m_sustainRequestTime + 300f)
			{
				m_sustainRequestTime = realTime;
				m_sustainRequestCount = 0;
			}
			if (realTime > m_burstRequestTime + 15f)
			{
				m_burstRequestTime = realTime;
				m_burstRequestCount = 0;
			}
		}
	}
}
