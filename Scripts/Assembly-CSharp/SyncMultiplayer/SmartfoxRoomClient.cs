using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;

namespace SyncMultiplayer
{
	public class SmartfoxRoomClient : ISmartfoxEventsHandler
	{
		private const short SAME_ROOM_ALREADY_EXISTS_CODE = 24;

		private SmartfoxClient m_client;

		private string m_roomName;

		private List<Sfs2X.Entities.User> m_recipientsCache = new List<Sfs2X.Entities.User>(16);

		public SmartfoxRoomOptions RoomOptions { get; private set; }

		public string Host { get; private set; }

		public int Port { get; private set; }

		public ClientState State => m_client.State;

		public Sfs2X.Entities.User SelfUser => m_client.Sfs.MySelf;

		public Room CurrentRoom => m_client.Sfs.LastJoinedRoom;

		public Room LastJoinedRoom => m_client.Sfs.LastJoinedRoom;

		public List<Room> Rooms => m_client.Sfs.RoomList;

		public event InternalErrorReport InternalError;

		public event ErrorDelegate AuthenticationError;

		public event StateChanged StateChanged;

		public event ClientEvent JoinedLobby;

		public event RoomMessage RoomMessage;

		public event ServerCommand ServerCommand;

		public event PlayerEvent PlayerJoined;

		public event PlayerEvent PlayerLeft;

		public event RoomEvent JoinedRoom;

		public event RoomOptionsEvent RoomOptionsChanged;

		public event UserVariablesUpdate UserVariablesUpdate;

		public event ErrorDelegate JoinRoomError;

		public event ErrorDelegate CreateRoomError;

		public event RoomEvent RoomAdded;

		public event RoomEvent RoomRemoved;

		public event ErrorDelegate SocketError;

		public SmartfoxRoomClient(string nickname, string token, string host, int port, SmartfoxPlayerProperties playerProperties)
		{
			m_client = new SmartfoxClient(nickname, token, this, playerProperties);
			Host = host;
			Port = port;
		}

		public void OnJoinConfirmed()
		{
			TickManager.RegisterEarlyUpdate(EarlyUpdate, 10, "SFS_ProcessEvents");
		}

		public bool IsConnectionDead()
		{
			if (!m_client.Sfs.IsConnected)
			{
				return !m_client.Sfs.IsConnecting;
			}
			return false;
		}

		public bool IsConnected()
		{
			return m_client.State == ClientState.JoinedLobby;
		}

		public Room GetRoomByName(string name)
		{
			return m_client.Sfs.GetRoomByName(name);
		}

		public void Connect(string zone)
		{
			m_client.Connect(Host, Port, zone);
		}

		public void Disconnect()
		{
			m_client.Disconnect();
		}

		public void Update()
		{
			m_client.Update();
		}

		private void EarlyUpdate()
		{
			m_client.EarlyUpdate();
		}

		public void SendRAW(IRequest data)
		{
			m_client.Sfs.Send(data);
		}

		public void UpdatePlayerProperties(SmartfoxPlayerProperties properties)
		{
			m_client.UpdateProperties(properties);
		}

		public void SendChangedRoomOptions(SmartfoxRoomOptions options)
		{
			ICollection<RoomVariable> collection = options.ToChangedSfsVariables();
			if (collection.Count > 0)
			{
				m_client.Sfs.Send(new SetRoomVariablesRequest(collection));
			}
		}

		public void SendServerCommand(string cmd, ISFSObject data, out long sentTime)
		{
			m_client.SendServerCommand(cmd, data, out sentTime);
		}

		public void Send(SmartfoxDataPackage data, bool reliable)
		{
			m_client.Send(data.ToDataPackage(), reliable);
		}

		public void Send(SmartfoxDataPackage data, bool reliable, int[] receivers)
		{
			m_recipientsCache.Clear();
			for (int i = 0; i < receivers.Length; i++)
			{
				Sfs2X.Entities.User userById = CurrentRoom.GetUserById(receivers[i]);
				if (userById != null)
				{
					m_recipientsCache.Add(userById);
				}
			}
			if (m_recipientsCache.Count > 0)
			{
				m_client.Send(data.ToDataPackage(), reliable, m_recipientsCache);
			}
		}

		public void SendIncludingSelf(SmartfoxDataPackage data, bool reliable)
		{
			m_client.Send(data.ToDataPackage(), reliable);
			OnRoomMessage((SFSUser)SelfUser, data);
		}

		public void OnJoinRoom(string roomID)
		{
			m_roomName = roomID;
			m_client.JoinRoom(roomID, null);
		}

