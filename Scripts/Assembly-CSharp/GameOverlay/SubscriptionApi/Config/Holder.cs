using System;

namespace GameOverlay.SubscriptionApi.Config
{
	public class Holder : IDisposable
	{
		private Receiver m_receiver;

		private UpdateScheduler m_updateScheduler;

		public SdrRestServerPlayerCarxSubscriptionOwnership config { get; private set; }

		public bool isValid
		{
			get
			{
				if (config != null)
				{
					return m_updateScheduler.isUpdated;
				}
				return false;
			}
		}

		public void Init(Receiver receiver, UpdateScheduler updateScheduler)
		{
			m_receiver = receiver;
			m_receiver.onReceived += OnReceivedHandler;
			m_updateScheduler = updateScheduler;
		}

		public void Dispose()
		{
			m_receiver.onReceived -= OnReceivedHandler;
			m_receiver = null;
			config = null;
			m_updateScheduler = null;
		}

		private void OnReceivedHandler(SdrRestServerPlayerCarxSubscriptionOwnership config, float _)
		{
			this.config = config;
		}
	}
}
