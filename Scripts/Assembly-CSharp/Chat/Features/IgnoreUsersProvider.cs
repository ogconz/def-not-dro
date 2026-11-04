using System;
using System.Collections.Generic;
using DI;

namespace Chat.Features
{
	public class IgnoreUsersProvider
	{
		private GamePrefs m_prefs;

		private HashSet<UserInfo> m_ignorelist;

		public event Action onListChanged;

		public IgnoreUsersProvider()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_ignorelist = m_prefs.globals.chatPrefsDto.ignoredPlayers;
			ClearInvalidUsers();
		}

		public IReadOnlyCollection<UserInfo> GetAll()
		{
			return m_ignorelist;
		}

		public bool Exist(UserInfo user)
		{
			return m_ignorelist.Contains(user);
		}

		public void Add(UserInfo user)
		{
			if (user.IsValid())
			{
				m_ignorelist.Add(user);
				Save();
			}
		}

		public void Remove(UserInfo user)
		{
			m_ignorelist.Remove(user);
			Save();
		}

		private void Save()
		{
			m_prefs.globals.Save();
			this.onListChanged.SafeCall();
		}

		private void ClearInvalidUsers()
		{
			if (m_ignorelist.RemoveWhere((UserInfo x) => !x.IsValid()) > 0)
			{
				Save();
			}
		}
	}
}
