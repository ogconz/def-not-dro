using SplineLogic;
using UnityEngine;

public class RaceCalculator
{
	private Track m_track;

	private BaseCar m_car;

	private bool m_initialized;

	private Vector3[] m_cfront;

	private Vector3[] m_cright;

	private Vector3[] m_raccel;

	private float[] m_maxV;

	private int m_pointCount;

	private float m_stepLength;

	private float m_stepLengthInv;

	private float m_totalLength;

	private int m_leftBorderSegmentIndex;

	private int m_rightBorderSegmentIndex;

	public void Init(BaseCar car, Track track)
	{
		m_car = car;
		m_track = track;
		if (!m_track || !m_car)
		{
			m_initialized = false;
			return;
		}
		InitSpeedMap();
		m_initialized = true;
	}

	public void Clear()
	{
		m_initialized = false;
		m_cfront = null;
		m_cright = null;
		m_raccel = null;
		m_maxV = null;
	}

	public float GetMaxSpeedAtDistance(float distance)
	{
		if (!m_initialized)
		{
			return float.MaxValue;
		}
		distance = Mathf.Repeat(distance, m_totalLength);
		float num = distance * m_stepLengthInv;
		int num2 = Mathf.FloorToInt(num);
		int num3 = num2 + 1;
		if (num3 >= m_pointCount)
		{
			num3 -= m_pointCount;
		}
		return Mathf.Lerp(m_maxV[num2], m_maxV[num3], num - (float)num2);
	}

	public int? GetLapDriftPoints()
	{
		if (!m_initialized)
		{
			return null;
		}
		double num = 0.0;
		for (int i = 0; i < m_pointCount - 1; i++)
		{
			int num2 = i;
			int num3 = i + 1;
			Vector3 lhs = m_cfront[num2];
			Vector3 rhs = m_cfront[num3];
			float num4 = Mathf.Acos(Mathf.Clamp01(Vector3.Dot(lhs, rhs)));
			float num5 = Mathf.Max(m_maxV[num2], m_maxV[num3]);
			num += 2.0 * (double)num4 * (double)num5;
		}
		return Mathf.CeilToInt((float)num / 100f) * 100;
	}

	public float? GetLapTime(float startSpeed)
	{
		if (!m_initialized)
		{
			return null;
		}
		float num = startSpeed;
		float num2 = 0f;
		int optimalGear = m_car.carX.GetOptimalGear(num, 0f, 1, simulation: true);
		for (int i = 0; i < m_pointCount - 1; i++)
		{
			int num3 = i;
			int num4 = i + 1;
			float num5 = m_maxV[num3];
			float num6 = m_maxV[num4];
			Vector3 cfront = m_cfront[num3] + m_cfront[num4];
			cfront.Normalize();
			Vector3 cright = m_cright[num3] + m_cright[num4];
			cright.Normalize();
			Vector3 r_accel = (m_raccel[num3] + m_raccel[num4]) * 0.5f;
			if (num > num5)
			{
				num = num5;
			}
			optimalGear = m_car.carX.GetOptimalGear(num, 0f, optimalGear, simulation: true);
			m_car.carX.CalcMaxAccel(optimalGear, num, cfront, cright, r_accel, out var drag_accel_eng, out var drag_accel_tyre, out var _, out var _, out var side_accel, out var side_accel_max, out var ext_accel_front, out var _);
			side_accel = Mathf.Abs(side_accel);
			if (side_accel > side_accel_max)
			{
				side_accel_max = side_accel;
			}
			float num7 = Mathf.Sqrt(1f - side_accel * side_accel / (side_accel_max * side_accel_max)) * drag_accel_tyre;
			if (num7 > drag_accel_eng)
			{
				num7 = drag_accel_eng;
			}
			num7 += ext_accel_front;
			float num8 = num;
			float stepLength = m_stepLength;
			float num9 = 0f;
			if (num7 > 0.001f)
			{
				num9 = (0f - num + Mathf.Sqrt(num * num + 2f * num7 * stepLength)) / num7;
				num8 = num + num7 * num9;
			}
			else if (num > 0.01f)
			{
				num9 = m_stepLength / num;
				num8 = num;
			}
			if (num <= num5 && num8 < num6)
			{
				num2 += num9;
				num = num8;
			}
			else if (num > num5 - 0.001f && num8 > num6 - 0.001f)
			{
				float num10 = 2f * stepLength / (num5 + num6);
				num2 += num10;
				num8 = num6;
				num = num8;
			}
			else if (num8 > num6)
			{
				float num11 = 2f * stepLength / (num + num6);
				num2 += num11;
				num8 = num6;
				num = num8;
			}
			else
			{
				num = num8;
			}
		}
		return num2;
	}

