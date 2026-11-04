using Epic.OnlineServices;
using UnityEngine;

namespace GameOverlay
{
	public class EOSFriend : IPlayer
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
				Debug.LogWarning("EOSFriend > id: set not allowed!");
			}
		}

		public EpicAccountId epicId { get; set; }

		public Texture2D avatar { get; set; }

		public string nickName { get; set; }

		public PlayerState state { get; set; }

		public string country { get; set; }

		public EOSFriend(EpicAccountId eosId)
		{
			m_friendId = new PlayerId(UserPlatform.Id.EpicGames, eosId.ToString());
		}
	}
}
