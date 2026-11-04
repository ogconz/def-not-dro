using System;
using SplineLogic;
using UnityEngine;

namespace TrackLayout.Data
{
	[Serializable]
	public struct PathData
	{
		public Vector3[] centralPoints;

		public Vector3[] leftPoints;

		public Vector3[] rightPoints;

		public bool closed;

		public ISpline BuildSpline(PathLaneType type)
		{
			return type switch
			{
				PathLaneType.Left => new PathSpline(leftPoints, closed), 
				PathLaneType.Central => new PathSpline(centralPoints, closed), 
				PathLaneType.Right => new PathSpline(rightPoints, closed), 
				PathLaneType.RaceAI => BuildAISpline(centralPoints, closed), 
				_ => null, 
			};
		}

		public static ISpline BuildAISpline(Vector3[] sourcePoints, bool closed)
		{
			int num = sourcePoints.Length;
			Vector3[] points = (Vector3[])sourcePoints.Clone();
			float[] array = new float[num];
			Vector3[] array2 = new Vector3[num];
			array[0] = 0f;
			for (int i = 1; i < num; i++)
			{
				Vector3 vector = sourcePoints[i] - sourcePoints[i - 1];
				float magnitude = vector.magnitude;
				array[i] = array[i - 1] + magnitude;
				array2[i - 1] = vector / magnitude;
			}
			return new SplineModel(0, points, array2, array, closed);
		}
	}
}
