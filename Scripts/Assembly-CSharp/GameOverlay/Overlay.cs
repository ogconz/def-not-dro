using System;
using System.Collections;
using System.Collections.Generic;
using DI;
using Services.GameAnalytics.Sentry;
using UnityEngine;

namespace GameOverlay
{
	public static class Overlay
	{
		public class AvatarInfo : GameEvents.EventInfo
		{
			public PlayerId userId;

			public readonly Texture2D avatar;

			public AvatarInfo(PlayerId userId, Texture2D avatar)
			{
				this.userId = userId;
				this.avatar = avatar;
			}
		}

		public class UserInfo : GameEvents.EventInfo
		{
			public PlayerId userId;

			public string name;

			public UserInfo(PlayerId userId, string name)
			{
				this.userId = userId;
				this.name = name;
			}
		}

		public const string UNKNOWN_USER_NAME = "Unknown";

		public static Action OnReady;

		public static GameEvent<AvatarInfo> OnPlayerAvatarDecoded;

		public static GameEvent<UserInfo> OnUserNameUpdate;

		public static GameEvent<UserInfo> OnUserAvatarUpdate;

		private static IGameOverlay m_instance;

		public static IFilterTextManager filterTextManager { get; private set; }

		public static IGameOverlay instance => m_instance;

		public static PlayerId playerId => instance.player.id;

		static Overlay()
		{
			OnPlayerAvatarDecoded = new GameEvent<AvatarInfo>();
			OnUserNameUpdate = new GameEvent<UserInfo>();
			OnUserAvatarUpdate = new GameEvent<UserInfo>();
			m_instance = new SteamOverlay();
			filterTextManager = m_instance.CreateFilterTextManager();
		}

		public static IStorageSystem CreateStorageSystem()
		{
			return m_instance.CreateStorageSystem();
		}

		public static UserPlatform.Id GetPlatform()
		{
			return m_instance.GetPlatform();
		}

		public static string GetPlayerHashString()
		{
			return instance.GetUnicPlayerHashString();
		}

		public static bool IsReady()
		{
			if (m_instance != null)
			{
				return m_instance.IsReadyToStart();
			}
			return false;
		}

		public static IGameOverlay Create()
		{
			if (m_instance.Init())
			{
				return m_instance;
			}
			Debug.LogError($"[Overlay] Error init Overlay: {m_instance}");
			return null;
		}

		public static IEnumerator PreparePlayer()
		{
			yield return m_instance.PrepareFirstPlayer();
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			yield return gamePrefs.InitCoroutine();
			OnReady.SafeCall();
		}

		public static void NotifyMPFeature(bool notify)
		{
		}

		public static void InitAnalyticsService()
		{
			List<ICommonGameAnalytics> commonAnalytics = new List<ICommonGameAnalytics>
			{
				new FilteredAnalyticsService(new PlayFabAnalyticsService(), new PlayFabAnalyticsEventFilter()),
				new FilteredAnalyticsService(new SentryAnalyticsService(), new SentryAnalyticsEventFilter())
			};
			AnalyticsService.InitAsync(playerId.accountId.ToString(), commonAnalytics).Forget();
		}

		public static string UpdateNameIfNeed(string currentName, string requestedName)
		{
			if (!string.IsNullOrEmpty(requestedName) && (string.IsNullOrEmpty(currentName) || !requestedName.Equals("Unknown")))
			{
				return requestedName;
			}
			return currentName;
		}

		public static void ShowNoNightModeDLCDialog(ContextOwner owner)
		{
			UIMessageBox.ShowDLCDialog(LocalizationKeys.no_dlc_night_mode.Localized(), DLCCode.NightMode);
		}
	}
}
