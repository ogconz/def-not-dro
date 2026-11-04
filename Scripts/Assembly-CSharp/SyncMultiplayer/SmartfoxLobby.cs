using System;
using System.Collections.Generic;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using Sfs2X.Entities.Data;
using SimpleJSON;
using UnityEngine;

namespace SyncMultiplayer
{
	public class SmartfoxLobby
	{
		public class RoomLobbyInfo
		{
			public string publicIp;

			public int port;

			public SmartfoxRoomOptions options;

			public string roomId => options.Name;
		}

		private class LobbyNodes
		{
			public string restrictType;

			public long restrictTimeEnd;

			public float refreshRate = 3f;

			public List<LobbySingleNodeInfo> nodes;
		}

		private class LobbySingleNodeInfo
		{
			public string publicIp;

			public int port;

			public List<RoomSingleInfo> rooms;
		}

		public class RoomSingleInfo
		{
			public string id;

			public string name;

			public int usersNumber;

			public Dictionary<string, string> properties;
		}

		private class MasterRoomCreationOptions
		{
			public string name;

			public Dictionary<string, string> publicAttributes;

			public Dictionary<string, string> privateAttributes;
		}

		private class LobbyFilterObject
		{
			public string roomName;

			public string track;

			public bool driftMode;

			public bool timeAttack;

			public bool clippingPoints;

			public int collisionType;

			public bool vote;

			public bool nightMode;

			public bool password;

			public string passwordValue;

			public bool fullRoom;

			public string friends;

			public string netVersion;

			public bool crossPlayMaskEnabled;

			public string crossPlayMaskCurrent;

			public int adhLimit;

			public int pdLimit;

			public int wdLimit;

			public bool externalTrack;

			public bool assistVersion;

			public bool tcMode;

			public string ignore;
		}

		private class MasterRoomCreatedInfo : RoomInfo
		{
			public string restrictType;

			public long restrictTimeEnd;
		}

		private class RoomInfo
		{
			public string roomId;

			public string name;

			public string publicIp;

			public int clientPort;
		}

		public const string Zone = "SteamRealtime";

		private const float JoinTimeout = 15f;

		private const short CodeUserBannedOrCheating = 401;

		private const short CodeRoomNotExist = 402;

		private const short CodeFullRoom = 403;

		private const short CodeJoinTimeout = 504;

		private const short CodeUnknownError = 505;

		private static ApiClient m_apiClient;

		private float m_joinRoomTimeOut;

		private string m_nickname;

		private SmartfoxPlayerProperties m_playerProperties;

		private SmartfoxRoomClient m_nodeTryingToJoinRoom;

		private SmartfoxRoomClient m_nodeToJoinRoom;

		private string m_idToJoinRoom;

		public static Action<bool> sendLobbyRequestAction;

		private bool m_refreshLock;

		private bool m_restrictCrossMpByAdmin;

		private TimeKeeper m_refreshTimer = new TimeKeeper(3000);

		private List<DB.Meta.Track> m_tracks;

		private GamePrefs m_prefs;

		private StringBuilder m_sb;

		public static bool lobbyPostRequestInProgress;

		public bool RestrictCrossMpByAdmin
		{
			get
			{
				return m_restrictCrossMpByAdmin;
			}
			private set
			{
				m_restrictCrossMpByAdmin = value;
				if (value)
				{
					m_prefs.globals.crossPlatformPlay = false;
				}
			}
		}

		public long RestrictCrossMpByAdminTimeEnd { get; private set; }

		public bool RestrictAcessMpByAdmin { get; private set; }

		public long RestrictAcessMpByAdminTimeEnd { get; private set; }

		public List<SmartfoxRoomClient> Nodes { get; private set; }

		public List<RoomLobbyInfo> UnfilteredRooms { get; private set; }

		public List<RoomLobbyInfo> Rooms { get; private set; }

		public ClientState State { get; private set; }

		public event ErrorDelegate AuthenticationError;

		public event NoArgsEvent JoinedLobby;

		public event RoomUpdateEvent RoomUpdated;

		public event RoomEvent RoomAdded;

		public event RoomEvent RoomRemoved;

		public event RoomEvent JoinedRoom;

		public event ErrorDelegate JoinRoomError;

