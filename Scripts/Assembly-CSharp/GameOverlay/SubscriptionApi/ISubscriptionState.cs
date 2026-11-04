using GameOverlay.SubscriptionApi.Config;

namespace GameOverlay.SubscriptionApi
{
	public interface ISubscriptionState
	{
		void Update(SdrRestServerPlayerCarxSubscriptionOwnership sdrConfig);

		void Reset();
	}
}