	private void InitSpeedMap()
	{
		float num = m_car.carX.CalcTopSpeed();
		ISpline racingLine = m_track.racingLine;
		m_totalLength = racingLine.length;
		m_pointCount = 2;
		m_stepLength = m_totalLength;
		if (m_stepLength > 1f)
		{
			m_pointCount = Mathf.CeilToInt(m_stepLength / 1f) + 1;
			if (m_pointCount > 3000)
			{
				m_pointCount = 3000;
			}
			m_stepLength = m_totalLength / (float)(m_pointCount - 1);
			m_stepLengthInv = 1f / m_stepLength;
		}
		m_cfront = new Vector3[m_pointCount];
		m_cright = new Vector3[m_pointCount];
		m_raccel = new Vector3[m_pointCount];
		m_maxV = new float[m_pointCount];
		float num2 = 0f;
		m_leftBorderSegmentIndex = -1;
		m_rightBorderSegmentIndex = -1;
		float drag_accel_eng;
		float drag_accel_tyre;
		float brake_accel_br;
		float brake_accel_tyre;
		float side_accel;
		float side_accel_max;
		float ext_accel_front;
		for (int i = 0; i < m_pointCount; i++)
		{
			if (i == m_pointCount - 1)
			{
				num2 = m_totalLength;
			}
			GetMagicVectors(num2, out var _, out var front, out var right, out var r_accel);
			m_cfront[i] = front;
			m_cright[i] = right;
			m_raccel[i] = r_accel;
			float Vmax = 30f;
			for (int j = 0; j < 3; j++)
			{
				m_car.carX.CalcMaxAccel(1, Vmax, front, right, r_accel, out drag_accel_eng, out drag_accel_tyre, out brake_accel_br, out brake_accel_tyre, out side_accel, out side_accel_max, out ext_accel_front, out Vmax);
				if (Vmax > num * 1.5f)
				{
					Vmax = num * 1.5f;
				}
			}
			m_maxV[i] = Vmax;
			num2 += m_stepLength;
		}
		float num3 = m_maxV[m_pointCount - 1];
		bool flag = false;
		for (int num4 = m_pointCount - 1; num4 >= 0; num4--)
		{
			float Vmax2 = 30f;
			m_car.carX.CalcMaxAccel(1, num3, m_cfront[num4], m_cright[num4], m_raccel[num4], out drag_accel_eng, out drag_accel_tyre, out brake_accel_br, out brake_accel_tyre, out side_accel, out side_accel_max, out ext_accel_front, out Vmax2);
			if (Vmax2 > num * 1.5f)
			{
				Vmax2 = num * 1.5f;
			}
			if (num3 > Vmax2)
			{
				num3 = Vmax2;
			}
			if (num3 < m_maxV[num4])
			{
				m_maxV[num4] = num3;
			}
			side_accel = Mathf.Abs(side_accel);
			if (side_accel > side_accel_max)
			{
				side_accel_max = side_accel;
			}
			float num5 = Mathf.Sqrt(1f - side_accel * side_accel / (side_accel_max * side_accel_max)) * brake_accel_tyre;
			if (num5 > brake_accel_br)
			{
				num5 = brake_accel_br;
			}
			num5 -= ext_accel_front;
			float num7;
			if (num5 > 0.001f)
			{
				float stepLength = m_stepLength;
				float num6 = (0f - num3 + Mathf.Sqrt(num3 * num3 + 2f * num5 * stepLength)) / num5;
				num7 = num3 + num5 * num6;
			}
			else
			{
				num7 = num3;
			}
			num3 = num7;
			if (num4 == 0 && !flag)
			{
				num4 = m_pointCount - 1;
				flag = true;
			}
		}
	}

