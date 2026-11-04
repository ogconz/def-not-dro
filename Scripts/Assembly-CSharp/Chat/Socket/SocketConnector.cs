using System;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Chat.Socket
{
	public class SocketConnector
	{
		private ClientWebSocket m_socket;

		private EventHandler m_eventHandler;

		private CancellationTokenSource m_sendTaskCts;

		private CancellationToken m_cancellationToken;

		private Uri m_uri;

		private bool m_connectLoopStarted;

		public void Setup(ClientWebSocket socket, EventHandler eventHandler)
		{
			m_socket = socket;
			m_eventHandler = eventHandler;
			m_sendTaskCts = new CancellationTokenSource();
			m_cancellationToken = m_sendTaskCts.Token;
		}

		public void Release()
		{
			m_sendTaskCts?.Cancel();
			m_sendTaskCts = null;
		}

		private void InternalRelease()
		{
			m_eventHandler = null;
			m_socket = null;
		}

		public bool CanConnect()
		{
			if (!IsConnected() && !IsConnecting())
			{
				return !IsUsed();
			}
			return false;
		}

		public bool IsConnected()
		{
			return m_socket.State == WebSocketState.Open;
		}

		private bool IsConnecting()
		{
			return m_socket.State == WebSocketState.Connecting;
		}

		private bool IsOpen()
		{
			if (!IsConnecting())
			{
				return IsConnected();
			}
			return true;
		}

		private bool IsUsed()
		{
			if (m_socket.State != WebSocketState.Closed)
			{
				return m_socket.State == WebSocketState.Aborted;
			}
			return true;
		}

		public void Connect(Uri uri)
		{
			m_uri = uri;
			GameLog.Print(AlertLevel.Notify, "Chat", $"Build socket, uri: {uri}");
			RunConnectLoopIfNeed();
		}

		private void RunConnectLoopIfNeed()
		{
			if (!m_connectLoopStarted)
			{
				m_connectLoopStarted = true;
				UniTask.RunOnThreadPool((Func<UniTask>)InternalConnectLoopAsync, true, m_cancellationToken).Forget();
			}
		}

		private async UniTask InternalConnectLoopAsync()
		{
			while (!m_cancellationToken.IsCancellationRequested && m_socket != null && !IsUsed())
			{
				if (!IsOpen())
				{
					await ConnectToServerAsync();
				}
				await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, m_cancellationToken);
			}
			if (m_socket != null && m_socket.State == WebSocketState.Open)
			{
				await m_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
			}
			m_eventHandler.OnDisconnectHandler(1000, "Client closing");
			InternalRelease();
			m_connectLoopStarted = false;
		}

		private async UniTask ConnectToServerAsync()
		{
			try
			{
				await m_socket.ConnectAsync(m_uri, m_cancellationToken);
				if (IsConnected())
				{
					m_eventHandler.OnConnectHandler();
				}
				else
				{
					m_eventHandler.OnDisconnectHandler(1001, "Server is unavailable");
				}
			}
			catch (WebSocketException ex)
			{
				m_eventHandler.OnErrorHandler("WebSocket error: " + ex.Message);
			}
			catch (Exception ex2)
			{
				m_eventHandler.OnErrorHandler("An error occurred: " + ex2.Message);
			}
		}
	}
}
