using System.Collections.Generic;
using GameOverlay.SubscriptionApi.Config;

namespace GameOverlay.SubscriptionApi
{
	public class DLCUnlockDispatcher : UnlockDispatcher<DLCCode>
	{
		private readonly HashSet<DLCCode> m_onceUnlocked = new HashSet<DLCCode>();

		public override void Init(ConfigManager configManager, GamePrefs prefs)
		{
			base.Init(configManager, prefs);
			m_subscriptionsStates = new List<SubscriptionState<DLCCode>>
			{
				new DLCSubscriptionState(DLCCode.SeasonPass)
			};
		}

		public override bool IsUnlocked(DLCCode dlcCode)
		{
			if (IsDlcUnlockedInSession(dlcCode))
			{
				return true;
			}
			RequestRefreshIfNeeded();
			foreach (SubscriptionState<DLCCode> subscriptionsState in m_subscriptionsStates)
			{
				if (subscriptionsState.subject == dlcCode)
				{
					if (subscriptionsState.status == SubscriptionStatus.Active)
					{
						m_onceUnlocked.Add(dlcCode);
						return true;
					}
					return false;
				}
			}
			return false;
		}

		public override bool TryGetSubscriptionRecord(DLCCode subject, out SubscriptionState<DLCCode> subscriptionRecord)
		{
			return m_prefs.dlcSubscriptionsRecord.TryGet((SubscriptionState<DLCCode> subscription) => subscription.subject == subject, out subscriptionRecord);
		}

		public override bool TryGetSubscription(DLCCode subject, out SubscriptionState<DLCCode> subscription)
		{
			subscription = m_subscriptionsStates.Find((SubscriptionState<DLCCode> subscriptionState) => subscriptionState.subject == subject);
			return subscription != null;
		}

		protected override void RecordSubscriptionsStatus()
		{
			base.RecordSubscriptionsStatus();
			foreach (DLCSubscriptionState subscriptionsState in m_subscriptionsStates)
			{
				SubscriptionStatus status = subscriptionsState.status;
				if ((uint)(status - 2) <= 1u)
				{
					m_prefs.dlcSubscriptionsRecord.Record(subscriptionsState);
				}
			}
		}

		private bool IsDlcUnlockedInSession(DLCCode dlcCode)
		{
			return m_onceUnlocked.Contains(dlcCode);
		}
	}
}
