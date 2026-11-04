using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Chat
{
	public class ConnectionHolder
	{
		private ChatClient m_client;

		private SessionTicketHolder m_sessionTicketHolder;

		private bool m_work;

		public ConnectionHolder(ChatClient client, SessionTicketHolder sessionTicketHolder)
		{
			m_client = client;
			m_sessionTicketHolder = sessionTicketHolder;
		}

		public void Start()
		{
			m_work = true;
			UniTask.RunOnThreadPool((Func<UniTask>)ConnectionHolderTask, true, default(CancellationToken)).Forget();
		}

		public void Stop()
		{
			m_work = false;
		}

		private async UniTask ConnectionHolderTask()
		{
			GameLog.Print(AlertLevel.Warning, "Chat", "Service: Start connection holder");
			while (m_work && m_client != null)
			{
				try
				{
					if (!GameOverlay.Overlay.IsReady())
					{
						await UniTask.Delay(1000);
						continue;
					}
					if (m_client.IsConnected())
					{
						m_client.Ping();
					}
					else if (m_client.CanConnect())
					{
						await ConnectClientAsync();
					}
					await UniTask.Delay(m_client.IsConnected() ? 10000 : 1000);
				}
				catch (Exception arg)
				{
					GameLog.Print(AlertLevel.Error, "Chat", $"ConnectionHolder: Error in loop: {arg}");
					await UniTask.Delay(15000);
				}
			}
			GameLog.Print(AlertLevel.Warning, "Chat", "Service: Stop connection holder");
		}

		private async UniTask ConnectClientAsync()
		{
			await m_sessionTicketHolder.UpdateSessionTicket();
			string host = await GetServerUrlAsync();
			m_client.Connect(host);
		}

		private async UniTask<string> GetServerUrlAsync()
		{
			if (WebConfigManager.IsInited)
			{
				return WebConfigManager.ChatServerUrl;
			}
			AutoResetUniTaskCompletionSource<string> tokenResult = AutoResetUniTaskCompletionSource<string>.Create();
			new CancellationTokenSource(20000).Token.Register(delegate
			{
				tokenResult.TrySetCanceled();
			});
			WebConfigManager.OnInitCompleteEvent = (Action)Delegate.Remove(WebConfigManager.OnInitCompleteEvent, new Action(OnInitCompleteHandler));
			WebConfigManager.OnInitCompleteEvent = (Action)Delegate.Combine(WebConfigManager.OnInitCompleteEvent, new Action(OnInitCompleteHandler));
			return await tokenResult.Task;
			void OnInitCompleteHandler()
			{
				WebConfigManager.OnInitCompleteEvent = (Action)Delegate.Remove(WebConfigManager.OnInitCompleteEvent, new Action(OnInitCompleteHandler));
				tokenResult?.TrySetResult(WebConfigManager.ChatServerUrl);
			}
		}
	}
}
