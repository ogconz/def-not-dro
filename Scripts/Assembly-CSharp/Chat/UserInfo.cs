using System;
using SimpleJSON;

namespace Chat
{
	[Serializable]
	public class UserInfo : IEquatable<UserInfo>
	{
		[JsonSerialize]
		private DateTime m_lastUsedTime;

		[JsonSerialize]
		private Guid m_id;

		[JsonSerialize]
		private string m_username;

		public UserInfo()
		{
			m_id = Guid.Empty;
			m_username = string.Empty;
			UpdateRecord();
		}

		public UserInfo(Guid id, string username)
		{
			m_id = id;
			m_username = username;
			UpdateRecord();
		}

		public bool IsValid()
		{
			return GetId() != Guid.Empty;
		}

		public Guid GetId()
		{
			UpdateRecord();
			return m_id;
		}

		public string GetUsername()
		{
			UpdateRecord();
			return m_username;
		}

		public void SetUsername(string newUsername)
		{
			UpdateRecord();
			m_username = newUsername;
		}

		public DateTime GetLastUsedTime()
		{
			return m_lastUsedTime;
		}

		private void UpdateRecord()
		{
			m_lastUsedTime = DateTime.Now;
		}

		public bool Equals(UserInfo other)
		{
			if (other != null)
			{
				return m_id.Equals(other.m_id);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is UserInfo other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_id.GetHashCode();
		}
	}
}