		public event ErrorTypeDelegate CreateRoomError;

		public SmartfoxLobby(string nickname, SmartfoxPlayerProperties playerProperties)
		{
			lobbyPostRequestInProgress = false;
			Rooms = new List<RoomLobbyInfo>();
			UnfilteredRooms = new List<RoomLobbyInfo>();
			m_nickname = nickname;
			m_playerProperties = playerProperties;
			State = ClientState.Uninitialized;
			m_refreshLock = false;
			m_refreshTimer.ResetToNow();
			m_refreshTimer.ShouldExecute = false;
			m_tracks = DependencyInjector.Resolve<BaseModel>().QueryAll<DB.Meta.Track>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_sb = new StringBuilder();
		}

		public void Connect()
		{
			State = ClientState.StartedConnection;
		}

		private LobbyFilterObject ConstructPrivateFilterObject(string pass)
		{
			return new LobbyFilterObject
			{
				password = true,
				passwordValue = pass.Remove(0, 1)
			};
		}

		private LobbyFilterObject ConstructFilterObject()
		{
			LobbyFilterObject result = new LobbyFilterObject();
			result.roomName = m_prefs.globals.lobbyRoomNameInputFilter;
			DB.Meta.Track track = m_tracks.Find((DB.Meta.Track t) => t.name.Equals(m_prefs.globals.lobbyTrackNameSwitcherFilter));
			result.track = ((track == null) ? "-1" : track.id.ToString());
			LobbyFilterState lobbyFilterState = NetworkController.InstanceGame.lobbyFilterState;
			result.driftMode = lobbyFilterState.IsFlagEnabled(FilterButtonType.DriftMode);
			result.timeAttack = lobbyFilterState.IsFlagEnabled(FilterButtonType.TimeAttack);
			result.nightMode = lobbyFilterState.IsFlagEnabled(FilterButtonType.NightMode);
			result.password = lobbyFilterState.IsFlagEnabled(FilterButtonType.Password);
			result.fullRoom = lobbyFilterState.IsFlagEnabled(FilterButtonType.FullRoom);
			result.assistVersion = lobbyFilterState.IsFlagEnabled(FilterButtonType.AssistVersion);
			result.tcMode = lobbyFilterState.IsFlagEnabled(FilterButtonType.tcMode);
			result.vote = false;
			result.clippingPoints = false;
			m_sb.Clear();
			List<IPlayer> friends = Overlay.instance.friends;
			if (friends != null && lobbyFilterState.IsFlagEnabled(FilterButtonType.Friends))
			{
				foreach (IPlayer item in friends)
				{
					m_sb.Append(item.id.ToString());
					m_sb.Append(",");
				}
				if (string.IsNullOrEmpty(m_sb.ToString()))
				{
					m_sb.Append(Overlay.playerId.ToString());
				}
			}
			result.friends = m_sb.ToString();
			result.netVersion = GameVersion.networkVersion;
			result.crossPlayMaskEnabled = m_prefs.globals.crossPlatformPlay;
			result.crossPlayMaskCurrent = NetworkController.InstanceGame.GetCurrentCrossPlayMask().ToString();
			result.collisionType = m_prefs.globals.collisionsIndexFilter;
			result.pdLimit = m_prefs.globals.lobbyPowerDensityIndexFilter;
			result.wdLimit = m_prefs.globals.lobbyWheelDriveTypeFilter;
			result.adhLimit = m_prefs.globals.lobbyAdhLimitFilter;
			SetRoomType(m_prefs.globals.roomType, ref result);
			SetExternalTrackViewValid(ref result);
			result.ignore = lobbyFilterState.GetIgnoredFilters();
			EnsurePdLimitInIgnoreList(result);
			return result;
		}

		private void EnsurePdLimitInIgnoreList(LobbyFilterObject lobbyFilterObject)
		{
			if (lobbyFilterObject.pdLimit == 0)
			{
				lobbyFilterObject.ignore += "pdLimit";
			}
		}

		private void SetRoomType(int roomType, ref LobbyFilterObject result)
		{
			switch (roomType)
			{
			case 1:
				result.driftMode = true;
				break;
			case 2:
				result.timeAttack = true;
				break;
			}
		}