		public void OnInternalError(string message)
		{
			UILockInputContext.DisableLock();
			GameLog.Print(AlertLevel.Error, "Multiplayer", "RoomClient internal error: " + message);
			if (this.InternalError != null)
			{
				this.InternalError(message);
			}
		}

		public void OnStateChanged(ClientState previousState, ClientState newState)
		{
			if (this.StateChanged != null)
			{
				this.StateChanged(previousState, newState);
			}
		}

		public void OnJoinedLobby(SmartfoxClient client)
		{
			if (this.JoinedLobby != null)
			{
				this.JoinedLobby(client);
			}
		}

		public void OnRoomJoined(Room room)
		{
			RoomOptions = new SmartfoxRoomOptions(room);
			JOIN_FLAG joinFlag = NetworkGame.GetJoinFlag(RoomOptions);
			if (joinFlag != JOIN_FLAG.ALLOW)
			{
				m_client.LeaveRoom(room);
				if (joinFlag.HasFlag(JOIN_FLAG.WRONG_V))
				{
					this.JoinRoomError(0, "Error: wrong game version");
				}
				else if (joinFlag.HasFlag(JOIN_FLAG.CROSS_DISABLE))
				{
					this.JoinRoomError(609, LocalizationKeys.wrong_mask.Localized());
				}
				else if (joinFlag.HasFlag(JOIN_FLAG.WRONG_TRACK))
				{
					this.JoinRoomError(0, "Error: wrong track id");
				}
				else
				{
					this.JoinRoomError((short)joinFlag, "Error: unknown");
				}
			}
			else if (this.JoinedRoom != null)
			{
				this.JoinedRoom(this, new SmartfoxRoom(room, RoomOptions));
			}
		}

		public void OnUserVariablesUpdate(SFSUser user, HashSet<string> changedProps)
		{
			this.UserVariablesUpdate?.Invoke(this, user, changedProps);
		}

		public void OnRoomVariablesUpdate(Room room, HashSet<string> changedProps)
		{
			if (room != null && room == CurrentRoom)
			{
				if (RoomOptions != null)
				{
					RoomOptions.GetChangedVars(room, changedProps);
				}
				else
				{
					RoomOptions = new SmartfoxRoomOptions(room);
				}
				if (this.RoomOptionsChanged != null)
				{
					this.RoomOptionsChanged(this, RoomOptions, changedProps);
				}
			}
		}

		public void OnRoomJoinError(short code, string message)
		{
			if (this.JoinRoomError != null)
			{
				this.JoinRoomError(code, message);
			}
		}

		public void OnRoomCreateError(short code, string message)
		{
			if (code == 24)
			{
				m_client.JoinRoom(m_roomName, null);
			}
			else if (this.CreateRoomError != null)
			{
				this.CreateRoomError(code, message);
			}
		}

		public void OnPlayerJoinedRoom(Sfs2X.Entities.User user)
		{
			if (this.PlayerJoined != null)
			{
				this.PlayerJoined(user);
			}
		}

		public void OnPlayerLeftRoom(Sfs2X.Entities.User user)
		{
			if (this.PlayerLeft != null)
			{
				this.PlayerLeft(user);
			}
		}

		public void OnRoomMessage(SFSUser sender, ISFSObject data)
		{
			OnRoomMessage(sender, new SmartfoxDataPackage(data));
		}

		public void OnServerCommand(string cmd, ISFSObject parameters)
		{
			if (this.ServerCommand != null)
			{
				this.ServerCommand(cmd, parameters);
			}
		}

		public void OnRoomMessage(SFSUser sender, SmartfoxDataPackage data)
		{
			if (this.RoomMessage != null)
			{
				this.RoomMessage(sender, data);
			}
		}

		public void OnRoomAdded(Room room)
		{
			if (this.RoomAdded != null)
			{
				this.RoomAdded(this, new SmartfoxRoom(room));
			}
		}

		public void OnRoomRemoved(Room room)
		{
			if (this.RoomRemoved != null)
			{
				this.RoomRemoved(this, new SmartfoxRoom(room));
			}
		}

		public void OnLoginError(short code, string message)
		{
			if (this.AuthenticationError != null)
			{
				this.AuthenticationError(code, message);
			}
		}

		public void OnSocketError(short code, string message)
		{
			if (this.SocketError != null)
			{
				this.SocketError(code, message);
			}
		}

		public void Destroy()
		{
			m_client.Destroy();
			TickManager.UnregisterEarlyUpdate(EarlyUpdate);
		}
	}
}
