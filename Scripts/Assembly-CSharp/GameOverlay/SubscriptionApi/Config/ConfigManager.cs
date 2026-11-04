namespace GameOverlay.SubscriptionApi.Config
{
	public class ConfigManager
	{
		public UpdateScheduler updateScheduler { get; private set; }

		public Receiver receiver { get; private set; }

		public Holder holder { get; private set; }

		public void Init()
		{
			receiver = new Receiver();
			holder = new Holder();
			updateScheduler = new UpdateScheduler();
			receiver.Init(updateScheduler);
			holder.Init(receiver, updateScheduler);
			updateScheduler.Init(receiver);
		}

		public void Dispose()
		{
			receiver?.Dispose();
			receiver = null;
			holder?.Dispose();
			holder = null;
			updateScheduler?.Dispose();
			updateScheduler = null;
		}

		public void Update()
		{
			updateScheduler?.Update();
		}
	}
}
