using System;

namespace TrackLayout.Data
{
	[Serializable]
	public struct SpeedRuleData : IRadarSectorRoad
	{
		public float distance;

		public float length;

		public float deltaV;

		public bool collisionOnly;

		public bool display;

		public bool isDisplayed
		{
			get
			{
				return display;
			}
			set
			{
				display = value;
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
