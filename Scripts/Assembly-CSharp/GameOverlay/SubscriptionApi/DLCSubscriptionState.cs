using GameOverlay.SubscriptionApi.Config;

namespace GameOverlay.SubscriptionApi
{
	public class DLCSubscriptionState : SubscriptionState<DLCCode>
	{
		public DLCSubscriptionState()
		{
		}

		public DLCSubscriptionState(DLCCode dlcCode)
			: base(dlcCode)
		{
		}

		public override void Update(SdrRestServerPlayerCarxSubscriptionOwnership sdrConfig)
		{
			base.ownerships = null;
			if (IsConfigNull(sdrConfig))
			{
				OnUpdated(SubscriptionStatus.Disabled);
				return;
			}
			if (!SubscriptionDlcIds.dlcIdPerCode.TryGetValue(subject, out var value))
			{
				OnUpdated(SubscriptionStatus.Disabled);
				return;
			}
			for (int i = 0; i < sdrConfig.ownerships.Count; i++)
			{
				Ownerships ownerships = sdrConfig.ownerships[i];
				if (ownerships.subscriptionContent.content.ownerships.Contains(value))
				{
					base.ownerships = ownerships;
					OnUpdated(SubscriptionStatus.Active);
					return;
				}
			}
			OnUpdated(SubscriptionStatus.Disabled);
		}

		public DLCSubscriptionState GetCopy()
		{
			return new DLCSubscriptionState(subject)
			{
				status = base.status
			};
		}

		private void OnUpdated(SubscriptionStatus result)
		{
			base.status = result;
		}
	}
}
