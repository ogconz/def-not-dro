using UnityEngine;

namespace GameOverlay
{
	public class PS4Player : IPlayer
	{
		private Texture2D m_avatar;

		public PlayerId id { get; set; }

		public Texture2D avatar
		{
			get
			{
				return m_avatar;
			}
			set
			{
				m_avatar = value;
			}
		}

		public string nickName { get; set; }

		public PlayerState state
		{
			get
			{
				return PlayerState.Online;
			}
			set
			{
				Debug.LogWarning("IPS4Player>state: set not allowed!");
			}
		}
	}
}
