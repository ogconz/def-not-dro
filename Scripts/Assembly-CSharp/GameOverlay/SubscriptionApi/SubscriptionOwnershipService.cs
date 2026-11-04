using System;
using DI;
using GameOverlay.SubscriptionApi.Config;
using GameOverlay.SubscriptionApi.Debug;

namespace GameOverlay.SubscriptionApi
{
	public static class SubscriptionOwnershipService
	{
		private static GamePrefs s_prefs;

		public static ConfigManager configManager { get; private set; }

		public static UnlockDispatcher<DLCCode> dlcUnlockDispatcher { get; private set; }

		public static DebugManager debug { get; private set; }

		public static bool isStarted { get; private set; }

		public static event Action onConfigExpired;

		public static void Start()
		{
			if (!isStarted)
			{
				isStarted = true;
			}
		}

		static SubscriptionOwnershipService()
		{
			Init();
		}

		private static void Init()
		{
			if (s_prefs == null)
			{
				s_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			configManager = new ConfigManager();
			configManager.Init();
			dlcUnlockDispatcher = new DLCUnlockDispatcher();
			dlcUnlockDispatcher.Init(configManager, s_prefs);
			LifetimeHandler lifetimeHandler = configManager.updateScheduler.lifetimeHandler;
			lifetimeHandler.onExpired = (Action)Delegate.Combine(lifetimeHandler.onExpired, new Action(OnConfigExpiredHandler));
			debug = new DebugManager();
		}

		public static void Dispose()
		{
			if (configManager?.updateScheduler?.lifetimeHandler != null)
			{
				LifetimeHandler lifetimeHandler = configManager.updateScheduler.lifetimeHandler;
				lifetimeHandler.onExpired = (Action)Delegate.Remove(lifetimeHandler.onExpired, new Action(OnConfigExpiredHandler));
			}
			configManager?.Dispose();
			configManager = null;
			dlcUnlockDispatcher?.Dispose();
			dlcUnlockDispatcher = null;
			isStarted = false;
		}

		public static void Update()
		{
			if (isStarted)
			{
				configManager?.Update();
			}
		}

		private static void OnConfigExpiredHandler()
		{
			SubscriptionOwnershipService.onConfigExpired?.SafeCall();
		}
	}
}
