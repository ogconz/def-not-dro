using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Ecom;
using Epic.OnlineServices.Friends;
using Epic.OnlineServices.IntegratedPlatform;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Stats;
using Epic.OnlineServices.UI;
using Epic.OnlineServices.UserInfo;
using Steamworks;
using UnityEngine;

namespace EpicUnity
{
	public class EOSManager
	{
		private const int TickAsyncOperation = 1;

		private const float TickInSecondAsyncOperation = 0.001f;

		private const string LibPath = "Assets/Plugins/EOSSDK/EOSSDK-Win64-Shipping";

		private static EOSManager m_instance;

		public Dictionary<LogCategory, LogLevel> logLevels = new Dictionary<LogCategory, LogLevel> { 
		{
			LogCategory.AllCategories,
			LogLevel.Warning
		} };

		public AuthScopeFlags[] ScopeFlags = new AuthScopeFlags[4]
		{
			AuthScopeFlags.BasicProfile,
			AuthScopeFlags.FriendsList,
			AuthScopeFlags.Presence,
			AuthScopeFlags.Country
		};

		private ulong connectLoginRefreshingHandle;

		private ulong authLoginRefreshingHandle;

		private LogMessageFunc m_logMessageCallback;

		private bool m_isOverlayEnabled;

		private Task m_isOverlayTask;

		private Epic.OnlineServices.Auth.LoginOptions m_authOptions;

		private Epic.OnlineServices.Connect.LoginOptions m_loginOptions;

		private LoginStatus m_loginStatus;

		public static EOSManager instance
		{
			get
			{
				if (m_instance != null)
				{
					return m_instance;
				}
				m_instance = new EOSManager();
				return m_instance;
			}
		}

		public bool IsShutdown { get; private set; } = true;

		public bool InputBlocked { get; private set; }

		public EpicAccountId EpicAccountId { get; private set; }

		public ProductUserId ProductUserId { get; private set; }

		public bool IsOverlayEnabled
		{
			get
			{
				if (m_isOverlayEnabled && m_isOverlayTask != null)
				{
					m_isOverlayTask = TimerCallback(0.1f, delegate
					{
						m_isOverlayEnabled = GetUIOverlayActivity();
						m_isOverlayTask = null;
					});
				}
				else
				{
					m_isOverlayEnabled = GetUIOverlayActivity();
				}
				if (m_isOverlayEnabled)
				{
					return InputBlocked;
				}
				return false;
			}
		}

		private PlatformInterface EOSPlatformInterface { get; set; }

		private bool GetUIOverlayActivity()
		{
			if (EpicAccountId == null || !EpicAccountId.IsValid())
			{
				return false;
			}
			GetFriendsExclusiveInputOptions options = new GetFriendsExclusiveInputOptions
			{
				LocalUserId = EpicAccountId
			};
			UIInterface uIInterface = EOSPlatformInterface.GetUIInterface();
			if (uIInterface != null)
			{
				return uIInterface.GetFriendsExclusiveInput(ref options);
			}
			return false;
		}

		private async Task TimerCallback(float time, Action callback)
		{
			await Task.Delay(Mathf.FloorToInt(time * 1000f));
			callback?.Invoke();
		}

		private AuthScopeFlags GetCombinedScopeFlags()
		{
			AuthScopeFlags authScopeFlags = AuthScopeFlags.NoFlags;
			AuthScopeFlags[] scopeFlags = ScopeFlags;
			foreach (AuthScopeFlags authScopeFlags2 in scopeFlags)
			{
				authScopeFlags |= authScopeFlags2;
			}
			return authScopeFlags;
		}

		public void Init(LogMessageFunc logMessageFunc)
		{
			m_logMessageCallback = logMessageFunc;
			LoadNativeSDK();
			IsShutdown = false;
		}

		public void Tick()
		{
			if (!IsShutdown)
			{
				UpdatePlatformInterface();
			}
		}

		public void Shutdown()
		{
			if (!IsShutdown)
			{
				Log("EOSManager", LogLevel.Warning, "Shutdown");
				m_instance = null;
				ReleasePlatformInterface();
				UnloadNativeSDK();
			}
		}

		private void LoadNativeSDK()
		{
		}

		private void UnloadNativeSDK()
		{
		}

		private string FindWithPrefix(string[] args, string prefix)
		{
			foreach (string text in args)
			{
				if (text.StartsWith(prefix))
				{
					return text;
				}
			}
			return null;
		}

		private T[] SelectItems<T, U>(U[] args, Func<U, T> act)
		{
			T[] array = new T[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				array[i] = act(args[i]);
			}
			return array;
		}

