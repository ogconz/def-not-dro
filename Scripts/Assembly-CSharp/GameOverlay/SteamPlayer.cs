using Steamworks;
using UnityEngine;

namespace GameOverlay
{
	public class SteamPlayer : IPlayer
	{
		public PlayerId id { get; set; }

		public SteamId steamId => (ulong)id.accountId;

		public Texture2D avatar { get; set; }

		public string nickName
		{
			get
			{
				return SteamClient.Name;
			}
			set
			{
				Debug.LogWarning("ISteamPlayer>nickName: set not allowed!");
			}
		}

		public PlayerState state
		{
			get
			{
				return PlayerState.Online;
			}
			set
			{
				if (value != PlayerState.Online)
				{
					Debug.LogWarning($"ISteamPlayer>state: set({value}) not allowed!");
				}
			}
		}
	}
}
