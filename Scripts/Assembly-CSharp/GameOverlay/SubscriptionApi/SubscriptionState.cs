using System;
using System.Collections.Generic;
using GameOverlay.SubscriptionApi.Config;
using SimpleJSON;

namespace GameOverlay.SubscriptionApi
{
	public abstract class SubscriptionState<T> : ISubscriptionState
	{
		public readonly T subject;

		public SubscriptionStatus status { get; protected set; }

		[JsonIgnore]
		public Ownerships ownerships { get; protected set; }

		public SubscriptionState(T subject)
		{
			this.subject = subject;
		}

		public SubscriptionState()
		{
		}

		protected bool IsConfigNull(SdrRestServerPlayerCarxSubscriptionOwnership sdrConfig)
		{
			List<Ownerships> list = sdrConfig.ownerships;
			if (list != null)
			{
				return list.Count <= 0;
			}
			return true;
		}

		public abstract void Update(SdrRestServerPlayerCarxSubscriptionOwnership sdrConfig);

		public virtual void Reset()
		{
			status = SubscriptionStatus.Unknown;
		}

		public bool TryGetRemainingDays(out int daysCount)
		{
			daysCount = 0;
			if (ownerships == null)
			{
				return false;
			}
			daysCount = (ownerships.endAt - DateTime.Today).Days;
			return true;
		}
	}
}
