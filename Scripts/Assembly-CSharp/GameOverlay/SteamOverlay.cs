using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CarX.Platform;
using DI;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.UI;
using Epic.OnlineServices.UserInfo;
using EpicUnity;
using GameInput;
using GameOverlay.Regions;
using GameOverlay.Steam;
using GameOverlay.SubscriptionApi;
using Rewired;
using Steamworks;
using Steamworks.Data;
using SyncMultiplayer;
using UnityEngine;

namespace GameOverlay
{
	public class SteamOverlay : IGameOverlay
	{
		public const uint APP_ID = 635260u;

		private const uint DLC_STEAM_ID_GOLD = 752450u;

		private const uint DLC_STEAM_ID_TRIO = 752440u;

		private const uint DLC_STEAM_ID_TIME_ATTACK = 858690u;

		private const uint DLC_STEAM_ID_POWER = 769710u;

		private const uint DLC_STEAM_ID_FREE_CONTENT = 776950u;

		private const uint DLC_STEAM_ID_POLICE = 842050u;

		private const uint DLC_STEAM_ID_ORIGINS = 906130u;

		private const uint DLC_STEAM_ID_NEW_STYLE = 937630u;

		private const uint DLC_STEAM_ID_NEW_STYLE2 = 967320u;

		private const uint DLC_STEAM_ID_DRIFT_WAR = 998560u;

		private const uint DLC_STEAM_ID_HALLOWEEN = 965040u;

		private const uint DLC_STEAM_ID_CANYON_TRIP = 1112420u;

		private const uint DLC_STEAM_ID_RUSSIAN_DRIFT_STYLE = 1112460u;

		private const uint DLC_STEAM_ID_JAPAN_FEST = 1112470u;

		private const uint DLC_STEAM_ID_AMERICAN_WAYS = 1159010u;

		private const uint DLC_STEAM_ID_HALLOWEEN_VINYLS = 1178950u;

		private const uint DLC_STEAM_ID_NEW_YEAR_STICKERS2020 = 1214350u;

		private const uint DLC_STEAM_ID_STREET_CLASS_STICKERS_VINYLS = 1308680u;

		private const uint DLC_STEAM_ID_PRO_DRIFT_STICKERS_VINYLS = 1308670u;

		private const uint DLC_STEAM_ID_NIGHT_MODE = 1449350u;

		private const uint DLC_STEAM_ID_SEASON_PASS = 1535220u;

		private const uint DLC_STEAM_ID_STREET_TUNERS = 1786050u;

		private const uint DLC_STEAM_ID_HIT_THE_WALL = 1901670u;

		private const uint DLC_STEAM_ID_SAYAKA = 2096530u;

		private const uint DLC_STEAM_ID_SWAPS = 2154970u;

		private const uint DLC_STEAM_ID_YOUNG_TIMERS = 2395750u;

		private const uint DLC_STEAM_ID_SAYAKA_180SX = 2638160u;

		private const uint DLC_STEAM_ID_SAYAKA_RX7 = 3292330u;

		private const uint DLC_STEAM_ID_WILD_UNION = 3559630u;

		private const uint DLC_STEAM_ID_MEOWTOR_MADNESS = 3559650u;

		private const uint DLC_STEAM_ID_CAR_PRESETS_UNLOCK = 3559570u;

		private const uint DLC_STEAM_ID_FRESHX = 5253680u;

		private const string SOUTH_AMERICA_DLC_NAME = "CarX Drift Racing Online - South American Style";

		private const string AUTO_JOIN_KEY = "-autoroom=";

		private readonly SteamPlayer m_player = new SteamPlayer();

		private SteamId m_cachedSteamId;

		private SteamRegionDataManager m_regionDataManager = new SteamRegionDataManager();

		private AuthTicket m_authTicket;

		private string m_authTicketStr;

		private readonly Dictionary<PlayerId, string> m_nickNames = new Dictionary<PlayerId, string>();

		private Dictionary<ulong, string> m_dlcNames;

		private readonly HashSet<ulong> m_knownDLCs = new HashSet<ulong>();

		public Action<string, bool> onCloseOverlayKeyboardSubmit;

		public Action<string> onCloseOverlayKeyboard;

		private LoginCallbackInfo? m_loginCallbackInfo;

		private SteamRichPresence m_richPresence = new SteamRichPresence();

		private IVirtualKeyboard m_virtualKeyboard = new SteamVirtualKeyboard();

		private EOSInviteService m_eosInviteService;

		private EOSFriendManager m_eosFriendsManager;

		private GamePrefs m_prefs;

		private Task<LoginCallbackInfo> m_eosLoginTicketTask;

		private Task<LogoutCallbackInfo> m_eosLogoutTicketTask;

