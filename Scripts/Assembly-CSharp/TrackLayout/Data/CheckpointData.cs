using System;

namespace TrackLayout.Data
{
	[Serializable]
	public struct CheckpointData
	{
		public CheckpointType type;

		public float distance;

		public int zoneID;
	}
}