		private void SetExternalTrackViewValid(ref LobbyFilterObject result)
		{
			string lobbyTrackNameSwitcherFilter = m_prefs.globals.lobbyTrackNameSwitcherFilter;
			if (lobbyTrackNameSwitcherFilter == LocalizationKeys.external_tracks.KeyValue)
			{
				result.externalTrack = true;
			}
			else if (lobbyTrackNameSwitcherFilter == LocalizationKeys.standard_tracks.KeyValue)
			{
				result.externalTrack = false;
			}
		}

		private void UpdateRoomList()
		{
			if (State == ClientState.RoomCreating || lobbyPostRequestInProgress)
			{
				return;
			}
			Dictionary<string, string> dictionary = argsMpToken();
			string value = JSON.Serialize(ConstructFilterObject());
			dictionary.Add("filter", value);
			dictionary.Add("zone", "SteamRealtime");
			m_refreshLock = true;
			sendLobbyRequestAction.SafeCall(param: true);
			lobbyPostRequestInProgress = true;
			masterClient().CallPost("lobby2", delegate(LobbyNodes nodes, Error error)
			{
				lobbyPostRequestInProgress = false;
				sendLobbyRequestAction.SafeCall(param: false);
				m_refreshLock = false;
				if (nodes == null || nodes.nodes == null)
				{
					State = ClientState.Uninitialized;
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Multiplayer master server lobby: void nodes");
					if (this.AuthenticationError != null)
					{
						this.AuthenticationError(0, "Multiplayer master server lobby: void nodes");
					}
				}
				else if (error.type == Error.EType.None)
				{
					RestrictCrossMpByAdmin = nodes.restrictType == AdminBanReason.ACCESS_CROSSPLATFORM_ROOMS.ToString();
					if (RestrictCrossMpByAdmin)
					{
						RestrictCrossMpByAdminTimeEnd = nodes.restrictTimeEnd;
					}
					m_refreshTimer.Interval = Mathf.RoundToInt(Mathf.Max(3f, nodes.refreshRate) * 1000f);
					UpdateRoomList(nodes);
					State = ClientState.JoinedLobby;
					NetworkController.TryShowConnectionRestoreMsg();
				}
				else
				{
					State = ClientState.Uninitialized;
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Multiplayer master server lobby: " + error.debugMessage);
					if (this.AuthenticationError != null)
					{
						this.AuthenticationError(0, error.code + " " + error.debugMessage);
					}
				}
			}, null, dictionary);
		}

		public void Update()
		{
			if (NetworkController.InstanceGame.CurrentRoom == null && State != ClientState.RoomCreating && !m_refreshLock && m_refreshTimer.ShouldExecute)
			{
				m_refreshTimer.Reset();
				UpdateRoomList();
			}
			if (Nodes == null || Nodes.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < Nodes.Count; i++)
			{
				SmartfoxRoomClient smartfoxRoomClient = Nodes[i];
				smartfoxRoomClient.Update();
				if (smartfoxRoomClient == m_nodeToJoinRoom && smartfoxRoomClient.IsConnected())
				{
					try
					{
						JoinRoom(m_nodeToJoinRoom, m_idToJoinRoom);
					}
					catch
					{
					}
					m_nodeToJoinRoom = null;
				}
			}
			if (m_nodeTryingToJoinRoom != null && Time.unscaledTime > m_joinRoomTimeOut)
			{
				SetJoinRoomErrorAndDisconnect(504, LocalizationKeys.unknown_error.Localized());
			}
		}

		private void SetJoinRoomErrorAndDisconnect(short errorCode, string message)
		{
			OnJoinRoomError(errorCode, message);
			DisconnectFromNodesExcept(null);
			if ((errorCode == 401 || errorCode == 504) && (bool)(States.mainCurrent as BaseRaceModeState))
			{
				GarageGUIState.ExitToGarage();
			}
		}

		public bool JoinRoom(RoomLobbyInfo meta)
		{
			if (meta != null)
			{
				ProcessNodeConnection(meta.publicIp, meta.port);
				m_idToJoinRoom = meta.options.Name;
				return true;
			}
			return false;
		}

