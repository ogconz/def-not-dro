using System;

namespace TrackLayout.Data
{
	[Serializable]
	public struct ClipZoneData
	{
		public ClipZoneType type;

		public ClipZoneFlagType flag;

		public float factor;

		public float distance;

		public float length;

		public float width;

		public float bias;

		public float height;

		public float triggerWidth;
	}
}
