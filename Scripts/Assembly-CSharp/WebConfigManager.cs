using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BestHTTP;
using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using DI;
using GameOverlay;
using SimpleJSON;
using SyncMultiplayer;
using UnityEngine;

public static class WebConfigManager
{
	public enum EStatus
	{
		Ok = 0,
		HaveNoInternet = 1
	}

	public enum EStateInit
	{
		None = 0,
		Loading = 1,
		UpToDate = 2
	}

	public class WebConfigData
	{
		public string baseServerUrl;

		public string chatServer2;

		public Dictionary<string, string> rtServers = new Dictionary<string, string>();

		public string subscriptionUrl;

		public string subscriptionListId;

		public string localizationRoot;

		public string cdnRoot;
	}

	private const float INIT_TIMEOUT = 15f;

	public static readonly string[] CfgKeys;

	public const string PrefixCfgPath = "https://";

	public const string CfgPath = ".com/gcs/config/sdr/{0}/{1}/{2}";

	private static int m_completeId;

	public static Action OnInitCompleteEvent;

	public static Action<string> changeRegionAction;

	private static Dictionary<string, string> m_availableServers;

	public static EStateInit state { get; private set; }

	public static EStatus status { get; private set; }

	public static int CountServers => CfgKeys.Length;

	public static string Lanscape => "PROD";

	private static string m_baseServerUrl { get; set; }

	public static string LastConnectionURL { get; private set; }

	public static string BaseServerUrl => m_baseServerUrl + "/sdr";

	public static string CdnRootUrl { get; private set; }

	public static string ImageUrl => CdnRootUrl + "/images";

	public static string ChatServerUrl { get; private set; }

	public static string MultiplayerMasterServerUrl { get; private set; }

	public static string PatchNotesUrl { get; private set; }

	public static string SubscriptionUrl { get; private set; }

	public static string SubscriptionListId { get; private set; }

	public static string LocalizationRoot { get; private set; }

	public static int CrossPlatformMask { get; private set; }

	public static bool ServerAllowCrossPlatform => (CrossPlatformMask & UserPlatform.AllMask & ~Overlay.playerId.platformMask) != 0;

	public static bool IsInited { get; private set; }

	public static bool IsPROD { get; private set; }

	public static Dictionary<string, string> availableServers => m_availableServers;

	static WebConfigManager()
	{
		CfgKeys = new string[5] { "config.carx-online", "config-1.carx-online", "config-2.carx-online", "config-1.driftracingonline", "config-2.driftracingonline" };
		m_completeId = -1;
		m_availableServers = new Dictionary<string, string>();
		status = EStatus.HaveNoInternet;
		IsPROD = !Application.isEditor && Lanscape == "PROD";
	}

	private static ObscuredString GetAbsolutePath(string url)
	{
		IGameOverlay instance = Overlay.instance;
		string arg = $"{instance.GetRegion()}.{instance.GetPlatform()}.{GameVersion.versionAsString}";
		return string.Format(url, arg, LocalizationKeys.localization_id.Localized(), Lanscape);
	}

	private static void InitConfig(Action<EStatus> doneCallback, Action<WebConfigData> callback)
	{
		ConnectionUtils.IsInternetAccessAvailable(delegate(bool internetAvailable)
		{
			if (internetAvailable)
			{
				if (state != EStateInit.UpToDate)
				{
					state = EStateInit.Loading;
					int timeOutScale = 1;
					TimeController.CallWithTimeout(delegate(Action finished)
					{
						string text = "";
						for (int i = 0; i < CountServers; i++)
						{
							text = text + CfgKeys[i] + ", ";
							using HTTPRequest hTTPRequest = new HTTPRequest(new Uri(GetAbsolutePath("https://" + CfgKeys[i] + ".com/gcs/config/sdr/{0}/{1}/{2}")), HTTPMethods.Get, delegate(HTTPRequest req, HTTPResponse resp)
							{
								int num = (m_completeId = (int)req.Tag);
								if (resp != null && resp.IsSuccess && state == EStateInit.Loading)
								{
									finished.SafeCall();
									GameLog.Print(AlertLevel.Notify, "System", "Load web config from SV:" + Lanscape + " for " + CfgKeys[num]);
									if (JSON.TryParse<WebConfigData>(resp.DataAsText, out var result))
									{
										state = EStateInit.UpToDate;
										status = EStatus.Ok;
										callback.Call(result);
										doneCallback.Call(EStatus.Ok);
										int value = 0;
										GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
										if (gamePrefs.stats.serversStats.ContainsKey(CfgKeys[num]))
										{
											gamePrefs.stats.serversStats.TryGetValue(CfgKeys[num], out value);
											value = (gamePrefs.stats.serversStats[CfgKeys[num]] = value + 1);
										}
										else
										{
											gamePrefs.stats.serversStats.Add(CfgKeys[num], ++value);
										}
									}
								}
							});
							hTTPRequest.Tag = i;
							hTTPRequest.Timeout = new TimeSpan(0, 0, 5 * timeOutScale);
							hTTPRequest.ConnectTimeout = new TimeSpan(0, 0, 15 * timeOutScale);
							hTTPRequest.Send();
						}
						text = text.TrimEnd(',', ' ');
						GameLog.Print(AlertLevel.Notify, "System", "Prepare " + Lanscape + " for " + text);
					}, 30f * (float)timeOutScale, null, delegate
					{
						GameLog.Print(AlertLevel.Error, "System", "Timeout while config connect!");
						state = EStateInit.None;
					});
				}
			}
			else
			{
				m_completeId = 0;
				status = EStatus.HaveNoInternet;
				doneCallback.Call(EStatus.HaveNoInternet);
			}
		});
	}

