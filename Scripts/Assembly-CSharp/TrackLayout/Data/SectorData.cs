using System;

namespace TrackLayout.Data
{
	[Serializable]
	public struct SectorData : IRadarSectorRoad
	{
		public float distance;

		public float length;

		public float weight;

		public float sideChangeFactor;

		public float minSpeed;

		public bool isBlank;

		public float width;

		public bool isDisplayed
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		public float distanceRoad
		{
			get
			{
				return distance;
			}
			set
			{
				distance = value;
			}
		}

		public float lengthRoad
		{
			get
			{
				return length;
			}
			set
			{
				length = value;
			}
		}
	}
}
