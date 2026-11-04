using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Logging;
using Sfs2X.Requests;
using Sfs2X.Util;
using SmartfoxClientServices;
using UnityEngine;

namespace SyncMultiplayer
{
	public class SmartfoxClient
	{
		private class InnerEventQueue
		{
			private Queue<(BaseEvent arg, EventListenerDelegate action)> m_queue;

			public int Count => m_queue.Count;

			public InnerEventQueue(int capacity)
			{
				m_queue = new Queue<(BaseEvent, EventListenerDelegate)>(capacity);
			}

			public void AddToQueue(BaseEvent evt, EventListenerDelegate callback)
			{
				m_queue.Enqueue((evt, callback));
			}

			public void Clear()
			{
				m_queue.Clear();
			}

			public bool ProcessQueue()
			{
				if (m_queue.Count > 0)
				{
					(BaseEvent, EventListenerDelegate) tuple = m_queue.Dequeue();
					if (tuple.Item2 != null && tuple.Item1 != null)
					{
						tuple.Item2(tuple.Item1);
					}
					return true;
				}
				return false;
			}
		}

		private const string REALTIME_DATA_CMD = "1";

		private const string NOT_ALLOWED_ENTER_CMD = "na";

		private const short ERROR_ALREADY_SUBSCRIBED = 24;

		private const string REALTIME_DATA_USER_KEY = "_";

		private const string REALTIME_DATA_RECIPIENTS = "rs";

		private SfsUdpMeasurer m_udpMeasurer;

		private string m_nickname;

		private string m_token;

		private string m_zone;

		private string m_host;

		private int m_udpPort;

		private SmartfoxPlayerProperties m_playerProperties;

		private ISmartfoxEventsHandler m_eventsHandler;

		private float m_lastInitUDPTime;

		private float m_extensionRequestSentTime;

		private float m_extensionRequestReceiveTime;

		private const float INIT_UDP_PERIOD = 3f;

		private const int EXTENSION_DISCONNECT_TIMEOUT = 15;

		private InnerEventQueue m_innerEventQueue = new InnerEventQueue(22);

		public SmartFox Sfs { get; private set; }

		public static int SmartfoxRTT { get; private set; } = -1;

		public ClientState State { get; private set; }

