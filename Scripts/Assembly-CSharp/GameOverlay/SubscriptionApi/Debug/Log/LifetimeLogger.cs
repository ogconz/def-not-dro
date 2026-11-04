using System;
using GameOverlay.SubscriptionApi.Config;
using GameOverlay.SubscriptionApi.Debug.Notifier;

namespace GameOverlay.SubscriptionApi.Debug.Log
{
	public class LifetimeLogger : Logger
	{
		private LifetimeHandler m_lifetimeHandler;

		public LifetimeLogger(INotifier notifier)
			: base(notifier)
		{
		}

		protected override void SubscribeOnEvents()
		{
			m_lifetimeHandler = SubscriptionOwnershipService.configManager.updateScheduler.lifetimeHandler;
			LifetimeHandler lifetimeHandler = m_lifetimeHandler;
			lifetimeHandler.onExpired = (Action)Delegate.Combine(lifetimeHandler.onExpired, new Action(OnExpiredHandler));
		}

		protected override void UnsubscribeFromEvents()
		{
			LifetimeHandler lifetimeHandler = m_lifetimeHandler;
			lifetimeHandler.onExpired = (Action)Delegate.Remove(lifetimeHandler.onExpired, new Action(OnExpiredHandler));
			m_lifetimeHandler = null;
		}

		private void OnExpiredHandler()
		{
			LogExpired();
		}

		private void LogExpired()
		{
			m_notifier.Notify("Config has been expired!");
		}
	}
}
