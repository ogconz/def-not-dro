using System;
using System.Collections.Generic;
using SplineLogic;
using UnityEngine;

namespace TrackLayout
{
	public class PathSpline : ISpline
	{
		private static readonly Vector2[] s_gaussLengendreCoefficients = new Vector2[5]
		{
			new Vector2(0f, 0.5688889f),
			new Vector2(-0.5384693f, 0.47862867f),
			new Vector2(0.5384693f, 0.47862867f),
			new Vector2(-0.90617985f, 0.23692688f),
			new Vector2(0.90617985f, 0.23692688f)
		};

		private readonly bool m_isLooped;

		private readonly Vector3 m_endPoint;

		private readonly Vector3[] m_segments;

		private readonly float[] m_lengths;

		private readonly float m_totalLength;

		private readonly int m_segmentsCount;

		public float totalLength => m_totalLength;

		public int segmentsCount => m_segmentsCount;

		public bool isLooped => m_isLooped;

		public float length => m_totalLength;

		public int pointCount => m_segmentsCount;

		public bool closed => m_isLooped;

		public PathSpline(Vector3[] points, bool isLooped)
		{
			if (points.Length < 2)
			{
				return;
			}
			m_isLooped = isLooped;
			if (m_isLooped)
			{
				int num = 0;
				if ((points[0] - points[^1]).sqrMagnitude < 0.01f)
				{
					num = 1;
					if (points.Length - num < 2)
					{
						return;
					}
				}
				m_segmentsCount = points.Length - num;
				m_endPoint = points[0];
			}
			else
			{
				m_segmentsCount = points.Length - 1;
				m_endPoint = points[^1];
			}
			m_segments = new Vector3[m_segmentsCount * 4];
			m_lengths = new float[m_segmentsCount];
			for (int i = 0; i < m_segmentsCount; i++)
			{
				int num2;
				int num3;
				int num4;
				int num5;
				if (m_isLooped)
				{
					num2 = ((i == 0) ? (m_segmentsCount - 1) : (i - 1));
					num3 = i;
					num4 = ((i != m_segmentsCount - 1) ? (i + 1) : 0);
					num5 = ((num4 != m_segmentsCount - 1) ? (num4 + 1) : 0);
				}
				else
				{
					num2 = ((i == 0) ? i : (i - 1));
					num3 = i;
					num4 = i + 1;
					num5 = ((num4 == m_segmentsCount) ? num4 : (num4 + 1));
				}
				Vector3 vector = points[num2] * 0.5f;
				Vector3 vector2 = points[num3] * 0.5f;
				Vector3 vector3 = points[num4] * 0.5f;
				Vector3 vector4 = points[num5] * 0.5f;
				Vector3 vector5 = 2f * vector2;
				Vector3 vector6 = vector3 - vector;
				Vector3 vector7 = 2f * vector - 5f * vector2 + 4f * vector3 - vector4;
				Vector3 vector8 = -vector + 3f * vector2 - 3f * vector3 + vector4;
				m_segments[i * 4] = vector5;
				m_segments[i * 4 + 1] = vector6;
				m_segments[i * 4 + 2] = vector7;
				m_segments[i * 4 + 3] = vector8;
				float num6 = 0f;
				for (int j = 0; j < s_gaussLengendreCoefficients.Length; j++)
				{
					float t = 0.5f * (1f + s_gaussLengendreCoefficients[j].x);
					ComputeTangent(vector6, vector7, vector8, t, out var result);
					num6 += result.magnitude * s_gaussLengendreCoefficients[j].y;
				}
				num6 *= 0.5f;
				m_totalLength += num6;
				m_lengths[i] = num6;
			}
		}

		public float GetLength(int segmentIndex)
		{
			return m_lengths[segmentIndex];
		}

		public Vector3 GetPoint(int index)
		{
			if (index >= m_segmentsCount)
			{
				return m_endPoint;
			}
			return m_segments[index * 4];
		}

		private void LimitDistance(ref float distance)
		{
			if (isLooped)
			{
				if (distance > m_totalLength)
				{
					distance -= m_totalLength;
				}
				else if (distance < 0f)
				{
					distance += m_totalLength;
				}
			}
			else
			{
				distance = Mathf.Clamp(distance, 0f, m_totalLength);
			}
		}

