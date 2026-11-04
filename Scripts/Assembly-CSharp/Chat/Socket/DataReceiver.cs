using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Chat.Socket
{
	public class DataReceiver
	{
		private const int BufferSize = 4096;

		private const int LoopDelayMs = 10;

		private WebSocket m_socket;

		private EventHandler m_eventHandler;

		private byte[] m_buffer;

		private CancellationTokenSource m_receiveTaskCts;

		private CancellationToken m_cancellationToken;

		public void Setup(WebSocket socket, EventHandler eventHandler)
		{
			m_socket = socket;
			m_eventHandler = eventHandler;
			m_buffer = new byte[4096];
			m_receiveTaskCts = new CancellationTokenSource();
			m_cancellationToken = m_receiveTaskCts.Token;
			UniTask.RunOnThreadPool((Func<UniTask>)ReceiveLoopTask, true, m_cancellationToken).Forget();
		}

		public void Release()
		{
			m_receiveTaskCts?.Cancel();
			m_receiveTaskCts = null;
		}

		private void InternalRelease()
		{
			m_buffer = null;
			m_eventHandler = null;
			m_socket = null;
		}

		private async UniTask ReceiveLoopTask()
		{
			while (!m_cancellationToken.IsCancellationRequested)
			{
				await ReceiveDataAsync();
				await UniTask.Delay(10, ignoreTimeScale: false, PlayerLoopTiming.Update, m_cancellationToken);
			}
			InternalRelease();
		}

		private async UniTask ReceiveDataAsync()
		{
			_ = 1;
			try
			{
				if (m_socket.State == WebSocketState.Open)
				{
					WebSocketReceiveResult result = await m_socket.ReceiveAsync(m_buffer, m_cancellationToken);
					if (result.MessageType == WebSocketMessageType.Close)
					{
						WebSocketCloseStatus status = result.CloseStatus ?? WebSocketCloseStatus.Empty;
						await m_socket.CloseAsync(status, result.CloseStatusDescription, m_cancellationToken);
						m_eventHandler.OnDisconnectHandler((ushort)status, result.CloseStatusDescription);
					}
					if (result.MessageType == WebSocketMessageType.Text)
					{
						string message = Encoding.UTF8.GetString(m_buffer, 0, result.Count);
						m_eventHandler.OnMessageHandler(message);
					}
				}
			}
			catch (WebSocketException ex)
			{
				m_eventHandler.OnConnectionErrorHandler("WebSocket error: " + ex.Message);
			}
			catch (Exception ex2)
			{
				m_eventHandler.OnErrorHandler("An error occurred: " + ex2.Message);
			}
		}
	}
}