	private void GetMagicVectors(float dist, out Vector3 point, out Vector3 front, out Vector3 right, out Vector3 r_accel)
	{
		m_track.racingLine.SamplePoint(dist, out point, out var vel, out var acc);
		front = Vector3.Normalize(vel);
		right = GetRight(front, point);
		r_accel = Vector3.Normalize(acc) / (GetRadius(m_track.racingLine, dist, point) + 0.0001f);
	}

	private Vector3 GetRight(Vector3 front, Vector3 pnt)
	{
		Vector3 projectPoint = GetProjectPoint(pnt, m_track.leftBorderLine, ref m_leftBorderSegmentIndex);
		Vector3 rhs = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(GetProjectPoint(pnt, m_track.rightBorderLine, ref m_rightBorderSegmentIndex) - projectPoint), front));
		return Vector3.Normalize(Vector3.Cross(front, rhs));
	}

	private Vector3 GetProjectPoint(Vector3 pnt, ISpline sp, ref int segmentIndex)
	{
		float distance = 0f;
		sp.SamplePoint(distance, out pnt);
		return pnt;
	}

	private Vector3 GetRAccel(TrackSpline spline, float currDist, Vector3 currPnt)
	{
		float distance = Mathf.Repeat(currDist - 1.5f, spline.length);
		float distance2 = Mathf.Repeat(currDist + 1.5f, spline.length);
		spline.SamplePoint(distance, out var pnt);
		spline.SamplePoint(distance2, out var pnt2);
		return GetRAccelFrom3Points(pnt, currPnt, pnt2);
	}

	private Vector3 GetRAccelFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		Vector3 vector = p2 - p1;
		Vector3 vector2 = p3 - p2;
		Vector2 vector3 = new Vector2(vector.x, vector.z);
		Vector2 vector4 = new Vector2(vector2.x, vector2.z);
		Vector2 rhs = new Vector2(p1.x, p1.z) + vector3 * 0.5f;
		Vector2 rhs2 = new Vector2(p2.x, p2.z) + vector4 * 0.5f;
		float num = 0f - Vector2.Dot(vector3, rhs);
		float num2 = 0f - Vector2.Dot(vector4, rhs2);
		float x = vector3.x;
		float y = vector3.y;
		float x2 = vector4.x;
		float y2 = vector4.y;
		float num3 = x * y2 - x2 * y;
		float num4 = (0f - num) * y2 + num2 * y;
		float num5 = (0f - num2) * x + num * x2;
		float num6 = num4 * num4 + num5 * num5 + num3 * num3 * (p2.x * p2.x + p2.z * p2.z) - 2f * num3 * (num4 * p2.x + num5 * p2.z);
		float num7 = 1f / num6;
		return new Vector3(num3 * (num4 - p2.x * num3) * num7, 0f, num3 * (num5 - p2.z * num3) * num7);
	}

	private float GetRadius(ISpline spline, float currDist, Vector3 currPnt)
	{
		float distance = Mathf.Repeat(currDist - 1.5f, spline.length);
		float distance2 = Mathf.Repeat(currDist + 1.5f, spline.length);
		spline.SamplePoint(distance, out var pnt);
		spline.SamplePoint(distance2, out var pnt2);
		return GetRadiusFrom3Points(pnt, currPnt, pnt2);
	}

	private float GetRadiusFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		Vector3 rhs = p2 - p1;
		Vector3 lhs = p3 - p2;
		Vector2 vector = new Vector2(rhs.x, rhs.z);
		Vector2 vector2 = new Vector2(lhs.x, lhs.z);
		Vector2 rhs2 = new Vector2(p1.x, p1.z) + vector * 0.5f;
		Vector2 rhs3 = new Vector2(p2.x, p2.z) + vector2 * 0.5f;
		float num = 0f - Vector2.Dot(vector, rhs2);
		float num2 = 0f - Vector2.Dot(vector2, rhs3);
		float x = vector.x;
		float y = vector.y;
		float x2 = vector2.x;
		float y2 = vector2.y;
		float num3 = x * y2 - x2 * y;
		if (Mathf.Abs(num3) < 0.001f)
		{
			return float.MaxValue;
		}
		float x3 = ((0f - num) * y2 + num2 * y) / num3;
		float y3 = ((0f - num2) * x + num * x2) / num3;
		float num4 = Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(x3, y3));
		if (Vector3.Cross(lhs, rhs).z < 0f)
		{
			num4 = 0f - num4;
		}
		return num4;
	}
}
