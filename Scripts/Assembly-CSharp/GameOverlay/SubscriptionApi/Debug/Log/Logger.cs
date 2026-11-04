using GameOverlay.SubscriptionApi.Debug.Notifier;

namespace GameOverlay.SubscriptionApi.Debug.Log
{
	public abstract class Logger
	{
		protected INotifier m_notifier;

		public bool isActive { get; private set; }

		protected Logger(INotifier notifier)
		{
			m_notifier = notifier;
		}

		public void SetActive(bool value)
		{
			if (isActive != value)
			{
				if (value)
				{
					SubscribeOnEvents();
				}
				else
				{
					UnsubscribeFromEvents();
				}
				isActive = value;
			}
		}

		protected abstract void SubscribeOnEvents();

		protected abstract void UnsubscribeFromEvents();
	}
}
