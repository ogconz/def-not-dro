using System;

namespace GameOverlay.SubscriptionApi.Config
{
	public class UpdateScheduler : IDisposable
	{
		public Action onNeedUpdate;

		private Receiver m_receiver;

		public LifetimeHandler lifetimeHandler { get; private set; }

		public float lastTimeReceived { get; private set; } = float.MinValue;

		public bool isUpdated => !lifetimeHandler.IsExpired();

		public void Init(Receiver receiver)
		{
			m_receiver = receiver;
			m_receiver.onReceived += OnConfigReceivedHandler;
			lifetimeHandler = new LifetimeHandler();
			lifetimeHandler.Init(this);
			LifetimeHandler obj = lifetimeHandler;
			obj.onExpired = (Action)Delegate.Combine(obj.onExpired, new Action(OnConfigExpiredHandler));
		}

		public void Dispose()
		{
			LifetimeHandler obj = lifetimeHandler;
			obj.onExpired = (Action)Delegate.Remove(obj.onExpired, new Action(OnConfigExpiredHandler));
			lifetimeHandler.Dispose();
			lifetimeHandler = null;
			m_receiver.onReceived -= OnConfigReceivedHandler;
			m_receiver = null;
		}

		public void Update()
		{
			lifetimeHandler?.Update();
		}

		private void OnConfigExpiredHandler()
		{
			onNeedUpdate?.SafeCall();
		}

		private void OnConfigReceivedHandler(SdrRestServerPlayerCarxSubscriptionOwnership _, float lastTimeReceived)
		{
			this.lastTimeReceived = lastTimeReceived;
		}
	}
}
