using System;
using System.Collections.Generic;
using GameOverlay;

namespace Chat.Overlay.UI.MessageList.Actions
{
	public class MessageActionContainer
	{
		private ChatService m_service;

		private Message m_message;

		private Dictionary<string, Action> m_buffer = new Dictionary<string, Action>();

		public void Setup(ChatService service, Message message)
		{
			m_service = service;
			m_message = message;
			AddBlockUserAction();
			AddOpenProfileAction();
		}

		public void Release()
		{
			m_buffer.Clear();
			m_service = null;
			m_message = null;
		}

		public Dictionary<string, Action> GetActions()
		{
			return m_buffer;
		}

		public bool HasActions()
		{
			return m_buffer.Count > 0;
		}

		private void AddBlockUserAction()
		{
			if (!m_message.isLocalOwner)
			{
				m_buffer[LocalizationKeys.chat_block_user.Localized()] = BlockUser;
			}
		}

		private void BlockUser()
		{
			UserInfo fromMessage = m_service.userInfoProvider.GetFromMessage(m_message);
			m_service.ignoreUsersProvider.Add(fromMessage);
		}

		private void AddOpenProfileAction()
		{
			if (!m_message.isLocalOwner && CanOpenProfile())
			{
				m_buffer[LocalizationKeys.cht_popup_overlayprofile.Localized()] = OpenProfileAction;
			}
		}

		private void OpenProfileAction()
		{
			GameOverlay.Overlay.instance.ShowProfile(m_message.playerId);
		}

		private bool CanOpenProfile()
		{
			bool num = GameOverlay.Overlay.instance.HasProfileSupport();
			UserPlatform.Id platform = GameOverlay.Overlay.instance.player.id.platform;
			if (num)
			{
				return platform == m_message.playerId.platform;
			}
			return false;
		}
	}
}
