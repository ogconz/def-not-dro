using UnityEngine;

namespace GameOverlay
{
	public class XboxPlayer : IPlayer
	{
		public PlayerId id { get; set; }

		public Texture2D avatar { get; set; }

		public string nickName { get; set; }

		public PlayerState state
		{
			get
			{
				return PlayerState.Online;
			}
			set
			{
				Debug.LogWarning("XboxPlayer>state: set not allowed!");
			}
		}
	}
}
