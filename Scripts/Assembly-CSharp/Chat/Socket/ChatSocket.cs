using System;
using System.Net.WebSockets;

namespace Chat.Socket
{
	public class ChatSocket
	{
		private const int InternalPingSecondsInterval = 3;

		private ClientWebSocket m_socket;

		private DataReceiver m_dataReceiver;

		private DataSender m_dataSender;

		private EventHandler m_eventHandler;

		private SocketConnector m_connector;

		private bool m_initialized;

		public DataSender dataSender => m_dataSender;

		public SocketConnector connector => m_connector;

		public bool IsInitialized()
		{
			return m_initialized;
		}

		public void Setup(EventHandler eventHandler)
		{
			if (!IsInitialized())
			{
				m_socket = new ClientWebSocket();
				m_eventHandler = eventHandler;
				m_dataReceiver = new DataReceiver();
				m_dataReceiver.Setup(m_socket, m_eventHandler);
				m_dataSender = new DataSender();
				m_dataSender.Setup(m_socket, m_eventHandler);
				m_connector = new SocketConnector();
				m_connector.Setup(m_socket, m_eventHandler);
				m_initialized = true;
			}
		}

		public void Release()
		{
			if (IsInitialized())
			{
				m_initialized = false;
				m_socket.Dispose();
				m_connector.Release();
				m_connector = null;
				m_dataSender.Release();
				m_dataSender = null;
				m_dataReceiver.Release();
				m_dataReceiver = null;
				m_socket = null;
				m_eventHandler = null;
			}
		}

		public void Connect(string host, ClientInfo clientInfo)
		{
			if (IsInitialized() && connector.CanConnect())
			{
				SetupSocketOptions();
				FillClientHeaders(clientInfo);
				m_connector.Connect(BuildUri(host));
			}
		}

		private void FillClientHeaders(ClientInfo clientInfo)
		{
			m_socket.Options.SetRequestHeader("Version", clientInfo.apiVersion.ToString());
			m_socket.Options.SetRequestHeader("Network-Version", clientInfo.networkVersion);
			m_socket.Options.SetRequestHeader("Nick-Name", clientInfo.nickName.EncodeBase64());
			m_socket.Options.SetRequestHeader("Token", clientInfo.token);
			m_socket.Options.SetRequestHeader("Token-Type", clientInfo.authType.ToString());
			m_socket.Options.SetRequestHeader("External-Id", clientInfo.externalId);
			m_socket.Options.SetRequestHeader("Temporal-Token", clientInfo.temporalToken);
		}

		private void SetupSocketOptions()
		{
			m_socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(3.0);
		}

		private Uri BuildUri(string host)
		{
			host = host.Replace("https", "wss");
			host = host.Replace("http", "ws");
			return new Uri(host + "/ws/chat?project=dro");
		}
	}
}