		public bool CreateOrJoinTop32Room(SmartfoxRoomOptions options, Action<bool> callback)
		{
			if (options != null)
			{
				GetOrCreateRoomRequest(options, delegate(MasterRoomCreatedInfo info, bool success)
				{
					if (success)
					{
						GameLog.Print(AlertLevel.Notify, "Top32", "Top32 roominfo ''" + info.name + "'' received. Attempting to join...");
						OnMasterRoomCreated(info);
						RoomEvent onJoined = null;
						ErrorDelegate onJoinedError = null;
						onJoined = delegate
						{
							JoinedRoom -= onJoined;
							JoinRoomError -= onJoinedError;
							GameLog.Print(AlertLevel.Notify, "Top32", "Joined Top32 room successfully!");
							callback?.SafeCall(param: true);
						};
						onJoinedError = delegate(short code, string msg)
						{
							JoinedRoom -= onJoined;
							JoinRoomError -= onJoinedError;
							GameLog.Print(AlertLevel.Error, "Top32", "Failed to join Top32 race room. ''" + msg + "''");
							callback?.SafeCall(param: false);
						};
						JoinedRoom += onJoined;
						JoinRoomError += onJoinedError;
					}
					else
					{
						GameLog.Print(AlertLevel.Error, "Top32", $"Failed to receive info for room {options.Name}.");
						callback?.SafeCall(param: false);
					}
				});
				return true;
			}
			GameLog.Print(AlertLevel.Error, "Top32", "Invalid room settinfs");
			callback?.SafeCall(param: false);
			return false;
		}

		public void CreateMPRoomAndJoin(SmartfoxRoomOptions options)
		{
			GetOrCreateRoomRequest(options, delegate(MasterRoomCreatedInfo info, bool success)
			{
				if (success)
				{
					OnMasterRoomCreated(info);
				}
			});
		}

		[Obsolete]
		public void CreateRoom(SmartfoxRoomOptions options)
		{
			Dictionary<string, string> dictionary = argsMpToken();
			MasterRoomCreationOptions masterRoomCreationOptions = new MasterRoomCreationOptions();
			masterRoomCreationOptions.name = options.Name;
			masterRoomCreationOptions.publicAttributes = options.ToMasterServerRoomSettings();
			if (string.IsNullOrEmpty(masterRoomCreationOptions.name))
			{
				GameLog.Print(AlertLevel.Error, "System", "SmartfoxLobby>CreateRoom failed: generic error");
				return;
			}
			dictionary.Add("description", JSON.Serialize(masterRoomCreationOptions));
			dictionary.Add("zone", "SteamRealtime");
			masterRoomCreationOptions = JSON.Parse<MasterRoomCreationOptions>(dictionary["description"]);
			if (string.IsNullOrEmpty(masterRoomCreationOptions.name))
			{
				GameLog.Print(AlertLevel.Error, "System", "SmartfoxLobby>CreateRoom failed: serializator error");
				return;
			}
			masterClient().CallPost("getOrCreateRoomByDescription", delegate(MasterRoomCreatedInfo info, Error error)
			{
				if (error.type == Error.EType.None)
				{
					RestrictAcessMpByAdmin = info.restrictType == AdminBanReason.ACCESS_MP.ToString();
					if (RestrictAcessMpByAdmin)
					{
						RestrictAcessMpByAdminTimeEnd = info.restrictTimeEnd;
						State = ClientState.JoinedLobby;
						ShowAdminRestrictAccessError(RestrictAcessMpByAdminTimeEnd, LocalizationKeys.mp_ban_date_msg);
					}
					else
					{
						OnMasterRoomCreated(info);
					}
				}
				else
				{
					State = ClientState.JoinedLobby;
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Multiplayer master server creating room: " + error.debugMessage);
					this.CreateRoomError?.Invoke(error.type, error.guiMessage);
				}
			}, null, dictionary);
			State = ClientState.RoomCreating;
		}

