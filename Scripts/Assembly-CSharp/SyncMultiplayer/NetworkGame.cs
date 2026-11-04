using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using HUD;
using Services.UGC.Utils;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using SyncMultiplayer.NetGameSystems;
using SyncMultiplayer.NetGameSystems.Collisions;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters;
using TrackLayout.Data;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetworkGame
	{
		public struct TextureNetworkData
		{
			public byte[] textureData;

			public int textureSize;

			public int textureFormat;
		}

		public const int MaxPlayersPerRoomCount = 16;

		public static string USER_CREATED_ROOM_NAME = null;

		public Action<List<string>> OnGameListUpdate;

		private RaceMode m_raceMode = RaceMode.NetworkFreeride;

		private string m_ErrorMessageToShow;

		private SmartfoxLobby m_lobby;

		private SmartfoxRoomClient m_client;

		private Dictionary<string, Action<ISFSObject>> m_serverCommandsHandler = new Dictionary<string, Action<ISFSObject>>();

		private LocalCarLiveryCaptureHelper m_textureCaptureHelper = new LocalCarLiveryCaptureHelper();

		private NetworkLOD m_networkLOD;

		private NetworkGameStats m_stats = new NetworkGameStats();

		private bool m_firstRoomEnter;

		private bool m_isInRoom;

		public SmartfoxRoomOptions lastRoomOptions = new SmartfoxRoomOptions();

		public bool isLastRoomPassword;

		public List<NetworkPlayer> Players = new List<NetworkPlayer>();

		public static HashSet<string> reportedByVinylsPlayers = new HashSet<string>();

		private NetworkPlayer m_playerRoomCreator;

		private Dictionary<int, NetworkPlayer> m_playersByNetworkId = new Dictionary<int, NetworkPlayer>();

		private HashSet<PlayerId> m_invitedFriends = new HashSet<PlayerId>();

		private UserPropsUpdateManager m_userPropsUpdateManager;

		private RoomMessageUpdateManager m_roomMessageUpdateManager;

		private static HashSet<int> m_allowedTracks = new HashSet<int>();

		private List<string> m_cachedRoomList = new List<string>();

		public SmartfoxLobby lobby => m_lobby;

		public SmartfoxRoomClient Client => m_client;

		public LobbyFilterState lobbyFilterState { get; set; } = new LobbyFilterState();

		public CarLiveryUploadService carLiveryUploadService { get; private set; } = new CarLiveryUploadService();

		public MasterPacketsHandlerSystem packetHandler { get; }

		public NetGameSystemsManager systems { get; }

		public BaseModel baseModel { get; }

		public static GamePrefs prefs { get; private set; }

		public NetworkGameStats stats => m_stats;

		public NetworkPlayer playerRoomCreator
		{
			get
			{
				if (m_playerRoomCreator == null || !m_playerRoomCreator.PlayerId.ToString().ToLower().Contains(lastRoomOptions.RoomCreatorId.ToLower()))
				{
					m_playerRoomCreator = TryGetRoomCreatorFromPlayerList();
				}
				return m_playerRoomCreator;
			}
		}

		public string roomCreatorName { get; private set; }

		public bool playerRoomCreatorInRoom => TryGetRoomCreatorFromPlayerList() != null;

		public NetworkPlayer MasterPlayer { get; private set; }

		public NetworkPlayer LocalPlayer { get; private set; }

		public Room CurrentRoom
		{
			get
			{
				if (m_client == null)
				{
					return null;
				}
				return m_client.LastJoinedRoom;
			}
		}

		public SmartfoxRoomOptions CurrentRoomOptions
		{
			get
			{
				if (m_client == null)
				{
					return null;
				}
				return m_client.RoomOptions;
			}
		}

		public bool isRestrictCrossMpByAdmin
		{
			get
			{
				if (m_lobby != null)
				{
					return m_lobby.RestrictCrossMpByAdmin;
				}
				return false;
			}
		}

		public long restrictCrossMpByAdminTimeEnd
		{
			get
			{
				if (m_lobby != null)
				{
					return m_lobby.RestrictCrossMpByAdminTimeEnd;
				}
				return 0L;
			}
		}

		public bool isRestrictAcessMpByAdmin
		{
			get
			{
				if (m_lobby != null)
				{
					return m_lobby.RestrictAcessMpByAdmin;
				}
				return false;
			}
		}

		public long restrictAcessMpByAdminTimeEnd
		{
			get
			{
				if (m_lobby != null)
				{
					return m_lobby.RestrictAcessMpByAdminTimeEnd;
				}
				return 0L;
			}
		}

		public ClientState State
		{
			get
			{
				if (m_lobby != null)
				{
					return m_lobby.State;
				}
				if (m_client == null)
				{
					return ClientState.Uninitialized;
				}
				return m_client.State;
			}
		}

		public ClientState LobbyState
		{
			get
			{
				if (m_lobby == null)
				{
					return ClientState.Uninitialized;
				}
				return m_lobby.State;
			}
		}

		public int MasterClientId
		{
			get
			{
				if (MasterPlayer != null)
				{
					return MasterPlayer.NetworkID;
				}
				return -1;
			}
		}

		public static event Action onLeaveRoomEvent;

		public static event Action onEnterRoomEvent;

		public NetworkPlayer FindPlayer(PlayerId playerId)
		{
			string uid = playerId.accountId.ToString().ToLower();
			return Players.Find((NetworkPlayer x) => x.PlayerId.accountId.ToString().ToLower().Contains(uid));
		}

		public NetworkPlayer FindPlayerByUID(string uid)
		{
			return Players.Find((NetworkPlayer x) => x.PlayerId.accountId.ToString() == uid);
		}

		public NetworkPlayer FindPlayerByNetworkId(int id)
		{
			m_playersByNetworkId.TryGetValue(id, out var value);
			return value;
		}

		public bool IsLocalPlayerRoomCreator()
		{
			if (lastRoomOptions != null)
			{
				return Overlay.instance.player.id.ToString().ToLower().Contains(lastRoomOptions.RoomCreatorId.ToLower());
			}
			return true;
		}

		public NetworkGame()
		{
			prefs = DependencyInjector.Resolve<GamePrefs>();
			baseModel = DependencyInjector.Resolve<BaseModel>();
			m_networkLOD = new NetworkLOD(this);
			LocalPlayer = new NetworkPlayer(this, isLocal: false, new SmartfoxPlayerProperties
			{
				PlayerID = prefs.playerProfile.id
			});
			LocalPlayer.NetworkID = -1;
			packetHandler = MasterPacketsHandlerSystem.GetInstance();
			systems = new NetGameSystemsManager(this)
			{
				new NetGameSyncTimeSystem(),
				new NetGameSyncSupport(),
				new NetGameSyncPosSystem(),
				new NetGameSyncPointsSystem(),
				new NetGameSyncCarLightsSystem(),
				new NetGameSubroomsSystem(),
				new NetGameCollisionManager(),
				new NetGameServerVoteSystem(),
				new DataSyncManager(),
				new NetRoomPlayersListSystem(),
				new NetPlayerInputSystem(),
				new NetGameIgnoreSystem(),
				new NetGameTextureTransferSystem(),
				new NetGameSyncObjectSystem(),
				new NetGameNotifySystem(),
				new NetGameCarSwapSystem(),
				new NetGameDynoSystem(),
				new NetGamePlayerStateSystem()
			}.AttachSystems();
			GameConsole.SetCmd("net_test", delegate
			{
				NetworkController.CreateRoom(new RoomSettings(15, 0uL, "", "Dev room", "Dev track", 15, 0, nightMode: false, 0, crossPlay: true, string.Empty, 0, 0, 0, 0, 0, 0, 0));
			}, GameConsole.Flags.Debug);
			GameEvents.OnUGCCarReady += new GameEventHandler<GameEvents.UGCReadyInfo>(UGCReadyHandle);
		}

		public void Connect()
		{
			Disconnect();
			FillLocalPlayerData();
			m_lobby = new SmartfoxLobby(LocalPlayer.PlayerId.ToString(), LocalPlayer.PlayerProperties);
			m_lobby.JoinedLobby += OnJoinedLobby;
			m_lobby.RoomAdded += OnRoomAdded;
			m_lobby.RoomUpdated += OnRoomUpdated;
			m_lobby.RoomRemoved += OnRoomRemoved;
			m_lobby.JoinedRoom += OnJoinedRoom;
			m_lobby.JoinRoomError += OnRoomJoinedError;
			m_lobby.CreateRoomError += OnCreateRoomError;
			m_lobby.AuthenticationError += OnAuthenticationError;
			m_lobby.Connect();
		}

		public void Disconnect()
		{
			if (m_lobby != null)
			{
				m_lobby.JoinedLobby -= OnJoinedLobby;
				m_lobby.RoomAdded -= OnRoomAdded;
				m_lobby.RoomUpdated -= OnRoomUpdated;
				m_lobby.RoomRemoved -= OnRoomRemoved;
				m_lobby.JoinedRoom -= OnJoinedRoom;
				m_lobby.JoinRoomError -= OnRoomJoinedError;
				m_lobby.CreateRoomError -= OnCreateRoomError;
				m_lobby.AuthenticationError -= OnAuthenticationError;
				m_lobby.DisconnectFromNodesExcept(null);
				m_lobby = null;
				NetworkGame.onLeaveRoomEvent.Call();
			}
			if (m_client != null)
			{
				NetworkGame.onLeaveRoomEvent.Call();
				m_client.InternalError -= OnInternalError;
				m_client.AuthenticationError -= OnAuthenticationError;
				m_client.JoinedRoom -= OnJoinedRoom;
				m_client.RoomOptionsChanged -= OnRoomOptionsChanged;
				m_client.UserVariablesUpdate -= OnUserVariablesUpdate;
				m_client.JoinRoomError -= OnRoomJoinedError;
				m_client.StateChanged -= OnStateChanged;
				m_client.PlayerJoined -= OnPlayerJoined;
				m_client.PlayerLeft -= OnPlayerLeft;
				m_client.RoomMessage -= OnRoomMessage;
				m_client.ServerCommand -= OnServerMessage;
				m_client.SocketError -= OnSocketError;
				m_client.Disconnect();
				m_client.Destroy();
				m_client = null;
			}
			m_userPropsUpdateManager?.Dispose();
			m_roomMessageUpdateManager?.Dispose();
		}

		private void OnInternalError(string message)
		{
			m_ErrorMessageToShow = "Internal error " + message;
			DebugReturn(m_ErrorMessageToShow);
		}

		public SmartfoxLobby.RoomLobbyInfo FindInUnfilteredRooms(string roomIdOrName)
		{
			if (m_lobby != null)
			{
				SmartfoxLobby.RoomLobbyInfo roomLobbyInfo = m_lobby.UnfilteredRooms.Find((SmartfoxLobby.RoomLobbyInfo r) => r.options.Name == (ObscuredString)roomIdOrName);
				if (roomLobbyInfo == null)
				{
					roomIdOrName = roomIdOrName.ToLower();
					roomLobbyInfo = m_lobby.UnfilteredRooms.Find((SmartfoxLobby.RoomLobbyInfo r) => r.options.NotFilteredCaption.ToLower() == roomIdOrName);
				}
				return roomLobbyInfo;
			}
			return null;
		}

		public void FindRoomLobbyInfoRequest(string roomID, Action<SmartfoxLobby.RoomLobbyInfo> callback)
		{
			m_lobby.FindRoomLobbyInfoRequest(roomID, callback);
		}

		public void RestoreFilteredRoomToNormalList(SmartfoxLobby.RoomLobbyInfo roomLobbyInfo)
		{
			if (m_lobby != null)
			{
				m_lobby.UnfilteredRooms.RemoveAll((SmartfoxLobby.RoomLobbyInfo x) => x.options.Name == roomLobbyInfo.options.Name);
				if (m_lobby.Rooms.Find((SmartfoxLobby.RoomLobbyInfo x) => x.options.Name == roomLobbyInfo.options.Name) == null)
				{
					m_lobby.Rooms.Add(roomLobbyInfo);
				}
			}
		}

		public void ShowAdminRestrictAccessError(long timeEnd, LocalizationKeys.Key localizedFormatkey = null, bool isSeconds = true)
		{
			if (m_lobby != null)
			{
				m_lobby.ShowAdminRestrictAccessError(timeEnd, localizedFormatkey, isSeconds);
			}
		}

		public List<SmartfoxLobby.RoomLobbyInfo> GetUnfilteredRooms()
		{
			if (m_lobby == null)
			{
				return new List<SmartfoxLobby.RoomLobbyInfo>();
			}
			return m_lobby.UnfilteredRooms;
		}

		public List<SmartfoxLobby.RoomLobbyInfo> GetRooms()
		{
			if (m_lobby == null)
			{
				return new List<SmartfoxLobby.RoomLobbyInfo>();
			}
			return m_lobby.Rooms;
		}

		private void OnJoinedLobby()
		{
			OnGameListUpdate.Call(null);
		}

		private void OnAuthenticationError(short code, string message)
		{
			m_ErrorMessageToShow = "Authentication error: " + code + " " + message;
			DebugReturn(m_ErrorMessageToShow);
			GameSync.RaiseTokenDepletedState();
			GameSync.ForceAuthorizeTime();
		}

		private void OnJoinedRoom(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			OnJoinedRoomAsync(client, room).Forget();
		}

		private async UniTask OnJoinedRoomAsync(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			if (m_client == null || m_client.CurrentRoom != null)
			{
				m_client = client;
				m_client.OnJoinConfirmed();
				CurrentRoomOptions.FilterCaption();
				InitializeClientAfterEnterRoom();
				ReleaseRoom();
				await RoomFound();
				LoadRace();
				NetworkGame.onEnterRoomEvent?.Invoke();
			}
		}

		private void OnRoomJoinedError(short code, string message)
		{
			if (!(States.mainCurrent as SyncNetFreerideRaceModeState) || code != 22)
			{
				if (code == 504)
				{
					NetworkGame.onLeaveRoomEvent.Call();
				}
				ReplaceRoomnameIfNeed(ref message);
				UILockInputContext.DisableLock();
				GUICommonGodVoice.ShowText("Join room error: " + code + " " + message);
				NetworkController.TryReturnToGarageFromJoinRoomError();
			}
		}

		private void OnCreateRoomError(Error.EType code, string message)
		{
			ReplaceRoomnameIfNeed(ref message);
			UILockInputContext.DisableLock();
			if (!string.IsNullOrEmpty(message) && message.Contains("[CREATE_ROOM]"))
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_ban_self.Localized());
			}
			else
			{
				GUICommonGodVoice.ShowText("Create room error: " + code.ToString() + " " + message);
			}
		}

		public void SetRoomCreatorName(string creatorName)
		{
			roomCreatorName = creatorName;
		}

		public async UniTask CaptureAndUploadCarLivery()
		{
			RaceCar raceCar = null;
			BaseState mainCurrent = States.mainCurrent;
			if (mainCurrent is GarageGUIState { contextOwner: var contextOwner })
			{
				if (contextOwner == null)
				{
					return;
				}
				GarageCar3dViewContext garageCar3dViewContext = contextOwner.Get<GarageCar3dViewContext>();
				if (!garageCar3dViewContext)
				{
					return;
				}
				raceCar = garageCar3dViewContext.currentCar;
			}
			else if (mainCurrent is BaseRaceState baseRaceState)
			{
				raceCar = baseRaceState.userCar;
			}
			if ((bool)raceCar)
			{
				await CaptureLocalCarTextures(raceCar);
				await UploadTexture();
			}
		}

		public async UniTask CaptureLocalCarTextures(RaceCar userCar)
		{
			if (!(userCar == null) && userCar.carModel != null)
			{
				await m_textureCaptureHelper.CaptureLocalCarLivery(userCar);
			}
		}

		private async UniTask UploadTexture()
		{
			if (m_textureCaptureHelper.liveryNetworkDataReadyToUpload)
			{
				byte[] liveryNetworkData = m_textureCaptureHelper.liveryNetworkData;
				string carLiveryHash = m_textureCaptureHelper.carLiveryHash;
				await carLiveryUploadService.UploadCarLiveryIfNeed(carLiveryHash, liveryNetworkData);
			}
		}

		public void OnLocalPlayerPlacedOnTrack()
		{
			LocalPlayer.spawnGeneration++;
			NetworkController.InstanceGame.systems.Get<NetGameCollisionManager>().PlayerSpawned(LocalPlayer);
		}

		private void ReplaceRoomnameIfNeed(ref string message)
		{
			foreach (SmartfoxLobby.RoomLobbyInfo room in GetRooms())
			{
				if (room != null && room.options != null && !string.IsNullOrEmpty(message) && message.Contains(room.options.Name))
				{
					message = message.Replace(room.options.Name, room.options.Caption);
				}
			}
		}

		private void UGCReadyHandle(GameEvents.UGCReadyInfo info)
		{
			foreach (NetworkPlayer player in Players)
			{
				if (player.PlayerProperties.VisualSettings.networkparts.ContainsKey(info.packageId) && PlayerHasUGCCar(player))
				{
					player.ugcCarReadyForConstruction = true;
				}
			}
		}

		public bool PlayerHasUGCCar(NetworkPlayer player)
		{
			CarVisualManager.VisualSettings visualSettings = player.PlayerProperties.VisualSettings;
			int count = visualSettings.networkparts.Count;
			player.PlayerProperties.VisualSettings = visualSettings.MutateToLocalParts();
			int count2 = visualSettings.networkparts.Count;
			if (count != 0)
			{
				return count2 == 0;
			}
			return false;
		}

		private void OnPlayerJoined(Sfs2X.Entities.User user)
		{
			OnPlayerJoinedAsync(user).Forget();
		}

		private async UniTask OnPlayerJoinedAsync(Sfs2X.Entities.User user)
		{
			if (m_playersByNetworkId.TryGetValue(user.Id, out var value))
			{
				Players.Remove(value);
			}
			SmartfoxPlayerProperties smartfoxPlayerProperties = new SmartfoxPlayerProperties();
			await UniTask.RunOnThreadPool(delegate
			{
				smartfoxPlayerProperties.SetFromUser(user);
			});
			if (smartfoxPlayerProperties.PlayerID.IsInvalid)
			{
				return;
			}
			NetworkPlayer networkPlayer = CreatePlayer(isLocal: false, smartfoxPlayerProperties);
			networkPlayer.NetworkID = user.Id;
			m_playersByNetworkId[user.Id] = networkPlayer;
			Players.Add(networkPlayer);
			string filteredNickName = networkPlayer.FilteredNickName;
			PlayerId playerId = networkPlayer.PlayerId;
			UINotifications instance = UINotifications.instance;
			if (instance != null && Overlay.instance.IsFriend(playerId))
			{
				Color green = Color.green;
				_ = string.Empty;
				string text;
				if (m_invitedFriends.Contains(playerId))
				{
					m_invitedFriends.Remove(playerId);
					text = LocalizationKeys.mp_joined_friend_by_invitation.LocalizedFormat(filteredNickName);
				}
				else
				{
					text = LocalizationKeys.mp_joined_friend.LocalizedFormat(filteredNickName);
				}
				instance.Add(text, green);
			}
			if ((bool)systems)
			{
				systems.DoPlayerEnter(networkPlayer);
				Overlay.instance.SetPlayedWith(networkPlayer.PlayerId);
			}
			if (MasterPlayer == null)
			{
				MasterPlayer = networkPlayer;
				MasterPlayer.IsMasterClient = true;
			}
			else if (MasterPlayer.NetworkID > networkPlayer.NetworkID)
			{
				MasterPlayer.IsMasterClient = false;
				MasterPlayer = networkPlayer;
				MasterPlayer.IsMasterClient = true;
			}
			UpdateRoomMaskByMaster(networkPlayer, isLeft: false);
		}

		private void OnPlayerLeft(Sfs2X.Entities.User user)
		{
			if (m_playersByNetworkId.TryGetValue(user.Id, out var value))
			{
				if (value.IsLocal)
				{
					LocalPlayer.NetworkID = -1;
				}
				value.OnLeave();
				Players.Remove(value);
				m_playersByNetworkId.Remove(user.Id);
				if (MasterPlayer == value)
				{
					MasterPlayer = LocalPlayer;
					int num = int.MaxValue;
					foreach (NetworkPlayer player in Players)
					{
						if (player.NetworkID < num)
						{
							MasterPlayer = player;
							num = player.NetworkID;
						}
					}
					MasterPlayer.IsMasterClient = true;
				}
				Overlay.NotifyMPFeature(Players.Count > 1);
				value.OnPostLeave();
			}
			UpdateRoomMaskByMaster(value, isLeft: true);
		}

		private void UpdateRoomMaskByMaster(NetworkPlayer player, bool isLeft)
		{
			if (!LocalPlayer.IsMasterClient)
			{
				return;
			}
			int num = CurrentRoomOptions.InitialCrossPlayMask;
			int currentCrossPlayMask = CurrentRoomOptions.CurrentCrossPlayMask;
			foreach (NetworkPlayer player2 in Players)
			{
				if (!isLeft || player2 != player)
				{
					num &= player2.PlayerProperties.CrossPlayMask;
				}
			}
			if (num == 0)
			{
				num = currentCrossPlayMask;
			}
			if (isLeft && num < currentCrossPlayMask)
			{
				num = currentCrossPlayMask;
			}
			string text = string.Format("Master update room from: {0} to: {1} after player: {2}({3}) {4}", currentCrossPlayMask, num, player?.FilteredNickName, player?.PlayerProperties.CrossPlayMask, isLeft ? "left" : "entered");
			GameLog.Print(AlertLevel.Notify, "Multiplayer", text);
			if (num != currentCrossPlayMask)
			{
				SmartfoxRoomOptions smartfoxRoomOptions = CurrentRoomOptions.Clone();
				smartfoxRoomOptions.CurrentCrossPlayMask = num;
				SendChangedRoomOptions(smartfoxRoomOptions);
			}
		}

		private void OnServerMessage(string cmd, ISFSObject parameters)
		{
			Action<ISFSObject> value;
			if (cmd.Equals("na"))
			{
				GUICommonGodVoice.ShowText("Access denided!");
			}
			else if (cmd.Equals("e"))
			{
				string text = parameters.GetUtfString("cmd") + ": ";
				string utfString = parameters.GetUtfString("em");
				if (utfString == "Error while trying to create vote: Vote cooldown in progress")
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.vote_cooldown_in_progress.Localized());
				}
				else if (utfString.Contains("There already is an ongoing vote"))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.vote_is_already_going.Localized());
				}
				else
				{
					GUICommonGodVoice.ShowText(text + utfString);
				}
			}
			else if (m_serverCommandsHandler.TryGetValue(cmd, out value))
			{
				value?.Invoke(parameters);
			}
		}

		private NetworkPlayer TryGetRoomCreatorFromPlayerList()
		{
			return Players.Find((NetworkPlayer player) => player.PlayerId.ToString() == lastRoomOptions?.RoomCreatorId);
		}

		private void OnRoomMessage(SFSUser sender, SmartfoxDataPackage data)
		{
			if (sender != null)
			{
				m_roomMessageUpdateManager.Process(sender, data);
			}
		}

		private void OnRoomAdded(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			if (State != ClientState.Joined)
			{
				m_cachedRoomList.Clear();
				OnGameListUpdate.Call(m_cachedRoomList);
			}
		}

		private void OnRoomUpdated(SmartfoxRoomClient client, SmartfoxRoomOptions options)
		{
			if (State != ClientState.Joined)
			{
				m_cachedRoomList.Clear();
				OnGameListUpdate.Call(m_cachedRoomList);
			}
		}

		private void OnRoomRemoved(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			if (State != ClientState.Joined)
			{
				m_cachedRoomList.Clear();
				m_cachedRoomList.Add(room.Room.Name.ToString());
				OnGameListUpdate.Call(m_cachedRoomList);
			}
		}

		protected NetworkPlayer CreatePlayer(bool isLocal, SmartfoxPlayerProperties playerProperties)
		{
			return new NetworkPlayer(this, isLocal, playerProperties);
		}

		public void LateUpdate()
		{
			SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
			if ((bool)syncNetFreerideRaceModeState && !syncNetFreerideRaceModeState.isRaceEnded)
			{
				systems.LateUpdate();
			}
		}

		public void Update()
		{
			if (m_lobby != null)
			{
				m_lobby.Update();
			}
			else if (m_client != null)
			{
				m_client.Update();
			}
			SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
			if ((bool)syncNetFreerideRaceModeState && !syncNetFreerideRaceModeState.isRaceEnded)
			{
				if (State == ClientState.Disconnected || State == ClientState.Uninitialized)
				{
					NetworkController.LobbyEnter();
				}
				if (LobbyState == ClientState.JoinedLobby)
				{
					RestoreRoom();
					NetworkController.TryShowConnectionRestoreMsg();
				}
				if (State == ClientState.Joined)
				{
					systems.Update();
					m_networkLOD.Update();
				}
			}
		}

		public void FixedUpdate()
		{
			SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
			if ((bool)syncNetFreerideRaceModeState && !syncNetFreerideRaceModeState.isRaceEnded && State == ClientState.Joined)
			{
				systems.FixedUpdate();
			}
		}

		public void OnDrawGizmos()
		{
			if ((bool)systems && State == ClientState.Joined)
			{
				systems.OnDrawGizmos();
			}
		}

		public void CreateRoom(RoomSettings roomSetting)
		{
			bool flag = roomSetting.trackID == -1;
			if (GameConsole.Bool["ChampionshipMode"] && string.IsNullOrEmpty(roomSetting.password))
			{
				UILockInputContext.DisableLock();
				GUICommonGodVoice.ShowText("Championship rooms should be password protected.");
				return;
			}
			if (!flag)
			{
				SetWinterTires(roomSetting.trackID);
			}
			SetTireProtector(roomSetting.trackID, roomSetting.adhLimit);
			FillLocalPlayerData();
			isLastRoomPassword = !string.IsNullOrEmpty(roomSetting.password);
			lastRoomOptions = new SmartfoxRoomOptions();
			lastRoomOptions.MaxPlayers = GameConsole.Int["sv_maxplayers"];
			lastRoomOptions.TrackID = (flag ? 1 : roomSetting.trackID);
			lastRoomOptions.Name = ((!isLastRoomPassword) ? Guid.NewGuid().ToString() : roomSetting.password);
			lastRoomOptions.Caption = roomSetting.roomName;
			lastRoomOptions.Version = GameVersion.networkVersion;
			lastRoomOptions.InitialCrossPlayMask = GetCrossPlayMaskFromFlag(roomSetting.crossPlay);
			lastRoomOptions.ModName = (flag ? ("#" + roomSetting.externalId) : ModelValidator.modelName);
			lastRoomOptions.NewRoomSettings = (RoomData.RoomSettingFlags)roomSetting.newFlags;
			lastRoomOptions.OldRoomSettings = RoomData.RoomSettingFlags.VOTE_ALLOWED | RoomData.RoomSettingFlags.COLLISIONS_ACTIVE;
			lastRoomOptions.XboxRoomGuid = string.Empty;
			lastRoomOptions.RoomPlayersList = string.Empty;
			lastRoomOptions.RoomCreatorId = Overlay.instance.player.id.ToString();
			lastRoomOptions.RoomNightMode = roomSetting.nightMode;
			lastRoomOptions.RoomLightPreset = roomSetting.lightPreset;
			lastRoomOptions.RoomAdhLimit = roomSetting.adhLimit;
			lastRoomOptions.ChampModeRoom = false;
			lastRoomOptions.ExternalTrackId = roomSetting.externalId;
			lastRoomOptions.ugcServiceId = roomSetting.ugcServiceId;
			lastRoomOptions.SpawnPointIndex = roomSetting.spawnPoint;
			lastRoomOptions.AssistVersion = roomSetting.assistVersion;
			lastRoomOptions.TrackName = roomSetting.trackName;
			lastRoomOptions.RoomPowerDensityLimit = roomSetting.pdLimit;
			lastRoomOptions.RoomWheelDriveType = roomSetting.wdLimit;
			lastRoomOptions.RoomTractionControlMode = roomSetting.tcMode;
			lastRoomOptions.CollisionType = roomSetting.collisionType;
			ValidatePlayerData((int)(lastRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE) > 0);
			SetRaceMode(RaceMode.NetworkFreeride);
			m_lobby.CreateMPRoomAndJoin(lastRoomOptions);
			m_firstRoomEnter = true;
		}

		public void AddToInvitedFriends(PlayerId playerId)
		{
			m_invitedFriends.Add(playerId);
		}

		public int GetCurrentCrossPlayMask()
		{
			if (prefs.globals.isCrossMPCreatedRoom.HasValue)
			{
				return GetCrossPlayMaskFromFlag(prefs.globals.isCrossMPCreatedRoom.Value);
			}
			if (prefs.globals.crossPlatformPlay)
			{
				return WebConfigManager.CrossPlatformMask;
			}
			return Overlay.playerId.platformMask;
		}

		private int GetCrossPlayMaskFromFlag(bool crossPlay)
		{
			if (!crossPlay)
			{
				return Overlay.playerId.platformMask;
			}
			return WebConfigManager.CrossPlatformMask;
		}

		public bool IsCrossPlatformGloballyEnabled()
		{
			return GetCurrentCrossPlayMask() != Overlay.playerId.platformMask;
		}

		public void SetToNullCrossMPCreatedRoom()
		{
			if (prefs.globals.isCrossMPCreatedRoom.HasValue)
			{
				prefs.globals.isCrossMPCreatedRoom = null;
			}
		}

		public static bool IsTrackIDAllowed(int trackId)
		{
			if (m_allowedTracks.Count == 0)
			{
				foreach (DB.Meta.Track item in NetworkController.InstanceGame.baseModel.QueryAll<DB.Meta.Track>())
				{
					if (item.enabled)
					{
						m_allowedTracks.Add(item.id);
					}
				}
			}
			return m_allowedTracks.Contains(trackId);
		}

		public static JOIN_FLAG GetJoinFlag(SmartfoxRoomOptions options)
		{
			JOIN_FLAG jOIN_FLAG = JOIN_FLAG.ALLOW;
			if (string.IsNullOrEmpty(options.Version))
			{
				jOIN_FLAG |= JOIN_FLAG.EMPTY_V;
			}
			if (prefs.globals.isCrossMPCreatedRoom != true && !NetworkController.InstanceGame.IsAllowCross(options.CurrentCrossPlayMask))
			{
				jOIN_FLAG |= JOIN_FLAG.CROSS_DISABLE;
			}
			if (options.Version != GameVersion.networkVersion)
			{
				jOIN_FLAG |= JOIN_FLAG.WRONG_V;
			}
			if (!IsTrackIDAllowed(options.TrackID) && options.ExternalTrackId == 0L)
			{
				jOIN_FLAG |= JOIN_FLAG.WRONG_TRACK;
			}
			if (GameConsole.Bool["sv_crazyNetworkTracks"] && UnityEngine.Random.value > 0.5f)
			{
				jOIN_FLAG |= JOIN_FLAG.WRONG_TRACK;
			}
			return jOIN_FLAG;
		}

		public bool IsAllowCross(int roomMask, bool considerPrefs = true)
		{
			if ((!prefs.globals.crossPlatformPlay && considerPrefs) || !WebConfigManager.ServerAllowCrossPlatform)
			{
				return roomMask == Overlay.playerId.platformMask;
			}
			return (GetCurrentCrossPlayMask() & roomMask & Overlay.playerId.platformMask) != 0;
		}

		public bool IsDifferentPlatformRoom(int roomMask)
		{
			return roomMask != Overlay.playerId.platformMask;
		}

		public bool JoinRoom(SmartfoxLobby.RoomLobbyInfo meta)
		{
			if (meta != null)
			{
				SmartfoxRoomOptions options = meta.options;
				SetWinterTires(options.TrackID);
			}
			if (meta != null)
			{
				SetTireProtector(meta.options.TrackID, meta.options.RoomAdhLimit);
			}
			FillLocalPlayerData();
			if (meta != null)
			{
				SmartfoxRoomOptions options2 = meta.options;
				if (options2.ModName != ModelValidator.modelName)
				{
					if (States.current as BaseGameState != null)
					{
						if (ModelValidator.isValid)
						{
							UIMessageBox.ShowHint("", LocalizationKeys.mod_wrong_version_info.Localized(), LocalizationKeys.mod_wrong_version.Localized());
						}
						else
						{
							UIMessageBox.ShowHint("", LocalizationKeys.mod_wrong_version_info2.Localized(), LocalizationKeys.mod_wrong_version.Localized());
						}
					}
					return false;
				}
				ValidatePlayerData((int)(lastRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE) > 0);
				if ((int)(options2.NewRoomSettings & RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE) > 0 && LocalPlayer.PlayerProperties.VisualSettings.networkparts.Count > 0)
				{
					if (States.current as BaseGameState != null)
					{
						UIMessageBox.ShowHint("", LocalizationKeys.mod_wrong_version_info2.Localized(), LocalizationKeys.mod_wrong_version.Localized());
					}
					return false;
				}
				if (m_lobby.JoinRoom(meta))
				{
					SetRaceMode(RaceMode.NetworkFreeride);
					m_firstRoomEnter = true;
					return true;
				}
				return false;
			}
			if (!NetworkController.isAutoJoinInProgress)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.wrong_room_pass.Localized(), 3f);
			}
			return false;
		}

		private void InitializeClientAfterEnterRoom()
		{
			if (m_lobby != null)
			{
				m_lobby.DisconnectFromNodesExcept(m_client);
				m_lobby = null;
			}
			if (m_client != null)
			{
				m_client.InternalError += OnInternalError;
				m_client.AuthenticationError += OnAuthenticationError;
				m_client.JoinedRoom += OnJoinedRoom;
				m_client.RoomOptionsChanged += OnRoomOptionsChanged;
				m_client.UserVariablesUpdate += OnUserVariablesUpdate;
				m_client.JoinRoomError += OnRoomJoinedError;
				m_client.StateChanged += OnStateChanged;
				m_client.PlayerJoined += OnPlayerJoined;
				m_client.PlayerLeft += OnPlayerLeft;
				m_client.RoomMessage += OnRoomMessage;
				m_client.ServerCommand += OnServerMessage;
				m_client.SocketError += OnSocketError;
				if (m_roomMessageUpdateManager == null)
				{
					m_roomMessageUpdateManager = new RoomMessageUpdateManager();
				}
				m_roomMessageUpdateManager.Init(packetHandler);
				if (m_userPropsUpdateManager == null)
				{
					m_userPropsUpdateManager = new UserPropsUpdateManager();
				}
				m_userPropsUpdateManager.Init(systems.Get<NetGameNotifySystem>());
			}
		}

		public void RestoreRoom()
		{
			if (m_lobby == null)
			{
				Disconnect();
				return;
			}
			foreach (NetworkPlayer player in Players)
			{
				player.OnLeave();
			}
			Players.Clear();
			m_playersByNetworkId.Clear();
			m_invitedFriends.Clear();
			m_lobby.CreateMPRoomAndJoin(lastRoomOptions);
		}

		public void PrepareForChangeMap()
		{
			m_firstRoomEnter = true;
		}

		private async UniTask RoomFound()
		{
			if (m_isInRoom)
			{
				return;
			}
			m_isInRoom = true;
			lastRoomOptions = m_client.RoomOptions;
			Players.Clear();
			int curId = int.MaxValue;
			MasterPlayer = null;
			foreach (Sfs2X.Entities.User p in m_client.CurrentRoom.PlayerList)
			{
				SmartfoxPlayerProperties props = new SmartfoxPlayerProperties();
				await UniTask.RunOnThreadPool(delegate
				{
					props.SetFromUser(p);
				});
				NetworkPlayer networkPlayer = CreatePlayer(m_client.SelfUser.Id == p.Id, props);
				networkPlayer.NetworkID = p.Id;
				networkPlayer.IsMasterClient = false;
				m_playersByNetworkId[networkPlayer.NetworkID] = networkPlayer;
				if (m_client.SelfUser.Id == p.Id)
				{
					Players.Insert(0, networkPlayer);
					LocalPlayer = networkPlayer;
				}
				else
				{
					Players.Add(networkPlayer);
					Overlay.instance.SetPlayedWith(networkPlayer.PlayerId);
				}
				if (p.Id < curId)
				{
					curId = p.Id;
					MasterPlayer = networkPlayer;
				}
			}
			MasterPlayer = MasterPlayer ?? LocalPlayer;
			MasterPlayer.IsMasterClient = true;
			if (m_playerRoomCreator == null)
			{
				m_playerRoomCreator = TryGetRoomCreatorFromPlayerList();
			}
			if (m_firstRoomEnter)
			{
				GameConsole.SetCollisions(lastRoomOptions.CollisionType != 0);
				CheckNightModeWithRoomOptions();
				CheckLightPresetWithRoomOptions();
				m_firstRoomEnter = false;
				stats.Clear();
				systems.OnGameConnect();
			}
			systems.OnReconnect();
		}

		public void ReleaseRoom()
		{
			if (!m_isInRoom)
			{
				return;
			}
			systems.GameLost();
			foreach (NetworkPlayer player in Players)
			{
				player.OnLeave();
			}
			MasterPlayer = LocalPlayer;
			Players.Clear();
			m_playersByNetworkId.Clear();
			m_invitedFriends.Clear();
			LocalPlayer.NetworkID = -1;
			m_isInRoom = false;
		}

		private void CheckNightModeWithRoomOptions()
		{
			if (BaseRaceState.NightModeActive != lastRoomOptions.RoomNightMode)
			{
				systems.Get<NetGameServerVoteSystem>().forceUpdateNightMode = true;
			}
		}

		private void CheckLightPresetWithRoomOptions()
		{
			if (lastRoomOptions == null)
			{
				return;
			}
			DB.Meta.Track byId = baseModel.GetById<DB.Meta.Track>(lastRoomOptions.TrackID);
			if (byId == null)
			{
				return;
			}
			TrackLightVariantsBase instance = ScriptableSingleton<TrackLightVariantsBase>.instance;
			if (instance == null)
			{
				return;
			}
			string sceneName = instance.GetSceneName(byId.scene, lastRoomOptions.RoomNightMode);
			if (lastRoomOptions.RoomLightPreset != prefs.globals.lightPresetName || sceneName != prefs.globals.sceneName)
			{
				prefs.globals.sceneName = sceneName;
				NetGameServerVoteSystem netGameServerVoteSystem = systems.Get<NetGameServerVoteSystem>();
				if (netGameServerVoteSystem != null)
				{
					netGameServerVoteSystem.forceUpdateLightPreset = true;
				}
			}
		}

		public void FillLocalPlayerData()
		{
			int lastCarId = prefs.globals.lastCarId;
			int profileIdForCar = prefs.carSettings.GetProfileIdForCar(lastCarId);
			PlayerCarProfileMetaExtensions.ValidateEngineSwapProfileControllers(profileIdForCar);
			SmartfoxPlayerProperties smartfoxPlayerProperties = ((LocalPlayer == null) ? new SmartfoxPlayerProperties() : LocalPlayer.PlayerProperties);
			smartfoxPlayerProperties.Name = prefs.playerProfile.name;
			smartfoxPlayerProperties.CarId = lastCarId;
			smartfoxPlayerProperties.ProfileId = profileIdForCar;
			smartfoxPlayerProperties.SwapId = prefs.carSettings.GetEngineIdForCar(lastCarId);
			smartfoxPlayerProperties.CrossPlayMask = GetCurrentCrossPlayMask();
			smartfoxPlayerProperties.Profile = prefs.carSettings.GetProfileInfo(profileIdForCar, lastCarId).Copy();
			smartfoxPlayerProperties.VisualSettings = prefs.carSettings.GetVisualForCar(lastCarId).Copy().MutateToNetworkParts();
			smartfoxPlayerProperties.HasLivery = smartfoxPlayerProperties.VisualSettings.vinylLayers.Count > 0;
			smartfoxPlayerProperties.InputDeviceType = systems.Get<NetPlayerInputSystem>().cachedDeviceType;
			if (LocalPlayer == null)
			{
				LocalPlayer = CreatePlayer(isLocal: true, smartfoxPlayerProperties);
				LocalPlayer.PlayerId = prefs.playerProfile.id;
			}
			else
			{
				Client?.UpdatePlayerProperties(smartfoxPlayerProperties);
			}
		}

		private void ValidatePlayerData(bool championshipMode)
		{
			if (championshipMode && LocalPlayer != null)
			{
				ProfilesInfo.Profile profile = LocalPlayer.PlayerProperties.Profile;
				profile.controllers.Remove("front_tire_type");
				profile.controllers.Remove("rear_tire_type");
			}
		}

		public void SetRaceMode(RaceMode raceMode)
		{
			if (m_raceMode != raceMode)
			{
				m_raceMode = raceMode;
			}
		}

		public void LoadRace(bool isRoomNameChanged = false)
		{
			int trackID = lastRoomOptions.TrackID;
			SetWinterTires(trackID);
			if ((bool)(States.mainCurrent as LoadRaceState))
			{
				return;
			}
			SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
			GameEvents.OnRaceLoaded.Subscribe(GameEventFlag.OneCall, delegate
			{
				if (NetworkController.IsActive)
				{
					if (BaseRaceState.NightModeActive != lastRoomOptions.RoomNightMode)
					{
						BaseRaceState.NightModeActive = lastRoomOptions.RoomNightMode;
					}
					if (prefs.globals.lightPresetName != lastRoomOptions.RoomLightPreset)
					{
						prefs.globals.lightPresetName = lastRoomOptions.RoomLightPreset;
					}
					DB.Meta.Track byId = baseModel.GetById<DB.Meta.Track>(prefs.globals.lastTrackId);
					prefs.globals.sceneName = ScriptableSingleton<TrackLightVariantsBase>.instance.GetSceneName(byId.scene, lastRoomOptions.RoomNightMode);
				}
			});
			string modName = lastRoomOptions.ModName;
			if (!string.IsNullOrEmpty(modName) && modName.StartsWith("#") && ulong.TryParse(modName.Substring(1), out var result))
			{
				if ((bool)syncNetFreerideRaceModeState && prefs.globals.lastExternalID == result)
				{
					return;
				}
				prefs.globals.lastExternalID = result;
				SoundController.PlayFX(FXSample.RaceStartSound);
				States.State<LoadRaceState>().LoadExternalRace(result);
				GameEvents.OnRaceLoaded.Subscribe(GameEventFlag.OneCall, delegate
				{
					if (NetworkController.IsActive)
					{
						systems.OnRaceLoaded();
					}
				});
			}
			else
			{
				if (!(!syncNetFreerideRaceModeState || (lastRoomOptions.ExternalTrackId == 0L && prefs.globals.lastTrackId != trackID) || prefs.globals.lastExternalID != lastRoomOptions.ExternalTrackId || isRoomNameChanged))
				{
					return;
				}
				SoundController.PlayFX(FXSample.RaceStartSound);
				if (lastRoomOptions.ExternalTrackId != 0L)
				{
					DB.Meta.Track track = UGCSceneBundleManager.TryGetMetaByExternalId(lastRoomOptions.ExternalTrackId);
					if (track != null)
					{
						prefs.globals.lastTrackId = track.id;
						prefs.stats.SetLastRaceModeForTrack(track.id, m_raceMode);
					}
				}
				else
				{
					prefs.globals.lastTrackId = lastRoomOptions.TrackID;
					prefs.stats.SetLastRaceModeForTrack(trackID, m_raceMode);
				}
				prefs.globals.lastExternalID = lastRoomOptions.ExternalTrackId;
				States.State<LoadRaceState>().LoadLocalRace();
				GameEvents.OnRaceLoaded.Subscribe(GameEventFlag.OneCall, delegate
				{
					if (NetworkController.IsActive)
					{
						systems.OnRaceLoaded();
					}
				});
			}
		}

		private void SetWinterTires(int trackId)
		{
			prefs.carSettings.SetWinterTiresForcar(prefs.globals.lastCarId, TrackMetaDB.IsWinterTrackWithIce(trackId));
		}

		public bool SetTireProtector(int trackId, int adhLimit)
		{
			if (!TrackMetaDB.IsWinterTrackWithIce(trackId))
			{
				CarVisualManager.VisualSettings visualForCar = prefs.carSettings.GetVisualForCar(prefs.globals.lastCarId);
				if (visualForCar == null)
				{
					return false;
				}
				if (adhLimit == 0 || adhLimit == 15)
				{
					return false;
				}
				CarVisualManager.TyreProtectorGripType tyreProtectorGripType = RoomData.AdhLimitToProtectorType((RoomData.AdhLimitFlags)adhLimit);
				bool flag = false;
				if (visualForCar.frontTire.ProtectorGrip != tyreProtectorGripType)
				{
					visualForCar.frontTire.ProtectorGrip = RoomData.AdhLimitToProtectorType((RoomData.AdhLimitFlags)adhLimit);
					flag = true;
				}
				if (visualForCar.rearTire.ProtectorGrip != tyreProtectorGripType)
				{
					visualForCar.rearTire.ProtectorGrip = RoomData.AdhLimitToProtectorType((RoomData.AdhLimitFlags)adhLimit);
					flag = true;
				}
				if (flag)
				{
					prefs.carSettings.SetVisualForCar(prefs.globals.lastCarId, visualForCar);
					return true;
				}
			}
			return false;
		}

		public void DebugReturn(string message)
		{
			GameLog.Print(AlertLevel.Notify, "Photon", message);
		}

		public void OnSocketError(short code, string message)
		{
			Disconnect();
			Debug.LogWarning("Exception on connection level: " + code + " " + message);
		}

		public void SubscribeOnServerCommand(string cmd, Action<ISFSObject> action)
		{
			if (m_serverCommandsHandler.TryGetValue(cmd, out var value))
			{
				value = (Action<ISFSObject>)Delegate.Combine(value, action);
			}
			else
			{
				m_serverCommandsHandler.Add(cmd, action);
			}
		}

		public void SendServerCommand(string cmd, ISFSObject data)
		{
			m_client?.SendServerCommand(cmd, data, out var _);
		}

		public void SendServerCommand(string cmd, ISFSObject data, out long sentTime)
		{
			sentTime = -1L;
			m_client?.SendServerCommand(cmd, data, out sentTime);
		}

		public void SendRealtimeData(SmartfoxDataPackage data)
		{
			m_client?.Send(data, reliable: false);
		}

		public void SendRealtimeData(SmartfoxDataPackage data, int receiver)
		{
			m_client?.Send(data, reliable: false, new int[1] { receiver });
		}

		public void SendOthers(SmartfoxDataPackage data, bool reliable)
		{
			m_client?.Send(data, reliable);
		}

		public void SendOthers(SmartfoxDataPackage data, bool reliable, int[] receivers)
		{
			m_client?.Send(data, reliable, receivers);
		}

		public void SendIncludeSelf(SmartfoxDataPackage data, bool reliable)
		{
			m_client?.SendIncludingSelf(data, reliable);
		}

		public void SendChangedRoomOptions(SmartfoxRoomOptions options)
		{
			if (m_client != null)
			{
				SetWinterTires(options.TrackID);
				FillLocalPlayerData();
				m_client.SendChangedRoomOptions(options);
			}
		}

		public void SendRAW(IRequest data)
		{
			m_client?.SendRAW(data);
		}

		private void OnUserVariablesUpdate(SmartfoxRoomClient client, SFSUser user, HashSet<string> changed)
		{
			if (user != null)
			{
				m_userPropsUpdateManager.Process(user, changed);
			}
		}

		private void OnRoomOptionsChanged(SmartfoxRoomClient client, SmartfoxRoomOptions options, HashSet<string> changed)
		{
			if (CurrentRoom != null)
			{
				CheckNightModeWithRoomOptions();
				CheckLightPresetWithRoomOptions();
				if (options.Room.Id == CurrentRoom.Id)
				{
					systems.Get<NetGameServerVoteSystem>().OnRoomPropsChanged(options, changed);
				}
			}
		}

		private void OnStateChanged(ClientState previousState, ClientState newState)
		{
			if (m_client != null && m_client.CurrentRoom == null)
			{
				ReleaseRoom();
			}
			if (newState == ClientState.Uninitialized || newState == ClientState.Disconnected)
			{
				ReleaseRoom();
			}
		}

		public void Service()
		{
			if (m_client != null)
			{
				m_client.Update();
			}
		}
	}
}