		private Task<Epic.OnlineServices.Result> m_eosConnectResultTask;

		public IPlayer player => m_player;

		public SteamId SteamId => m_cachedSteamId;

		public List<IPlayer> friends { get; } = new List<IPlayer>();

		public IInviteService inviteService => new VoidInviteService();

		public IRegionDetection<RegionData> regionDetection => m_regionDataManager;

		public bool isShut { get; private set; }

		public bool IsValidAge => true;

		public bool modLoaderFound { get; private set; }

		public Action<PlayerId, PlayerState> friendPresenceChangedAction { get; set; }

		public AccountId localUniqId => m_player.steamId.Value;

		public bool NeedUpdatePatch => true;

		public bool IsFriendShowUGCAllowed => false;

		public bool DoneProcessingInvitation => true;

		public bool IsUserGeneratedContentRestricted => false;

		public bool IsUserShareContentRestricted => false;

		public bool IsCrossPlayRestricted => false;

		public bool IsCommunicationRestricted => false;

		public bool IsCrossTextCommunicationRestricted => false;

		public IRichPresence richPresence => m_richPresence;

		public IVirtualKeyboard virtualKeyboard => m_virtualKeyboard;

		public bool Init()
		{
			try
			{
				if (SteamClient.RestartAppIfNecessary(635260u))
				{
					Application.Quit();
					return false;
				}
				SteamClient.Init(635260u, asyncCallbacks: false);
				m_cachedSteamId = SteamClient.SteamId;
				ResolveAuthTicket();
				player.id = new PlayerId(UserPlatform.Id.Steam, m_cachedSteamId.Value);
				SteamFriends.OnPersonaStateChange += OnPersonaStateChange;
				SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;
				SteamFriends.OnGameOverlayActivated += OnGameOverlayActivated;
				GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
				{
					string text = ParseAutoJoinRoom(Environment.CommandLine);
					if (!string.IsNullOrEmpty(text))
					{
						NetworkController.SaveInvitation(text);
					}
				});
				if (GameManager.IsLinuxOsPlatform())
				{
					SteamUtils.OnGamepadTextInputDismissed += OnCloseOverlayKeyboardInput;
				}
				CheckLegacyScreenshots();
				UpdateDLCInfo();
				if (UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
				{
					InitializeAvatarsApproved();
				}
				else
				{
					UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(TryInitializeAvatars));
					UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(TryInitializeAvatars));
				}
				InitEpicPlatform(initSteamIntegrationPlatform: true);
			}
			catch (Exception ex)
			{
				Debug.LogError("Steam initialization failed: " + ex.Message);
				return false;
			}
			DetectMods();
			return true;
		}

		public void Shutdown()
		{
			EOSManager.instance.Shutdown();
			m_regionDataManager.Dispose();
			SteamClient.Shutdown();
			SubscriptionOwnershipService.Dispose();
			richPresence.ClearAll();
			isShut = true;
		}

		public void ShowOverlayKeyboard(string title, string text, int length = 64, bool isPassword = false, GamepadTextInputLineMode lineMode = GamepadTextInputLineMode.SingleLine)
		{
			SteamUtils.ShowGamepadTextInput(isPassword ? GamepadTextInputMode.Password : GamepadTextInputMode.Normal, lineMode, title, length, text);
		}

		private void OnCloseOverlayKeyboardInput(bool isSubmit)
		{
			string enteredGamepadText = SteamUtils.GetEnteredGamepadText();
			onCloseOverlayKeyboardSubmit.SafeCall(enteredGamepadText, isSubmit);
			onCloseOverlayKeyboard.SafeCall(enteredGamepadText);
		}

		public void Update()
		{
			if ((ulong)SteamId == 0L)
			{
				return;
			}
			if (!m_loginCallbackInfo.HasValue || m_loginCallbackInfo.Value.ResultCode == Epic.OnlineServices.Result.Success)
			{
				EOSManager.instance.Tick();
			}
			SteamClient.RunCallbacks();
			SubscriptionOwnershipService.Update();
			if (m_prefs == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			else
			{
				if (!m_prefs.globals.autoConnectToEpicServices || IsEpicPlatformConnected())
				{
					return;
				}
				AuthEpicViaSteamIntegratedPlatform(delegate(LoginCallbackInfo? info, Epic.OnlineServices.Result result)
				{
					if (result != Epic.OnlineServices.Result.Success)
					{
						m_prefs.globals.autoConnectToEpicServices = false;
					}
				});
			}
		}

		public bool IsReadyToStart()
		{
			return (ulong)m_player.steamId != 0;
		}

		public bool IsReadyToSync()
		{
			return true;
		}

		public bool IsSystemOverlayShowed()
		{
			if (!Application.isEditor)
			{
				return SteamUtils.IsOverlayEnabled;
			}
			return false;
		}

		public UserPlatform.Id GetPlatform()
		{
			return UserPlatform.Id.Steam;
		}

		public string GetUnicPlayerHashString()
		{
			return $"{player.id.accountId.ToUInt64()}|{(int)player.id.platform}".GetHashCode().ToString("X");
		}

		public string GetInitiateAttachErrorText()
		{
			return "Fatal error: can't init Steam. Try Restart...";
		}

		public IEnumerator PrepareFirstPlayer()
		{
			while ((ulong)m_player.steamId == 0L)
			{
				yield return null;
			}
		}

		public void UpdateDLCInfo()
		{
			if (m_dlcNames != null)
			{
				m_dlcNames.Clear();
			}
			else
			{
				m_dlcNames = new Dictionary<ulong, string>();
			}
			string text = "";
			string text2 = "";
			foreach (DlcInformation item in SteamApps.DlcInformation())
			{
				if ((!item.Available && !IsSeasonPassInstalled()) || (uint)item.AppId == 0)
				{
					continue;
				}
				m_dlcNames[(uint)item.AppId] = item.Name;
				if (IsDlcInstalledInternal(item.AppId))
				{
					if (!m_knownDLCs.Contains((uint)item.AppId))
					{
						m_knownDLCs.Add((uint)item.AppId);
						text = text + item.Name + ", ";
					}
				}
				else if (m_knownDLCs.Contains((uint)item.AppId))
				{
					m_knownDLCs.Remove((uint)item.AppId);
					text2 = text2 + item.Name + ", ";
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				text = text.Replace("CarX Drift Racing Online - ", "").TrimEnd(',', ' ');
				GameLog.Print(AlertLevel.Notify, "System", "DLC installed: " + text);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				text2 = text2.Replace("CarX Drift Racing Online - ", "").TrimEnd(',', ' ');
				GameLog.Print(AlertLevel.Notify, "System", "DLC uninstalled: " + text2);
			}
		}

		private bool IsDLCAvailable(DLCCode code)
		{
			uint num = DlcCodeToAppId(code);
			foreach (DlcInformation item in SteamApps.DlcInformation())
			{
				if ((uint)item.AppId == num)
				{
					return item.Available;
				}
			}
			return false;
		}

		public bool IsDLCInstalled(DLCCode code)
		{
			if (code != DLCCode.FreeContent && code != DLCCode.Unknown && IsSeasonPassInstalled() && IsDLCAvailable(code))
			{
				return true;
			}
			if (code != DLCCode.RuntimeDLC && code != DLCCode.SouthAmericaStyle && !IsDlcInstalledInternal(DlcCodeToAppId(code)))
			{
				return SubscriptionOwnershipService.dlcUnlockDispatcher.IsUnlocked(code);
			}
			return true;
		}

		private static bool IsSeasonPassInstalled()
		{
			if (!IsDlcInstalledInternal(1535220u))
			{
				return SubscriptionOwnershipService.dlcUnlockDispatcher.IsUnlocked(DLCCode.SeasonPass);
			}
			return true;
		}

		private static bool IsDlcInstalledInternal(uint appid)
		{
			try
			{
				return SteamApps.IsDlcInstalled(appid);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			return false;
		}

		public bool IsDlcSeasonPassInstalled()
		{
			return IsDlcInstalledInternal(1535220u);
		}

		public void ShowBuyDLCPage(DLCCode code)
		{
			uint num = DlcCodeToAppId(code);
			if (num != 0 && num != 776950)
			{
				SteamFriends.OpenStoreOverlay(num);
			}
		}

		public string GetDLCName(DLCCode code)
		{
			if (code == DLCCode.SouthAmericaStyle)
			{
				return "CarX Drift Racing Online - South American Style";
			}
			try
			{
				uint num = DlcCodeToAppId(code);
				if (num == 0)
				{
					return string.Empty;
				}
				string text = TryGetDlcName(num);
				return string.IsNullOrEmpty(text) ? string.Empty : ("DLC \"" + text + "\"");
			}
			catch
			{
				return string.Empty;
			}
		}

		private string TryGetDlcName(uint appID)
		{
			if (m_dlcNames == null)
			{
				GameLog.Print(AlertLevel.Error, "System", "DLC info not loaded.");
				return string.Empty;
			}
			if (!m_dlcNames.TryGetValue(appID, out var value))
			{
				return string.Empty;
			}
			return value;
		}

		public string GetDLCShortName(DLCCode code)
		{
			return GetDLCName(code).Replace("CarX Drift Racing Online - ", "");
		}

		public async void RequestUserName(PlayerId id)
		{
			if (m_nickNames.ContainsKey(id))
			{
				return;
			}
			UserPlatform.Id platform = id.platform;
			if (platform != UserPlatform.Id.MyGames && platform == UserPlatform.Id.EpicGames && IsSuccessLoginToEpic())
			{
				if (id.accountId.format != UidFormat.Hex128BString)
				{
					return;
				}
				if (m_nickNames.TryGetValue(id, out var value))
				{
					SetUserName(id, value);
					return;
				}
				string text = id.accountId.ToHexString();
				Task<UserInfoData?> task = EOSManager.instance.GetPlayerInfo(EpicAccountId.FromString(text));
				await task;
				if (task.Result.HasValue)
				{
					SetUserName(id, task.Result.Value.DisplayName);
				}
			}
			if (!IsSupported(id))
			{
				return;
			}
			foreach (Friend friend2 in SteamFriends.GetFriends())
			{
				if (!(friend2.Id.Value != id.accountId))
				{
					SetUserName(id, friend2.Name);
					return;
				}
			}
			if (!SteamFriends.RequestUserInformation((ulong)id.accountId))
			{
				Friend friend = new Friend((ulong)id.accountId);
				SetUserName(id, friend.Name);
			}
		}

		public void SetUserName(PlayerId id, string name)
		{
			if (!m_nickNames.TryGetValue(id, out var value) || !(value == name))
			{
				m_nickNames[id] = name;
				Overlay.OnUserNameUpdate.Invoke(new Overlay.UserInfo(id, name));
			}
		}

		public string GetUserName(PlayerId id)
		{
			return m_nickNames.GetValueOrDefault(id, "");
		}

		public bool IsSupported(PlayerId id)
		{
			return id.platform == UserPlatform.Id.Steam;
		}

		public void OnPhotoMode(bool isEnter)
		{
		}

		public IEnumerator ShareScreenshot(Func<Texture2D> screenCapture)
		{
			Texture2D texture = screenCapture();
			yield return null;
			if (texture == null)
			{
				yield break;
			}
			try
			{
				texture = FlipTexture(texture);
				UnityEngine.Color[] pixels = texture.GetPixels();
				byte[] array = new byte[pixels.Length * 3];
				int num = 0;
				int num2 = 0;
				while (num < array.Length)
				{
					array[num] = (byte)(pixels[num2].r * 255f);
					array[num + 1] = (byte)(pixels[num2].g * 255f);
					array[num + 2] = (byte)(pixels[num2].b * 255f);
					num += 3;
					num2++;
				}
				SteamScreenshots.WriteScreenshot(array, texture.width, texture.height);
				Debug.Log("Screenshot saved");
			}
			finally
			{
				UnityEngine.Object.Destroy(texture);
			}
		}

		public void SetPlayedWith(PlayerId id)
		{
			if (IsSupported(id))
			{
				SteamFriends.SetPlayedWith((ulong)id.accountId);
			}
		}

		public void ShowLastPlayed()
		{
			SteamFriends.OpenOverlay("players");
		}

		public bool HasProfileSupport()
		{
			return true;
		}

		public void ShowProfile(PlayerId id)
		{
			if (IsSupported(id))
			{
				SteamFriends.OpenUserOverlay((ulong)id.accountId, "steamid");
			}
		}

		public void InviteFriend(PlayerId id)
		{
			if (id.platform == UserPlatform.Id.EpicGames && IsSuccessLoginToEpic())
			{
				m_eosInviteService.SendInvite(EpicAccountId.FromString(id.accountId.ToHexString()));
			}
			if (!IsSupported(id))
			{
				return;
			}
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame.CurrentRoom == null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_failed_invite.Localized(), 3f);
				return;
			}
			Friend friend = new Friend(0uL);
			foreach (Friend friend2 in SteamFriends.GetFriends())
			{
				if (!(friend2.Id.Value != id.accountId))
				{
					friend = friend2;
					break;
				}
			}
			if ((ulong)friend.Id != 0L)
			{
				friend.InviteToGame("-autoroom=" + instanceGame.CurrentRoom.Name + "|" + WebConfigManager.GetCurrentServer());
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_invite_sent.Localized(), 3f);
			}
		}

		public bool IsFriend(PlayerId id)
		{
			return friends.Find((IPlayer p) => p.id == id) != null;
		}

		public bool IsBlockedUser(PlayerId id)
		{
			return false;
		}

		public IEnumerator UpdateAuthSessionTicket()
		{
			bool complete = false;
			ResolveAuthTicket(delegate
			{
				complete = true;
			});
			yield return new WaitUntil(() => complete);
		}

		public string GetAuthSessionTicket()
		{
			return m_authTicketStr;
		}

		public bool IsSessionTicketValid()
		{
			return !string.IsNullOrEmpty(m_authTicketStr);
		}

		public void RequestUserAvatarAndName(PlayerId id, Action<PlayerId, string, Texture2D> result, AvatarSize avatarSize = AvatarSize.Small)
		{
			if (IsSupported(id))
			{
				if (SteamFriends.RequestUserInformation(m_player.steamId, nameonly: false))
				{
					uint avatarCallback = 0u;
					avatarCallback = Overlay.OnUserAvatarUpdate.Subscribe(GameEventFlag.Global, delegate(Overlay.UserInfo e)
					{
						if ((ulong)e.userId.accountId == (ulong)m_player.steamId)
						{
							Overlay.OnUserAvatarUpdate.Unsubscribe(avatarCallback);
							new SteamAvatar(m_player.steamId, avatarSize).Load(delegate(Texture2D texture)
							{
								result(id, GetUserName(id), texture);
							});
						}
					});
				}
				else
				{
					new SteamAvatar((ulong)id.accountId, avatarSize).Load(delegate(Texture2D texture)
					{
						result(id, GetUserName(id), texture);
					});
				}
			}
			else
			{
				result.Call(id, GetUserName(id), GameManager.instance.defaultAvatar);
			}
		}

		public string GetLoginCmd(Dictionary<string, string> prms)
		{
			prms.Add("authSessionTicket", m_authTicketStr);
			prms.Add("steamUserId", Overlay.playerId.accountId.ToString());
			return "login";
		}

		public void OnLoginError(Error error)
		{
		}

		public void TryEnterToMPZone(bool isAuthorized, System.Action approvedAction, Action<string> disabledAction)
		{
			TimeController.StartNow(CheckEnterToMP(approvedAction, disabledAction));
		}

		private IEnumerator CheckEnterToMP(System.Action approvedAction, Action<string> disabledAction)
		{
			bool inetCheckDone = false;
			bool inetAccessable = false;
			ConnectionUtils.IsInternetAccessAvailable(delegate(bool success)
			{
				inetAccessable = success;
				inetCheckDone = true;
			});
			yield return new WaitUntil(() => inetCheckDone);
			if (!inetAccessable)
			{
				disabledAction.SafeCall(LocalizationKeys.no_internet_connection.Localized());
				yield break;
			}
			if (!GameSync.isAuthorized)
			{
				yield return GameSync.Authorize();
			}
			while (GameSync.isAuthorizationInProgress)
			{
				yield return null;
			}
			if (!GameSync.isAuthorized)
			{
				disabledAction.SafeCall(GetStateMessage(isAuthorized: false));
			}
			else
			{
				approvedAction.SafeCall();
			}
		}

		public string GetStateMessage(bool isAuthorized)
		{
			if (!isAuthorized)
			{
				return LocalizationKeys.sync_not_connect.Localized() + " " + LocalizationKeys.multiplayer_unavailable.Localized();
			}
			if (ConnectedToInternet())
			{
				return string.Empty;
			}
			return LocalizationKeys.no_internet_connection.Localized();
		}

		public bool ConnectedToInternet()
		{
			return true;
		}

		public void ShowUGCRestrictionDialog()
		{
		}

		public void ShowCrossPlayRestrictionDialog()
		{
		}

		public void ShowCommunicationRestrictionDialog()
		{
		}

		public string GetRegion()
		{
			return m_regionDataManager.GetRegionData().GetFormattedRegionStr();
		}

		public void CheckOnlinePlayAbility()
		{
		}

		public IEnumerator CheckCommunicationRestrictions(System.Action callback)
		{
			callback.SafeCall();
			yield break;
		}

		public bool IsNativePlatformController(Rewired.Controller controller)
		{
			return true;
		}

		public void CheckSystemCrossPlatformAbility(Action<bool> isRestrictedCrossMpCallback)
		{
			isRestrictedCrossMpCallback.SafeCall(param: false);
		}

		public void ShowDLCList()
		{
			SteamFriends.OpenStoreOverlay(635260);
		}

		public IStorageSystem CreateStorageSystem()
		{
			return new SteamStorageSystem();
		}

		public IStatsWorker CreateStatsWorker()
		{
			return new SteamStatsWorker();
		}

		public IFilterTextManager CreateFilterTextManager()
		{
			return new EmptyFilterTextManager();
		}

		public IEnumerator TryFilterText(string nonFiltered, Action<string> callback)
		{
			callback.Call(nonFiltered);
			yield break;
		}

		public static uint DlcCodeToAppId(DLCCode dlc)
		{
			switch (dlc)
			{
			case DLCCode.Gold:
				return 752450u;
			case DLCCode.Trio:
				return 752440u;
			case DLCCode.TimeAttack:
				return 858690u;
			case DLCCode.Power:
				return 769710u;
			case DLCCode.FreeContent:
				return 776950u;
			case DLCCode.Police:
				return 842050u;
			case DLCCode.Origins:
				return 906130u;
			case DLCCode.NewStyle:
				return 937630u;
			case DLCCode.NewStyle2:
				return 967320u;
			case DLCCode.DriftWar:
				return 998560u;
			case DLCCode.Halloween:
				return 965040u;
			case DLCCode.CanyonTrip:
				return 1112420u;
			case DLCCode.RussianDriftStyle:
				return 1112460u;
			case DLCCode.JapanFest:
				return 1112470u;
			case DLCCode.AmericanWays:
				return 1159010u;
			case DLCCode.HalloweenVinyls:
				return 1178950u;
			case DLCCode.NewYearStickers2020:
				return 1214350u;
			case DLCCode.StreetClassStickersVinyls:
				return 1308680u;
			case DLCCode.ProDriftStickersVinyls:
				return 1308670u;
			case DLCCode.StreetTuners:
				return 1786050u;
			case DLCCode.HitTheWall:
				return 1901670u;
			case DLCCode.SayakaS15:
				return 2096530u;
			case DLCCode.NightMode:
				return 1449350u;
			case DLCCode.SeasonPass:
				return 1535220u;
			case DLCCode.Swaps:
				return 2154970u;
			case DLCCode.YoungTimers:
				return 2395750u;
			case DLCCode.Sayaka180SX:
				return 2638160u;
			case DLCCode.SayakaRX7:
				return 3292330u;
			case DLCCode.MeowtorMadness:
				return 3559650u;
			case DLCCode.WildUnion:
				return 3559630u;
			case DLCCode.CarPresetsUnlock:
				return 3559570u;
			case DLCCode.FreshX:
				return 5253680u;
			case DLCCode.Unknown:
			case DLCCode.RuntimeDLC:
				return 0u;
			default:
				return 0u;
			}
		}

		private async void ResolveAuthTicket(System.Action onDone = null)
		{
			if (m_authTicket != null)
			{
				onDone.SafeCall();
				return;
			}
			m_authTicketStr = string.Empty;
			AuthTicket authTicket = await SteamUser.GetAuthSessionTicketAsync(20.0);
			if (authTicket == null)
			{
				onDone.SafeCall();
				return;
			}
			byte[] data = authTicket.Data;
			if (data == null || data.Length == 0)
			{
				onDone.SafeCall();
				return;
			}
			m_authTicket = authTicket;
			m_authTicketStr = ByteArrayToHexString(data);
			onDone.SafeCall();
		}

		private void TryInitializeAvatars(bool isApproved)
		{
			if (isApproved)
			{
				InitializeAvatarsApproved();
			}
		}

		public bool IsResetInput()
		{
			return IsSystemOverlayShowedEpic();
		}

		private void InitializeAvatarsApproved()
		{
			new SteamAvatar(SteamId, AvatarSize.Large).Load(delegate(Texture2D texture)
			{
				player.avatar = texture;
				Overlay.AvatarInfo args = new Overlay.AvatarInfo(player.id, texture);
				Overlay.OnPlayerAvatarDecoded.ActivateInvokeOnSubscribeMode(args);
				Overlay.OnPlayerAvatarDecoded.Invoke(args);
			});
			foreach (Friend friend in SteamFriends.GetFriends())
			{
				SteamFriend newFriend = new SteamFriend(friend.Id)
				{
					nickName = friend.Name,
					state = ConvertPersonaState(friend.State)
				};
				new SteamAvatar(friend.Id, AvatarSize.Small).Load(delegate(Texture2D texture)
				{
					newFriend.avatar = texture;
				});
				friends.Add(newFriend);
			}
		}

		private void OnPersonaStateChange(Friend persona)
		{
			IPlayer player = null;
			if ((ulong)persona.Id == (ulong)SteamId)
			{
				player = m_player;
			}
			else
			{
				foreach (IPlayer friend2 in friends)
				{
					if (!(friend2.id.accountId != persona.Id.Value))
					{
						player = friend2;
						break;
					}
				}
			}
			PlayerId id = new PlayerId(UserPlatform.Id.Steam, persona.Id.Value);
			if (player == null)
			{
				SetUserName(id, persona.Name);
				if (persona.Relationship == Relationship.Friend)
				{
					SteamFriend friend = new SteamFriend(persona.Id)
					{
						nickName = persona.Name,
						state = ConvertPersonaState(persona.State)
					};
					new SteamAvatar(friend.steamId, AvatarSize.Small).Load(delegate(Texture2D texture)
					{
						friend.avatar = texture;
					});
					friends.Add(friend);
				}
			}
			else
			{
				SetUserName(id, player.nickName);
				friendPresenceChangedAction.SafeCall(player.id, player.state);
				player.state = ConvertPersonaState(persona.State);
				if (persona.Relationship != Relationship.Friend)
				{
					friends.Remove(player);
				}
			}
		}

		private void OnGameRichPresenceJoinRequested(Friend friend, string connect)
		{
			NetworkController.SaveInvitation(ParseAutoJoinRoom(connect));
		}

		private void OnGameOverlayActivated(bool active)
		{
			TimeController.StartNow(DelayedOverlayStatusChange(active));
			GameEvents.OverlayStateChanged(active);
		}

		private IEnumerator DelayedOverlayStatusChange(bool isActive)
		{
			yield return new WaitForSeconds(0.1f);
			GameInput.InputManager.steamOverlayActive = isActive;
		}

		private static PlayerState ConvertPersonaState(FriendState state)
		{
			if (state == FriendState.Offline || state == FriendState.Busy)
			{
				return PlayerState.Offline;
			}
			return PlayerState.Online;
		}

		private static string ParseAutoJoinRoom(string launchParams)
		{
			if (string.IsNullOrEmpty(launchParams))
			{
				return string.Empty;
			}
			int num = launchParams.IndexOf("-autoroom=", StringComparison.Ordinal);
			if (num >= 0)
			{
				return launchParams.Substring(num + "-autoroom=".Length);
			}
			return string.Empty;
		}

		private void CheckLegacyScreenshots()
		{
			FileInfo[] files = new DirectoryInfo(Application.persistentDataPath).GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				if (fileInfo.Extension == ".jpg")
				{
					SteamScreenshots.AddScreenshot(fileInfo.FullName, null, 0, 0);
					fileInfo.Delete();
				}
			}
		}

		private static string ByteArrayToHexString(byte[] bArray)
		{
			StringBuilder stringBuilder = new StringBuilder(bArray.Length * 2);
			foreach (byte b in bArray)
			{
				stringBuilder.AppendFormat("{0:x2}", b);
			}
			return stringBuilder.ToString();
		}

		private static object GetModInfo(Component c)
		{
			PropertyInfo property = c.GetType().GetProperty("Info");
			if (property != null)
			{
				object value = property.GetValue(c, null);
				if (value != null)
				{
					return value.ToString();
				}
			}
			return string.Empty;
		}

		private void DetectMods()
		{
			try
			{
				modLoaderFound = Directory.Exists("BepInEx");
			}
			catch (Exception arg)
			{
				Debug.LogError($"Unable to locate BepInEx loader directory, exception: {arg}");
				modLoaderFound = false;
			}
			Debug.Log($"Mod loader found: {modLoaderFound}");
			GameObject gameObject = GameObject.Find("BepInEx_Manager");
			if (gameObject == null)
			{
				Debug.Log("No mods found");
				return;
			}
			Component[] components = gameObject.GetComponents<Component>();
			Debug.Log($" > Founded mods: {components.Length - 1}");
			string text = $"Mods({components.Length - 1}):\n";
			Component[] array = components;
			foreach (Component component in array)
			{
				if (!(component.GetType() == typeof(Transform)))
				{
					Debug.Log($" >> Mod: {GetModInfo(component)} {component.GetType()}");
					text += $"  {GetModInfo(component)}\n";
				}
			}
			UIStartScreen.modsReport = text;
		}

		public static Texture2D FlipTexture(Texture2D original)
		{
			if (original == null)
			{
				return null;
			}
			Texture2D texture2D = new Texture2D(original.width, original.height, TextureFormat.RGBA32, mipChain: false);
			int width = original.width;
			int height = original.height;
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					texture2D.SetPixel(i, height - j - 1, original.GetPixel(i, j));
				}
			}
			texture2D.Apply(updateMipmaps: true);
			return texture2D;
		}

		private bool IsSuccessLoginToEpic()
		{
			if (IsReadyToStart() && m_eosLogoutTicketTask == null)
			{
				return IsEpicPlatformConnected();
			}
			return false;
		}

		private bool IsNotLoginToEpic()
		{
			if (IsReadyToStart() && m_eosLoginTicketTask == null)
			{
				return !IsEpicPlatformConnected();
			}
			return false;
		}

		public bool IsSystemOverlayShowedEpic()
		{
			if (GetEpicResultCode().HasValue)
			{
				return EOSManager.instance.IsOverlayEnabled;
			}
			return false;
		}

		public void CreateAndLinkAccount(LoginCallbackInfo loginCallbackInfo, Action<Epic.OnlineServices.Result> callback = null)
		{
			EOSManager.instance.CreateAndLinkAccount(loginCallbackInfo, delegate(LinkAccountCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					AuthEpicViaSteamIntegratedPlatform(delegate(LoginCallbackInfo? _, Epic.OnlineServices.Result result1)
					{
						callback?.Invoke(result1);
					});
				}
			});
		}

		public async void AuthEpicViaSteamIntegratedPlatform(Action<LoginCallbackInfo?, Epic.OnlineServices.Result> callback = null)
		{
			if (!IsNotLoginToEpic())
			{
				return;
			}
			m_eosLoginTicketTask = EOSManager.instance.AuthLoginViaSteamAppTicket(m_authTicketStr);
			await m_eosLoginTicketTask;
			m_loginCallbackInfo = m_eosLoginTicketTask.Result;
			if (m_loginCallbackInfo.Value.ResultCode == Epic.OnlineServices.Result.Success)
			{
				m_eosConnectResultTask = EOSManager.instance.ConnectLoginOrRegister();
				await m_eosConnectResultTask;
				if (m_eosConnectResultTask.Result == Epic.OnlineServices.Result.Success)
				{
					InitEpicService();
				}
				callback?.Invoke(m_loginCallbackInfo, m_eosConnectResultTask.Result);
				m_eosConnectResultTask = null;
				m_eosLoginTicketTask = null;
			}
			else
			{
				callback?.Invoke(m_loginCallbackInfo, m_loginCallbackInfo.Value.ResultCode);
				m_loginCallbackInfo = null;
				m_eosLoginTicketTask = null;
			}
		}

		public bool IsEpicPlatformConnected()
		{
			LoginCallbackInfo? loginCallbackInfo = m_loginCallbackInfo;
			if (loginCallbackInfo.HasValue && loginCallbackInfo.GetValueOrDefault().ResultCode == Epic.OnlineServices.Result.Success)
			{
				return EOSManager.instance.EpicAccountId != null;
			}
			return false;
		}

		public Epic.OnlineServices.Result? GetEpicResultCode()
		{
			return m_loginCallbackInfo?.GetResultCode();
		}

		private void InitEpicPlatform(bool initSteamIntegrationPlatform)
		{
			EOSManager.instance.logLevels = new Dictionary<LogCategory, LogLevel>
			{
				{
					LogCategory.AllCategories,
					LogLevel.Warning
				},
				{
					LogCategory.Overlay,
					LogLevel.Fatal
				}
			};
			EOSManager.instance.ScopeFlags = new AuthScopeFlags[4]
			{
				AuthScopeFlags.BasicProfile,
				AuthScopeFlags.FriendsList,
				AuthScopeFlags.Presence,
				AuthScopeFlags.Country
			};
			EOSManager.instance.Init(PrintEOSLogMessage);
			InitializeOptions product = new InitializeOptions
			{
				ProductName = Application.productName,
				ProductVersion = GameVersion.versionAsString
			};
			Options options = new Options
			{
				ProductId = SingletonScriptableObject<EOSConfig>.instance.ProductId,
				SandboxId = SingletonScriptableObject<EOSConfig>.instance.SandboxIdProd,
				DeploymentId = SingletonScriptableObject<EOSConfig>.instance.DeploymentIdProd,
				ClientCredentials = new ClientCredentials
				{
					ClientId = SingletonScriptableObject<EOSConfig>.instance.ClientId,
					ClientSecret = SingletonScriptableObject<EOSConfig>.instance.ClientSecret
				}
			};
			EOSManager.instance.InitPlatformInterface(ref product, ref options, initSteamIntegrationPlatform);
		}

		private async void InitEpicService()
		{
			m_eosFriendsManager = new EOSFriendManager();
			List<EOSFriend> collection = await m_eosFriendsManager.FetchFriends();
			friends.AddRange(collection);
			m_eosInviteService = new EOSInviteService();
			m_eosInviteService.SubscribeOnAllEvent();
			EOSManager.instance.SubscribeUIChange(delegate(OnDisplaySettingsUpdatedCallbackInfo info)
			{
				GameInput.InputManager.steamOverlayActive = info.IsExclusiveInput;
				if (!info.IsVisible)
				{
					Input.ResetInputAxes();
				}
			});
		}

		private void PrintEOSLogMessage(ref LogMessage logMessage)
		{
			switch (logMessage.Level)
			{
			case LogLevel.Fatal:
			case LogLevel.Error:
				GameLog.Print(AlertLevel.Error, "EOSOverlay", logMessage.Message);
				break;
			case LogLevel.Warning:
				GameLog.Print(AlertLevel.Warning, "EOSOverlay", logMessage.Message);
				break;
			default:
				GameLog.Print(AlertLevel.Notify, "EOSOverlay", logMessage.Message);
				break;
			}
		}
	}
}
