using Chat.Protocol;

namespace Chat.EventHandlers
{
	public class PlayerBanDataHandler : IEventHandler
	{
		private bool m_isBanned;

		private string m_reason;

		public bool IsBanned()
		{
			return m_isBanned;
		}

		public string GetReason()
		{
			return m_reason;
		}

		public void AddListeners(EventDispatcher dispatcher)
		{
			dispatcher.AddListener<BanInfoPayload>(BanInfoHandler);
		}

		public void RemoveListeners(EventDispatcher dispatcher)
		{
			dispatcher.RemoveListener<BanInfoPayload>(BanInfoHandler);
		}

		private void BanInfoHandler(BanInfoPayload payload)
		{
			m_isBanned = true;
			m_reason = payload.reason;
		}
	}
}
