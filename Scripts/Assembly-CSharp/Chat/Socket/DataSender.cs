using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Chat.Socket
{
	public class DataSender
	{
		private const int BufferSize = 4096;

		private const int LoopDelayMs = 10;

		private WebSocket m_socket;

		private EventHandler m_eventHandler;

		private byte[] m_buffer;

		private Queue<string> m_sendDataQueue;

		private CancellationTokenSource m_sendTaskCts;

		private CancellationToken m_cancellationToken;

		public void Setup(WebSocket socket, EventHandler eventHandler)
		{
			m_socket = socket;
			m_eventHandler = eventHandler;
			m_buffer = new byte[4096];
			m_sendDataQueue = new Queue<string>();
			m_sendTaskCts = new CancellationTokenSource();
			m_cancellationToken = m_sendTaskCts.Token;
			UniTask.RunOnThreadPool((Func<UniTask>)SendLoopTask, true, m_cancellationToken).Forget();
		}

		public void Release()
		{
			m_sendTaskCts?.Cancel();
			m_sendTaskCts = null;
		}

		private void InternalRelease()
		{
			m_sendDataQueue = null;
			m_buffer = null;
			m_eventHandler = null;
			m_socket = null;
		}

		public void Send(string data)
		{
			m_sendDataQueue.Enqueue(data);
		}

		private async UniTask SendLoopTask()
		{
			while (!m_cancellationToken.IsCancellationRequested)
			{
				await SendDataAsync();
				await UniTask.Delay(10, ignoreTimeScale: false, PlayerLoopTiming.Update, m_cancellationToken);
			}
			InternalRelease();
		}

		private async UniTask SendDataAsync()
		{
			try
			{
				if (m_socket.State == WebSocketState.Open)
				{
					while (m_sendDataQueue.Count > 0 && !m_cancellationToken.IsCancellationRequested)
					{
						string text = m_sendDataQueue.Dequeue();
						int bytes = Encoding.UTF8.GetBytes(text, 0, text.Length, m_buffer, 0);
						ArraySegment<byte> buffer = new ArraySegment<byte>(m_buffer, 0, bytes);
						await m_socket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, m_cancellationToken);
					}
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
