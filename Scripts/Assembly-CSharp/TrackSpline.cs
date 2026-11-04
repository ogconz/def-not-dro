using System;
using SplineLogic;
using UnityEngine;

[AddComponentMenu("Scripts/Track/TrackSpline")]
public class TrackSpline : MonoBehaviour, ISpline
{
	[SerializeField]
	private bool m_closed;

	[SerializeField]
	private Vector3[] m_localPoints = new Vector3[0];

	[SerializeField]
	private Vector3[] m_localVelocities = new Vector3[0];

	[SerializeField]
	private Vector3[] m_worldPoints = new Vector3[0];

	[SerializeField]
	private Vector3[] m_worldVelocities = new Vector3[0];

	[SerializeField]
	private float[] m_distances = new float[0];

	private int m_cachedInstanceID = -1;

	private static Matrix4x4 s_posMtx = GetPosMtx();

	private static Matrix4x4 s_velMtx = GetVelMtx();

	private static Matrix4x4 s_accMtx = GetAccMtx();

	private static Matrix4x4 s_jrkMtx = GetJrkMtx();

	public bool closed
	{
		get
		{
			return m_closed;
		}
		set
		{
			if (m_closed != value)
			{
				m_closed = value;
				Build();
			}
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

	public int pointCount => m_localPoints.Length;

	public void SetLocalPoints(Vector3[] points)
	{
		Array.Resize(ref m_localPoints, points.Length);
		points.CopyTo(m_localPoints, 0);
		Build();
	}

	public void GetLocalPoints(out Vector3[] points)
	{
		points = new Vector3[m_localPoints.Length];
		m_localPoints.CopyTo(points, 0);
	}

	public void SetWorldPoints(Vector3[] points)
	{
		Vector3[] array = new Vector3[points.Length];
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = worldToLocalMatrix.MultiplyPoint(points[i]);
		}
		SetLocalPoints(array);
	}

	public void GetWorldPoints(out Vector3[] points)
	{
		points = new Vector3[m_worldPoints.Length];
		m_worldPoints.CopyTo(points, 0);
	}

	public Vector3 GetLocalPoint(int index)
	{
		return m_localPoints[index];
	}

	public void SetLocalPoint(int index, Vector3 point)
	{
		m_localPoints[index] = point;
		Build();
	}

	public void SetWorldPoint(int index, Vector3 point)
	{
		SetLocalPoint(index, base.transform.worldToLocalMatrix.MultiplyPoint3x4(point));
	}

	public Vector3 GetWorldPoint(int index)
	{
		return m_worldPoints[index];
	}

	public float GetDistanceAt(int index)
	{
		return m_distances[index];
	}

	public Vector3 GetWorldVelocity(int index)
	{
		return m_worldVelocities[index];
	}

	public Projection ProjectPoint(Vector3 point, ref int segmentIndex, out float along, out float across, int tillSegmentIndex = -1)
	{
		float projT;
		float sqrAcross;
		Projection result = ProjectPointImpl(point, out segmentIndex, out projT, out sqrAcross, segmentIndex, tillSegmentIndex);
		GetAlongAcrossDistances(point, segmentIndex, projT, sqrAcross, out along, out across);
		return result;
	}

	public Projection ProjectPoint(Vector3 point, out float along, out float across)
	{
		int segmentIndex = 0;
		return ProjectPoint(point, ref segmentIndex, out along, out across);
	}

	public Projection ProjectPoint(Vector3 point, ref int segmentIndex, out Vector3 pos, out Vector3 dir, int tillSegmentIndex = -1)
	{
		float projT;
		float sqrAcross;
		Projection result = ProjectPointImpl(point, out segmentIndex, out projT, out sqrAcross, segmentIndex, tillSegmentIndex);
		dir = m_worldPoints[segmentIndex + 1] - m_worldPoints[segmentIndex];
		pos = m_worldPoints[segmentIndex] + dir * projT;
		return result;
	}

	public Projection ProjectPoint(Vector3 point, out Vector3 pos, out Vector3 dir)
	{
		int segmentIndex = 0;
		return ProjectPoint(point, ref segmentIndex, out pos, out dir, -1);
	}

	public Projection ProjectPoint(Vector3 point, out Vector3 pos, out Vector3 dir, out float along, out float across)
	{
		int segmentIndex;
		float projT;
		float sqrAcross;
		Projection result = ProjectPointImpl(point, out segmentIndex, out projT, out sqrAcross);
		dir = m_worldPoints[segmentIndex + 1] - m_worldPoints[segmentIndex];
		pos = m_worldPoints[segmentIndex] + dir * projT;
		GetAlongAcrossDistances(point, segmentIndex, projT, sqrAcross, out along, out across);
		return result;
	}

	public void GetAlongAcrossDistances(Vector3 point, int segmentIndex, float projT, float sqrAcross, out float along, out float across)
	{
		along = m_distances[segmentIndex];
		along += (m_distances[segmentIndex + 1] - along) * projT;
		across = (float)Math.Sqrt(sqrAcross);
		if (Vector3.Cross(point - m_worldPoints[segmentIndex], m_worldPoints[segmentIndex + 1] - m_worldPoints[segmentIndex]).y > 0f)
		{
			across = 0f - across;
		}
	}

	private Projection ProjectPointOnSegment(Vector3 point, int segmentIndex, out float projT, out float sqrAcross)
	{
		Vector3 vector = m_worldPoints[segmentIndex];
		Vector3 vector2 = m_worldPoints[segmentIndex + 1];
		Vector3 vector3 = vector2 - vector;
		float num = 0f - (vector3.x * point.x + vector3.y * point.y + vector3.z * point.z);
		float num2 = vector3.x * vector3.x + vector3.y * vector3.y + vector3.z * vector3.z;
		float num3 = (0f - (num + (vector3.x * vector.x + vector3.y * vector.y + vector3.z * vector.z))) / num2;
		Projection projection = Projection.Inside;
		Vector3 vector4 = default(Vector3);
		if (num3 < 0f)
		{
			projection = Projection.Before;
			num3 = 0f;
			vector4.x = vector.x - point.x;
			vector4.y = vector.y - point.y;
			vector4.z = vector.z - point.z;
		}
		else if (num3 > 1f)
		{
			projection = Projection.After;
			num3 = 1f;
			vector4.x = vector2.x - point.x;
			vector4.y = vector2.y - point.y;
			vector4.z = vector2.z - point.z;
		}
		else
		{
			projection = Projection.Inside;
			float num4 = 1f - num3;
			Vector3 vector5 = default(Vector3);
			vector5.x = vector.x * num4 + vector2.x * num3;
			vector5.y = vector.y * num4 + vector2.y * num3;
			vector5.z = vector.z * num4 + vector2.z * num3;
			vector4.x = vector5.x - point.x;
			vector4.y = vector5.y - point.y;
			vector4.z = vector5.z - point.z;
		}
		projT = num3;
		sqrAcross = vector4.x * vector4.x + vector4.y * vector4.y + vector4.z * vector4.z;
		return projection;
	}

	private Projection ProjectPointImpl(Vector3 point, out int segmentIndex, out float projT, out float sqrAcross, int fromSegmentIndex = -1, int tillSegmentIndex = -1)
	{
		int num = 0;
		float num2 = 0f;
		float num3 = float.MaxValue;
		sqrAcross = float.MaxValue;
		Projection result = Projection.Inside;
		fromSegmentIndex = Mathf.Max(fromSegmentIndex, 0);
		int num4 = ((tillSegmentIndex == -1) ? (m_worldPoints.Length - 1) : Mathf.Min(tillSegmentIndex, m_worldPoints.Length - 1));
		Vector3 vector3 = default(Vector3);
		Vector3 vector4 = default(Vector3);
		Vector3 vector5 = default(Vector3);
		Vector3 vector;
		float num9;
		for (int i = fromSegmentIndex; i < num4; i++)
		{
			vector = m_worldPoints[i];
			Vector3 vector2 = m_worldPoints[i + 1];
			vector3.x = vector2.x - vector.x;
			vector3.y = vector2.y - vector.y;
			vector3.z = vector2.z - vector.z;
			float num5 = 0f - (vector3.x * point.x + vector3.y * point.y + vector3.z * point.z);
			float num6 = vector3.x * vector3.x + vector3.y * vector3.y + vector3.z * vector3.z;
			float num7 = (0f - (num5 + (vector3.x * vector.x + vector3.y * vector.y + vector3.z * vector.z))) / num6;
			if (!(num7 < 0f) && !(num7 > 1f))
			{
				float num8 = 1f - num7;
				vector4.x = vector.x * num8 + vector2.x * num7;
				vector4.y = vector.y * num8 + vector2.y * num7;
				vector4.z = vector.z * num8 + vector2.z * num7;
				vector5.x = vector4.x - point.x;
				vector5.y = vector4.y - point.y;
				vector5.z = vector4.z - point.z;
				num9 = vector5.x * vector5.x * 1f + vector5.y * vector5.y * 100f + vector5.z * vector5.z * 1f;
				if (num9 < num3)
				{
					sqrAcross = vector5.x * vector5.x + vector5.y * vector5.y + vector5.z * vector5.z;
					num3 = num9;
					num = i;
					num2 = num7;
				}
			}
		}
		vector = m_worldPoints[fromSegmentIndex];
		vector5.x = vector.x - point.x;
		vector5.y = vector.y - point.y;
		vector5.z = vector.z - point.z;
		num9 = vector5.x * vector5.x * 1f + vector5.y * vector5.y * 100f + vector5.z * vector5.z * 1f;
		if (num9 < num3)
		{
			sqrAcross = vector5.x * vector5.x + vector5.y * vector5.y + vector5.z * vector5.z;
			num3 = num9;
			num = 0;
			num2 = 0f;
			result = Projection.Before;
		}
		for (int j = fromSegmentIndex; j < num4; j++)
		{
			vector = m_worldPoints[j + 1];
			vector5.x = vector.x - point.x;
			vector5.y = vector.y - point.y;
			vector5.z = vector.z - point.z;
			num9 = vector5.x * vector5.x * 1f + vector5.y * vector5.y * 100f + vector5.z * vector5.z * 1f;
			if (num9 < num3)
			{
				sqrAcross = vector5.x * vector5.x + vector5.y * vector5.y + vector5.z * vector5.z;
				num3 = num9;
				num = j;
				num2 = 1f;
				if (j == num4 - 1)
				{
					result = Projection.After;
				}
			}
		}
		segmentIndex = num;
		projT = num2;
		return result;
	}

	public Projection ProjectPointContinuously(Vector3 point, ref int segmentIndex, out float along, out float across)
	{
		float projT;
		float sqrAcross;
		Projection result = ProjectPointContinuouslyImpl(point, ref segmentIndex, out projT, out sqrAcross);
		GetAlongAcrossDistances(point, segmentIndex, projT, sqrAcross, out along, out across);
		return result;
	}

	public Projection ProjectPointContinuously(Vector3 point, ref int segmentIndex, out Vector3 pos, out Vector3 dir)
	{
		float projT;
		float sqrAcross;
		Projection result = ProjectPointContinuouslyImpl(point, ref segmentIndex, out projT, out sqrAcross);
		dir = m_worldPoints[segmentIndex + 1] - m_worldPoints[segmentIndex];
		pos = m_worldPoints[segmentIndex] + dir * projT;
		return result;
	}

	public Projection ProjectPointContinuously(Vector3 point, ref int segmentIndex, out Vector3 pos, out Vector3 dir, out float along, out float across)
	{
		float projT;
		float sqrAcross;
		Projection result = ProjectPointContinuouslyImpl(point, ref segmentIndex, out projT, out sqrAcross);
		dir = m_worldPoints[segmentIndex + 1] - m_worldPoints[segmentIndex];
		pos = m_worldPoints[segmentIndex] + dir * projT;
		GetAlongAcrossDistances(point, segmentIndex, projT, sqrAcross, out along, out across);
		return result;
	}

	private Projection ProjectPointContinuouslyImpl(Vector3 point, ref int segmentIndex, out float projT, out float sqrAcross)
	{
		if (segmentIndex != -1)
		{
			Projection projection = ProjectPointOnSegment(point, segmentIndex, out projT, out sqrAcross);
			if (projection == Projection.Inside)
			{
				return projection;
			}
		}
		return ProjectPointImpl(point, out segmentIndex, out projT, out sqrAcross);
	}

	public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
	{
		SampleCache cache = default(SampleCache);
		cache.Reset();
		SamplePoint(distance, ref cache, out pnt, out vel, out acc);
	}

	public void SamplePoint(float distance, out int segmentIndex, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
	{
		SampleCache cache = default(SampleCache);
		cache.Reset();
		SamplePoint(distance, ref cache, out pnt, out vel, out acc);
		segmentIndex = cache.segmentIndex;
	}

	public void SamplePoint(float distance, ref SampleCache cache, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
	{
		if (m_distances.Length == 0)
		{
			pnt = Vector3.zero;
			vel = Vector3.forward;
			acc = Vector3.zero;
			return;
		}
		if (cache.instanceID != m_cachedInstanceID)
		{
			cache.Reset();
			cache.instanceID = m_cachedInstanceID;
		}
		int i = 0;
		for (int num = m_distances.Length - 1; i < num && (m_distances[i] < distance || i == 0); i++)
		{
		}
		if (i != cache.segmentIndex)
		{
			cache.segmentIndex = i;
			float num2 = m_distances[i] - m_distances[i - 1];
			cache.invSegmentLength = 1f / num2;
			Vector3 vector = m_worldPoints[i - 1];
			cache.mtx1.m00 = vector.x;
			cache.mtx1.m10 = vector.y;
			cache.mtx1.m20 = vector.z;
			vector = m_worldPoints[i];
			cache.mtx1.m01 = vector.x;
			cache.mtx1.m11 = vector.y;
			cache.mtx1.m21 = vector.z;
			vector = m_worldVelocities[i - 1] * num2;
			cache.mtx1.m02 = vector.x;
			cache.mtx1.m12 = vector.y;
			cache.mtx1.m22 = vector.z;
			vector = m_worldVelocities[i] * num2;
			cache.mtx1.m03 = vector.x;
			cache.mtx1.m13 = vector.y;
			cache.mtx1.m23 = vector.z;
			cache.mtx3 = cache.mtx1 * s_accMtx;
			cache.mtx2 = cache.mtx1 * s_velMtx;
			cache.mtx1 *= s_posMtx;
		}
		float num3 = (distance - m_distances[i - 1]) * cache.invSegmentLength;
		Vector3 point = new Vector3(num3 * num3 * num3, num3 * num3, num3);
		pnt = cache.mtx1.MultiplyPoint3x4(point);
		vel = cache.mtx2.MultiplyPoint3x4(point);
		acc = cache.mtx3.MultiplyPoint3x4(point);
	}

	public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel)
	{
		SampleCache cache = default(SampleCache);
		cache.Reset();
		SamplePoint(distance, ref cache, out pnt, out vel);
	}

	public void SamplePoint(float distance, ref SampleCache cache, out Vector3 pnt, out Vector3 vel)
	{
		if (cache.instanceID != m_cachedInstanceID)
		{
			cache.Reset();
			cache.instanceID = m_cachedInstanceID;
		}
		int i = 0;
		for (int num = m_distances.Length - 1; i < num && (m_distances[i] < distance || i == 0); i++)
		{
		}
		if (i != cache.segmentIndex)
		{
			cache.segmentIndex = i;
			float num2 = m_distances[i] - m_distances[i - 1];
			cache.invSegmentLength = 1f / num2;
			Vector3 vector = m_worldPoints[i - 1];
			cache.mtx1.m00 = vector.x;
			cache.mtx1.m10 = vector.y;
			cache.mtx1.m20 = vector.z;
			vector = m_worldPoints[i];
			cache.mtx1.m01 = vector.x;
			cache.mtx1.m11 = vector.y;
			cache.mtx1.m21 = vector.z;
			vector = m_worldVelocities[i - 1] * num2;
			cache.mtx1.m02 = vector.x;
			cache.mtx1.m12 = vector.y;
			cache.mtx1.m22 = vector.z;
			vector = m_worldVelocities[i] * num2;
			cache.mtx1.m03 = vector.x;
			cache.mtx1.m13 = vector.y;
			cache.mtx1.m23 = vector.z;
			cache.mtx2 = cache.mtx1 * s_velMtx;
			cache.mtx1 *= s_posMtx;
		}
		float num3 = (distance - m_distances[i - 1]) * cache.invSegmentLength;
		Vector3 point = new Vector3(num3 * num3 * num3, num3 * num3, num3);
		pnt = cache.mtx1.MultiplyPoint3x4(point);
		vel = cache.mtx2.MultiplyPoint3x4(point);
	}

	public void SamplePoint(float distance, out Vector3 pnt)
	{
		SampleCache cache = default(SampleCache);
		cache.Reset();
		SamplePoint(distance, ref cache, out pnt);
	}

	public void SamplePoint(float distance, ref SampleCache cache, out Vector3 pnt)
	{
		if (cache.instanceID != m_cachedInstanceID)
		{
			cache.Reset();
			cache.instanceID = m_cachedInstanceID;
		}
		int i = 0;
		for (int num = m_distances.Length - 1; i < num && (m_distances[i] < distance || i == 0); i++)
		{
		}
		if (i != cache.segmentIndex)
		{
			cache.segmentIndex = i;
			float num2 = m_distances[i] - m_distances[i - 1];
			cache.invSegmentLength = 1f / num2;
			Vector3 vector = m_worldPoints[i - 1];
			cache.mtx1.m00 = vector.x;
			cache.mtx1.m10 = vector.y;
			cache.mtx1.m20 = vector.z;
			vector = m_worldPoints[i];
			cache.mtx1.m01 = vector.x;
			cache.mtx1.m11 = vector.y;
			cache.mtx1.m21 = vector.z;
			vector = m_worldVelocities[i - 1] * num2;
			cache.mtx1.m02 = vector.x;
			cache.mtx1.m12 = vector.y;
			cache.mtx1.m22 = vector.z;
			vector = m_worldVelocities[i] * num2;
			cache.mtx1.m03 = vector.x;
			cache.mtx1.m13 = vector.y;
			cache.mtx1.m23 = vector.z;
			cache.mtx1 *= s_posMtx;
		}
		float num3 = (distance - m_distances[i - 1]) * cache.invSegmentLength;
		pnt = cache.mtx1.MultiplyPoint3x4(new Vector3(num3 * num3 * num3, num3 * num3, num3));
	}

	public int Traverse(float stepLength, int stepCount, ref float distance, ref float[] radiusArray, int startIndex, bool loop = true)
	{
		Vector3 vector = default(Vector3);
		Vector3 vector2 = default(Vector3);
		Vector3 vector3 = default(Vector3);
		Matrix4x4 matrix4x = default(Matrix4x4);
		Matrix4x4 matrix4x2 = default(Matrix4x4);
		Matrix4x4 matrix4x3 = default(Matrix4x4);
		Matrix4x4 matrix4x4 = default(Matrix4x4);
		float num = 0f;
		int num2 = -1;
		float num3 = length;
		int i = 0;
		int num4 = m_distances.Length - 1;
		for (int j = 0; j < stepCount; j++)
		{
			if (distance > num3)
			{
				distance -= num3;
				if (!loop)
				{
					return j;
				}
				i = 0;
			}
			for (; i < num4 && (m_distances[i] < distance || i == 0); i++)
			{
			}
			if (i != num2)
			{
				num2 = i;
				float num5 = m_distances[i - 1];
				float num6 = m_distances[i] - num5;
				num = 1f / num6;
				Vector3 vector4 = m_worldPoints[i - 1];
				matrix4x.m00 = vector4.x;
				matrix4x.m10 = vector4.y;
				matrix4x.m20 = vector4.z;
				vector4 = m_worldPoints[i];
				matrix4x.m01 = vector4.x;
				matrix4x.m11 = vector4.y;
				matrix4x.m21 = vector4.z;
				vector4 = m_worldVelocities[i - 1];
				matrix4x.m02 = vector4.x * num6;
				matrix4x.m12 = vector4.y * num6;
				matrix4x.m22 = vector4.z * num6;
				vector4 = m_worldVelocities[i];
				matrix4x.m03 = vector4.x * num6;
				matrix4x.m13 = vector4.y * num6;
				matrix4x.m23 = vector4.z * num6;
				matrix4x2 = matrix4x * s_velMtx;
				matrix4x3 = matrix4x * s_accMtx;
				matrix4x4 = matrix4x * s_jrkMtx;
				float num7 = (distance - num5) * num;
				Vector3 point = new Vector3(num7 * num7 * num7, num7 * num7, num7);
				vector = matrix4x2.MultiplyPoint3x4(point);
				vector2 = matrix4x3.MultiplyPoint3x4(point);
				vector3.x = matrix4x4.m03;
				vector3.y = matrix4x4.m13;
				vector3.z = matrix4x4.m23;
			}
			float num8 = stepLength * num;
			vector2.x += vector3.x * num8;
			vector2.y += vector3.y * num8;
			vector2.z += vector3.z * num8;
			vector.x += vector2.x * num8;
			vector.y += vector2.y * num8;
			vector.z += vector2.z * num8;
			radiusArray[startIndex + j] = (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z) / Mathf.Sqrt(vector2.x * vector2.x + vector2.y * vector2.y + vector2.z * vector2.z);
			distance += stepLength;
		}
		return stepCount;
	}

	public void OnChangeWorldOrigin(Vector3 originOffset)
	{
		int i = 0;
		for (int num = m_worldPoints.Length; i < num; i++)
		{
			m_worldPoints[i] -= originOffset;
		}
	}

	public void OnUpdateWorldTransform()
	{
		Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
		int i = 0;
		for (int num = m_worldPoints.Length; i < num; i++)
		{
			m_worldPoints[i] = localToWorldMatrix.MultiplyPoint3x4(m_localPoints[i]);
		}
		int j = 0;
		for (int num2 = m_worldVelocities.Length; j < num2; j++)
		{
			m_worldVelocities[j] = localToWorldMatrix.MultiplyVector(m_localVelocities[j]);
		}
	}

	[ContextMenu("Flip X")]
	public void FlipX()
	{
		GetWorldPoints(out var points);
		for (int i = 0; i < points.Length; i++)
		{
			points[i].x = 0f - points[i].x;
		}
		SetWorldPoints(points);
	}

	[ContextMenu("Flip Z")]
	public void FlipZ()
	{
		FlipZ(0f);
	}

	public void FlipZ(float zOrigin)
	{
		GetWorldPoints(out var points);
		for (int i = 0; i < points.Length; i++)
		{
			points[i].z = zOrigin - points[i].z;
		}
		SetWorldPoints(points);
	}

	[ContextMenu("Reverse")]
	public void Reverse()
	{
		GetWorldPoints(out var points);
		Array.Reverse(points);
		SetWorldPoints(points);
	}

	[ContextMenu("Align X to Zero")]
	public void AlignZeroX()
	{
		GetWorldPoints(out var points);
		for (int i = 0; i < points.Length; i++)
		{
			points[i].x = 0f;
		}
		SetWorldPoints(points);
	}

	[ContextMenu("Align pivot only to Zero")]
	public void AlignZeroPivotOnly()
	{
		GetWorldPoints(out var points);
		base.transform.localPosition = Vector3.zero;
		SetWorldPoints(points);
	}

	[ContextMenu("Align X as first point")]
	public void AlignFirstX()
	{
		GetWorldPoints(out var points);
		if (points.Length > 1)
		{
			float x = points[0].x;
			for (int i = 1; i < points.Length; i++)
			{
				points[i].x = x;
			}
		}
		SetWorldPoints(points);
	}

	[ContextMenu("Project points to gorund (Y axis)")]
	public void ProjectToGround()
	{
		GetWorldPoints(out var points);
		for (int i = 0; i < points.Length; i++)
		{
			if (Physics.Raycast(new Ray
			{
				origin = points[i] + new Vector3(0f, 100f, 0f),
				direction = new Vector3(0f, -1f, 0f)
			}, out var hitInfo, 200f))
			{
				points[i].y = hitInfo.point.y;
			}
		}
		SetWorldPoints(points);
	}

	[ContextMenu("Reset Y to 0")]
	public void ResetY()
	{
		GetWorldPoints(out var points);
		for (int i = 0; i < points.Length; i++)
		{
			points[i].y = 0f;
		}
		SetWorldPoints(points);
	}

	private void OnDrawGizmosSelected()
	{
		if (length != 0f)
		{
			SampleCache cache = default(SampleCache);
			cache.Reset();
			Vector3 vector = GetWorldPoint(0);
			float num = 0f;
			while (num < length)
			{
				SamplePoint(num, ref cache, out var pnt);
				Gizmos.color = Color.magenta;
				Gizmos.DrawLine(vector, pnt);
				Gizmos.DrawLine(vector + new Vector3(0f, 1f, 0f), pnt + new Vector3(0f, 1f, 0f));
				Gizmos.DrawLine(vector, vector + new Vector3(0f, 1f, 0f));
				Gizmos.DrawLine(pnt, pnt + new Vector3(0f, 1f, 0f));
				num += 1f;
				vector = pnt;
			}
		}
	}

	[ContextMenu("Build")]
	public void Build()
	{
		int num = pointCount;
		bool flag = m_closed && num >= 3;
		if (flag)
		{
			Vector3 vector = m_localPoints[0];
			Vector3 b = m_localPoints[num - 1];
			if (Vector3.Distance(vector, b) < 1f)
			{
				m_localPoints[num - 1] = vector;
			}
		}
		m_localVelocities = new Vector3[num];
		m_distances = new float[num];
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			if (i != 0)
			{
				num2 += Vector3.Distance(m_localPoints[i - 1], m_localPoints[i]);
			}
			m_distances[i] = num2;
		}
		for (int j = 1; j < num - 1; j++)
		{
			m_localVelocities[j] = GetMidVel(j, j - 1, j + 1);
		}
		if (num > 0)
		{
			if (flag)
			{
				m_localVelocities[0] = GetMidVel(0, num - 2, 1);
				m_localVelocities[num - 1] = GetMidVel(num - 1, num - 2, 1);
			}
			else
			{
				m_localVelocities[0] = GetStartVel(0);
				m_localVelocities[num - 1] = GetEndVel(num - 1);
			}
		}
		m_worldPoints = new Vector3[num];
		m_worldVelocities = new Vector3[num];
		OnUpdateWorldTransform();
	}

	[ContextMenu("Smooth")]
	public void Smooth()
	{
		int num = pointCount;
		if (num >= 3)
		{
			Vector3 vector = GetStartVel(0);
			for (int i = 1; i < num - 1; i++)
			{
				Vector3 vector2 = (GetEndVel(i) * m_distances[i] + GetStartVel(i) * m_distances[i - 1]) * (1f / (m_distances[i - 1] + m_distances[i]));
				m_localVelocities[i - 1] = vector;
				vector = vector2;
			}
			m_localVelocities[num - 1] = GetEndVel(num - 1);
			m_localVelocities[num - 2] = vector;
			OnUpdateWorldTransform();
		}
	}

	[ContextMenu("Reset Transform")]
	public void ResetTransform()
	{
		GetWorldPoints(out var points);
		base.transform.position = Vector3.zero;
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		SetWorldPoints(points);
	}

	[ContextMenu("Reset Scale")]
	public void ResetScale()
	{
		GetLocalPoints(out var points);
		Vector3 localScale = base.transform.localScale;
		for (int i = 0; i < points.Length; i++)
		{
			points[i].x = points[i].x * localScale.x;
			points[i].y = points[i].y * localScale.y;
			points[i].z = points[i].z * localScale.z;
		}
		base.transform.localScale = Vector3.one;
		SetLocalPoints(points);
	}

	[ContextMenu("Shift left")]
	public void ShiftLeft()
	{
		GetWorldPoints(out var points);
		Vector3[] array = new Vector3[points.Length];
		for (int i = 0; i < points.Length - 1; i++)
		{
			array[i] = points[i + 1];
		}
		array[points.Length - 1] = points[0];
		SetWorldPoints(array);
	}

	[ContextMenu("Shift right")]
	public void ShiftRight()
	{
		GetWorldPoints(out var points);
		Vector3[] array = new Vector3[points.Length];
		for (int i = 1; i < points.Length; i++)
		{
			array[i] = points[i - 1];
		}
		array[0] = points[^1];
		SetWorldPoints(array);
	}

	private void Awake()
	{
		m_cachedInstanceID = GetInstanceID();
	}

	private Vector3 GetMidVel(int curr, int prev, int next)
	{
		Vector3 vector = Vector3.Normalize(m_localPoints[next] - m_localPoints[curr]);
		Vector3 vector2 = Vector3.Normalize(m_localPoints[prev] - m_localPoints[curr]);
		return Vector3.Normalize(vector - vector2);
	}

	private Vector3 GetStartVel(int index)
	{
		return (3f * (m_localPoints[index + 1] - m_localPoints[index]) / (m_distances[index + 1] - m_distances[index]) - m_localVelocities[index + 1]) * 0.5f;
	}

	private Vector3 GetEndVel(int index)
	{
		return (3f * (m_localPoints[index] - m_localPoints[index - 1]) / (m_distances[index] - m_distances[index - 1]) - m_localVelocities[index - 1]) * 0.5f;
	}

	public static float CalcCurvature(Vector3 vel, Vector3 acc)
	{
		float num = vel.sqrMagnitude / acc.magnitude;
		if (!(vel.z * acc.x - vel.x * acc.z < 0f))
		{
			return num;
		}
		return 0f - num;
	}

	private static Matrix4x4 GetPosMtx()
	{
		Matrix4x4 result = default(Matrix4x4);
		result.SetRow(0, new Vector4(2f, -3f, 0f, 1f));
		result.SetRow(1, new Vector4(-2f, 3f, 0f, 0f));
		result.SetRow(2, new Vector4(1f, -2f, 1f, 0f));
		result.SetRow(3, new Vector4(1f, -1f, 0f, 0f));
		return result;
	}

	private static Matrix4x4 GetVelMtx()
	{
		Matrix4x4 result = default(Matrix4x4);
		result.SetRow(0, new Vector4(0f, 6f, -6f, 0f));
		result.SetRow(1, new Vector4(0f, -6f, 6f, 0f));
		result.SetRow(2, new Vector4(0f, 3f, -4f, 1f));
		result.SetRow(3, new Vector4(0f, 3f, -2f, 0f));
		return result;
	}

	private static Matrix4x4 GetAccMtx()
	{
		Matrix4x4 result = default(Matrix4x4);
		result.SetRow(0, new Vector4(0f, 0f, 12f, -6f));
		result.SetRow(1, new Vector4(0f, 0f, -12f, 6f));
		result.SetRow(2, new Vector4(0f, 0f, 6f, -4f));
		result.SetRow(3, new Vector4(0f, 0f, 6f, -2f));
		return result;
	}

	private static Matrix4x4 GetJrkMtx()
	{
		Matrix4x4 result = default(Matrix4x4);
		result.SetRow(0, new Vector4(0f, 0f, 0f, 12f));
		result.SetRow(1, new Vector4(0f, 0f, 0f, -12f));
		result.SetRow(2, new Vector4(0f, 0f, 0f, 6f));
		result.SetRow(3, new Vector4(0f, 0f, 0f, 6f));
		return result;
	}

	public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across)
	{
		GameLog.Print(AlertLevel.Warning, "System", "Slow projection used!");
		return new SplineModel(m_cachedInstanceID, m_worldPoints, m_worldVelocities, m_distances, m_closed).ProjectPoint(pnt, ref segmentIndex, out along, out across, -1);
	}
}
