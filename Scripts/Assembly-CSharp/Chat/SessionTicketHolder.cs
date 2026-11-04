using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Chat
{
	public class SessionTicketHolder
	{
		private ClientInfo m_clientInfo;

		private ChatClient m_client;

		private bool m_isTokenHolding;

		public SessionTicketHolder(ClientInfo clientInfo, ChatClient client)
		{
			m_clientInfo = clientInfo;
			m_client = client;
			m_client.eventHandler.onDisconnected += OnDisconnectHandler;
		}

		public void Release()
		{
			m_client.eventHandler.onDisconnected -= OnDisconnectHandler;
			m_clientInfo = null;
			m_client = null;
		}

		private void OnDisconnectHandler()
		{
			if (m_client.eventHandler.lastDisconnectInternalCode == 700)
			{
				InvalidateTicket();
			}
		}

		public async UniTask UpdateSessionTicket()
		{
			if (!IsTicketValid() && !m_isTokenHolding)
			{
				m_isTokenHolding = true;
				await UniTask.SwitchToMainThread();
				await GameOverlay.Overlay.instance.UpdateAuthSessionTicket().ToUniTask(TimeController.instance);
				await UniTask.SwitchToThreadPool();
				m_clientInfo.token = GameOverlay.Overlay.instance.GetAuthSessionTicket();
			}
		}

		private bool IsTicketValid()
		{
			return !string.IsNullOrWhiteSpace(m_clientInfo.temporalToken);
		}

		private void InvalidateTicket()
		{
			m_clientInfo.temporalToken = null;
			m_isTokenHolding = false;
		}
	}
}
