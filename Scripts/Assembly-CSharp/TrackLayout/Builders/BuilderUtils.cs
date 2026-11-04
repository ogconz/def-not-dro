using System.Collections.Generic;
using SplineLogic;
using TrackLayout.Data;
using UnityEngine;

namespace TrackLayout.Builders
{
	public static class BuilderUtils
	{
		public static void GetLeftRightPoints(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData data, float distance, out Vector3 leftPoint, out Vector3 rightPoint)
		{
			centralSpline.SamplePoint(distance, out var pnt);
			leftSpline.ProjectPoint(pnt, out Vector3 pos, out Vector3 _);
			rightSpline.ProjectPoint(pnt, out Vector3 pos2, out Vector3 _);
			Vector3 vector = pos2 - pos;
			float magnitude = vector.magnitude;
			Vector3 vector2 = vector / magnitude;
			Vector3 vector3 = pos;
			Vector3 vector4 = vector2 * data.width;
			Vector3 vector5 = vector2 * (magnitude - data.width) * data.bias;
			leftPoint = vector3 + vector5;
			rightPoint = vector3 + vector4 + vector5;
		}

		public static void CalcStep(float length, float desiredStep, out float step, out int samplesCount)
		{
			int num = Mathf.CeilToInt(Mathf.Abs(length) / desiredStep);
			step = length / (float)num;
			samplesCount = num + 1;
		}

		public static Vector3[] SampleClipZone(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData data, float step)
		{
			CalcStep(data.length, step, out step, out var samplesCount);
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			List<Vector3> list = new List<Vector3>();
			List<Vector3> list2 = new List<Vector3>();
			for (int i = 0; i < samplesCount; i++)
			{
				centralSpline.SamplePoint(data.distance + (float)i * step, out var pnt);
				leftSpline.ProjectPoint(pnt, out Vector3 pos, out Vector3 _);
				rightSpline.ProjectPoint(pnt, out Vector3 pos2, out Vector3 _);
				Vector3 vector3 = pos2 - pos;
				float magnitude = vector3.magnitude;
				Vector3 vector4 = vector3 / magnitude;
				Vector3 vector5 = pos;
				Vector3 vector6 = vector4 * data.width;
				Vector3 vector7 = vector4 * (magnitude - data.width) * data.bias;
				Vector3 vector8 = vector5 + vector7;
				Vector3 vector9 = vector5 + vector6 + vector7;
				if (i > 0)
				{
					float num = step * 0.2f;
					num *= num;
					if ((vector8 - vector).sqrMagnitude > num)
					{
						list2.Add(vector8);
						vector = vector8;
					}
					if ((vector9 - vector2).sqrMagnitude > num)
					{
						list.Add(vector9);
						vector2 = vector9;
					}
				}
				else
				{
					list2.Add(vector8);
					list.Add(vector9);
					vector = vector8;
					vector2 = vector9;
				}
			}
			list.Reverse();
			list2.AddRange(list);
			return list2.ToArray();
		}
	}
}
