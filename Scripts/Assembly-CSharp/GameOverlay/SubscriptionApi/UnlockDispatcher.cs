using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameOverlay.SubscriptionApi.Config;

namespace GameOverlay.SubscriptionApi
{
	public abstract class UnlockDispatcher<T> : IDisposable
	{
		protected ConfigManager m_configManager;

		protected List<SubscriptionState<T>> m_subscriptionsStates;

		protected GamePrefs m_prefs;

		public IEnumerable<SubscriptionState<T>> subscriptions => m_subscriptionsStates;

		public event Action<IEnumerable<SubscriptionState<T>>> onStatusUpdated;

		public virtual void Init(ConfigManager configManager, GamePrefs prefs)
		{
			m_configManager = configManager;
			m_prefs = prefs;
			m_configManager.receiver.onReceiveFailed += OnReceiveFailedHandler;
			m_configManager.receiver.onReceived += OnReceiveConfigHandler;
		}

		public virtual void Dispose()
		{
			if (m_configManager?.receiver != null)
			{
				m_configManager.receiver.onReceiveFailed -= OnReceiveFailedHandler;
				m_configManager.receiver.onReceived -= OnReceiveConfigHandler;
			}
			m_configManager = null;
		}

		public abstract bool IsUnlocked(T value);

		public abstract bool TryGetSubscription(T subject, out SubscriptionState<T> subscription);

		public virtual bool TryGetSubscriptionRecord(T subject, out SubscriptionState<T> subscriptionRecord)
		{
			subscriptionRecord = null;
			return false;
		}

		protected void RequestRefreshIfNeeded()
		{
			if (SubscriptionOwnershipService.isStarted && !m_configManager.holder.isValid)
			{
				m_configManager.receiver.ReceiveAsync().Forget();
			}
		}

		protected virtual void RecordSubscriptionsStatus()
		{
		}

		private void OnReceiveFailedHandler()
		{
			foreach (SubscriptionState<T> subscriptionsState in m_subscriptionsStates)
			{
				subscriptionsState.Reset();
			}
			this.onStatusUpdated?.SafeCall(m_subscriptionsStates);
			RecordSubscriptionsStatus();
		}

		private void OnReceiveConfigHandler(SdrRestServerPlayerCarxSubscriptionOwnership config, float lastTimeReceived)
		{
			UpdateSubscriptionsStates(config);
		}

		private void UpdateSubscriptionsStates(SdrRestServerPlayerCarxSubscriptionOwnership config)
		{
			foreach (SubscriptionState<T> subscriptionsState in m_subscriptionsStates)
			{
				subscriptionsState.Update(config);
			}
			this.onStatusUpdated?.SafeCall(m_subscriptionsStates);
			RecordSubscriptionsStatus();
		}
	}
}