		public SmartfoxClient(string nickname, string token, ISmartfoxEventsHandler eventsHandler, SmartfoxPlayerProperties playerProperties)
		{
			m_nickname = nickname;
			m_token = token;
			m_playerProperties = playerProperties;
			m_eventsHandler = eventsHandler;
			bool debug = false;
			Sfs = new SmartFox(debug);
			Sfs.ThreadSafeMode = true;
			Sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
			Sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			Sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
			Sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
			Sfs.AddEventListener(SFSEvent.ROOM_GROUP_SUBSCRIBE, OnJoinLobby);
			Sfs.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVariableUpdate);
			Sfs.AddEventListener(SFSEvent.ROOM_GROUP_SUBSCRIBE_ERROR, OnJoinLobbyError);
			Sfs.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdd);
			Sfs.AddEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemoved);
			Sfs.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomAddError);
			Sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
			Sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
			Sfs.AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, OnRoomVariablesUpdate);
			Sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
			Sfs.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessage);
			Sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
			Sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
			Sfs.AddEventListener(SFSEvent.SOCKET_ERROR, OnSocketError);
			Sfs.AddEventListener(SFSEvent.UDP_INIT, OnUDPInit);
			Sfs.AddEventListener(SFSEvent.CRYPTO_INIT, OnCryptoInit);
			Sfs.AddEventListener(SFSEvent.PING_PONG, OnPing);
			Sfs.AddLogListener(LogLevel.ERROR, OnInternalError);
			Sfs.AddLogListener(LogLevel.WARN, OnInternalWarning);
			m_udpMeasurer = new SfsUdpMeasurer(Sfs, OnUdpPingHandler);
		}

		private void OnPing(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnPingHandler);
			}
			else
			{
				OnPingHandler(evt);
			}
		}

		private void OnPingHandler(BaseEvent evt)
		{
			SmartfoxRTT = (int)evt.Params["lagValue"];
			AnalyticsService.SendSfxPingTcp(SmartfoxRTT);
			m_udpMeasurer?.MeasurePing();
		}

		private void OnUdpPingHandler(int ping)
		{
			if (ping >= 0)
			{
				AnalyticsService.SendSfxPingUdp(SmartfoxRTT);
			}
		}

		private void OnInternalError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnInternalErrorHandler);
			}
			else
			{
				OnInternalErrorHandler(evt);
			}
		}

		private void OnInternalErrorHandler(BaseEvent evt)
		{
			string message = (string)evt.Params["message"];
			m_eventsHandler.OnInternalError(message);
		}

		private void OnInternalWarning(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnInternalWarningHandler);
			}
			else
			{
				OnInternalWarningHandler(evt);
			}
		}

		private void OnInternalWarningHandler(BaseEvent evt)
		{
			string text = (string)evt.Params["message"];
			if (text.Contains("InitUDP()") && Time.realtimeSinceStartup - m_lastInitUDPTime > 3f)
			{
				Debug.LogWarning("Do InitUDP by Internal warning: " + text);
				InitUDP();
			}
		}

		private void InitUDP()
		{
			m_lastInitUDPTime = Time.realtimeSinceStartup;
			Sfs.InitUDP(m_host, m_udpPort);
		}

		public void Connect(string host, int port, string zone)
		{
			if (ClientState.StartedConnection != State)
			{
				State = ClientState.StartedConnection;
				ConfigData configData = new ConfigData();
				configData.Host = host;
				configData.Port = port;
				configData.UdpHost = host;
				configData.UdpPort = port + 1;
				configData.HttpsPort = 8443;
				configData.Zone = zone;
				m_zone = zone;
				m_host = host;
				m_udpPort = configData.UdpPort;
				Sfs.Connect(configData);
			}
		}

		public void Update()
		{
			if (IsNeedForceDisconnect())
			{
				Disconnect();
			}
			if (m_innerEventQueue.Count > 0)
			{
				while (m_innerEventQueue.ProcessQueue())
				{
				}
			}
			Sfs.ProcessEvents();
		}

		public void EarlyUpdate()
		{
			if (State == ClientState.Joined)
			{
				Sfs.ProcessEvents();
			}
		}

		public void Disconnect()
		{
			Sfs.Disconnect();
		}

		public void JoinRoom(string roomName, string password)
		{
			Sfs.Send(new JoinRoomRequest(roomName, password));
		}

		public void LeaveRoom(Room room)
		{
			Sfs.Send(new LeaveRoomRequest(room));
		}

		private void UploadProperties()
		{
			ICollection<UserVariable> collection = m_playerProperties.ToSfsVariables();
			Sfs.MySelf.SetVariables(collection);
			Sfs.Send(new SetUserVariablesRequest(collection));
		}

		public void UpdateProperties(SmartfoxPlayerProperties properties)
		{
			m_playerProperties.SetFromProps(properties);
			ICollection<UserVariable> collection = m_playerProperties.ToChangedSfsVariables();
			Sfs.MySelf.SetVariables(collection);
			Sfs.Send(new SetUserVariablesRequest(collection));
		}

		public void SendServerCommand(string cmd, ISFSObject data, out long sentTime)
		{
			Sfs.Send(new ExtensionRequest(cmd, data, Sfs.LastJoinedRoom, useUDP: false));
			sentTime = PreciseTime.realTimeMs;
		}

		public void Send(ISFSObject data, bool reliable, ICollection<Sfs2X.Entities.User> recipients = null)
		{
			if (Sfs.LastJoinedRoom == null)
			{
				State = ClientState.Disconnected;
				m_eventsHandler.OnSocketError(0, "Connection lost");
			}
			else if (reliable)
			{
				if (recipients == null)
				{
					Sfs.Send(new ObjectMessageRequest(data, Sfs.LastJoinedRoom));
				}
				else
				{
					Sfs.Send(new ObjectMessageRequest(data, Sfs.LastJoinedRoom, recipients));
				}
			}
			else
			{
				if (data == null)
				{
					return;
				}
				data.PutInt("_", Sfs.MySelf.Id);
				if (recipients != null)
				{
					int num = 0;
					int[] array = new int[recipients.Count];
					foreach (Sfs2X.Entities.User recipient in recipients)
					{
						array[num] = recipient.Id;
					}
					data.PutIntArray("rs", array);
				}
				Sfs.Send(new ExtensionRequest("1", data, Sfs.LastJoinedRoom, useUDP: true));
				if (NetworkController.IsLocalCarLoaded())
				{
					m_extensionRequestSentTime = Time.unscaledTime;
				}
			}
		}

		private void OnConnection(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnConnectionHandler);
			}
			else
			{
				OnConnectionHandler(evt);
			}
		}

		private void OnConnectionHandler(BaseEvent evt)
		{
			if ((bool)evt.Params["success"])
			{
				State = ClientState.ConnectedToGameserver;
				Sfs.InitCrypto();
			}
			else
			{
				State = ClientState.Uninitialized;
				GameLog.Print(AlertLevel.Error, "Multiplayer", "OnConnection failed");
			}
		}

		private void Login()
		{
			string token = m_token;
			ISFSObject iSFSObject = new SFSObject();
			iSFSObject.PutUtfString("token", token);
			Sfs.Send(new LoginRequest(m_nickname, "123", m_zone, iSFSObject));
		}

		private void OnConnectionLost(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnConnectionLostHandler);
			}
			else
			{
				OnConnectionLostHandler(evt);
			}
		}

		private void OnConnectionLostHandler(BaseEvent evt)
		{
			State = ClientState.Disconnected;
			string text = (string)evt.Params["reason"];
			GameLog.Print(AlertLevel.Error, "Multiplayer", "Node connection lost, Reason : " + text);
		}

		private void OnLogin(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnLoginHandler);
			}
			else
			{
				OnLoginHandler(evt);
			}
		}

		private void OnLoginHandler(BaseEvent evt)
		{
			State = ClientState.Authenticated;
			InitUDP();
			Sfs.Send(new SubscribeRoomGroupRequest("default"));
			Sfs.EnableLagMonitor(enabled: true);
		}

		private void OnLoginError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnLoginErrorHandler);
			}
			else
			{
				OnLoginErrorHandler(evt);
			}
		}

		private void OnLoginErrorHandler(BaseEvent evt)
		{
			State = ClientState.Uninitialized;
			if (m_eventsHandler != null)
			{
				ExtractCodeAndMessage(evt, out var code, out var message);
				m_eventsHandler.OnLoginError(code, message);
			}
		}

		private void OnJoinLobby(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnJoinLobbyHandler);
			}
			else
			{
				OnJoinLobbyHandler(evt);
			}
		}

		private void OnJoinLobbyHandler(BaseEvent evt)
		{
			State = ClientState.JoinedLobby;
			UploadProperties();
			m_eventsHandler.OnJoinedLobby(this);
		}

		private void OnJoinLobbyError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnJoinLobbyErrorHandler);
			}
			else
			{
				OnJoinLobbyErrorHandler(evt);
			}
		}

		private void OnJoinLobbyErrorHandler(BaseEvent evt)
		{
			State = ClientState.Uninitialized;
			ExtractCodeAndMessage(evt, out var code, out var message);
			if (code == 24)
			{
				OnJoinLobbyHandler(evt);
			}
			else
			{
				m_eventsHandler.OnLoginError(code, message);
			}
		}

		private bool IsNeedSkipEventsFrom(Room room)
		{
			if (State == ClientState.Joined)
			{
				Room room2 = NetworkController.InstanceGame?.Client?.CurrentRoom;
				if (room2 != null && room2 != room)
				{
					return true;
				}
			}
			return false;
		}

		private void OnRoomAdd(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomAddHandler);
			}
			else
			{
				OnRoomAddHandler(evt);
			}
		}

		private void OnRoomAddHandler(BaseEvent evt)
		{
			Room room = (Room)evt.Params["room"];
			if (!IsNeedSkipEventsFrom(room))
			{
				m_eventsHandler.OnRoomAdded(room);
			}
		}

		private void OnRoomRemoved(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomRemovedHandler);
			}
			else
			{
				OnRoomRemovedHandler(evt);
			}
		}

		private void OnRoomRemovedHandler(BaseEvent evt)
		{
			Room room = (Room)evt.Params["room"];
			if (!IsNeedSkipEventsFrom(room))
			{
				m_eventsHandler.OnRoomRemoved(room);
			}
		}

		private void OnRoomAddError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomAddErrorHandler);
			}
			else
			{
				OnRoomAddErrorHandler(evt);
			}
		}

		private void OnRoomAddErrorHandler(BaseEvent evt)
		{
			if (m_eventsHandler != null)
			{
				ExtractCodeAndMessage(evt, out var code, out var message);
				m_eventsHandler.OnRoomCreateError(code, message);
			}
		}

		private void OnRoomJoin(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomJoinHandler);
			}
			else
			{
				OnRoomJoinHandler(evt);
			}
		}

		private void OnRoomJoinHandler(BaseEvent evt)
		{
			State = ClientState.Joined;
			Room room = (Room)evt.Params["room"];
			if (!IsNeedSkipEventsFrom(room))
			{
				m_eventsHandler.OnRoomJoined(room);
			}
		}

		private void OnRoomJoinError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomJoinErrorHandler);
			}
			else
			{
				OnRoomJoinErrorHandler(evt);
			}
		}

		private void OnRoomJoinErrorHandler(BaseEvent evt)
		{
			if (m_eventsHandler != null)
			{
				ExtractCodeAndMessage(evt, out var code, out var message);
				m_eventsHandler.OnRoomJoinError(code, message);
			}
		}

		private void OnRoomVariablesUpdate(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnRoomVariablesUpdateHandler);
			}
			else
			{
				OnRoomVariablesUpdateHandler(evt);
			}
		}

		private void OnRoomVariablesUpdateHandler(BaseEvent evt)
		{
			Room room = (Room)evt.Params["room"];
			if (IsNeedSkipEventsFrom(room))
			{
				return;
			}
			ICollection obj = (ICollection)evt.Params["changedVars"];
			HashSet<string> hashSet = new HashSet<string>();
			foreach (object item in obj)
			{
				hashSet.Add(item as string);
			}
			m_eventsHandler.OnRoomVariablesUpdate(room, hashSet);
		}

		private void OnUserVariableUpdate(BaseEvent evt)
		{
			OnUserVariableUpdateHandler(evt);
		}

		private void OnUserVariableUpdateHandler(BaseEvent evt)
		{
			SFSUser sender = (SFSUser)evt.Params["user"];
			ICollection obj = (ICollection)evt.Params["changedVars"];
			HashSet<string> hashSet = new HashSet<string>();
			foreach (object item in obj)
			{
				hashSet.Add(item as string);
			}
			m_eventsHandler.OnUserVariablesUpdate(sender, hashSet);
		}

		private void OnExtensionResponse(BaseEvent evt)
		{
			bool flag = ((string)evt.Params["cmd"]).Equals("1");
			if (NetworkController.IsLocalCarLoaded() && (m_extensionRequestReceiveTime == 0f || flag))
			{
				m_extensionRequestReceiveTime = Time.unscaledTime;
			}
			if (flag)
			{
				if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.EarlyUpdate)
				{
					_ = PreciseTime.loopPhase;
					_ = 2;
				}
				OnExtensionResponseHandler(evt);
			}
			else if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnExtensionResponseHandler);
			}
			else
			{
				OnExtensionResponseHandler(evt);
			}
		}

		private bool IsNeedForceDisconnect()
		{
			if (!NetworkController.IsLocalCarLoaded())
			{
				return false;
			}
			return m_extensionRequestSentTime - m_extensionRequestReceiveTime > 15f;
		}

		private void OnExtensionResponseHandler(BaseEvent evt)
		{
			string text = (string)evt.Params["cmd"];
			if (!text.Equals("1"))
			{
				m_eventsHandler.OnServerCommand(text, (ISFSObject)evt.Params["params"]);
				return;
			}
			if (Sfs.LastJoinedRoom == null)
			{
				if (State != ClientState.JoinedLobby)
				{
					State = ClientState.Disconnected;
					m_eventsHandler.OnSocketError(0, "Connection lost");
				}
				return;
			}
			ISFSObject iSFSObject = (ISFSObject)evt.Params["params"];
			int num = iSFSObject.GetInt("_");
			if (num == Sfs.MySelf.Id)
			{
				return;
			}
			SFSUser sFSUser = (SFSUser)Sfs.LastJoinedRoom.GetUserById(num);
			if (sFSUser == null)
			{
				NetworkPlayer networkPlayer = null;
				bool flag = false;
				if (NetworkController.InstanceGame != null)
				{
					networkPlayer = NetworkController.InstanceGame.FindPlayerByNetworkId(num);
					flag = NetworkController.InstanceGame.Client?.CurrentRoom != Sfs.LastJoinedRoom;
				}
				if (networkPlayer != null)
				{
					networkPlayer.Ping = (flag ? (-200) : (-100));
				}
			}
			m_eventsHandler.OnRoomMessage(sFSUser, iSFSObject);
		}

		private void OnObjectMessage(BaseEvent evt)
		{
			OnObjectMessageHandler(evt);
		}

		private void OnObjectMessageHandler(BaseEvent evt)
		{
			SFSUser sender = (SFSUser)evt.Params["sender"];
			ISFSObject data = (ISFSObject)evt.Params["message"];
			m_eventsHandler.OnRoomMessage(sender, data);
		}

		private void OnUserEnterRoom(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnUserEnterRoomHandler);
			}
			else
			{
				OnUserEnterRoomHandler(evt);
			}
		}

		private void OnUserEnterRoomHandler(BaseEvent evt)
		{
			Room room = (Room)evt.Params["room"];
			if (!IsNeedSkipEventsFrom(room))
			{
				Sfs2X.Entities.User user = (Sfs2X.Entities.User)evt.Params["user"];
				m_eventsHandler.OnPlayerJoinedRoom(user);
			}
		}

		private void OnUserExitRoom(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnUserExitRoomHandler);
			}
			else
			{
				OnUserExitRoomHandler(evt);
			}
		}

		private void OnUserExitRoomHandler(BaseEvent evt)
		{
			Room room = (Room)evt.Params["room"];
			if (!IsNeedSkipEventsFrom(room))
			{
				Sfs2X.Entities.User user = (Sfs2X.Entities.User)evt.Params["user"];
				m_eventsHandler.OnPlayerLeftRoom(user);
			}
		}

		private void OnUDPInit(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnUDPInitHandler);
			}
			else
			{
				OnUDPInitHandler(evt);
			}
		}

		private void OnUDPInitHandler(BaseEvent evt)
		{
			if (!(bool)evt.Params["success"])
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "UDP failed!");
				GUICommonGodVoice.ShowText("UDP failed!", 3f);
			}
		}

		private void OnCryptoInit(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnCryptoInitHandler);
			}
			else
			{
				OnCryptoInitHandler(evt);
			}
		}

		private void OnCryptoInitHandler(BaseEvent evt)
		{
			if ((bool)evt.Params["success"])
			{
				Login();
				return;
			}
			State = ClientState.Disconnected;
			GUICommonGodVoice.ShowText(LocalizationKeys.error_server_error.Localized(), 3f);
			UILockInputContext.DisableLock();
			GameLog.Print(AlertLevel.FatalError, "Multiplayer", "CryptoInit failed!");
		}

		private void OnSocketError(BaseEvent evt)
		{
			if (PreciseTime.loopPhase != PreciseTime.PlayerLoopPhase.Update)
			{
				m_innerEventQueue.AddToQueue(evt, OnSocketErrorHandler);
			}
			else
			{
				OnSocketErrorHandler(evt);
			}
		}

		private void OnSocketErrorHandler(BaseEvent evt)
		{
			State = ClientState.Disconnected;
			if (m_eventsHandler != null)
			{
				ExtractCodeAndMessage(evt, out var code, out var message);
				m_eventsHandler.OnSocketError(code, message);
			}
		}

		private static void ExtractCodeAndMessage(BaseEvent evt, out short code, out string message)
		{
			code = -1;
			message = "null";
			if (evt != null && evt.Params != null)
			{
				if (evt.Params.ContainsKey("errorCode"))
				{
					code = (short)evt.Params["errorCode"];
				}
				if (evt.Params.ContainsKey("errorMessage"))
				{
					message = (string)evt.Params["errorMessage"];
				}
			}
		}

		public void Destroy()
		{
			Sfs.RemoveAllEventListeners();
			m_eventsHandler = new SmartfoxEventsNOOPHandler();
			try
			{
				Sfs.KillConnection();
			}
			catch
			{
			}
			m_innerEventQueue.Clear();
		}
	}
}
