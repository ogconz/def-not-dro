using Steamworks;
using UnityEngine;

namespace GameOverlay
{
	public class SteamFriend : IPlayer
	{
		private PlayerId m_friendId;

		public PlayerId id
		{
			get
			{
				return m_friendId;
			}
			set
			{
				Debug.LogWarning("SteamFriend > id: set not allowed!");
			}
		}

		public SteamId steamId => (ulong)id.accountId;

		public Texture2D avatar { get; set; }

		public string nickName { get; set; }

		public PlayerState state { get; set; }

		public SteamFriend(ulong steamId)
		{
			m_friendId = new PlayerId(UserPlatform.Id.Steam, steamId);
		}
	}
}
