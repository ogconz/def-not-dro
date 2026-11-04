using System;
using UnityEngine;

namespace TrackLayout.Style
{
	[Serializable]
	public struct RadarCheckpointStyle
	{
		public CheckpointType type;

		public Color color;

		public float size;

		public Texture texture;

		public bool aligned;
	}
}
