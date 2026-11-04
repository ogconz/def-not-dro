using System;
using System.Collections.Generic;
using GameOverlay.SubscriptionApi;
using SimpleJSON;

namespace PrefsModels
{
	public class PrefsDlcSubscriptionsRecords : BasePrefsModel
	{
		[JsonSerialize]
		private List<DLCSubscriptionState> m_recordedSubscriptionsStates = new List<DLCSubscriptionState>();

		public void Record(DLCSubscriptionState subscriptionState)
		{
			int num = m_recordedSubscriptionsStates.FindIndex((DLCSubscriptionState state) => state.subject == subscriptionState.subject);
			if (num >= 0)
			{
				m_recordedSubscriptionsStates[num] = subscriptionState.GetCopy();
			}
			else
			{
				m_recordedSubscriptionsStates.Add(subscriptionState.GetCopy());
			}
		}

		public bool TryGet<T>(Predicate<T> match, out T subscriptionState)
		{
			subscriptionState = default(T);
			foreach (DLCSubscriptionState recordedSubscriptionsState in m_recordedSubscriptionsStates)
			{
				if (recordedSubscriptionsState is T val && match(val))
				{
					subscriptionState = val;
					return true;
				}
			}
			return false;
		}
	}
}