	public static void UpdateWebConfigMask(int mask)
	{
		if (mask == 0)
		{
			mask = UserPlatform.ToMask(Overlay.GetPlatform());
		}
		CrossPlatformMask = mask;
	}

	public static IEnumerator TryInit()
	{
		if (status == EStatus.Ok)
		{
			yield break;
		}
		bool done = false;
		float time = Time.unscaledTime;
		float timeOut = time + 15f;
		Initialize(delegate(EStatus s)
		{
			if (s == EStatus.Ok)
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig <color=green>resolved</color> in " + (Time.unscaledTime - time));
				DependencyInjector.Resolve<MultiplayerManager>().Initialize(BaseServerUrl);
			}
			else
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig resolve <color=red>failed</color> in " + (Time.unscaledTime - time));
			}
			done = true;
			GameSync.AuthorizeReset();
		});
		while (!done)
		{
			if (Time.unscaledTime > timeOut)
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig resolve <color=red>failed</color> by timeout");
				break;
			}
			yield return null;
		}
	}

	public static async UniTask TryInitAsync(CancellationToken token)
	{
		if (status == EStatus.Ok)
		{
			return;
		}
		bool done = false;
		float time = Time.unscaledTime;
		float timeOut = time + 15f;
		Initialize(delegate(EStatus s)
		{
			if (s == EStatus.Ok)
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig <color=green>resolved</color> in " + (Time.unscaledTime - time));
				DependencyInjector.Resolve<MultiplayerManager>().Initialize(BaseServerUrl);
			}
			else
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig resolve <color=red>failed</color> in " + (Time.unscaledTime - time));
			}
			done = true;
			GameSync.AuthorizeReset();
		});
		while (!done)
		{
			if (Time.unscaledTime > timeOut)
			{
				GameLog.Print(AlertLevel.Notify, "System", "WebConfig resolve <color=red>failed</color> by timeout");
				break;
			}
			await UniTask.Yield(token);
		}
	}

	private static void Initialize(Action<EStatus> doneCallback)
	{
		InitConfig(doneCallback, delegate(WebConfigData config)
		{
			GameLog.Print(AlertLevel.Notify, "System", "Web config loaded: Base: " + config.baseServerUrl);
			ChatServerUrl = config.chatServer2;
			m_availableServers = config.rtServers;
			string text = Overlay.instance.GetRegion();
			if (!SetMultiplayerMasterServerUrl(text))
			{
				using Dictionary<string, string>.Enumerator enumerator = config.rtServers.GetEnumerator();
				if (enumerator.MoveNext())
				{
					text = enumerator.Current.Key;
					SetMultiplayerMasterServerUrl(text);
				}
			}
			GameLog.Print(AlertLevel.Notify, "System", "Gameserver selected (" + text + "): " + MultiplayerMasterServerUrl);
			LocalizationRoot = config.localizationRoot;
			m_baseServerUrl = config.baseServerUrl;
			while (m_baseServerUrl.EndsWith("/sdr"))
			{
				m_baseServerUrl = m_baseServerUrl.Remove(m_baseServerUrl.Length - 4);
			}
			SubscriptionUrl = config.subscriptionUrl;
			SubscriptionListId = config.subscriptionListId;
			CdnRootUrl = config.cdnRoot;
			IsInited = true;
			OnInitCompleteEvent.Call();
		});
	}

	public static bool SetMultiplayerMasterServerUrl(string region, bool reconnectLobby = false, bool showGodvoice = false)
	{
		if (availableServers.TryGetValue(region, out var value))
		{
			MultiplayerMasterServerUrl = value;
			DependencyInjector.Resolve<GamePrefs>().globals.currentRegion = region;
			if (reconnectLobby)
			{
				NetworkController.InstanceGame?.Connect();
			}
			string localizedServer = GetLocalizedServer(region);
			if (showGodvoice)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.server_chosen.LocalizedFormat(localizedServer), 2f);
			}
			changeRegionAction.SafeCall(region);
			return true;
		}
		return false;
	}

	public static string GetCurrentServer()
	{
		string text = string.Empty;
		foreach (KeyValuePair<string, string> availableServer in m_availableServers)
		{
			if (availableServer.Value == MultiplayerMasterServerUrl)
			{
				text = availableServer.Key;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Overlay.instance.GetRegion();
	}

	public static string GetLocalizedServer(string region)
	{
		string text = region.ToLower();
		if (text.Contains(" "))
		{
			text = text.Replace(" ", "_");
		}
		if (!LocalizationService.IsKeyExist(text))
		{
			return region;
		}
		string text2 = new LocalizationKeys.Key(text).Localized();
		if (!string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		return region;
	}

	public static void CheckWebConfig(Action onLoad)
	{
		if (IsInited)
		{
			onLoad();
		}
		else
		{
			OnInitCompleteEvent = (Action)Delegate.Combine(OnInitCompleteEvent, onLoad);
		}
	}
}