		public void FindRoomLobbyInfoRequest(string roomID, Action<RoomLobbyInfo> callback)
		{
			Dictionary<string, string> dictionary = argsMpToken();
			if (roomID.StartsWith("@"))
			{
				string value = JSON.Serialize(ConstructPrivateFilterObject(roomID));
				dictionary.Add("filter", value);
			}
			masterClient().CallPost("lobby2", delegate(LobbyNodes info, Error error)
			{
				if (info == null || info.nodes == null || info.nodes.Count == 0)
				{
					callback.SafeCall(null);
				}
				else if (error.type == Error.EType.None)
				{
					RestrictAcessMpByAdmin = info.restrictType == AdminBanReason.ACCESS_MP.ToString();
					if (RestrictAcessMpByAdmin)
					{
						RestrictAcessMpByAdminTimeEnd = info.restrictTimeEnd;
						callback.SafeCall(null);
					}
					else if (RestrictCrossMpByAdmin)
					{
						callback.SafeCall(null);
					}
					else
					{
						RoomLobbyInfo param = null;
						foreach (LobbySingleNodeInfo node in info.nodes)
						{
							if (node.rooms != null)
							{
								foreach (RoomSingleInfo room in node.rooms)
								{
									if (room.name == roomID)
									{
										param = new RoomLobbyInfo
										{
											publicIp = node.publicIp,
											port = node.port,
											options = new SmartfoxRoomOptions(room)
										};
										break;
									}
								}
							}
						}
						callback.SafeCall(param);
					}
				}
				else
				{
					callback.SafeCall(null);
				}
			}, null, dictionary);
		}

		public void ShowAdminRestrictAccessError(long timeEnd, LocalizationKeys.Key localizedFormatkey = null, bool isSeconds = true)
		{
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dateTime = ((!isSeconds) ? dateTime.AddMilliseconds(timeEnd).ToLocalTime() : dateTime.AddSeconds(timeEnd).ToLocalTime());
			localizedFormatkey = localizedFormatkey ?? LocalizationKeys.unknown_error;
			GUICommonGodVoice.ShowText(localizedFormatkey.LocalizedFormat(dateTime), 5f);
			UILockInputContext.DisableLock();
		}

		public RoomLobbyInfo GetRoomByName(string roomName)
		{
			return Rooms.Find((RoomLobbyInfo room) => room.options.Name == (ObscuredString)roomName);
		}

		public void DisconnectFromNodesExcept(SmartfoxRoomClient clientToKeepConnected)
		{
			if (Nodes == null)
			{
				return;
			}
			foreach (SmartfoxRoomClient node in Nodes)
			{
				if (node != clientToKeepConnected)
				{
					node.JoinedLobby -= NodeJoinedLobby;
					node.RoomOptionsChanged -= NodeRoomUpdated;
					node.RoomAdded -= NodeRoomAdded;
					node.RoomRemoved -= NodeRoomRemoved;
					node.AuthenticationError -= NodeAuthenticationError;
					node.InternalError -= NodeInternalError;
					node.SocketError -= NodeSocketError;
					node.ServerCommand -= NodeServerCommand;
					try
					{
						node.Disconnect();
						node.Destroy();
					}
					catch (Exception ex)
					{
						GameLog.Print(AlertLevel.Error, "Multiplayer", "Nodeclient Disconnect Error: " + ex.Message);
					}
				}
			}
			Nodes.Clear();
			if (clientToKeepConnected != null)
			{
				Nodes.Add(clientToKeepConnected);
			}
		}

		private void UpdateRoomList(LobbyNodes info)
		{
			Rooms.Clear();
			foreach (LobbySingleNodeInfo node in info.nodes)
			{
				if (node.rooms == null)
				{
					continue;
				}
				foreach (RoomSingleInfo room in node.rooms)
				{
					RoomLobbyInfo item = new RoomLobbyInfo
					{
						publicIp = node.publicIp,
						port = node.port,
						options = new SmartfoxRoomOptions(room)
					};
					Rooms.Add(item);
				}
			}
			UnfilteredRooms.Clear();
			UnfilteredRooms.AddRange(Rooms);
			Rooms.RemoveAll((RoomLobbyInfo r) => (NetworkGame.GetJoinFlag(r.options) != JOIN_FLAG.ALLOW) ? true : false);
			if (this.RoomAdded != null)
			{
				this.RoomAdded(null, null);
			}
		}

