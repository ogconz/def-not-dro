using System;
using UnityEngine;

namespace SplineLogic
{
	public class SplineModel : ISpline
	{
		private Vector3[] m_worldPoints = new Vector3[0];

		private Vector3[] m_worldVelocities = new Vector3[0];

		private float[] m_distances = new float[0];

		private int m_cachedInstanceID = -1;

		private bool m_closed;

		public bool closed
		{
			get
			{
				return m_closed;
			}
			set
			{
				m_closed = value;
			}
		}

		public float length
		{
			get
			{
				if (pointCount <= 1)
				{
					return 0f;
				}
				return m_distances[pointCount - 1];
			}
		}

		public int pointCount => m_worldPoints.Length;

		public SplineModel(int cachedInstanceID, Vector3[] points, Vector3[] velocities, float[] distances, bool closed)
		{
			ResetData(cachedInstanceID, points, velocities, distances, closed);
		}

		public void ResetData(int cachedInstanceID, Vector3[] points, Vector3[] velocities, float[] distances, bool closed)
		{
			m_cachedInstanceID = cachedInstanceID;
			m_worldPoints = new Vector3[points.Length];
			points.CopyTo(m_worldPoints, 0);
			m_worldVelocities = new Vector3[velocities.Length];
			velocities.CopyTo(m_worldVelocities, 0);
			m_distances = new float[distances.Length];
			distances.CopyTo(m_distances, 0);
			this.closed = closed;
		}

		public void SetWorldPoints(Vector3[] points)
		{
			m_worldPoints = new Vector3[points.Length];
			points.CopyTo(m_worldPoints, 0);
		}

		public Vector3 GetWorldPoint(int index)
		{
			return m_worldPoints[index];
		}

		public void GetWorldPoints(out Vector3[] points)
		{
			points = new Vector3[m_worldPoints.Length];
			m_worldPoints.CopyTo(points, 0);
		}

		public float GetDistanceAt(int index)
		{
			return m_distances[index];
		}

