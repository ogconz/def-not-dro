using System.Collections.Generic;
using UnityEngine;

namespace CarX.XDSPoints
{
	public class XDSSector
	{
		public Vector3 leftPoint;

		public Vector3 rightPoint;

		public bool isBlank;

		public float weight;

		public float sideChangeFactor;

		public float minSpeed;

		public List<int> clipZoneIndices;
	}
}