		public void Sample(float step, ref List<Vector3> samples)
		{
			if (m_segmentsCount == 0)
			{
				return;
			}
			int num = Mathf.CeilToInt(m_totalLength / step);
			int num2 = num + 1;
			step = m_totalLength / (float)num;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = 0;
			float num6 = m_lengths[num5];
			for (int i = 0; i < num2; i++)
			{
				while (num3 > num4 + num6 && num3 < m_totalLength)
				{
					num5++;
					num4 += num6;
					num6 = m_lengths[num5];
				}
				float t = (num3 - num4) / num6;
				Sample(num5, t, out var position);
				samples.Add(position);
				num3 += step;
			}
		}

		public void Sample(int segmentIndex, float t, out Vector3 position)
		{
			segmentIndex *= 4;
			Vector3 a = m_segments[segmentIndex];
			Vector3 b = m_segments[segmentIndex + 1];
			Vector3 c = m_segments[segmentIndex + 2];
			Vector3 d = m_segments[segmentIndex + 3];
			ComputePosition(a, b, c, d, t, out position);
		}

		public void Sample(float distance, out Vector3 position)
		{
			LimitDistance(ref distance);
			GetSegmentDistance(distance, out var segmentIndex, out var coveredLength);
			float num = m_lengths[segmentIndex];
			float t = (num - (coveredLength - distance)) / num;
			segmentIndex *= 4;
			Vector3 a = m_segments[segmentIndex];
			Vector3 b = m_segments[segmentIndex + 1];
			Vector3 c = m_segments[segmentIndex + 2];
			Vector3 d = m_segments[segmentIndex + 3];
			ComputePosition(a, b, c, d, t, out position);
		}

		public void Sample(float distance, out Vector3 position, out Vector3 tangent)
		{
			LimitDistance(ref distance);
			GetSegmentDistance(distance, out var segmentIndex, out var coveredLength);
			float num = m_lengths[segmentIndex];
			float t = (num - (coveredLength - distance)) / num;
			segmentIndex *= 4;
			Vector3 a = m_segments[segmentIndex];
			Vector3 b = m_segments[segmentIndex + 1];
			Vector3 c = m_segments[segmentIndex + 2];
			Vector3 d = m_segments[segmentIndex + 3];
			ComputePosition(a, b, c, d, t, out position);
			ComputeTangent(b, c, d, t, out tangent);
		}

		public void Sample(float distance, out int segmentIndex, out Vector3 position, out Vector3 tangent, out Vector3 normal)
		{
			LimitDistance(ref distance);
			GetSegmentDistance(distance, out segmentIndex, out var coveredLength);
			int num = segmentIndex;
			float num2 = m_lengths[num];
			float t = (num2 - (coveredLength - distance)) / num2;
			num *= 4;
			Vector3 a = m_segments[num];
			Vector3 b = m_segments[num + 1];
			Vector3 c = m_segments[num + 2];
			Vector3 d = m_segments[num + 3];
			ComputePosition(a, b, c, d, t, out position);
			ComputeTangent(b, c, d, t, out tangent);
			ComputeCurvature(c, d, t, out normal);
		}

		public void Sample(float distance, out Vector3 position, out Vector3 tangent, out Vector3 normal)
		{
			int segmentIndex = 0;
			Sample(distance, out segmentIndex, out position, out tangent, out normal);
		}

		public void GetSegmentDistance(float distance, out int segmentIndex, out float coveredLength)
		{
			coveredLength = 0f;
			for (int i = 0; i < m_lengths.Length; i++)
			{
				coveredLength += m_lengths[i];
				if (distance < coveredLength)
				{
					segmentIndex = i;
					return;
				}
			}
			segmentIndex = m_lengths.Length - 1;
		}

		public Vector3 InaccurateFindNearest(Vector3 point, out int bestSegmentIndex, out float bestT, out Vector3 bestDelta, out float bestDistanceSq, int fromSegmentIndex = -1, int tillSegmentIndex = -1)
		{
			int num = 0;
			float num2 = float.MaxValue;
			int num3 = ((tillSegmentIndex == -1) ? m_segmentsCount : Mathf.Min(tillSegmentIndex, m_segmentsCount));
			fromSegmentIndex = Mathf.Max(fromSegmentIndex, 0);
			Vector3 vector2 = default(Vector3);
			for (int i = fromSegmentIndex; i < num3; i++)
			{
				Vector3 point2 = GetPoint(i);
				Vector3 point3 = GetPoint(i + 1);
				Vector3 vector = ClosestPointOnSegemnt(point2, point3, point);
				vector2.x = vector.x - point.x;
				vector2.y = vector.y - point.y;
				vector2.z = vector.z - point.z;
				float num4 = vector2.x * vector2.x * 1f + vector2.y * vector2.y * 100f + vector2.z * vector2.z * 1f;
				if (num4 < num2)
				{
					num2 = num4;
					num = i;
				}
			}
			bestSegmentIndex = 0;
			bestT = 0f;
			bestDelta.x = 0f;
			bestDelta.y = 0f;
			bestDelta.z = 0f;
			bestDistanceSq = float.MaxValue;
			Vector3 result = default(Vector3);
			result.x = 0f;
			result.y = 0f;
			result.z = 0f;
			for (int j = -1; j < 2; j++)
			{
				int num5 = num + j;
				if (num5 < 0)
				{
					num5 = m_segmentsCount - 1;
				}
				else if (num5 >= m_segmentsCount)
				{
					num5 -= m_segmentsCount;
				}
				Vector3 foundPoint;
				Vector3 delta;
				float foundDistanceSq;
				float num6 = InaccurateFindNearestOnSegment(point, num5, out foundPoint, out delta, out foundDistanceSq);
				if (foundDistanceSq < bestDistanceSq)
				{
					bestSegmentIndex = num5;
					bestT = num6;
					result = foundPoint;
					bestDelta = delta;
					bestDistanceSq = foundDistanceSq;
				}
			}
			return result;
		}

