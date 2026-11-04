using UnityEngine;

namespace TrackLayout.Data
{
	[CreateAssetMenu(fileName = "background", menuName = "Track Layout/Background Data")]
	public class BackgroundData : ScriptableObject
	{
		public Texture texture;

		public Vector2 minPosition;

		public Vector2 maxPosition;
	}
}
