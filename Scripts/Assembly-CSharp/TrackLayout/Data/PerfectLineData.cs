using System;
using SplineLogic;
using UnityEngine;

namespace TrackLayout.Data
{
	[Serializable]
	public struct PerfectLineData
	{
		public Vector3[] points;

		public ISpline BuildSpline()
		{
			return new PathSpline(points, isLooped: false);
		}
	}
}
