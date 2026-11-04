using System;
using Chat.Protocol;
using Chat.Socket;

namespace Chat
{
	public class ChatClient
	{
		private ClientInfo m_clientInfo;

		private ChatSocket m_socket;

		private Chat.Socket.EventHandler m_eventHandler;

		private ChatProtocolSerializer m_serializer;

		private EventDispatcher m_dispatcher;

		public EventDispatcher dispatcher => m_dispatcher;

		public Chat.Socket.EventHandler eventHandler => m_eventHandler;

		public bool IsConnected()
		{
			if (m_socket.IsInitialized())
			{
				return m_socket.connector.IsConnected();
			}
			return false;
		}

		public bool CanConnect()
		{
			if (m_socket.IsInitialized())
			{
				if (m_socket.IsInitialized())
				{
					return m_socket.connector.CanConnect();
				}
				return false;
			}
			return true;
		}

		public ChatClient(ClientInfo clientInfo)
		{
			m_clientInfo = clientInfo;
			m_socket = new ChatSocket();
			m_eventHandler = new Chat.Socket.EventHandler();
			m_serializer = new ChatProtocolSerializer();
			m_dispatcher = new EventDispatcher();
			m_dispatcher.Setup(m_eventHandler);
			m_eventHandler.onError += Disconnect;
			m_eventHandler.onDisconnected += Disconnect;
		}

		public void Release()
		{
			m_eventHandler.onError -= Disconnect;
			m_eventHandler.onDisconnected -= Disconnect;
			m_dispatcher.Release();
			m_dispatcher = null;
			m_serializer = null;
			m_eventHandler = null;
			m_socket.Release();
			m_socket = null;
		}

		public void Connect(string host)
		{
			m_socket.Setup(m_eventHandler);
			m_socket.Connect(host, m_clientInfo);
		}

		public void Disconnect()
		{
			m_socket.Release();
		}

		public void BrowseRooms()
		{
			InternalSend(m_serializer.BrowseRooms(20, 0));
		}

		public void JoinRoom(Guid roomId, string password)
		{
			InternalSend(m_serializer.JoinRoom(roomId, password));
		}

		public void LeaveRoom(Guid roomId)
		{
			InternalSend(m_serializer.LeaveRoom(roomId));
		}

		public void GetAndJoinMpRoom(Guid roomId, string password)
		{
			InternalSend(m_serializer.GetAndJoinMpRoom(roomId, 16, password));
		}

		public void SendMessage(TargetInfo target, Guid id, string message)
		{
			InternalSend(m_serializer.SendMessage(target, id, message));
		}

		public void Ping()
		{
			InternalSend(m_serializer.Ping());
		}

		private void InternalSend(string data)
		{
			if (IsConnected())
			{
				GameLog.Print(AlertLevel.Notify, "Chat", "Send data: " + data);
				m_socket.dataSender.Send(data);
			}
		}
	}
}
