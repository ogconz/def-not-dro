using System;
using CarX.Platform;

namespace GameOverlay
{
	public struct PlayerId : IEquatable<PlayerId>, IComparable<PlayerId>
	{
		public static PlayerId Invalid = new PlayerId(UserPlatform.Id.Unknown, 0u);

		public UserPlatform.Id platform { get; private set; }

		public AccountId accountId { get; private set; }

		public int platformMask => UserPlatform.ToMask(platform);

		public bool IsInvalid
		{
			get
			{
				if (!accountId.IsInvalid)
				{
					return platform == UserPlatform.Id.Unknown;
				}
				return true;
			}
		}

		public PlayerId(UserPlatform.Id platform, AccountId id)
		{
			this.platform = platform;
			accountId = id;
		}

		public override string ToString()
		{
			return $"{accountId.ToString()}|{(int)platform}";
		}

		public static PlayerId Parse(string str)
		{
			TryParse(str, out var playerId);
			return playerId;
		}

		public static bool TryParse(string str, out PlayerId playerId)
		{
			playerId = Invalid;
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}
			int num = str.IndexOf('|');
			if (num == -1)
			{
				if (ulong.TryParse(str, out var result))
				{
					playerId = new PlayerId(UserPlatform.Id.Steam, result);
					return true;
				}
				return false;
			}
			AccountId id;
			if (!ulong.TryParse(str.Substring(0, num), out var result2))
			{
				id = new AccountId(str.Substring(0, num));
				if (id.IsInvalid)
				{
					return false;
				}
			}
			else
			{
				id = new AccountId(result2);
			}
			if (!int.TryParse(str.Substring(num + 1, str.Length - num - 1), out var result3))
			{
				return false;
			}
			playerId = new PlayerId((UserPlatform.Id)result3, id);
			return true;
		}

		public override bool Equals(object other)
		{
			if (other is PlayerId)
			{
				return this == (PlayerId)other;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return accountId.GetHashCode() ^ (int)platform;
		}

		public static bool operator ==(PlayerId x, PlayerId y)
		{
			if (x.platform == y.platform)
			{
				return x.accountId == y.accountId;
			}
			return false;
		}

		public static bool operator !=(PlayerId x, PlayerId y)
		{
			return !(x == y);
		}

		public bool Equals(PlayerId other)
		{
			return this == other;
		}

		public int CompareTo(PlayerId other)
		{
			return accountId.CompareTo(other.accountId);
		}
	}
}
