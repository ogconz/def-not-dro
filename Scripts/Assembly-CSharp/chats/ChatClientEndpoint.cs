using System;
using BestHTTP.WebSocket;

namespace chats
{
	public class ChatClientEndpoint
	{
		public string uri { get; private set; }

		public WebSocket websocket { get; private set; }

		public bool isConnected { get; private set; }

		public Action<byte[]> MessageHandler { get; set; }

		public Action OnLostConnection { get; set; }

		public ChatClientEndpoint(string uri)
		{
			this.uri = uri;
			CreateSocketAndConnect();
		}

		private void CreateSocketAndConnect()
		{
			if (websocket != null)
			{
				WebSocket webSocket = websocket;
				webSocket.OnOpen = (OnWebSocketOpenDelegate)Delegate.Remove(webSocket.OnOpen, new OnWebSocketOpenDelegate(OnOpen));
				WebSocket webSocket2 = websocket;
				webSocket2.OnError = (OnWebSocketErrorDelegate)Delegate.Remove(webSocket2.OnError, new OnWebSocketErrorDelegate(OnError));
				WebSocket webSocket3 = websocket;
				webSocket3.OnClosed = (OnWebSocketClosedDelegate)Delegate.Remove(webSocket3.OnClosed, new OnWebSocketClosedDelegate(OnClose));
				WebSocket webSocket4 = websocket;
				webSocket4.OnBinary = (OnWebSocketBinaryDelegate)Delegate.Remove(webSocket4.OnBinary, new OnWebSocketBinaryDelegate(OnMessage));
			}
			isConnected = false;
			websocket = new WebSocket(new Uri(uri));
			WebSocket webSocket5 = websocket;
			webSocket5.OnOpen = (OnWebSocketOpenDelegate)Delegate.Combine(webSocket5.OnOpen, new OnWebSocketOpenDelegate(OnOpen));
			WebSocket webSocket6 = websocket;
			webSocket6.OnError = (OnWebSocketErrorDelegate)Delegate.Combine(webSocket6.OnError, new OnWebSocketErrorDelegate(OnError));
			WebSocket webSocket7 = websocket;
			webSocket7.OnClosed = (OnWebSocketClosedDelegate)Delegate.Combine(webSocket7.OnClosed, new OnWebSocketClosedDelegate(OnClose));
			WebSocket webSocket8 = websocket;
			webSocket8.OnBinary = (OnWebSocketBinaryDelegate)Delegate.Combine(webSocket8.OnBinary, new OnWebSocketBinaryDelegate(OnMessage));
			websocket.Open();
		}

		public void Connect()
		{
			if (websocket.State != WebSocketStates.Connecting && !websocket.IsOpen)
			{
				if (websocket.State != WebSocketStates.Closed)
				{
					websocket.Close();
				}
				CreateSocketAndConnect();
			}
		}

		public void Disconnect()
		{
			if (websocket.IsOpen)
			{
				websocket.Close();
			}
		}

		public void Send(byte[] data)
		{
			if (isConnected)
			{
				websocket.Send(data);
			}
		}

		private void OnOpen(WebSocket sender)
		{
			isConnected = true;
			sender.StartPingThread = true;
		}

		private void OnMessage(WebSocket sender, byte[] data)
		{
			MessageHandler(data);
		}

		private void OnClose(WebSocket sender, ushort code, string message)
		{
			sender.StartPingThread = false;
			GameLog.Print(AlertLevel.Warning, "System", "Chat lost connection: " + message);
			isConnected = false;
			OnLostConnection.Call();
		}

		private void OnError(WebSocket sender, string reason)
		{
			sender.StartPingThread = false;
			GameLog.Print(AlertLevel.Warning, "System", "Chat error: " + reason);
			isConnected = sender.IsOpen;
			OnLostConnection.Call();
		}
	}
}
