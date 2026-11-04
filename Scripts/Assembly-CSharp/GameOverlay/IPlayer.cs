using UnityEngine;

namespace GameOverlay
{
	public interface IPlayer
	{
		PlayerId id { get; set; }

		Texture2D avatar { get; set; }

		string nickName { get; set; }

		PlayerState state { get; set; }
	}
}