		public static Vector3 ClosestPointOnSegemnt(Vector3 a, Vector3 b, Vector3 p)
		{
			Vector3 vector = default(Vector3);
			vector.x = b.x - a.x;
			vector.y = b.y - a.y;
			vector.z = b.z - a.z;
			Vector3 result = default(Vector3);
			result.x = p.x - a.x;
			result.y = p.y - a.y;
			result.z = p.z - a.z;
			float sqrMagnitude = vector.sqrMagnitude;
			float num = result.x * vector.x + result.y * vector.y + result.z * vector.z;
			if (num > 0f)
			{
				float num2 = 1f / Mathf.Sqrt(sqrMagnitude);
				num *= num2 * num2;
				if (num < 1f)
				{
					result.x = a.x + vector.x * num;
					result.y = a.y + vector.y * num;
					result.z = a.z + vector.z * num;
					return result;
				}
				return b;
			}
			return a;
		}

		private float InaccurateFindNearestOnSegment(Vector3 point, int segmentIndex, out Vector3 foundPoint, out Vector3 delta, out float foundDistanceSq)
		{
			segmentIndex *= 4;
			Vector3 a = m_segments[segmentIndex];
			Vector3 b = m_segments[segmentIndex + 1];
			Vector3 c = m_segments[segmentIndex + 2];
			Vector3 d = m_segments[segmentIndex + 3];
			Vector3 foundPoint2;
			Vector3 delta2;
			float distanceSq;
			float num = InaccurateFindNearestOnSegment(point, a, b, c, d, 0f, out foundPoint2, out delta2, out distanceSq);
			Vector3 foundPoint3;
			Vector3 delta3;
			float distanceSq2;
			float num2 = InaccurateFindNearestOnSegment(point, a, b, c, d, 0.5f, out foundPoint3, out delta3, out distanceSq2);
			Vector3 foundPoint4;
			Vector3 delta4;
			float distanceSq3;
			float num3 = InaccurateFindNearestOnSegment(point, a, b, c, d, 1f, out foundPoint4, out delta4, out distanceSq3);
			float result;
			if (distanceSq <= distanceSq2 && distanceSq <= distanceSq3)
			{
				foundDistanceSq = distanceSq;
				result = num;
				delta = delta2;
				foundPoint = foundPoint2;
			}
			else if (distanceSq2 <= distanceSq3)
			{
				foundDistanceSq = distanceSq2;
				result = num2;
				delta = delta3;
				foundPoint = foundPoint3;
			}
			else
			{
				foundDistanceSq = distanceSq3;
				result = num3;
				delta = delta4;
				foundPoint = foundPoint4;
			}
			return result;
		}

		private static float InaccurateFindNearestOnSegment(Vector3 targetPoint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float startT, out Vector3 foundPoint, out Vector3 delta, out float distanceSq)
		{
			float num = startT;
			float num2 = 1f;
			float num3 = 0.75f;
			float t = num;
			ComputePosition(a, b, c, d, t, out foundPoint);
			for (int i = 0; i < 3; i++)
			{
				ComputeTangent(b, c, d, t, out var result);
				delta.x = targetPoint.x - foundPoint.x;
				delta.y = targetPoint.y - foundPoint.y;
				delta.z = targetPoint.z - foundPoint.z;
				float value = (result.x * delta.x + result.y * delta.y + result.z * delta.z) / (result.x * result.x + result.y * result.y + result.z * result.z);
				num2 *= num3;
				value = Mathf.Clamp(value, 0f - num2, num2);
				num += value;
				num = Mathf.Clamp(num, 0f, 1f);
				num2 = Mathf.Abs(value);
				t = num;
				ComputePosition(a, b, c, d, t, out foundPoint);
			}
			delta.x = targetPoint.x - foundPoint.x;
			delta.y = targetPoint.y - foundPoint.y;
			delta.z = targetPoint.z - foundPoint.z;
			distanceSq = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
			return num;
		}