		public SmartfoxRoomClient ConnectToNode(string host, int port)
		{
			SmartfoxRoomClient smartfoxRoomClient = new SmartfoxRoomClient(m_nickname, mpToken(), host, port, m_playerProperties);
			smartfoxRoomClient.JoinedLobby += NodeJoinedLobby;
			smartfoxRoomClient.RoomOptionsChanged += NodeRoomUpdated;
			smartfoxRoomClient.RoomAdded += NodeRoomAdded;
			smartfoxRoomClient.RoomRemoved += NodeRoomRemoved;
			smartfoxRoomClient.AuthenticationError += NodeAuthenticationError;
			smartfoxRoomClient.InternalError += NodeInternalError;
			smartfoxRoomClient.SocketError += NodeSocketError;
			smartfoxRoomClient.ServerCommand += NodeServerCommand;
			smartfoxRoomClient.Connect("SteamRealtime");
			Nodes = new List<SmartfoxRoomClient> { smartfoxRoomClient };
			return smartfoxRoomClient;
		}

		private void NodeServerCommand(string cmd, ISFSObject parameters)
		{
			if (cmd == "na" && m_nodeTryingToJoinRoom != null)
			{
				switch (parameters.GetUtfString("em"))
				{
				case "bu":
				case "nac":
					SetJoinRoomErrorAndDisconnect(401, LocalizationKeys.voteresult_ban_self.Localized());
					break;
				case "nfr":
					SetJoinRoomErrorAndDisconnect(402, LocalizationKeys.network_room_not_exist.Localized());
					break;
				case "fr":
					SetJoinRoomErrorAndDisconnect(403, LocalizationKeys.network_game_room_full.Localized());
					break;
				default:
					SetJoinRoomErrorAndDisconnect(505, LocalizationKeys.unknown_error.Localized());
					break;
				}
			}
		}

		private void GetOrCreateRoomRequest(SmartfoxRoomOptions options, Action<MasterRoomCreatedInfo, bool> callback)
		{
			Dictionary<string, string> dictionary = argsMpToken();
			MasterRoomCreationOptions masterRoomCreationOptions = new MasterRoomCreationOptions();
			masterRoomCreationOptions.name = options.Name;
			masterRoomCreationOptions.publicAttributes = options.ToMasterServerRoomSettings();
			if (string.IsNullOrEmpty(masterRoomCreationOptions.name))
			{
				GameLog.Print(AlertLevel.Error, "System", "SmartfoxLobby>CreateRoom failed: generic error");
				callback.SafeCall(null, param2: false);
				return;
			}
			dictionary.Add("description", JSON.Serialize(masterRoomCreationOptions));
			dictionary.Add("zone", "SteamRealtime");
			masterRoomCreationOptions = JSON.Parse<MasterRoomCreationOptions>(dictionary["description"]);
			if (string.IsNullOrEmpty(masterRoomCreationOptions.name))
			{
				GameLog.Print(AlertLevel.Error, "System", "SmartfoxLobby>CreateRoom failed: serializator error");
				callback.SafeCall(null, param2: false);
				return;
			}
			bool isDifferentPlatformRoom = NetworkController.InstanceGame.IsDifferentPlatformRoom(options.CurrentCrossPlayMask);
			masterClient().CallPost("getOrCreateRoomByDescription", delegate(MasterRoomCreatedInfo info, Error error)
			{
				bool param = false;
				if (error.type == Error.EType.None)
				{
					RestrictAcessMpByAdmin = info.restrictType == AdminBanReason.ACCESS_MP.ToString();
					if (RestrictAcessMpByAdmin)
					{
						RestrictAcessMpByAdminTimeEnd = info.restrictTimeEnd;
						State = ClientState.JoinedLobby;
						ShowAdminRestrictAccessError(RestrictAcessMpByAdminTimeEnd, LocalizationKeys.mp_ban_date_msg);
					}
					if (RestrictCrossMpByAdmin && isDifferentPlatformRoom)
					{
						State = ClientState.JoinedLobby;
						ShowAdminRestrictAccessError(RestrictCrossMpByAdminTimeEnd, LocalizationKeys.admin_restricted_cross_mp);
					}
					param = ((!isDifferentPlatformRoom) ? (!RestrictAcessMpByAdmin) : (!RestrictAcessMpByAdmin && !RestrictCrossMpByAdmin));
				}
				else
				{
					State = ClientState.JoinedLobby;
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Multiplayer master server creating room: " + error.debugMessage);
					this.CreateRoomError?.Invoke(error.type, error.guiMessage);
				}
				callback.SafeCall(info, param);
			}, null, dictionary);
			State = ClientState.RoomCreating;
		}

