using System;
using UnityEngine;

namespace TrackLayout.Data
{
	[Serializable]
	public struct MinimapData
	{
		public Texture background;

		public Vector2 minPosition;

		public Vector2 maxPosition;
	}
}
