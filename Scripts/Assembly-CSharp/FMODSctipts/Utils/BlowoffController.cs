using System;
using UnityEngine;

namespace FMODSctipts.Utils
{
	public class BlowoffController
	{
		private float m_lastValue;

		private float m_cumulativeAccel;

		public bool enabled { get; private set; }

		public void Update(bool isPlaying, float throttleNormalized, float boostNormalized, float dt, Action<bool> onStateChangeCallback)
		{
			bool num = enabled;
			UpdateState(isPlaying, throttleNormalized, boostNormalized, dt);
			m_lastValue = throttleNormalized;
			if (num != enabled)
			{
				onStateChangeCallback.SafeCall(enabled);
			}
		}

		public void Reset()
		{
			enabled = false;
			m_lastValue = 0f;
			m_cumulativeAccel = 0f;
		}

		private void UpdateState(bool isPlaying, float throttleNormalized, float boostNormalized, float dt)
		{
			float num = throttleNormalized - m_lastValue;
			if (enabled)
			{
				if (!isPlaying)
				{
					m_cumulativeAccel = 0f;
					enabled = false;
				}
				else if (num > Mathf.Epsilon)
				{
					m_cumulativeAccel += num;
					if (m_cumulativeAccel > 0.5f || Mathf.Approximately(throttleNormalized, 1f))
					{
						enabled = false;
						m_cumulativeAccel = 0f;
					}
				}
			}
			else if (num < 0f - Mathf.Epsilon)
			{
				m_cumulativeAccel -= num;
				if (m_cumulativeAccel > 0.25f || Mathf.Approximately(throttleNormalized, 0f))
				{
					m_cumulativeAccel = 0f;
					enabled = true;
				}
			}
		}
	}
}
