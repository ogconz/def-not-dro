using System;
using UnityEngine;

namespace GameOverlay.SubscriptionApi.Config
{
	public class LifetimeHandler : IDisposable
	{
		public const float DefaultLifetimeSec = 120f;

		public const float CooldownDelayOnExpiredSec = 2f;

		public Action onExpired;

		private UpdateScheduler m_updateScheduler;

		private float m_lastTimeCooldownActive = 4.5f;

		private float m_lifeTime;

		public void Init(UpdateScheduler updateScheduler)
		{
			m_updateScheduler = updateScheduler;
			SetLifetime(120f);
		}

		public void SetLifetime(float value)
		{
			m_lifeTime = Math.Clamp(value, 2f, float.MaxValue);
		}

		public void Dispose()
		{
			m_updateScheduler = null;
		}

		public void Update()
		{
			if (IsExpired() && !IsCooldownActive())
			{
				Expire();
			}
		}

		public void Expire()
		{
			onExpired?.SafeCall();
			m_lastTimeCooldownActive = Time.time;
		}

		public bool IsExpired()
		{
			return Time.time > m_updateScheduler.lastTimeReceived + m_lifeTime;
		}

		private bool IsCooldownActive()
		{
			return Time.time <= m_lastTimeCooldownActive + 2f;
		}
	}
}