		private void ProcessNodeConnection(string host, int port)
		{
			SmartfoxRoomClient smartfoxRoomClient = ConnectToNode(host, port);
			DisconnectFromNodesExcept(smartfoxRoomClient);
			m_nodeToJoinRoom = smartfoxRoomClient;
		}

		private void NodeJoinedLobby(SmartfoxClient client)
		{
			if (State != ClientState.RoomCreating)
			{
				State = ClientState.JoinedLobby;
			}
			if (this.JoinedLobby != null)
			{
				this.JoinedLobby();
			}
		}

		private void NodeRoomUpdated(SmartfoxRoomClient client, SmartfoxRoomOptions options, HashSet<string> changedProps)
		{
			this.RoomUpdated(client, options);
		}

		private void NodeRoomAdded(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			this.RoomAdded(client, room);
		}

		private void NodeRoomRemoved(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			this.RoomRemoved(client, room);
		}

		private void OnMasterRoomCreated(RoomInfo roomInfo)
		{
			SmartfoxRoomClient smartfoxRoomClient = ConnectToNode(roomInfo.publicIp, roomInfo.clientPort);
			DisconnectFromNodesExcept(smartfoxRoomClient);
			m_nodeToJoinRoom = smartfoxRoomClient;
			m_idToJoinRoom = roomInfo.name;
		}

		private void JoinRoom(SmartfoxRoomClient nodeForRoom, string roomName)
		{
			m_nodeTryingToJoinRoom = nodeForRoom;
			m_joinRoomTimeOut = Time.unscaledTime + 15f;
			if (m_nodeTryingToJoinRoom != null)
			{
				nodeForRoom.JoinedRoom += OnNodeJoinedRoom;
				nodeForRoom.JoinRoomError += OnJoinRoomError;
				nodeForRoom.OnJoinRoom(roomName);
			}
			else
			{
				OnJoinRoomError(0, "Room not found!");
			}
		}

		private void OnNodeJoinedRoom(SmartfoxRoomClient client, SmartfoxRoom room)
		{
			if (m_nodeTryingToJoinRoom != null)
			{
				if (this.JoinedRoom != null)
				{
					this.JoinedRoom(m_nodeTryingToJoinRoom, room);
				}
				m_nodeTryingToJoinRoom.JoinedRoom -= OnNodeJoinedRoom;
				m_nodeTryingToJoinRoom.JoinRoomError -= OnJoinRoomError;
				m_nodeTryingToJoinRoom = null;
			}
		}

		private void OnJoinRoomError(short code, string message)
		{
			State = ClientState.JoinedLobby;
			m_nodeTryingToJoinRoom = null;
			if (this.JoinRoomError != null)
			{
				this.JoinRoomError(code, message);
			}
		}

		private void NodeAuthenticationError(short code, string message)
		{
			GameLog.Print(AlertLevel.Warning, "Multiplayer", "Node authentication error, code: " + code + ", message:" + message);
			OnJoinRoomError(code, message.Replace("Steam", string.Empty));
		}

		private void NodeInternalError(string message)
		{
			State = ClientState.JoinedLobby;
			GameLog.Print(AlertLevel.Warning, "Multiplayer", "Node internal error:" + message);
		}

		private void NodeSocketError(short code, string message)
		{
			State = ClientState.Disconnected;
			GameLog.Print(AlertLevel.Warning, "Multiplayer", "Node socket error, code: " + code + ", message:" + message);
		}

		private Dictionary<string, string> argsMpToken()
		{
			return new Dictionary<string, string> { 
			{
				"token",
				mpToken()
			} };
		}

		private string mpToken()
		{
			return "{\"token\":\"" + GameSync.token + "\", \"authority\":\"STEAM\"}";
		}

		private static ApiClient masterClient()
		{
			if (m_apiClient == null || m_apiClient.BasePath != WebConfigManager.MultiplayerMasterServerUrl)
			{
				m_apiClient = new ApiClient(WebConfigManager.MultiplayerMasterServerUrl, delegate(Error error)
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Multiplayer master server: " + error.debugMessage);
				});
			}
			return m_apiClient;
		}
	}
}
