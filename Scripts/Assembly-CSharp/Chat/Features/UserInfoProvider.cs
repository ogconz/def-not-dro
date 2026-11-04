using System;
using System.Collections.Generic;
using DI;

namespace Chat.Features
{
	public class UserInfoProvider
	{
		private const int CacheLimit = 100;

		private const int OverflowBuffer = 20;

		private GamePrefs m_prefs;

		private Dictionary<Guid, UserInfo> m_cache;

		public UserInfoProvider()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_cache = m_prefs.globals.chatPrefsDto.usersCache;
			ClearInvalidUsers();
		}

		public UserInfo GetFromMessage(Message message)
		{
			if (TryGet(message.userId, out var user))
			{
				return user;
			}
			user = new UserInfo(message.userId, message.username);
			Add(user.GetId(), user);
			return user;
		}

		public bool TryGet(Guid id, out UserInfo user)
		{
			return m_cache.TryGetValue(id, out user);
		}

		public bool Exist(Guid id)
		{
			return m_cache.ContainsKey(id);
		}

		public void Add(Guid id, UserInfo user)
		{
			if (user.IsValid())
			{
				ProcessCacheOverflow();
				m_cache[id] = user;
				Save();
			}
		}

		public void Remove(Guid id)
		{
			m_cache.Remove(id);
			Save();
		}

		private void Save()
		{
			m_prefs.globals.Save();
		}

		private void ProcessCacheOverflow()
		{
			if (m_cache.Count >= 120)
			{
				List<UserInfo> descendingSortedUsers = GetDescendingSortedUsers();
				for (int i = 100; i < descendingSortedUsers.Count; i++)
				{
					m_cache.Remove(descendingSortedUsers[i].GetId());
				}
			}
		}

		private List<UserInfo> GetDescendingSortedUsers()
		{
			List<UserInfo> list = new List<UserInfo>(m_cache.Values);
			list.Sort((UserInfo a, UserInfo b) => DateTime.Compare(a.GetLastUsedTime(), b.GetLastUsedTime()));
			return list;
		}

		private void ClearInvalidUsers()
		{
			List<Guid> list = new List<Guid>();
			foreach (var (item, userInfo2) in m_cache)
			{
				if (!userInfo2.IsValid())
				{
					list.Add(item);
				}
			}
			foreach (Guid item2 in list)
			{
				Remove(item2);
			}
		}
	}
}
