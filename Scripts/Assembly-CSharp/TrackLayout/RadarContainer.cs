using System.Collections.Generic;
using TrackLayout.Data;
using UnityEngine;

namespace TrackLayout
{
	public class RadarContainer
	{
		public Vector3 origin;

		public Vector2 size;

		public Vector3 startPosition;

		public Vector3 finishPosition;

		public Transform root;

		public Transform background;

		public List<Transform> roadsList = new List<Transform>();

		public List<Transform> sectorsList = new List<Transform>();

		public List<Transform> clippingZonesList = new List<Transform>();

		public Transform perfectLine;

		public List<Transform> marksList = new List<Transform>();

		public List<Transform> alignedMarksList = new List<Transform>();

		public List<BlipData> blips = new List<BlipData>();
	}
}