		private static void ComputePosition(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			result.x = a.x + t * (b.x + t * (c.x + d.x * t));
			result.y = a.y + t * (b.y + t * (c.y + d.y * t));
			result.z = a.z + t * (b.z + t * (c.z + d.z * t));
		}

		private static void ComputeTangent(Vector3 b, Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			result.x = b.x + t * (2f * c.x + 3f * d.x * t);
			result.y = b.y + t * (2f * c.y + 3f * d.y * t);
			result.z = b.z + t * (2f * c.z + 3f * d.z * t);
		}

		private static void ComputeCurvature(Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			result.x = c.x * 2f + 6f * d.x * t;
			result.y = c.y * 2f + 6f * d.y * t;
			result.z = c.z * 2f + 6f * d.z * t;
		}

		public Vector3 GetWorldPoint(int index)
		{
			return GetPoint(index);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir, int tillSegmentIndex = -1)
		{
			pos = InaccurateFindNearest(pnt, out segmentIndex, out var bestT, out var _, out var _, segmentIndex, tillSegmentIndex);
			Vector3 b = m_segments[segmentIndex * 4 + 1];
			Vector3 c = m_segments[segmentIndex * 4 + 2];
			Vector3 d = m_segments[segmentIndex * 4 + 3];
			ComputeTangent(b, c, d, bestT, out dir);
			return Projection.Inside;
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir)
		{
			int segmentIndex = 0;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, -1);
		}

		public Projection ProjectPoint(Vector3 pnt, out float along, out float across)
		{
			InaccurateFindNearest(pnt, out var bestSegmentIndex, out var bestT, out var bestDelta, out var bestDistanceSq);
			along = 0f;
			for (int i = 0; i < bestSegmentIndex; i++)
			{
				along += m_lengths[i];
			}
			along += m_lengths[bestSegmentIndex] * bestT;
			across = Mathf.Sqrt(bestDistanceSq);
			Vector3 b = m_segments[bestSegmentIndex * 4 + 1];
			Vector3 c = m_segments[bestSegmentIndex * 4 + 2];
			Vector3 d = m_segments[bestSegmentIndex * 4 + 3];
			ComputeTangent(b, c, d, bestT, out var result);
			if (bestDelta.z * result.x - bestDelta.x * result.z > 0f)
			{
				across = 0f - across;
			}
			return Projection.Inside;
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across, int tillSegmentIndex = -1)
		{
			segmentIndex = Mathf.Clamp(segmentIndex, 0, m_segmentsCount);
			InaccurateFindNearest(pnt, out segmentIndex, out var bestT, out var bestDelta, out var bestDistanceSq, segmentIndex, tillSegmentIndex);
			along = 0f;
			for (int i = 0; i < segmentIndex; i++)
			{
				along += m_lengths[i];
			}
			along += m_lengths[segmentIndex] * bestT;
			across = Mathf.Sqrt(bestDistanceSq);
			Vector3 b = m_segments[segmentIndex * 4 + 1];
			Vector3 c = m_segments[segmentIndex * 4 + 2];
			Vector3 d = m_segments[segmentIndex * 4 + 3];
			ComputeTangent(b, c, d, bestT, out var result);
			if (bestDelta.z * result.x - bestDelta.x * result.z > 0f)
			{
				across = 0f - across;
			}
			return Projection.Inside;
		}

		public void SamplePoint(float distance, out int segmentIndex, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			Sample(distance, out segmentIndex, out pnt, out vel, out acc);
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			Sample(distance, out pnt, out vel, out acc);
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel)
		{
			Sample(distance, out pnt, out vel);
		}

		public void SamplePoint(float distance, out Vector3 pnt)
		{
			Sample(distance, out pnt);
		}

		public Vector3 GetWorldVelocity(int index)
		{
			throw new NotImplementedException();
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc)
		{
			throw new NotImplementedException();
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos)
		{
			throw new NotImplementedException();
		}

		public void SetWorldPoints(Vector3[] points)
		{
			throw new NotImplementedException();
		}

		public void GetWorldPoints(out Vector3[] points)
		{
			throw new NotImplementedException();
		}
	}
}