		public bool IsLaunchedViaEpicLauncher()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-EpicPortal") != null;
		}

		public string GetExchangeCode()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-AUTH_PASSWORD=")?.Substring("-AUTH_PASSWORD=".Length);
		}

		public string GetExchangeLanguage()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-epiclocale=")?.Substring("-epiclocale=".Length);
		}

		public string GetExchangeUserName()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-epicusername=")?.Substring("-epicusername=".Length);
		}

		public string GetExchangeEpicId()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-epicuserid=")?.Substring("-epicuserid=".Length);
		}

		public string GetExchangeSandboxId()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-epicsandboxid=")?.Substring("-epicsandboxid=".Length);
		}

		public string GetExchangeDeploymentId()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			return FindWithPrefix(commandLineArgs, "-epicdeploymentid=")?.Substring("-epicdeploymentid=".Length);
		}

		public void InitPlatformInterface(ref InitializeOptions product, ref Epic.OnlineServices.Platform.Options options, bool createIntegratedPlatform)
		{
			Epic.OnlineServices.Result result = PlatformInterface.Initialize(ref product);
			if (result != Epic.OnlineServices.Result.Success)
			{
				Log("EOSManager", LogLevel.Warning, "Initialize platform: " + result);
			}
			foreach (var (logCategory2, logLevel2) in logLevels)
			{
				LoggingInterface.SetLogLevel(logCategory2, logLevel2);
			}
			LoggingInterface.SetCallback(m_logMessageCallback);
			if (createIntegratedPlatform)
			{
				IntegratedPlatformOptionsContainer container;
				Epic.OnlineServices.Result result2 = CreateSteamIntegratedPlatform(ref options, out container);
				options.IntegratedPlatformOptionsContainerHandle = container;
				if (result2 != Epic.OnlineServices.Result.Success)
				{
					Log("EOSManager", LogLevel.Warning, "Create integrated platform: " + result2);
				}
			}
			EOSPlatformInterface = PlatformInterface.Create(ref options);
			if (EOSPlatformInterface == null)
			{
				throw new Exception("[EOSManager] Failed to create platform");
			}
			SubscribeUIChange(delegate(OnDisplaySettingsUpdatedCallbackInfo info)
			{
				InputBlocked = info.IsExclusiveInput;
			});
		}

		private void UpdatePlatformInterface()
		{
			if (!(EOSPlatformInterface == null) && !(EOSPlatformInterface.InnerHandle == IntPtr.Zero))
			{
				EOSPlatformInterface.Tick();
			}
		}

		private void ReleasePlatformInterface()
		{
			if (EOSPlatformInterface != null)
			{
				EOSPlatformInterface.Release();
				EOSPlatformInterface = null;
				PlatformInterface.Shutdown();
			}
		}

		public void CreateAndLinkAccount(Epic.OnlineServices.Auth.LoginCallbackInfo loginCallbackInfo, Action<Epic.OnlineServices.Auth.LinkAccountCallbackInfo> linkAccountCallback)
		{
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			Epic.OnlineServices.Auth.LinkAccountOptions options = new Epic.OnlineServices.Auth.LinkAccountOptions
			{
				ContinuanceToken = loginCallbackInfo.ContinuanceToken
			};
			authInterface.LinkAccount(ref options, null, delegate(ref Epic.OnlineServices.Auth.LinkAccountCallbackInfo data)
			{
				if (data.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = data.LocalUserId;
					linkAccountCallback?.Invoke(data);
				}
			});
		}

		public Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaSteamAppTicket(string authTicket)
		{
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
			m_authOptions = new Epic.OnlineServices.Auth.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Auth.Credentials
				{
					Type = LoginCredentialType.ExternalAuth,
					Id = SteamClient.SteamId.Value.ToString(),
					Token = authTicket,
					ExternalType = ExternalCredentialType.SteamSessionTicket
				},
				ScopeFlags = GetCombinedScopeFlags()
			};
			authInterface.Login(ref m_authOptions, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = info.LocalUserId;
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		public Task<LogoutCallbackInfo> Logout()
		{
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			LogoutOptions options = new LogoutOptions
			{
				LocalUserId = EpicAccountId
			};
			TaskCompletionSource<LogoutCallbackInfo> tcs = new TaskCompletionSource<LogoutCallbackInfo>();
			authInterface.Logout(ref options, null, delegate(ref LogoutCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = null;
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		private Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaAccountPortal()
		{
			TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
			m_authOptions = new Epic.OnlineServices.Auth.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Auth.Credentials
				{
					Type = LoginCredentialType.AccountPortal,
					Id = null,
					Token = null
				},
				ScopeFlags = GetCombinedScopeFlags()
			};
			EOSPlatformInterface.GetAuthInterface().Login(ref m_authOptions, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = info.LocalUserId;
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		private Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaEpicLauncher()
		{
			TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
			if (!IsLaunchedViaEpicLauncher())
			{
				tcs.SetResult(new Epic.OnlineServices.Auth.LoginCallbackInfo
				{
					ResultCode = Epic.OnlineServices.Result.AuthExchangeCodeNotFound
				});
				return tcs.Task;
			}
			m_authOptions = new Epic.OnlineServices.Auth.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Auth.Credentials
				{
					Type = LoginCredentialType.ExchangeCode,
					Id = null,
					Token = GetExchangeCode()
				},
				ScopeFlags = GetCombinedScopeFlags()
			};
			EOSPlatformInterface.GetAuthInterface().Login(ref m_authOptions, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = info.LocalUserId;
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		private Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaDeveloperAuthTool(string host = "localhost:6547", string credentialName = "me")
		{
			TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			m_authOptions = new Epic.OnlineServices.Auth.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Auth.Credentials
				{
					Type = LoginCredentialType.Developer,
					Id = host,
					Token = credentialName
				},
				ScopeFlags = GetCombinedScopeFlags()
			};
			authInterface.Login(ref m_authOptions, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					EpicAccountId = info.LocalUserId;
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		public async Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLogin(bool authToolInEditor = false, string authToolHost = "localhost:6547", string authToolCredentialName = "me")
		{
			Epic.OnlineServices.Result resultCode;
			Epic.OnlineServices.Auth.LoginCallbackInfo result;
			if (authToolInEditor)
			{
				result = await AuthLoginViaDeveloperAuthTool(authToolHost, authToolCredentialName);
				resultCode = result.ResultCode;
				if (resultCode == Epic.OnlineServices.Result.Success || resultCode == Epic.OnlineServices.Result.Canceled)
				{
					return result;
				}
			}
			result = await AuthLoginViaEpicLauncher();
			resultCode = result.ResultCode;
			if (resultCode == Epic.OnlineServices.Result.Success || resultCode == Epic.OnlineServices.Result.Canceled)
			{
				return result;
			}
			result = await AuthLoginViaAccountPortal();
			if (result.ResultCode == Epic.OnlineServices.Result.Success)
			{
				StartRefreshingAuthLogin();
			}
			return result;
		}

		private Task<Epic.OnlineServices.Connect.LoginCallbackInfo> ConnectLoginViaAuth()
		{
			TaskCompletionSource<Epic.OnlineServices.Connect.LoginCallbackInfo> tcs = new TaskCompletionSource<Epic.OnlineServices.Connect.LoginCallbackInfo>();
			if (EpicAccountId == null)
			{
				Log("[EOSManager]", LogLevel.Error, "You cannot login into Connect via Auth, because there is nobody logged in via Auth.");
				return tcs.Task;
			}
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			ConnectInterface connectInterface = EOSPlatformInterface.GetConnectInterface();
			Epic.OnlineServices.Auth.CopyIdTokenOptions options = new Epic.OnlineServices.Auth.CopyIdTokenOptions
			{
				AccountId = EpicAccountId
			};
			authInterface.CopyIdToken(ref options, out var outIdToken);
			m_loginOptions = new Epic.OnlineServices.Connect.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Connect.Credentials
				{
					Type = ExternalCredentialType.EpicIdToken,
					Token = outIdToken?.JsonWebToken
				}
			};
			connectInterface.Login(ref m_loginOptions, null, delegate(ref Epic.OnlineServices.Connect.LoginCallbackInfo info)
			{
				if (info.ResultCode == Epic.OnlineServices.Result.Success)
				{
					ProductUserId = info.LocalUserId;
					StartRefreshingConnectLogin();
				}
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
			});
			return tcs.Task;
		}

		public async Task<Epic.OnlineServices.Result> ConnectLoginOrRegister()
		{
			Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo = await ConnectLoginViaAuth();
			if (loginCallbackInfo.ResultCode != Epic.OnlineServices.Result.InvalidUser)
			{
				return loginCallbackInfo.ResultCode;
			}
			CreateUserCallbackInfo createUserCallbackInfo = await ConnectCreateProductUser(loginCallbackInfo.ContinuanceToken);
			if (createUserCallbackInfo.ResultCode != Epic.OnlineServices.Result.Success)
			{
				return createUserCallbackInfo.ResultCode;
			}
			return (await ConnectLoginViaAuth()).ResultCode;
		}

		private Task<CreateUserCallbackInfo> ConnectCreateProductUser(ContinuanceToken continuanceToken, Action<CreateUserCallbackInfo> callback = null)
		{
			ConnectInterface connectInterface = EOSPlatformInterface.GetConnectInterface();
			TaskCompletionSource<CreateUserCallbackInfo> tcs = new TaskCompletionSource<CreateUserCallbackInfo>();
			CreateUserOptions options = new CreateUserOptions
			{
				ContinuanceToken = continuanceToken
			};
			connectInterface.CreateUser(ref options, null, delegate(ref CreateUserCallbackInfo info)
			{
				if (Common.IsOperationComplete(info.ResultCode))
				{
					tcs.SetResult(info);
				}
				callback?.Invoke(info);
			});
			return tcs.Task;
		}

		private void StartRefreshingConnectLogin()
		{
			if (connectLoginRefreshingHandle == 0L)
			{
				ConnectInterface connectInterface = EOSPlatformInterface.GetConnectInterface();
				AddNotifyAuthExpirationOptions options = default(AddNotifyAuthExpirationOptions);
				connectLoginRefreshingHandle = connectInterface.AddNotifyAuthExpiration(ref options, null, delegate(ref AuthExpirationCallbackInfo info)
				{
					PerformConnectLoginRefresh(info);
				});
			}
		}

		private async void PerformConnectLoginRefresh(AuthExpirationCallbackInfo info)
		{
			if (info.LocalUserId != ProductUserId || m_loginStatus != LoginStatus.LoggedIn)
			{
				return;
			}
			if (EpicAccountId == null)
			{
				Log("EOSManager", LogLevel.Error, "Connect login refresh failed: There is no logged in Epic Account anymore.");
				return;
			}
			Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo = await ConnectLoginViaAuth();
			if (loginCallbackInfo.ResultCode != Epic.OnlineServices.Result.Success)
			{
				Log("EOSManager", LogLevel.Error, $"Connect login refresh failed: {loginCallbackInfo.ResultCode}");
			}
		}

		private Epic.OnlineServices.Result CreateSteamIntegratedPlatform(ref Epic.OnlineServices.Platform.Options platformOptions, out IntegratedPlatformOptionsContainer container)
		{
			CreateIntegratedPlatformOptionsContainerOptions options = default(CreateIntegratedPlatformOptionsContainerOptions);
			IntegratedPlatformOptionsContainer outIntegratedPlatformOptionsContainerHandle;
			Epic.OnlineServices.Result result = IntegratedPlatformInterface.CreateIntegratedPlatformOptionsContainer(ref options, out outIntegratedPlatformOptionsContainerHandle);
			if (result != Epic.OnlineServices.Result.Success)
			{
				container = null;
				return result;
			}
			Utf8String overrideLibraryPath = new Utf8String(Application.dataPath + "/Plugins/x86_64/steam_api64.dll");
			SteamOptions other = new SteamOptions
			{
				OverrideLibraryPath = overrideLibraryPath,
				SteamMajorVersion = 1u,
				SteamMinorVersion = 57u
			};
			SteamOptionsInternal structure = default(SteamOptionsInternal);
			structure.Set(ref other);
			IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
			Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
			Epic.OnlineServices.IntegratedPlatform.Options value = new Epic.OnlineServices.IntegratedPlatform.Options
			{
				Type = IntegratedPlatformInterface.IptSteam,
				Flags = (IntegratedPlatformManagementFlags.LibraryManagedBySDK | IntegratedPlatformManagementFlags.PreferEOSIdentity),
				InitOptions = intPtr
			};
			IntegratedPlatformOptionsContainerAddOptions inOptions = new IntegratedPlatformOptionsContainerAddOptions
			{
				Options = value
			};
			platformOptions.IntegratedPlatformOptionsContainerHandle = outIntegratedPlatformOptionsContainerHandle;
			container = outIntegratedPlatformOptionsContainerHandle;
			return platformOptions.IntegratedPlatformOptionsContainerHandle.Add(ref inOptions);
		}

		public void RejectInvite(Utf8String invite, Action<Epic.OnlineServices.Lobby.RejectInviteCallbackInfo> rejectCallback = null)
		{
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			Epic.OnlineServices.Lobby.RejectInviteOptions options = new Epic.OnlineServices.Lobby.RejectInviteOptions
			{
				LocalUserId = ProductUserId,
				InviteId = invite
			};
			lobbyInterface.RejectInvite(ref options, null, delegate(ref Epic.OnlineServices.Lobby.RejectInviteCallbackInfo info)
			{
				rejectCallback?.Invoke(info);
			});
		}

		public void SubscribeUILobbySendInvite(Action<LobbyInviteReceivedCallbackInfo> inviteReceivedCallback = null)
		{
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			AddNotifyLobbyInviteReceivedOptions options = default(AddNotifyLobbyInviteReceivedOptions);
			lobbyInterface.AddNotifyLobbyInviteReceived(ref options, null, delegate(ref LobbyInviteReceivedCallbackInfo data)
			{
				if (data.GetResultCode() == Epic.OnlineServices.Result.Success)
				{
					inviteReceivedCallback?.Invoke(data);
				}
			});
		}

		public void SubscribeUIInviteAcceptedLobby(Action<Utf8String> lobbyConnectCallback)
		{
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			AddNotifyJoinLobbyAcceptedOptions options = default(AddNotifyJoinLobbyAcceptedOptions);
			lobbyInterface.AddNotifyJoinLobbyAccepted(ref options, null, delegate(ref JoinLobbyAcceptedCallbackInfo data)
			{
				CopyLobbyDetailsHandleByUiEventIdOptions options3 = new CopyLobbyDetailsHandleByUiEventIdOptions
				{
					UiEventId = data.UiEventId
				};
				lobbyInterface.CopyLobbyDetailsHandleByUiEventId(ref options3, out var outLobbyDetailsHandle);
				LobbyDetailsCopyInfoOptions options4 = default(LobbyDetailsCopyInfoOptions);
				outLobbyDetailsHandle.CopyInfo(ref options4, out var outLobbyDetailsInfo);
				if (outLobbyDetailsInfo.HasValue)
				{
					LobbyDetailsInfo value = outLobbyDetailsInfo.Value;
					if (value.AllowJoinById && value.AllowInvites && outLobbyDetailsInfo.Value.AvailableSlots != 0)
					{
						lobbyConnectCallback?.Invoke(outLobbyDetailsInfo.Value.LobbyId);
					}
					else
					{
						Log("EOSManager", LogLevel.Warning, "No possible connect to lobby");
					}
				}
				else
				{
					Log("EOSManager", LogLevel.Error, "Details lobby connect empty!");
				}
				outLobbyDetailsHandle.Release();
			});
			AddNotifyLobbyInviteAcceptedOptions options2 = default(AddNotifyLobbyInviteAcceptedOptions);
			lobbyInterface.AddNotifyLobbyInviteAccepted(ref options2, null, delegate(ref LobbyInviteAcceptedCallbackInfo data)
			{
				lobbyConnectCallback?.Invoke(data.LobbyId);
			});
		}

		public void SubscribeUILeaveLobby(Action<Utf8String> lobbyLeaveCallback)
		{
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			AddNotifyLeaveLobbyRequestedOptions options = default(AddNotifyLeaveLobbyRequestedOptions);
			lobbyInterface.AddNotifyLeaveLobbyRequested(ref options, null, delegate(ref LeaveLobbyRequestedCallbackInfo info)
			{
				lobbyLeaveCallback?.Invoke(info.LobbyId);
			});
		}

		public void SendInviteInLobbyToPlayer(EpicAccountId targetPlayer, Utf8String lobbyId, Action<Epic.OnlineServices.Lobby.SendInviteCallbackInfo> callback = null)
		{
			LobbyInterface connect = EOSPlatformInterface.GetLobbyInterface();
			GetProductUserIdFromEpicAccountId(targetPlayer, delegate(ProductUserId otherUserProductId)
			{
				Epic.OnlineServices.Lobby.SendInviteOptions options = new Epic.OnlineServices.Lobby.SendInviteOptions
				{
					LobbyId = lobbyId,
					LocalUserId = ProductUserId,
					TargetUserId = otherUserProductId
				};
				connect.SendInvite(ref options, null, delegate(ref Epic.OnlineServices.Lobby.SendInviteCallbackInfo data)
				{
					if (Common.IsOperationComplete(data.ResultCode))
					{
						callback?.Invoke(data);
					}
				});
			});
		}

		private void GetProductUserIdFromEpicAccountId(EpicAccountId epicAccountId, Action<ProductUserId> callback)
		{
			ConnectInterface connectInterface = EOSPlatformInterface.GetConnectInterface();
			Utf8String[] externalAccountIds = new Utf8String[1] { epicAccountId.ToString() };
			QueryExternalAccountMappingsOptions options = new QueryExternalAccountMappingsOptions
			{
				AccountIdType = ExternalAccountType.Epic,
				LocalUserId = ProductUserId,
				ExternalAccountIds = externalAccountIds
			};
			connectInterface.QueryExternalAccountMappings(ref options, null, delegate(ref QueryExternalAccountMappingsCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						GetExternalAccountMappingsOptions options2 = new GetExternalAccountMappingsOptions
						{
							AccountIdType = ExternalAccountType.Epic,
							LocalUserId = ProductUserId,
							TargetExternalUserId = epicAccountId.ToString()
						};
						ProductUserId externalAccountMapping = connectInterface.GetExternalAccountMapping(ref options2);
						callback(externalAccountMapping);
					}
					else
					{
						Log("EOSManager", LogLevel.Error, "Get epic user product id" + data.ResultCode);
					}
				}
			});
		}

		public void TryConnectOrCreateLobby(CreateLobbyOptions createLobbyOptions, Action<CreateLobbyCallbackInfo> callback = null)
		{
			if (EpicAccountId == null)
			{
				return;
			}
			JoinLobby(createLobbyOptions.LobbyId, delegate(JoinLobbyByIdCallbackInfo info)
			{
				if (info.ResultCode != Epic.OnlineServices.Result.Success)
				{
					LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
					createLobbyOptions.LocalUserId = ProductUserId;
					lobbyInterface.CreateLobby(ref createLobbyOptions, null, delegate(ref CreateLobbyCallbackInfo data)
					{
						if (Common.IsOperationComplete(data.ResultCode))
						{
							callback?.Invoke(data);
						}
					});
				}
			});
		}

		private void JoinLobby(Utf8String lobbyId, Action<JoinLobbyByIdCallbackInfo> callback = null)
		{
			if (EpicAccountId == null)
			{
				return;
			}
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
			{
				LocalUserId = ProductUserId,
				LobbyId = lobbyId,
				PresenceEnabled = true,
				CrossplayOptOut = true
			};
			lobbyInterface.JoinLobbyById(ref options, null, delegate(ref JoinLobbyByIdCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					callback?.Invoke(data);
				}
			});
		}

		public void LeaveLobby(Utf8String lobbyId, Action<LeaveLobbyCallbackInfo> callback = null)
		{
			LobbyInterface lobbyInterface = EOSPlatformInterface.GetLobbyInterface();
			LeaveLobbyOptions options = new LeaveLobbyOptions
			{
				LocalUserId = ProductUserId,
				LobbyId = lobbyId
			};
			lobbyInterface.LeaveLobby(ref options, null, delegate(ref LeaveLobbyCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					callback?.Invoke(data);
				}
			});
		}

		public void QueryStats(Action callback = null)
		{
			StatsInterface statsInterface = EOSPlatformInterface.GetStatsInterface();
			QueryStatsOptions options = new QueryStatsOptions
			{
				TargetUserId = ProductUserId,
				LocalUserId = ProductUserId
			};
			statsInterface.QueryStats(ref options, null, delegate(ref OnQueryStatsCompleteCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode != Epic.OnlineServices.Result.Success)
					{
						Log("EOSManager", LogLevel.Warning, $"Query stat failed: {data.ResultCode}");
					}
					else
					{
						callback?.Invoke();
					}
				}
			});
		}

		public bool TryGetStat(Utf8String keyStat, out int stats)
		{
			StatsInterface statsInterface = EOSPlatformInterface.GetStatsInterface();
			CopyStatByNameOptions options = new CopyStatByNameOptions
			{
				TargetUserId = ProductUserId,
				Name = keyStat
			};
			statsInterface.CopyStatByName(ref options, out var outStat);
			if (outStat.HasValue)
			{
				stats = outStat.Value.Value;
				return true;
			}
			stats = 0;
			return false;
		}

		public void IngestStat(Utf8String keyStat, int statValue, Action callback)
		{
			StatsInterface statsInterface = EOSPlatformInterface.GetStatsInterface();
			IngestData ingestData = new IngestData
			{
				IngestAmount = statValue,
				StatName = keyStat
			};
			IngestStatOptions options = new IngestStatOptions
			{
				LocalUserId = ProductUserId,
				TargetUserId = ProductUserId,
				Stats = new IngestData[1] { ingestData }
			};
			options.LocalUserId = ProductUserId;
			statsInterface.IngestStat(ref options, null, delegate(ref IngestStatCompleteCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode != Epic.OnlineServices.Result.Success)
					{
						Log("EOSManager", LogLevel.Warning, $"Set stat failed: {data.ResultCode}");
					}
					else if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						callback?.Invoke();
					}
				}
			});
		}

		public NetworkStatus GetNetworkStatus()
		{
			if (EOSPlatformInterface == null || EOSPlatformInterface.InnerHandle == IntPtr.Zero)
			{
				return NetworkStatus.Offline;
			}
			return EOSPlatformInterface.GetNetworkStatus();
		}

		public async Task<UserInfoData?> GetPlayerInfo(EpicAccountId player, float timeOut = 20f)
		{
			UserInfoInterface statsInterface = EOSPlatformInterface.GetUserInfoInterface();
			UserInfoData? result = null;
			QueryUserInfoOptions options = new QueryUserInfoOptions
			{
				LocalUserId = EpicAccountId,
				TargetUserId = player
			};
			statsInterface.QueryUserInfo(ref options, null, delegate(ref QueryUserInfoCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode != Epic.OnlineServices.Result.Success)
					{
						Log("EOSManager", LogLevel.Warning, $"Set stat failed: {data.ResultCode}");
					}
					else
					{
						CopyUserInfoOptions options2 = new CopyUserInfoOptions
						{
							LocalUserId = EpicAccountId,
							TargetUserId = player
						};
						statsInterface.CopyUserInfo(ref options2, out var outUserInfo);
						result = outUserInfo;
					}
				}
			});
			float time = 0f;
			while (!result.HasValue && timeOut > time)
			{
				await Task.Delay(1);
				time += 0.001f;
			}
			return result;
		}

		public async Task<EpicAccountId[]> FetchFriends(float timeOut)
		{
			FriendsInterface statsInterface = EOSPlatformInterface.GetFriendsInterface();
			EpicAccountId[] friend = Array.Empty<EpicAccountId>();
			QueryFriendsOptions options = new QueryFriendsOptions
			{
				LocalUserId = EpicAccountId
			};
			bool complete = false;
			statsInterface.QueryFriends(ref options, null, delegate(ref QueryFriendsCallbackInfo data)
			{
				if (!Common.IsOperationComplete(data.ResultCode))
				{
					complete = true;
				}
				else
				{
					if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						GetFriendsCountOptions options2 = new GetFriendsCountOptions
						{
							LocalUserId = EpicAccountId
						};
						int friendsCount = statsInterface.GetFriendsCount(ref options2);
						GetFriendAtIndexOptions options3 = new GetFriendAtIndexOptions
						{
							LocalUserId = EpicAccountId
						};
						friend = new EpicAccountId[friendsCount];
						options3.Index = 0;
						while (options3.Index < friendsCount)
						{
							friend[options3.Index] = statsInterface.GetFriendAtIndex(ref options3);
							options3.Index++;
						}
					}
					complete = true;
				}
			});
			float time = 0f;
			while (!complete && timeOut > time)
			{
				await Task.Delay(1);
				time += 0.001f;
			}
			return friend;
		}

		public async Task<Epic.OnlineServices.Auth.IdToken?> GetAuthToken(float timeOut)
		{
			AuthInterface statsInterface = EOSPlatformInterface.GetAuthInterface();
			Epic.OnlineServices.Auth.IdToken? token = null;
			Epic.OnlineServices.Result? result = null;
			QueryIdTokenOptions options = new QueryIdTokenOptions
			{
				LocalUserId = EpicAccountId,
				TargetAccountId = EpicAccountId
			};
			statsInterface.QueryIdToken(ref options, null, delegate(ref QueryIdTokenCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						Epic.OnlineServices.Auth.CopyIdTokenOptions options2 = new Epic.OnlineServices.Auth.CopyIdTokenOptions
						{
							AccountId = EpicAccountId
						};
						statsInterface.CopyIdToken(ref options2, out var outIdToken);
						if (outIdToken.HasValue)
						{
							token = outIdToken.Value;
							result = Epic.OnlineServices.Result.Success;
							return;
						}
					}
					result = Epic.OnlineServices.Result.Disabled;
				}
			});
			float time = 0f;
			while (!result.HasValue && timeOut > time)
			{
				await Task.Delay(1);
				time += 0.001f;
			}
			return token;
		}

		public void QueryOfferItemsCatalog(Action<CatalogItem[]> callback)
		{
			EcomInterface eInterface = EOSPlatformInterface.GetEcomInterface();
			QueryOffersOptions options = new QueryOffersOptions
			{
				LocalUserId = EpicAccountId,
				OverrideCatalogNamespace = null
			};
			eInterface.QueryOffers(ref options, null, delegate(ref QueryOffersCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						GetOfferCountOptions options2 = new GetOfferCountOptions
						{
							LocalUserId = EpicAccountId
						};
						uint offerCount = eInterface.GetOfferCount(ref options2);
						List<CatalogItem> list = new List<CatalogItem>();
						for (uint num = 0u; num < offerCount; num++)
						{
							CopyOfferByIndexOptions options3 = new CopyOfferByIndexOptions
							{
								LocalUserId = EpicAccountId,
								OfferIndex = num
							};
							eInterface.CopyOfferByIndex(ref options3, out var outOffer);
							if (outOffer.HasValue)
							{
								GetOfferItemCountOptions options4 = new GetOfferItemCountOptions
								{
									LocalUserId = EpicAccountId,
									OfferId = outOffer.Value.Id
								};
								uint offerItemCount = eInterface.GetOfferItemCount(ref options4);
								for (uint num2 = 0u; num2 < offerItemCount; num2++)
								{
									CopyOfferItemByIndexOptions options5 = new CopyOfferItemByIndexOptions
									{
										LocalUserId = EpicAccountId,
										ItemIndex = num2,
										OfferId = outOffer.Value.Id
									};
									eInterface.CopyOfferItemByIndex(ref options5, out var outItem);
									if (outItem.HasValue)
									{
										list.Add(outItem.Value);
									}
								}
							}
						}
						callback?.Invoke(list.ToArray());
					}
					else
					{
						Log("EOSManager", LogLevel.Warning, $"Query offers failed: {data.ResultCode}");
					}
				}
			});
		}

		public Utf8String[] TranslateCatalogItemsToCatalogItemIds(CatalogItem[] catalogOffers)
		{
			return SelectItems(catalogOffers, (CatalogItem offer) => offer.Id);
		}

		public void QueryOwnership(Utf8String[] catalogItemIds, Action<ItemOwnership[]> callback)
		{
			EcomInterface ecomInterface = EOSPlatformInterface.GetEcomInterface();
			QueryOwnershipOptions options = new QueryOwnershipOptions
			{
				LocalUserId = EpicAccountId,
				CatalogItemIds = catalogItemIds
			};
			ecomInterface.QueryOwnership(ref options, null, delegate(ref QueryOwnershipCallbackInfo data)
			{
				if (Common.IsOperationComplete(data.ResultCode))
				{
					if (data.ResultCode == Epic.OnlineServices.Result.Success)
					{
						callback?.Invoke(data.ItemOwnership);
					}
					else
					{
						Log("EOSManager", LogLevel.Warning, $"Query ownership failed: {data.ResultCode}");
					}
				}
			});
		}

		private void StartRefreshingAuthLogin()
		{
			if (authLoginRefreshingHandle != 0L)
			{
				return;
			}
			AuthInterface authInterface = EOSPlatformInterface.GetAuthInterface();
			Epic.OnlineServices.Auth.AddNotifyLoginStatusChangedOptions options = default(Epic.OnlineServices.Auth.AddNotifyLoginStatusChangedOptions);
			authLoginRefreshingHandle = authInterface.AddNotifyLoginStatusChanged(ref options, null, delegate(ref Epic.OnlineServices.Auth.LoginStatusChangedCallbackInfo data)
			{
				if (data.PrevStatus == LoginStatus.LoggedIn && data.CurrentStatus != LoginStatus.LoggedIn)
				{
					TryingReconnectToEpic();
				}
				m_loginStatus = data.CurrentStatus;
			});
		}

		private async void TryingReconnectToEpic()
		{
			Task<Epic.OnlineServices.Auth.LoginCallbackInfo> task = ((!IsLaunchedViaEpicLauncher()) ? instance.AuthLogin() : instance.AuthLoginViaEpicLauncher());
			await task;
			await instance.ConnectLoginOrRegister();
		}

		public void SubscribeUIChange(Action<OnDisplaySettingsUpdatedCallbackInfo> callback)
		{
			UIInterface uIInterface = EOSPlatformInterface.GetUIInterface();
			AddNotifyDisplaySettingsUpdatedOptions options = default(AddNotifyDisplaySettingsUpdatedOptions);
			uIInterface.AddNotifyDisplaySettingsUpdated(ref options, null, delegate(ref OnDisplaySettingsUpdatedCallbackInfo data)
			{
				callback?.Invoke(data);
			});
		}

		public void HideUI(Action<Epic.OnlineServices.Result> callback = null)
		{
			UIInterface uIInterface = EOSPlatformInterface.GetUIInterface();
			HideFriendsOptions options = new HideFriendsOptions
			{
				LocalUserId = EpicAccountId
			};
			uIInterface.HideFriends(ref options, null, delegate(ref HideFriendsCallbackInfo data)
			{
				callback?.Invoke(data.ResultCode);
			});
		}

		private void Log(string category, LogLevel level, string message)
		{
			LogMessage message2 = new LogMessage
			{
				Level = level,
				Category = category,
				Message = message
			};
			m_logMessageCallback?.Invoke(ref message2);
		}
	}
}
