using UnityEngine;

namespace GameOverlay
{
	public class XboxOneFriend : IPlayer
	{
		public PlayerId friendId { get; set; }

		public PlayerId id
		{
			get
			{
				return friendId;
			}
			set
			{
				Debug.LogWarning("XboxFriend > id: set not allowed!");
			}
		}

		public Texture2D avatar { get; set; }

		public string nickName { get; set; }

		public PlayerState state { get; set; }
	}
}
