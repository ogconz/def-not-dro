using System;

namespace TrackLayout.Style
{
	[Serializable]
	public struct CheckpointStyle
	{
		public CheckpointType type;

		public string referenceName;

		public float bias;
	}
}
