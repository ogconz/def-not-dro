using System;
using System.Collections;
using System.Collections.Generic;
using CarProfile;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using GameOverlay;
using LocalCaching.Manager;
using LocalCaching.Model;
using ModIO;
using PrefsModels;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetworkController : MonoBehaviour
	{
		public const int MaxReconnectTimes = 5;

		private const float InviteProcessingPeriod = 35f;

		private const float InvidePendingDelayOnLoadingBundle = 5f;

		private const float MinJoinCooldown = 2f;

		private const float ReconnectCooldownModifier = 2f;

		private static NetworkController m_LocalInstance;

		public static bool IsMapChanging = false;

		private GamePrefs m_prefs;

		private BaseModel m_model;

		private static float m_LastConnectionTime = float.MinValue;

		private static int m_curReconnectCounter = 0;

		private static string m_autoJoinRoomIdOrName;

		private static string m_autoJoinRoomRegion;

		private static string m_savedInvitation;

		private static bool m_joinByPassword;

		private static bool m_isSelfAutoInvitation;

		private static bool m_shownConnectionLostMsg;

		private static bool m_isAutoJoinInProgress;

		private static float m_lastAttemptJoinOnLoadingBundle;

		private static float m_roomReconnectAttemptCooldown = 2f;

		private ProfilesInfo.Profile m_currentCarProfile;

		private string m_cachedInvitation;

		private bool m_canTuneCar;

		public static NetworkGame InstanceGame { get; private set; }

		public static bool IsActive { get; private set; }

		public static bool ReconnectFailed { get; private set; }

		public static bool isAutoJoinInProgress => m_isAutoJoinInProgress;

		public static event Action onAutoJoinRoomError;

		public static event Action OnDisconnectDetected;

		private void Awake()
		{
			if (InstanceGame != null)
			{
				UnityEngine.Object.Destroy(this);
			}
			else
			{
				Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(Initialize));
			}
		}

		private void OnDestroy()
		{
			Overlay.OnReady = (Action)Delegate.Remove(Overlay.OnReady, new Action(Initialize));
		}

		private void Initialize()
		{
			if (InstanceGame == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
				m_model = DependencyInjector.Resolve<BaseModel>();
				InstanceGame = new NetworkGame();
				m_LocalInstance = this;
				GameConsole.SetCmd("disconnect", delegate
				{
					InstanceGame.Disconnect();
				});
			}
		}

		private void Update()
		{
			if (InstanceGame != null)
			{
				Reconnect();
				InstanceGame.Update();
				UpdateInvitation();
			}
		}

		private void UpdateInvitation()
		{
			if (!string.IsNullOrEmpty(m_savedInvitation) && !CheckIsLoadingState() && CheckEulaConfirm())
			{
				if (UGCSceneBundleManager.isLoadingSceneBundle)
				{
					m_lastAttemptJoinOnLoadingBundle = Time.time;
				}
				else if (!(m_lastAttemptJoinOnLoadingBundle > 0f) || !(Time.time - m_lastAttemptJoinOnLoadingBundle < 5f))
				{
					InstanceGame?.SetToNullCrossMPCreatedRoom();
					ForceContinuePlayIfNeed();
					ProceedInvitation(m_savedInvitation);
					m_savedInvitation = string.Empty;
				}
			}
		}

		private void LateUpdate()
		{
			InstanceGame?.LateUpdate();
		}

		private void FixedUpdate()
		{
			InstanceGame?.FixedUpdate();
		}

		public static bool IsVoteStartSystemAllowed()
		{
			if (InstanceGame != null)
			{
				if (!InstanceGame.IsLocalPlayerRoomCreator())
				{
					return !InstanceGame.playerRoomCreatorInRoom;
				}
				return true;
			}
			return false;
		}

		private void Reconnect()
		{
			if (IsActive && (InstanceGame.State == ClientState.Disconnected || InstanceGame.State == ClientState.Uninitialized))
			{
				TryReconnect();
			}
		}

		public static bool IsLocalCarLoaded()
		{
			if (InstanceGame == null)
			{
				return false;
			}
			RaceCar raceCar = InstanceGame.LocalPlayer?.userCar;
			if (raceCar == null)
			{
				return false;
			}
			if (raceCar != null)
			{
				return raceCar.IsVisualLoaded;
			}
			return false;
		}

		public static bool CheckIsLoadingState()
		{
			if (!(States.mainCurrent is LoadRaceState) && !(States.current is LoadRaceState))
			{
				return States.current is LoadSceneState;
			}
			return true;
		}

		public static void ForceContinuePlayIfNeed()
		{
			if (States.mainCurrent is BaseRaceState { startPlayPressed: false } baseRaceState)
			{
				baseRaceState.ForceContinuePlay();
			}
		}

		private bool CheckEulaConfirm()
		{
			if (UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
			{
				return true;
			}
			m_cachedInvitation = m_savedInvitation;
			m_savedInvitation = string.Empty;
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(TryRestoreSavedInvitationByEulaAccess));
			UIPrivacyPolicyEULAContext.Show();
			return false;
		}

		private void TryRestoreSavedInvitationByEulaAccess(bool isEulaAccepted)
		{
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(TryRestoreSavedInvitationByEulaAccess));
			if (isEulaAccepted)
			{
				m_savedInvitation = m_cachedInvitation;
			}
		}

		public static void TryReconnect()
		{
			if (!IsActive || !(Time.unscaledTime > m_LastConnectionTime + m_roomReconnectAttemptCooldown))
			{
				return;
			}
			m_roomReconnectAttemptCooldown *= 2f;
			m_LastConnectionTime = Time.unscaledTime;
			NetworkController.OnDisconnectDetected.SafeCall();
			if (!m_shownConnectionLostMsg)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.no_server_connection.Localized(), 3f);
				m_shownConnectionLostMsg = true;
			}
			m_curReconnectCounter++;
			if (m_curReconnectCounter > 5)
			{
				if (m_curReconnectCounter == 6)
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Reached max reconnection counter: " + 5);
					GUICommonGodVoice.ShowText(LocalizationKeys.no_server_connection2.Localized());
					ReconnectFailed = true;
					GarageGUIState.ExitToGarage();
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "Trying reconnection #" + m_curReconnectCounter);
				Connect();
			}
		}

		public static void LobbyEnter()
		{
			if (InstanceGame.State == ClientState.Uninitialized)
			{
				Connect();
			}
			IsActive = true;
			TryShowConnectionRestoreMsg(showMsg: false);
			ReconnectFailed = false;
		}

		public static void TryShowConnectionRestoreMsg(bool showMsg = true)
		{
			if (m_shownConnectionLostMsg)
			{
				m_shownConnectionLostMsg = false;
				if (showMsg)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.connection_restored.Localized(), 3f);
				}
			}
		}

		private static void Connect()
		{
			InstanceGame.Connect();
		}

		public static string GetCurrentRoomName()
		{
			if (InstanceGame != null && IsRoomCreatorBlocked(InstanceGame.CurrentRoomOptions?.RoomCreatorId))
			{
				return LocalizationKeys.network_game_servername_default.Localized();
			}
			if (!string.IsNullOrEmpty(NetworkGame.USER_CREATED_ROOM_NAME))
			{
				return NetworkGame.USER_CREATED_ROOM_NAME;
			}
			if (InstanceGame != null && InstanceGame.CurrentRoomOptions != null)
			{
				return InstanceGame.CurrentRoomOptions.Caption;
			}
			return string.Empty;
		}

		public static bool IsRoomCreatorBlocked(string playerId)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				return false;
			}
			if (PlayerId.TryParse(playerId, out var playerId2))
			{
				return Overlay.instance.IsBlockedUser(playerId2);
			}
			return false;
		}

		public static void LobbyExit()
		{
			InstanceGame.ReleaseRoom();
			IsActive = false;
			if (IsMapChanging)
			{
				InstanceGame.PrepareForChangeMap();
			}
			InstanceGame.Disconnect();
			IsMapChanging = false;
			m_curReconnectCounter = 0;
			m_roomReconnectAttemptCooldown = 2f;
		}

		public static bool JoinRoom(SmartfoxLobby.RoomLobbyInfo meta)
		{
			if (InstanceGame != null && InstanceGame.State == ClientState.JoinedLobby)
			{
				return InstanceGame.JoinRoom(meta);
			}
			return false;
		}

		public string GenerateRoomCode()
		{
			if (InstanceGame == null)
			{
				return "";
			}
			return UnityEngine.Random.Range(100000, 1000000).ToString();
		}

		public static string DecorateRoomCode(string code)
		{
			if (!string.IsNullOrEmpty(code))
			{
				return "@" + code;
			}
			return "";
		}

		public static void CreateRoom(RoomSettings roomSetting)
		{
			NetworkGame instanceGame = InstanceGame;
			if (instanceGame != null && instanceGame.State == ClientState.JoinedLobby)
			{
				CheckForCriticalParamsAndJoin(roomSetting.adhLimit, roomSetting.pdLimit, roomSetting.wdLimit, roomSetting.tcMode, delegate(bool powerDensityJoin)
				{
					if (powerDensityJoin)
					{
						CheckAdhAndJoin(roomSetting.adhLimit, delegate(bool adhCanJoin)
						{
							if (adhCanJoin)
							{
								CheckTractionControlAndJoin(roomSetting.tcMode, delegate(bool tractionControlCanJoin)
								{
									if (tractionControlCanJoin)
									{
										CheckAssistAndJoin(roomSetting.assistVersion, delegate(bool assistCanJoin)
										{
											if (assistCanJoin)
											{
												CheckWheelDriveTypeAndJoin(roomSetting.wdLimit, delegate(bool wheelDriveCanJoin)
												{
													if (wheelDriveCanJoin)
													{
														UILockInputContext.EnableLock();
														InstanceGame.CreateRoom(roomSetting);
													}
												});
											}
										});
									}
								});
							}
						});
					}
				});
			}
			else
			{
				UILockInputContext.DisableLock();
				GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized() + $" [{InstanceGame.State}]", 3f);
			}
		}

		public void OnApplicationPause(bool willPause)
		{
			if (InstanceGame != null && !willPause)
			{
				InstanceGame.Service();
			}
		}

		private void OnEnable()
		{
			GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
		}

		private void OnDisable()
		{
			GameEvents.OnGameEngineStop -= new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
		}

		private void OnGameEngineStop(GameEvents.EventInfo e)
		{
			IsActive = false;
			if (InstanceGame != null)
			{
				InstanceGame.Disconnect();
			}
		}

		public static void JoinByPass(string roomID)
		{
			m_joinByPassword = true;
			SaveInvitation(roomID);
		}

		public static void SaveInvitation(string roomName, bool selfAutoInvitation = false)
		{
			if (string.IsNullOrEmpty(roomName))
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "SaveInvitation - roomName is null");
				return;
			}
			m_isSelfAutoInvitation = selfAutoInvitation;
			m_savedInvitation = roomName;
			if (!m_joinByPassword)
			{
				if (m_isSelfAutoInvitation)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_autojoin_activity.Localized() ?? "", 3f);
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_autojoin_invitation.Localized() ?? "", 3f);
				}
			}
		}

		private static async void ProceedInvitation(string roomNameAndRegion)
		{
			string text = roomNameAndRegion;
			string[] array = roomNameAndRegion.Split('|');
			if (array.Length > 1)
			{
				text = array[0];
				m_autoJoinRoomRegion = array[1];
				int num = m_autoJoinRoomRegion.IndexOf('\0');
				if (num != -1)
				{
					m_autoJoinRoomRegion = m_autoJoinRoomRegion.Substring(0, num);
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (InstanceGame != null && InstanceGame.CurrentRoom != null && text == InstanceGame.CurrentRoom.Name)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_you_here.Localized(), 3f);
			}
			else if (!m_isAutoJoinInProgress)
			{
				m_autoJoinRoomIdOrName = text;
				if (States.current is MultiplayerPCState)
				{
					UILockInputContext.EnableLock();
					StartAutoJoin();
				}
				else
				{
					await InstanceGame.CaptureAndUploadCarLivery();
					GameManager.GameRestart();
				}
			}
		}

		public static void TryEnterToMultiplayer(ContextOwner owner, bool needLock = true)
		{
			if (needLock)
			{
				UILockInputContext.SetLock(owner);
			}
			GameSync.TryEnterToMPZone(ApprovedEnterToMultiplayer, delegate(string msg)
			{
				DisabledEnterToMultiplayer(msg);
			});
		}

		private static async void ApprovedEnterToMultiplayer()
		{
			if (!(States.mainCurrent is SyncNetFreerideRaceModeState))
			{
				await InstanceGame.CaptureAndUploadCarLivery();
			}
			UILockInputContext.DisableLock();
			States.Change<MultiplayerPCState>();
		}

		private static void DisabledEnterToMultiplayer(string msg, bool forceExitToGarage = false)
		{
			m_autoJoinRoomIdOrName = string.Empty;
			UILockInputContext.DisableLock();
			GUICommonGodVoice.ShowText(msg);
			TryReturnToGarageFromJoinRoomError(forceExitToGarage);
		}

		public static bool StartAutoJoin()
		{
			if (!NeedAutoJoin())
			{
				return false;
			}
			m_LocalInstance.StartCoroutine(m_LocalInstance.ProceedAutoJoin());
			return true;
		}

		public static bool NeedAutoJoin()
		{
			return !string.IsNullOrEmpty(m_autoJoinRoomIdOrName);
		}

		private IEnumerator ProceedAutoJoin()
		{
			if (m_isAutoJoinInProgress)
			{
				Debug.LogError("AutoJoin in progress. Ingored.");
				yield break;
			}
			m_isAutoJoinInProgress = true;
			if (!m_joinByPassword)
			{
				if (m_isSelfAutoInvitation)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_autojoin_activity.Localized() ?? "", 3f);
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_autojoin_invitation.Localized() ?? "", 3f);
				}
			}
			SmartfoxLobby.RoomLobbyInfo meta = null;
			yield return CheckRoomAvailability(delegate(SmartfoxLobby.RoomLobbyInfo result)
			{
				meta = result;
			});
			m_autoJoinRoomIdOrName = string.Empty;
			int adhLimit = meta?.options.RoomAdhLimit ?? 15;
			int assist = meta?.options.AssistVersion ?? 0;
			int spawnPointIndex = meta?.options.SpawnPointIndex ?? 0;
			int wheelDriveType = meta?.options.RoomWheelDriveType ?? 0;
			int powerDensity = meta?.options.RoomPowerDensityLimit ?? 0;
			int tractionControlParam = meta?.options.RoomTractionControlMode ?? 0;
			_ = meta?.options.CollisionType;
			CheckForCriticalParamsAndJoin(adhLimit, powerDensity, wheelDriveType, tractionControlParam, delegate(bool powerDensityJoin)
			{
				if (!powerDensityJoin)
				{
					OnFinished(success: false);
				}
				else
				{
					CheckWheelDriveTypeAndJoin(wheelDriveType, delegate(bool wheelDriveJoin)
					{
						if (!wheelDriveJoin)
						{
							OnFinished(success: false);
						}
						else
						{
							CheckAdhAndJoin(adhLimit, delegate(bool adhCanJoin)
							{
								if (!adhCanJoin)
								{
									OnFinished(success: false);
								}
								else
								{
									CheckAssistAndJoin(assist, delegate(bool assistCanJoin)
									{
										if (!assistCanJoin)
										{
											OnFinished(success: false);
										}
										else
										{
											m_prefs.globals.lastRoomAssist = assist;
											m_prefs.globals.lastRoomSpawnPoint = spawnPointIndex;
											if (meta == null)
											{
												ProcessJoin(successDownload: false);
											}
											else
											{
												CheckTractionControlAndJoin(tractionControlParam, delegate(bool tractionControlCanJoin)
												{
													if (!tractionControlCanJoin)
													{
														OnFinished(success: false);
													}
													else if (meta.options.ExternalTrackId != 0L && Overlay.instance.IsUserGeneratedContentRestricted)
													{
														Overlay.instance.ShowUGCRestrictionDialog();
														OnFinished(success: false);
													}
													else if (UGCServiceProvider.instance.IsContentInstalled(meta.options.ExternalTrackId) || meta.options.ExternalTrackId == 0L)
													{
														ProcessJoin(successDownload: true);
													}
													else
													{
														UILockInputContext.EnableLock();
														TimeController.AddCallback(3f, delegate
														{
															UILockInputContext.DisableLock();
															UIMessageBox.ShowComplexDialog(LocalizationKeys.do_you_want_download_ugc_map.Localized(), delegate
															{
																OnFinished(success: false);
															}, LocalizationKeys.Cancel.Localized(), delegate
															{
																DownloadExternalTrack(meta);
															}, LocalizationKeys.nav_download.Localized());
														});
													}
												});
											}
										}
									});
								}
							});
						}
					});
				}
			});
			void DownloadExternalTrack(SmartfoxLobby.RoomLobbyInfo selectedRoomLobbyInfo)
			{
				bool cancelRequested = false;
				IUGCManager.ServiceProvider ugcServiceId = (IUGCManager.ServiceProvider)selectedRoomLobbyInfo.options.ugcServiceId;
				if (ugcServiceId == IUGCManager.ServiceProvider.ModIO && ModIOUnity.IsModManagementBusy())
				{
					UIMessageBox.ShowWarning(LocalizationKeys.ugc_manager_is_not_ready.Localized(), delegate
					{
						cancelRequested = true;
						OnFinished(success: false);
					}, LocalizationKeys.Ok.Localized());
				}
				else
				{
					IUGCManager ugcManager = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, ugcServiceId);
					UIMessageBox.ShowProgressBar(LocalizationKeys.downloading_ugc_item.Localized(), delegate(Action<float> progressFill, Action closeProgressBar)
					{
						ugcManager.RequestPackage(selectedRoomLobbyInfo.options.ExternalTrackId, progressFill, delegate(bool success, string errorMessage)
						{
							closeProgressBar.SafeCall();
							if (!cancelRequested)
							{
								ProcessJoin(success, errorMessage);
							}
						});
					}, LocalizationKeys.Cancel.Localized(), delegate
					{
						ugcManager.TryStopRequestedPackage(selectedRoomLobbyInfo.options.ExternalTrackId);
						cancelRequested = true;
						OnFinished(success: false);
					});
				}
			}
			static void OnFinished(bool success)
			{
				if (!success)
				{
					UILockInputContext.DisableLock();
					TryReturnToGarageFromJoinRoomError();
				}
				m_isAutoJoinInProgress = false;
				m_joinByPassword = false;
			}
			void ProcessJoin(bool successDownload, string errorMessage = null)
			{
				UILockInputContext.EnableLock();
				bool flag = JoinRoom(meta);
				if (flag)
				{
					NetworkGame.USER_CREATED_ROOM_NAME = null;
				}
				if (!successDownload || !string.IsNullOrEmpty(errorMessage))
				{
					OnFinished(success: false);
					if (!string.IsNullOrEmpty(errorMessage))
					{
						TimeController.StartNow(ShowErrorInMainMenu(errorMessage));
					}
				}
				else if (meta == null || !flag || !successDownload)
				{
					OnFinished(success: false);
				}
				else
				{
					OnFinished(success: true);
				}
			}
			static IEnumerator ShowErrorInMainMenu(string errorMessage)
			{
				yield return new WaitUntil(() => States.current is GarageGUIState garageGUIState && garageGUIState.contextOwner.IsActive<UIMainMenuContext>());
				UIMessageBox.ShowWarning(errorMessage, null, LocalizationKeys.Ok.Localized());
			}
		}

		public static void TryReturnToGarageFromJoinRoomError(bool forceExit = true)
		{
			NetworkController.onAutoJoinRoomError.Call();
			if (forceExit || UIStartScreen.IsShown)
			{
				GarageGUIState.ExitToGarage();
			}
		}

		private IEnumerator CheckRoomAvailability(Action<SmartfoxLobby.RoomLobbyInfo> onCanJoin)
		{
			ChangeRegionIfNeed();
			float timeOutTime = Time.unscaledTime + 35f;
			yield return new WaitUntil(() => InstanceGame.LobbyState == ClientState.JoinedLobby || Time.unscaledTime > timeOutTime);
			if (InstanceGame.LobbyState != ClientState.JoinedLobby)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.invite_parse_error.Localized(), 3f);
				onCanJoin(null);
				yield break;
			}
			if (InstanceGame.CurrentRoom != null && InstanceGame.CurrentRoom.Name == m_autoJoinRoomIdOrName)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_you_here.Localized());
				onCanJoin(null);
				yield break;
			}
			SmartfoxLobby.RoomLobbyInfo roomLobbyInfo = null;
			bool roomLobbyInfoRequestDone = false;
			InstanceGame.FindRoomLobbyInfoRequest(m_autoJoinRoomIdOrName, delegate(SmartfoxLobby.RoomLobbyInfo meta)
			{
				roomLobbyInfo = meta;
				roomLobbyInfoRequestDone = true;
			});
			timeOutTime = Time.unscaledTime + 35f;
			yield return new WaitUntil(() => roomLobbyInfoRequestDone || Time.unscaledTime > timeOutTime);
			if (roomLobbyInfo != null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_autojoin.Localized() + " " + roomLobbyInfo.options.Caption, 3f);
				bool crossMPRestrictionCheckDone = false;
				bool crossMPRestricted = false;
				Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool isRestricted)
				{
					crossMPRestrictionCheckDone = true;
					crossMPRestricted = isRestricted;
					if (isRestricted)
					{
						Overlay.instance.ShowCrossPlayRestrictionDialog();
						m_prefs.globals.crossPlatformPlay = false;
					}
				});
				yield return new WaitUntil(() => crossMPRestrictionCheckDone);
				if (roomLobbyInfo.options.PlayerCount >= roomLobbyInfo.options.MaxPlayers)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.network_game_room_full.Localized(), 3f);
					onCanJoin(null);
					yield break;
				}
				JOIN_FLAG joinFlags = NetworkGame.GetJoinFlag(roomLobbyInfo.options);
				bool isDifferentPlatformRoom = InstanceGame.IsDifferentPlatformRoom(roomLobbyInfo.options.CurrentCrossPlayMask);
				if (joinFlags != JOIN_FLAG.ALLOW)
				{
					bool canGo = false;
					if (joinFlags == JOIN_FLAG.CROSS_DISABLE && InstanceGame.IsAllowCross(roomLobbyInfo.options.CurrentCrossPlayMask, considerPrefs: false) && isDifferentPlatformRoom)
					{
						if (InstanceGame.isRestrictCrossMpByAdmin)
						{
							m_prefs.globals.crossPlatformPlay = false;
							InstanceGame.ShowAdminRestrictAccessError(InstanceGame.restrictCrossMpByAdminTimeEnd, LocalizationKeys.admin_restricted_cross_mp);
							onCanJoin(null);
							yield break;
						}
						if (crossMPRestricted)
						{
							onCanJoin(null);
							yield break;
						}
						bool processed = false;
						UILockInputContext.DisableLock();
						UIMessageBox.ShowQuestion(LocalizationKeys.autojoin_to_crossplatform_room.Localized(), delegate
						{
							processed = true;
							canGo = false;
						}, LocalizationKeys.Cancel.Localized(), delegate
						{
							UILockInputContext.EnableLock();
							processed = true;
							canGo = true;
							m_prefs.globals.crossPlatformPlay = true;
							InstanceGame.RestoreFilteredRoomToNormalList(roomLobbyInfo);
						}, LocalizationKeys.Ok.Localized());
						yield return new WaitUntil(() => processed);
					}
					if (!canGo)
					{
						if (joinFlags == JOIN_FLAG.CROSS_DISABLE && isDifferentPlatformRoom)
						{
							GUICommonGodVoice.ShowText(LocalizationKeys.wrong_mask.Localized(), 3f);
							onCanJoin(null);
							yield break;
						}
						if (m_joinByPassword)
						{
							GUICommonGodVoice.ShowText(LocalizationKeys.wrong_room_pass.Localized(), 3f);
						}
						else
						{
							GUICommonGodVoice.ShowText(LocalizationKeys.invite_parse_error.Localized(), 3f);
						}
						onCanJoin(null);
						yield break;
					}
				}
				onCanJoin(roomLobbyInfo);
			}
			else
			{
				yield return null;
				if (InstanceGame.isRestrictAcessMpByAdmin)
				{
					InstanceGame.ShowAdminRestrictAccessError(InstanceGame.restrictAcessMpByAdminTimeEnd, LocalizationKeys.mp_ban_date_msg);
				}
				else if (m_joinByPassword)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.wrong_room_pass.Localized(), 3f);
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.network_room_not_exist.Localized(), 3f);
				}
				onCanJoin(null);
			}
		}

		private void ChangeRegionIfNeed()
		{
			if (!string.IsNullOrEmpty(m_autoJoinRoomRegion) && !(WebConfigManager.GetCurrentServer() == m_autoJoinRoomRegion))
			{
				WebConfigManager.SetMultiplayerMasterServerUrl(m_autoJoinRoomRegion, reconnectLobby: true, showGodvoice: true);
				m_autoJoinRoomRegion = string.Empty;
			}
		}

		public static void UpdateCurrentCarProfile()
		{
			if (!(m_LocalInstance == null))
			{
				PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
				int carId = m_LocalInstance.m_prefs.globals.lastCarId;
				int profileIdForCar = carSettings.GetProfileIdForCar(carId);
				string customSetupId = carSettings.GetInstalledCustomProfile(carId)?.id;
				m_LocalInstance.m_currentCarProfile = carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy();
				PlayerCarProfile playerCarProfile = m_LocalInstance.m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == m_LocalInstance.m_currentCarProfile.id);
				m_LocalInstance.m_canTuneCar = UIDynostandContext.IsCanDynoAdjust(playerCarProfile.name, playerCarProfile.dynoInfo);
			}
		}

		public static void CheckForCriticalParamsAndJoin(int adhLimit, int powerDensity, int wheelDriveType, int tractionControl, Action<bool> onProceedJoining)
		{
			int lastCarId = m_LocalInstance.m_prefs.globals.lastCarId;
			UpdateCurrentCarProfile();
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int profileIdForCar = carSettings.GetProfileIdForCar(lastCarId);
			string customSetupId = carSettings.GetInstalledCustomProfile(lastCarId)?.id;
			ProfilesInfo.Profile profile = carSettings.GetProfileInfo(profileIdForCar, lastCarId, customSetupId).Copy();
			RoomData.PowerDensityLimitFlag[] array = (RoomData.PowerDensityLimitFlag[])Enum.GetValues(typeof(RoomData.PowerDensityLimitFlag));
			bool flag = m_LocalInstance.CheckPowerDensity(profile, array[powerDensity]);
			bool flag2 = m_LocalInstance.CheckWheelDrive(profile, wheelDriveType);
			bool flag3 = m_LocalInstance.CheckAdh(profile, (RoomData.AdhLimitFlags)adhLimit);
			bool flag4 = m_LocalInstance.CheckTractionControl(profile, tractionControl);
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(profile.id);
			bool num = !flag || (!flag2 && !profileById.IsRacing()) || (!flag4 && !profileById.IsRacing()) || (!flag3 && (profileById.IsStock() || profileById.IsTurbo()));
			List<(bool, string)> paramGroup = new List<(bool, string)>
			{
				(flag, LocalizationKeys.garage_setup_power_density.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_power_density_explanation_parameter_change.Localized() + ") </color>"),
				(flag2, LocalizationKeys.network_game_params_drivetrain_type.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_available_on_racing_setup_message.Localized() + ") </color>"),
				(flag4, LocalizationKeys.netwotk_game_params_traction_control.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_available_on_racing_setup_message.Localized() + ") </color>"),
				(flag3, LocalizationKeys.network_game_params_adh_limit.Localized())
			};
			if (num)
			{
				UILockInputContext.DisableLock();
				UIMessageBox.ShowWarning(LocalizationKeys.network_game_unable_to_create_room_with_adh.Localized() + GetInvalidParamsByStringMessage(paramGroup), delegate
				{
					onProceedJoining.SafeCall(param: false);
				}, LocalizationKeys.Ok.Localized());
			}
			else
			{
				onProceedJoining.SafeCall(param: true);
			}
		}

		private static string GetInvalidParamsByStringMessage(List<(bool, string)> paramGroup)
		{
			string text = "<align=left>";
			foreach (var item in paramGroup)
			{
				if (!item.Item1)
				{
					text = text + "\n <color=#DF3A16FF> - " + item.Item2 + " </color>";
				}
			}
			return text + "</align>";
		}

		public static void CheckAssistAndJoin(int assist, Action<bool> onProceedJoining)
		{
			if (assist != 0 && (m_LocalInstance.m_prefs.globals.steeringAssistentWheel != GlobalModel.SteeringAssist.None || m_LocalInstance.m_prefs.globals.steeringAssistentGamepad != GlobalModel.SteeringAssist.None || m_LocalInstance.m_prefs.globals.steeringAssistentKeyboard != GlobalModel.SteeringAssist.None))
			{
				UILockInputContext.DisableLock();
				TimeController.AddCallback(0f, delegate
				{
					UIMessageBox.ShowQuestion(LocalizationKeys.network_game_change_asisst_to_continue.Localized(), delegate
					{
						onProceedJoining.SafeCall(param: false);
					}, LocalizationKeys.No.Localized(), delegate
					{
						m_LocalInstance.m_prefs.globals.steeringAssistentWheel = GlobalModel.SteeringAssist.None;
						m_LocalInstance.m_prefs.globals.steeringAssistentGamepad = GlobalModel.SteeringAssist.None;
						m_LocalInstance.m_prefs.globals.steeringAssistentKeyboard = GlobalModel.SteeringAssist.None;
						onProceedJoining.SafeCall(param: true);
					}, LocalizationKeys.Yes.Localized());
				});
			}
			else
			{
				onProceedJoining.SafeCall(param: true);
			}
		}

		public static void CheckWheelDriveTypeAndJoin(int wdLimit, Action<bool> onProceedJoining)
		{
			if (m_LocalInstance.m_currentCarProfile == null)
			{
				UILockInputContext.DisableLock();
				onProceedJoining.SafeCall(param: false);
				return;
			}
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int lastCarId = m_LocalInstance.m_prefs.globals.lastCarId;
			int profileIdForCar = carSettings.GetProfileIdForCar(lastCarId);
			string customSetupId = carSettings.GetInstalledCustomProfile(lastCarId)?.id;
			ProfilesInfo.Profile profile = carSettings.GetProfileInfo(profileIdForCar, lastCarId, customSetupId).Copy();
			if (!m_LocalInstance.CheckWheelDrive(profile, wdLimit))
			{
				UILockInputContext.DisableLock();
				TimeController.AddCallback(0f, delegate
				{
					UIMessageBox.ShowQuestion(LocalizationKeys.network_game_drivetrain_type_room_compatibility_error_ask_change.Localized(), delegate
					{
						onProceedJoining.SafeCall(param: false);
					}, LocalizationKeys.No.Localized(), delegate
					{
						m_LocalInstance.SetWheelDriveToServerStateLimit(lastCarId, profile, wdLimit);
						IconGeneratorContext.AddToCarHashQueue(new IconCarData
						{
							carId = lastCarId,
							iconName = string.Empty
						});
						onProceedJoining.SafeCall(param: true);
					}, LocalizationKeys.Yes.Localized());
				});
			}
			else
			{
				onProceedJoining.SafeCall(param: true);
			}
		}

		public static void CheckTractionControlAndJoin(int tcMode, Action<bool> onProceedJoining)
		{
			if (m_LocalInstance.m_currentCarProfile == null)
			{
				UILockInputContext.DisableLock();
				onProceedJoining.SafeCall(param: false);
			}
			else if (!m_LocalInstance.CheckTractionControl(m_LocalInstance.m_currentCarProfile, tcMode))
			{
				UILockInputContext.DisableLock();
				string tcQuestionKey = LocalizationKeys.network_game_traction_control_room_compatibility_error_ask_change.Localized();
				TimeController.AddCallback(0f, delegate
				{
					UIMessageBox.ShowQuestion(tcQuestionKey, delegate
					{
						onProceedJoining.SafeCall(param: false);
					}, LocalizationKeys.No.Localized(), delegate
					{
						m_LocalInstance.SetTractionControlToServerStateLimit(m_LocalInstance.m_prefs.globals.lastCarId, m_LocalInstance.m_currentCarProfile, tcMode);
						IconGeneratorContext.AddToCarHashQueue(new IconCarData
						{
							carId = m_LocalInstance.m_prefs.globals.lastCarId,
							iconName = string.Empty
						});
						onProceedJoining.SafeCall(param: true);
					}, LocalizationKeys.Yes.Localized());
				});
			}
			else
			{
				onProceedJoining.SafeCall(param: true);
			}
		}

		public static bool CheckRoomDisconnect()
		{
			if (!m_shownConnectionLostMsg && m_curReconnectCounter <= 0)
			{
				return InstanceGame.CurrentRoom == null;
			}
			return true;
		}

		public static async UniTask<bool> ValidateCarAsyncTop32(int adhLimit, int carId, int slotId)
		{
			AutoResetUniTaskCompletionSource<bool> taskCompletionSource = AutoResetUniTaskCompletionSource<bool>.Create();
			CheckAdhBySlotId(adhLimit, carId, slotId, delegate(bool adhChangeConfirmed)
			{
				taskCompletionSource.TrySetResult(adhChangeConfirmed);
			});
			return await taskCompletionSource.Task;
		}

		public static async UniTask<bool> ValidateCarAsyncFreeride(int adhLimit, int pdLimit, int wdLimit, int tcMode, int carId, int slotId)
		{
			AutoResetUniTaskCompletionSource<bool> taskCompletionSource = AutoResetUniTaskCompletionSource<bool>.Create();
			CheckForCriticalParamsBySlotId(adhLimit, pdLimit, wdLimit, tcMode, carId, slotId, delegate(bool criticalChangedConfirmed)
			{
				if (!criticalChangedConfirmed)
				{
					taskCompletionSource.TrySetResult(criticalChangedConfirmed);
				}
				else
				{
					CheckAdhBySlotId(adhLimit, carId, slotId, delegate(bool adhChangeConfirmed)
					{
						if (!adhChangeConfirmed)
						{
							taskCompletionSource.TrySetResult(adhChangeConfirmed);
						}
						else
						{
							CheckTractionControlBySlotId(tcMode, carId, slotId, delegate(bool tcChangeConfirmed)
							{
								if (!tcChangeConfirmed)
								{
									taskCompletionSource.TrySetResult(tcChangeConfirmed);
								}
								else
								{
									CheckWheelDriveBySlotId(wdLimit, carId, slotId, delegate(bool wdChangeConfirmed)
									{
										taskCompletionSource.TrySetResult(wdChangeConfirmed);
									});
								}
							});
						}
					});
				}
			});
			return await taskCompletionSource.Task;
		}

		public static void CheckForCriticalParamsBySlotId(int adhLimit, int pdLimit, int wdLimit, int tcMode, int carId, int slotId, Action<bool> criticalParamsChangeConfirmed)
		{
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int cachedSlotId = carSettings.GetActiveSlotId(carId);
			carSettings.SetActiveSlotId(carId, slotId);
			CheckForCriticalParamsByCarId(adhLimit, pdLimit, wdLimit, tcMode, carId, ProcessCallback);
			void ProcessCallback(bool result)
			{
				carSettings.SetActiveSlotId(carId, cachedSlotId);
				criticalParamsChangeConfirmed.SafeCall(result);
			}
		}

		public static void CheckForCriticalParamsByCarId(int adhLimit, int pdLimit, int wdLimit, int tcMode, int carId, Action<bool> criticalChangeConfirmed)
		{
			if (InstanceGame.lastRoomOptions == null)
			{
				criticalChangeConfirmed.SafeCall(param: false);
				return;
			}
			UpdateCurrentCarProfile();
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int profileIdForCar = carSettings.GetProfileIdForCar(carId);
			string customSetupId = carSettings.GetInstalledCustomProfile(carId)?.id;
			ProfilesInfo.Profile profile = carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy();
			if (profile == null)
			{
				if (States.mainCurrent is MultiplayerPCState)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized());
				}
				criticalChangeConfirmed.SafeCall(param: false);
				return;
			}
			RoomData.PowerDensityLimitFlag[] array = (RoomData.PowerDensityLimitFlag[])Enum.GetValues(typeof(RoomData.PowerDensityLimitFlag));
			bool flag = m_LocalInstance.CheckPowerDensity(profile, array[pdLimit]);
			bool flag2 = m_LocalInstance.CheckWheelDrive(profile, wdLimit);
			bool flag3 = m_LocalInstance.CheckAdh(profile, (RoomData.AdhLimitFlags)adhLimit);
			bool flag4 = m_LocalInstance.CheckTractionControl(profile, tcMode);
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(profile.id);
			bool num = !flag || (!flag2 && !profileById.IsRacing()) || (!flag4 && !profileById.IsRacing()) || (!flag3 && (profileById.IsStock() || profileById.IsTurbo()));
			List<(bool, string)> paramGroup = new List<(bool, string)>
			{
				(flag, LocalizationKeys.garage_setup_power_density.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_power_density_explanation_parameter_change.Localized() + ") </color>"),
				(flag2, LocalizationKeys.network_game_params_drivetrain_type.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_available_on_racing_setup_message.Localized() + ") </color>"),
				(flag4, LocalizationKeys.netwotk_game_params_traction_control.Localized() + "<color=#FFFFFFFF> (" + LocalizationKeys.network_game_available_on_racing_setup_message.Localized() + ") </color>"),
				(flag3, LocalizationKeys.car_profile_prm_caption_dyno_front_tire_type.Localized())
			};
			if (num)
			{
				UILockInputContext.DisableLock();
				UIMessageBox.ShowWarning(LocalizationKeys.network_game_unable_to_create_room_with_adh.Localized() + GetInvalidParamsByStringMessage(paramGroup), delegate
				{
					criticalChangeConfirmed.SafeCall(param: false);
				}, LocalizationKeys.Ok.Localized());
			}
			else
			{
				criticalChangeConfirmed.SafeCall(param: true);
			}
		}

		public static void CheckWheelDriveBySlotId(int wdLimit, int carId, int slotId, Action<bool> wdChangeConfirmed)
		{
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int cachedSlotId = carSettings.GetActiveSlotId(carId);
			carSettings.SetActiveSlotId(carId, slotId);
			CheckWheelDriveByCarId(wdLimit, carId, ProcessCallback);
			void ProcessCallback(bool result)
			{
				carSettings.SetActiveSlotId(carId, cachedSlotId);
				wdChangeConfirmed.SafeCall(result);
			}
		}

		public static void CheckWheelDriveByCarId(int wdLimit, int carId, Action<bool> wdChangeConfirmed)
		{
			if (InstanceGame.lastRoomOptions == null)
			{
				wdChangeConfirmed.SafeCall(param: false);
				return;
			}
			int profileIdForCar = m_LocalInstance.m_prefs.carSettings.GetProfileIdForCar(carId);
			string customSetupId = m_LocalInstance.m_prefs.carSettings.GetInstalledCustomProfile(carId)?.id;
			ProfilesInfo.Profile profile = m_LocalInstance.m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy();
			if (profile == null)
			{
				if (States.mainCurrent is MultiplayerPCState)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized());
				}
				wdChangeConfirmed.SafeCall(param: false);
				return;
			}
			if (m_LocalInstance.CheckWheelDrive(profile, wdLimit))
			{
				wdChangeConfirmed.SafeCall(param: true);
				return;
			}
			m_LocalInstance.m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profile.id);
			UILockInputContext.DisableLock();
			TimeController.AddCallback(0f, delegate
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.network_game_drivetrain_type_room_compatibility_error_ask_change.Localized(), delegate
				{
					wdChangeConfirmed.SafeCall(param: false);
				}, LocalizationKeys.No.Localized(), delegate
				{
					m_LocalInstance.SetWheelDriveToServerStateLimit(carId, profile, wdLimit);
					wdChangeConfirmed.SafeCall(param: true);
				}, LocalizationKeys.Yes.Localized());
			});
		}

		public static void CheckAdhBySlotId(int adhLimit, int carId, int slotId, Action<bool> adhChangeConfirmed)
		{
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int cachedSlotId = carSettings.GetActiveSlotId(carId);
			carSettings.SetActiveSlotId(carId, slotId);
			CheckAdhByCarId(adhLimit, carId, ProcessCallback);
			void ProcessCallback(bool result)
			{
				carSettings.SetActiveSlotId(carId, cachedSlotId);
				adhChangeConfirmed.SafeCall(result);
			}
		}

		public static void CheckAdhByCarId(int adhLimit, int carId, Action<bool> adhChangeConfirmed)
		{
			if (InstanceGame.lastRoomOptions == null)
			{
				adhChangeConfirmed.SafeCall(param: true);
				return;
			}
			int profileIdForCar = m_LocalInstance.m_prefs.carSettings.GetProfileIdForCar(carId);
			string customSetupId = m_LocalInstance.m_prefs.carSettings.GetInstalledCustomProfile(carId)?.id;
			ProfilesInfo.Profile profile = m_LocalInstance.m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy();
			if (profile == null)
			{
				if (States.mainCurrent is MultiplayerPCState)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized());
				}
				adhChangeConfirmed.SafeCall(param: false);
				return;
			}
			if (m_LocalInstance.CheckAdh(profile, (RoomData.AdhLimitFlags)adhLimit))
			{
				adhChangeConfirmed.SafeCall(param: true);
				return;
			}
			m_LocalInstance.m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profile.id);
			UILockInputContext.DisableLock();
			TimeController.AddCallback(0f, delegate
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.network_game_change_adh_to_continue.Localized(), delegate
				{
					adhChangeConfirmed.SafeCall(param: false);
				}, LocalizationKeys.No.Localized(), delegate
				{
					m_LocalInstance.SetAdhToServerStateLimit(carId, profile, (RoomData.AdhLimitFlags)adhLimit);
					adhChangeConfirmed.SafeCall(param: true);
				}, LocalizationKeys.Yes.Localized());
			});
		}

		public static void CheckTractionControlBySlotId(int tcMode, int carId, int slotId, Action<bool> tcChangeConfirme)
		{
			PrefsCarSlotsModel carSettings = m_LocalInstance.m_prefs.carSettings;
			int cachedSlotId = carSettings.GetActiveSlotId(carId);
			carSettings.SetActiveSlotId(carId, slotId);
			CheckTractionControlByCarId(tcMode, carId, ProcessCallback);
			void ProcessCallback(bool result)
			{
				carSettings.SetActiveSlotId(carId, cachedSlotId);
				tcChangeConfirme.SafeCall(result);
			}
		}

		public static void CheckTractionControlByCarId(int tcMode, int carId, Action<bool> tcChangeConfirmed)
		{
			if (InstanceGame.lastRoomOptions == null)
			{
				tcChangeConfirmed.SafeCall(param: false);
				return;
			}
			int profileIdForCar = m_LocalInstance.m_prefs.carSettings.GetProfileIdForCar(carId);
			string customSetupId = m_LocalInstance.m_prefs.carSettings.GetInstalledCustomProfile(carId)?.id;
			ProfilesInfo.Profile profile = m_LocalInstance.m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy();
			if (profile == null)
			{
				if (States.mainCurrent is MultiplayerPCState)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized());
				}
				tcChangeConfirmed.SafeCall(param: false);
				return;
			}
			if (m_LocalInstance.CheckTractionControl(profile, tcMode))
			{
				tcChangeConfirmed.SafeCall(param: true);
				return;
			}
			m_LocalInstance.m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profile.id);
			UILockInputContext.DisableLock();
			TimeController.AddCallback(0f, delegate
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.network_game_traction_control_room_compatibility_error_ask_change.Localized(), delegate
				{
					tcChangeConfirmed.SafeCall(param: false);
				}, LocalizationKeys.No.Localized(), delegate
				{
					m_LocalInstance.SetTractionControlToServerStateLimit(carId, profile, tcMode);
					tcChangeConfirmed.SafeCall(param: true);
				}, LocalizationKeys.Yes.Localized());
			});
		}

		public static void CheckAdhAndJoin(int adhLimit, Action<bool> onProceedJoining)
		{
			UpdateCurrentCarProfile();
			if (m_LocalInstance.m_currentCarProfile == null)
			{
				if (States.mainCurrent is MultiplayerPCState)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized());
				}
				UILockInputContext.DisableLock();
				onProceedJoining.SafeCall(param: false);
			}
			else if (!m_LocalInstance.CheckAdh(m_LocalInstance.m_currentCarProfile, (RoomData.AdhLimitFlags)adhLimit))
			{
				UILockInputContext.DisableLock();
				LocalizationKeys.Key adhQuestionKey = LocalizationKeys.network_game_change_adh_to_continue;
				TimeController.AddCallback(0f, delegate
				{
					UIMessageBox.ShowQuestion(adhQuestionKey.Localized(), delegate
					{
						onProceedJoining.SafeCall(param: false);
					}, LocalizationKeys.No.Localized(), delegate
					{
						m_LocalInstance.SetAdhToServerStateLimit(m_LocalInstance.m_prefs.globals.lastCarId, m_LocalInstance.m_currentCarProfile, (RoomData.AdhLimitFlags)adhLimit);
						IconGeneratorContext.AddToCarHashQueue(new IconCarData
						{
							carId = m_LocalInstance.m_prefs.globals.lastCarId,
							iconName = string.Empty
						});
						onProceedJoining.SafeCall(param: true);
					}, LocalizationKeys.Yes.Localized());
				});
			}
			else
			{
				onProceedJoining.SafeCall(param: true);
			}
		}

		private bool CheckAdh(ProfilesInfo.Profile carProfile, RoomData.AdhLimitFlags limit)
		{
			if (limit == RoomData.AdhLimitFlags.NONE || limit == RoomData.AdhLimitFlags.ALL)
			{
				return true;
			}
			bool flag = limit == RoomData.AdhLimitFlags.ADH_100;
			float value;
			bool flag2 = carProfile.values.TryGetValue("front_tire_type", out value);
			float value2;
			bool flag3 = carProfile.values.TryGetValue("rear_tire_type", out value2);
			float b = RoomData.AdhLimitAsFloat(limit);
			if (flag2 && flag3)
			{
				if (Mathf.Approximately(value, b))
				{
					return Mathf.Approximately(value2, b);
				}
				return false;
			}
			if (flag2)
			{
				return Mathf.Approximately(value, b) && flag;
			}
			if (flag3)
			{
				return Mathf.Approximately(value2, b) && flag;
			}
			return flag;
		}

		private bool CheckPowerDensity(ProfilesInfo.Profile carProfile, RoomData.PowerDensityLimitFlag pdLimit)
		{
			if (pdLimit == RoomData.PowerDensityLimitFlag.NONE || pdLimit == RoomData.PowerDensityLimitFlag.ALL)
			{
				return true;
			}
			PlayerCarFactory carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(carProfile.id);
			CarProfileCache carProfileCache = CarProfileCacheManager.GetCarProfileCache(carFactory, profileById);
			float num = RoomData.PowerDensityAsFloat(pdLimit);
			return carProfileCache.powerDensity < num;
		}

		private bool CheckTractionControl(ProfilesInfo.Profile carProfile, int tcMode)
		{
			if (tcMode == 0)
			{
				return true;
			}
			float b = tcMode - 1;
			carProfile.values.TryGetValue("traction_control", out var value);
			return Mathf.Approximately(value, b);
		}

		private bool CheckWheelDrive(ProfilesInfo.Profile carProfile, int wdLimit)
		{
			if (wdLimit == 0)
			{
				return true;
			}
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(carProfile.id);
			if (carProfile.values.Count == 0)
			{
				return Mathf.Approximately(ConvertWheelDriveTypeFromString(CarProfileCacheManager.GetCarProfileCache(DependencyInjector.Resolve<PlayerCarFactory>(), profileById).transmission), wdLimit);
			}
			float b = wdLimit - 1;
			carProfile.values.TryGetValue("drivetrain_type", out var value);
			value = ((value == 2f) ? 1f : value);
			return Mathf.Approximately(value, b);
		}

		public static int ConvertWheelDriveTypeFromString(string wheelDrive)
		{
			return wheelDrive switch
			{
				"RWD" => 1, 
				"GEAR_RWD" => 1, 
				"AWD" => 2, 
				"4WD" => 2, 
				"GEAR_AWD" => 2, 
				"GEAR_4WD" => 2, 
				_ => 0, 
			};
		}

		private void SetAdhToServerStateLimit(int carId, ProfilesInfo.Profile carProfile, RoomData.AdhLimitFlags limit)
		{
			int profileIdForCar = m_LocalInstance.m_prefs.carSettings.GetProfileIdForCar(carId);
			string customSetupId = m_LocalInstance.m_prefs.carSettings.GetInstalledCustomProfile(carId)?.id;
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(m_LocalInstance.m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy().id);
			CarProfileChanger changer = profileById.dynoInfo.GetChanger(CarProfileControllerType.Wheels, CarProfileParameterType.FrontTireType);
			CarProfileChanger changer2 = profileById.dynoInfo.GetChanger(CarProfileControllerType.Wheels, CarProfileParameterType.RearTireType);
			bool flag = true;
			if (!carProfile.values.TryGetValue("front_tire_type", out var value))
			{
				flag = false;
				value = RoomData.AdhLimitAsFloat(RoomData.AdhLimitFlags.ADH_100);
			}
			bool flag2 = true;
			if (!carProfile.values.TryGetValue("rear_tire_type", out var value2))
			{
				flag2 = false;
				value2 = RoomData.AdhLimitAsFloat(RoomData.AdhLimitFlags.ADH_100);
			}
			float num = RoomData.AdhLimitAsFloat(limit);
			if (!Mathf.Approximately(value, num))
			{
				if (flag)
				{
					carProfile.controllers["front_tire_type"] = changer.EvaluatePhase(num);
				}
				else
				{
					carProfile.values["front_tire_type"] = num;
					carProfile.controllers["front_tire_type"] = changer.EvaluatePhase(num);
				}
			}
			if (!Mathf.Approximately(value2, num))
			{
				if (flag2)
				{
					carProfile.controllers["rear_tire_type"] = changer2.EvaluatePhase(num);
				}
				else
				{
					carProfile.values["rear_tire_type"] = num;
					carProfile.controllers["rear_tire_type"] = changer2.EvaluatePhase(num);
				}
			}
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			int engineIdForCar = carSettings.GetEngineIdForCar(carId);
			CustomProfile installedCustomProfile = carSettings.GetInstalledCustomProfile(carId);
			if (installedCustomProfile != null)
			{
				carSettings.SetCustomProfileInfo(out var _, out var _, installedCustomProfile.name, carProfile.id, carId, engineIdForCar, carProfile);
			}
			else
			{
				carSettings.SetProfileInfo(carId, carProfile.id, carProfile);
			}
		}

		private void SetTractionControlToServerStateLimit(int carId, ProfilesInfo.Profile carProfile, int tcMode)
		{
			int profileIdForCar = m_LocalInstance.m_prefs.carSettings.GetProfileIdForCar(carId);
			string customSetupId = m_LocalInstance.m_prefs.carSettings.GetInstalledCustomProfile(carId)?.id;
			CarProfileChanger changer = PlayerCarProfileMetaExtensions.GetProfileById(m_LocalInstance.m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, customSetupId).Copy().id).dynoInfo.GetChanger(CarProfileControllerType.Brakes, CarProfileParameterType.TractionControl);
			int num = tcMode - 1;
			bool flag = true;
			if (!carProfile.values.TryGetValue("traction_control", out var value))
			{
				flag = false;
				value = 0f;
			}
			if (!Mathf.Approximately(value, num))
			{
				if (flag)
				{
					carProfile.controllers["traction_control"] = changer.EvaluatePhase(num);
				}
				else
				{
					carProfile.values["traction_control"] = num;
					carProfile.controllers["traction_control"] = changer.EvaluatePhase(num);
				}
			}
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			int engineIdForCar = carSettings.GetEngineIdForCar(carId);
			CustomProfile installedCustomProfile = carSettings.GetInstalledCustomProfile(carId);
			if (installedCustomProfile != null)
			{
				carSettings.SetCustomProfileInfo(out var _, out var _, installedCustomProfile.name, carProfile.id, carId, engineIdForCar, carProfile);
			}
			else
			{
				carSettings.SetProfileInfo(carId, carProfile.id, carProfile);
			}
		}

		private void SetWheelDriveToServerStateLimit(int carId, ProfilesInfo.Profile carProfile, int wdLimit)
		{
			PlayerCarProfile playerCarProfile = m_LocalInstance.m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == carProfile.id);
			CarProfileChanger changer = playerCarProfile.dynoInfo.GetChanger(CarProfileControllerType.Transmission, CarProfileParameterType.DrivetrainType);
			float num = wdLimit - 1;
			bool flag = true;
			if (!carProfile.values.TryGetValue("drivetrain_type", out var value))
			{
				flag = false;
				value = ConvertWheelDriveTypeFromString(CarProfileCacheManager.GetCarProfileCache(DependencyInjector.Resolve<PlayerCarFactory>(), playerCarProfile).transmission) - 1;
			}
			if (!Mathf.Approximately(value, num))
			{
				if (flag)
				{
					carProfile.controllers["drivetrain_type"] = changer.EvaluatePhase(num);
				}
				else
				{
					carProfile.values["drivetrain_type"] = num;
					carProfile.controllers["drivetrain_type"] = changer.EvaluatePhase(num);
				}
			}
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			int engineIdForCar = carSettings.GetEngineIdForCar(carId);
			CustomProfile installedCustomProfile = carSettings.GetInstalledCustomProfile(carId);
			if (installedCustomProfile != null)
			{
				carSettings.SetCustomProfileInfo(out var _, out var _, installedCustomProfile.name, carProfile.id, carId, engineIdForCar, carProfile);
			}
			else
			{
				carSettings.SetProfileInfo(carId, carProfile.id, carProfile);
			}
		}
	}
}
