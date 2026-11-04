using System;
using DI;

namespace Chat.Features
{
	public class ChannelHolder
	{
		private GamePrefs m_prefs;

		public ChannelHolder()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public bool HasSelectedChannel()
		{
			return GetSelectedChannel() != Guid.Empty;
		}

		public Guid GetSelectedChannel()
		{
			return m_prefs.globals.chatPrefsDto.selectedChannel;
		}

		public void SetSelectedChannel(Guid id)
		{
			if (GetSelectedChannel() != id)
			{
				m_prefs.globals.chatPrefsDto.selectedChannel = id;
				Save();
			}
		}

		public void ResetSelectedChannel()
		{
			SetSelectedChannel(Guid.Empty);
		}

		private void Save()
		{
			m_prefs.globals.Save();
		}
	}
}