		public Vector3 GetWorldVelocity(int index)
		{
			return m_worldVelocities[index];
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir)
		{
			int segmentIndex = -1;
			float T;
			return ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir, int tillSegmentIndex = -1)
		{
			float T;
			return ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir, tillSegmentIndex);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir)
		{
			float T;
			return ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir);
		}

		public Projection ProjectPoint(Vector3 pnt, out float along, out float across)
		{
			int segmentIndex = -1;
			Vector3 pos;
			Vector3 dir;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across, int tillSegmentIndex = -1)
		{
			Vector3 pos;
			Vector3 dir;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across, tillSegmentIndex);
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir, out float along, out float across)
		{
			int segmentIndex = -1;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir, out float along, out float across, int tillSegmentIndex = -1)
		{
			float T;
			Projection result = ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir, tillSegmentIndex);
			float num = m_distances[segmentIndex];
			float num2 = m_distances[segmentIndex + 1];
			along = num + (num2 - num) * T;
			float num3 = pnt.x - pos.x;
			float num4 = pnt.y - pos.y;
			float num5 = pnt.z - pos.z;
			across = (float)Math.Sqrt(num3 * num3 + num4 * num4 + num5 * num5);
			if ((pnt.z - pos.z) * dir.x - (pnt.x - pos.x) * dir.z > 0f)
			{
				across = 0f - across;
			}
			return result;
		}

		private Projection ProjectPointImpl(Vector3 pnt, ref int segmentIndex, out float T, out Vector3 pos, out Vector3 dir, int tillSegmentIndex = -1)
		{
			T = 0f;
			pos = default(Vector3);
			dir = default(Vector3);
			Projection result = Projection.Inside;
			float num = float.MaxValue;
			int num2 = m_worldPoints.Length - 1;
			int num3 = 0;
			int num4 = segmentIndex;
			bool flag = false;
			if (num4 >= 0 && num4 < num2)
			{
				flag = true;
				num3 = num4;
			}
			Vector3 pos2;
			Vector3 dir2;
			Projection projection = ProjectPointOnSegment(pnt, num3, out var T2, out pos2, out dir2);
			if (projection == Projection.Inside)
			{
				num = GetSqrDistance(pnt, pos2, 10f);
				result = projection;
				segmentIndex = num3;
				T = T2;
				pos = pos2;
				dir = dir2;
			}
			int num5 = ((tillSegmentIndex == -1) ? num2 : Mathf.Min(tillSegmentIndex, num2));
			float sqrDistance;
			float num6;
			for (int i = 0; i < num5; i++)
			{
				if (i == num3)
				{
					continue;
				}
				projection = ProjectPointOnSegment(pnt, i, out T2, out pos2, out dir2);
				if (projection == Projection.Inside)
				{
					sqrDistance = GetSqrDistance(pnt, pos2, 10f);
					num6 = sqrDistance;
					if (flag && !IsNeighboringSegments(i, num4))
					{
						num6 += 625f;
					}
					if (num6 < num)
					{
						num = num6;
						result = projection;
						segmentIndex = i;
						T = T2;
						pos = pos2;
						dir = dir2;
					}
				}
			}
			pos2 = m_worldPoints[0];
			dir2 = m_worldVelocities[0];
			sqrDistance = GetSqrDistance(pnt, pos2, 10f);
			num6 = sqrDistance;
			if (flag && !IsNeighboringSegments(0, num4))
			{
				num6 += 625f;
			}
			if (num6 < num)
			{
				num = num6;
				result = Projection.Before;
				segmentIndex = 0;
				T = 0f;
				pos = pos2;
				dir = dir2;
			}
			int j = 1;
			for (int num7 = m_worldPoints.Length; j < num7; j++)
			{
				pos2 = m_worldPoints[j];
				dir2 = m_worldVelocities[j];
				sqrDistance = GetSqrDistance(pnt, pos2, 10f);
				num6 = sqrDistance;
				if (flag && !IsNeighboringSegments(j - 1, num4))
				{
					num6 += 625f;
				}
				if (num6 < num)
				{
					num = num6;
					segmentIndex = j - 1;
					T = 1f;
					pos = pos2;
					dir = dir2;
					if (j == num7 - 1)
					{
						result = Projection.After;
					}
				}
			}
			return result;
		}

		private Projection ProjectPointOnSegment(Vector3 pnt, int segmentIndex, out float T, out Vector3 pos, out Vector3 dir)
		{
			Vector3 p = m_worldPoints[segmentIndex];
			Vector3 pos2 = m_worldPoints[segmentIndex + 1];
			Projection projection = ProjectPointOnLine(pnt, p, pos2, out T, out pos, out dir);
			if (T + 0.2f >= 0f && T - 0.2f <= 1f)
			{
				int num = 0;
				DivideSegmentOnSteps(segmentIndex, 10f, 0.1f, out var stepCount, out var stepLength);
				SampleCache sc = default(SampleCache);
				sc.Reset();
				FillSampleCache(ref sc, segmentIndex);
				float num2 = float.MaxValue;
				for (int i = 0; i < stepCount; i++)
				{
					GetSamplePos(ref sc, (float)(i + 1) * stepLength, ref pos2);
					float T2;
					Vector3 pos3;
					Vector3 dir2;
					Projection projection2 = ProjectPointOnLine(pnt, p, pos2, out T2, out pos3, out dir2);
					float sqrDistance = GetSqrDistance(pnt, pos3, 1f);
					if (sqrDistance < num2)
					{
						num2 = sqrDistance;
						projection = projection2;
						num = i;
						T2 = ((T2 < 0f) ? 0f : ((T2 > 1f) ? 1f : T2));
						T = ((float)i * stepLength + T2 * stepLength) * sc.invSegmentLength;
						pos = pos3;
						dir = dir2;
					}
					p.x = pos2.x;
					p.y = pos2.y;
					p.z = pos2.z;
				}
				if ((projection == Projection.Before && num != 0) || (projection == Projection.After && num != stepCount - 1))
				{
					projection = Projection.Inside;
				}
			}
			return projection;
		}

		private Projection ProjectPointOnLine(Vector3 pnt, Vector3 p1, Vector3 p2, out float T, out Vector3 pos, out Vector3 dir)
		{
			dir.x = p2.x - p1.x;
			dir.y = p2.y - p1.y;
			dir.z = p2.z - p1.z;
			float num = 0f - (dir.x * pnt.x + dir.y * pnt.y + dir.z * pnt.z);
			float num2 = dir.x * dir.x + dir.y * dir.y + dir.z * dir.z;
			T = (0f - (num + (dir.x * p1.x + dir.y * p1.y + dir.z * p1.z))) / num2;
			Projection result = Projection.Inside;
			if (T < 0f)
			{
				result = Projection.Before;
				pos.x = p1.x;
				pos.y = p1.y;
				pos.z = p1.z;
			}
			else if (T > 1f)
			{
				result = Projection.After;
				pos.x = p2.x;
				pos.y = p2.y;
				pos.z = p2.z;
			}
			else
			{
				pos.x = p1.x + dir.x * T;
				pos.y = p1.y + dir.y * T;
				pos.z = p1.z + dir.z * T;
			}
			return result;
		}

		public void SamplePoint(float distance, out int segmentIndex, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			SampleCache sc = default(SampleCache);
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt, out vel, out acc);
			segmentIndex = sc.segmentIndex;
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			SampleCache sc = default(SampleCache);
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt, out vel, out acc);
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel)
		{
			SampleCache sc = default(SampleCache);
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt, out vel);
		}

		public void SamplePoint(float distance, out Vector3 pnt)
		{
			SampleCache sc = default(SampleCache);
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc)
		{
			pos = (vel = (acc = default(Vector3)));
			int segmentIndexForDistance = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndexForDistance];
			FillSampleCache(ref sc, segmentIndexForDistance);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
			GetSampleVel(ref sc, distanceOnSegment, ref vel);
			GetSampleAcc(ref sc, distanceOnSegment, ref acc);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel)
		{
			pos = (vel = default(Vector3));
			int segmentIndexForDistance = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndexForDistance];
			FillSampleCache(ref sc, segmentIndexForDistance);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
			GetSampleVel(ref sc, distanceOnSegment, ref vel);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos)
		{
			pos = default(Vector3);
			int segmentIndexForDistance = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndexForDistance];
			FillSampleCache(ref sc, segmentIndexForDistance);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
		}

		private void GetSamplePos(ref SampleCache sc, float distanceOnSegment, ref Vector3 pos)
		{
			float num = distanceOnSegment * sc.invSegmentLength;
			float num2 = num * num;
			float num3 = num * num * num;
			pos.x = sc.c0 * num3 + sc.c3 * num2 + sc.c6 * num + sc.c9;
			pos.y = sc.c1 * num3 + sc.c4 * num2 + sc.c7 * num + sc.c10;
			pos.z = sc.c2 * num3 + sc.c5 * num2 + sc.c8 * num + sc.c11;
		}

		private void GetSampleVel(ref SampleCache sc, float distanceOnSegment, ref Vector3 vel)
		{
			float num = distanceOnSegment * sc.invSegmentLength;
			float num2 = num * 2f;
			float num3 = num * num * 3f;
			vel.x = sc.c0 * num3 + sc.c3 * num2 + sc.c6;
			vel.y = sc.c1 * num3 + sc.c4 * num2 + sc.c7;
			vel.z = sc.c2 * num3 + sc.c5 * num2 + sc.c8;
		}

		private void GetSampleAcc(ref SampleCache sc, float distanceOnSegment, ref Vector3 acc)
		{
			float num = distanceOnSegment * sc.invSegmentLength * 6f;
			acc.x = sc.c0 * num + sc.c3 * 2f;
			acc.y = sc.c1 * num + sc.c4 * 2f;
			acc.z = sc.c2 * num + sc.c5 * 2f;
		}

		private void GetSampleJrk(ref SampleCache sc, ref Vector3 jrk)
		{
			jrk.x = sc.c0 * 6f;
			jrk.y = sc.c1 * 6f;
			jrk.z = sc.c2 * 6f;
		}

		private bool FillSampleCache(ref SampleCache sc, int segmentIndex)
		{
			if (sc.instanceID != m_cachedInstanceID)
			{
				sc.segmentIndex = -1;
				sc.instanceID = m_cachedInstanceID;
			}
			if (segmentIndex != sc.segmentIndex)
			{
				sc.segmentIndex = segmentIndex;
				float num = m_distances[segmentIndex + 1] - m_distances[segmentIndex];
				sc.invSegmentLength = 1f / num;
				Vector3 vector = m_worldPoints[segmentIndex];
				Vector3 vector2 = m_worldPoints[segmentIndex + 1];
				Vector3 vector3 = m_worldVelocities[segmentIndex];
				Vector3 vector4 = m_worldVelocities[segmentIndex + 1];
				float num2 = vector3.x * num;
				float num3 = vector3.y * num;
				float num4 = vector3.z * num;
				float num5 = vector4.x * num;
				float num6 = vector4.y * num;
				float num7 = vector4.z * num;
				sc.c0 = vector.x * 2f - vector2.x * 2f + num2 + num5;
				sc.c1 = vector.y * 2f - vector2.y * 2f + num3 + num6;
				sc.c2 = vector.z * 2f - vector2.z * 2f + num4 + num7;
				sc.c3 = vector2.x * 3f - vector.x * 3f - num2 * 2f - num5;
				sc.c4 = vector2.y * 3f - vector.y * 3f - num3 * 2f - num6;
				sc.c5 = vector2.z * 3f - vector.z * 3f - num4 * 2f - num7;
				sc.c6 = num2;
				sc.c7 = num3;
				sc.c8 = num4;
				sc.c9 = vector.x;
				sc.c10 = vector.y;
				sc.c11 = vector.z;
				return true;
			}
			return false;
		}

		private int GetSegmentIndexForDistance(float distance)
		{
			int i = 0;
			int num;
			for (num = m_distances.Length - 1; i < num && m_distances[i + 1] < distance; i++)
			{
			}
			if (i >= num)
			{
				return num - 1;
			}
			return i;
		}

		private bool IsNeighboringSegments(int segmentIndex1, int segmentIndex2)
		{
			if (segmentIndex1 > segmentIndex2)
			{
				int num = segmentIndex1;
				segmentIndex1 = segmentIndex2;
				segmentIndex2 = num;
			}
			if (segmentIndex2 - segmentIndex1 > 2)
			{
				int num2 = m_worldPoints.Length - 1;
				if (!closed || segmentIndex2 + 2 < num2 || (segmentIndex2 + 2) % num2 < segmentIndex1)
				{
					return false;
				}
			}
			return true;
		}

		private void DivideSegmentOnSteps(int segmentIndex, float maxStepLength, float invMaxStepLength, out int stepCount, out float stepLength)
		{
			stepCount = 1;
			stepLength = m_distances[segmentIndex + 1] - m_distances[segmentIndex];
			if (stepLength > maxStepLength)
			{
				stepCount = (int)Math.Ceiling(stepLength * invMaxStepLength);
				stepLength /= stepCount;
			}
		}

		private float GetSqrDistance(Vector3 p1, Vector3 p2, float yScale)
		{
			float num = p1.x - p2.x;
			float num2 = p1.y - p2.y;
			float num3 = p1.z - p2.z;
			float num4 = num * num;
			num2 = num2 * num2 * yScale;
			num3 *= num3;
			return num4 + num2 + num3;
		}

		public static float CalcRadius(Vector3 vel, Vector3 acc)
		{
			float x = vel.x;
			float y = vel.y;
			float z = vel.z;
			float x2 = acc.x;
			float y2 = acc.y;
			float z2 = acc.z;
			float num = (x * x + y * y + z * z) / (float)Math.Sqrt(x2 * x2 + y2 * y2 + z2 * z2);
			if (!(z * x2 - x * z2 < 0f))
			{
				return num;
			}
			return 0f - num;
		}
	}
}
