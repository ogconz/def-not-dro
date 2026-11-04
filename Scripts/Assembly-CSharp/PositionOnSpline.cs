using SplineLogic;
using UnityEngine;

[AddComponentMenu("Scripts/Track/PositionOnSpline")]
public class PositionOnSpline : MonoBehaviour
{
	private Transform m_cachedTransform;

	private float m_distanceAlongCenterLine;

	private float m_distanceAcrossCenterLine;

	private float? m_prevDistanceAlongCenterLine;

	private float m_reverseDistanceTraveled;

	private float m_totalDistanceTraveled;

	private bool m_isInsideTrack;

	private Vector3? m_prevPosition;

	private SampleCache m_targetSampleCache;

	private const int kMaxSampleCount = 256;

	private Track m_track;

	private int m_nextCheckpointIndex = -1;

	private int m_clCurrPosSI = -1;

	private int m_lbCurrPosSI = -1;

	private int m_rbCurrPosSI = -1;

	public Track track
	{
		get
		{
			return m_track;
		}
		set
		{
			m_track = value;
			SetDirty();
		}
	}

	public float distanceAlongCenterLine
	{
		get
		{
			UpdatePositionIfNeeded();
			return m_distanceAlongCenterLine;
		}
	}

	public float distanceAcrossCenterLine
	{
		get
		{
			UpdatePositionIfNeeded();
			return m_distanceAcrossCenterLine;
		}
	}

	public bool isMovingInReverseDirection => m_reverseDistanceTraveled > 3f;

	public Transform cachedTransform => m_cachedTransform;

	public float totalDistanceTraveled => m_totalDistanceTraveled;

	public bool isInsideTrack
	{
		get
		{
			UpdatePositionIfNeeded();
			return m_isInsideTrack;
		}
	}

	public void SetDirty()
	{
		m_prevPosition = null;
		m_prevDistanceAlongCenterLine = null;
		m_targetSampleCache.Reset();
		m_lbCurrPosSI = -1;
		m_rbCurrPosSI = -1;
		m_clCurrPosSI = -1;
		m_nextCheckpointIndex = -1;
	}

	public float GetShiftedDistanceAlongCenterLine(float shift)
	{
		float num = distanceAlongCenterLine + shift;
		float length = m_track.centerLine.length;
		if (num >= 0f)
		{
			return Mathf.Repeat(num, length);
		}
		return length - Mathf.Repeat(Mathf.Abs(num), length);
	}

	public float GetForwardDistance(float from, float to)
	{
		float length = m_track.centerLine.length;
		float num;
		for (num = to - from; num < 0f; num += length)
		{
		}
		return Mathf.Repeat(num, length);
	}

	public float GetDistanceBetweenPositions(float newPos, float oldPos)
	{
		float length = m_track.centerLine.length;
		float num = GetForwardDistance(oldPos, newPos);
		if (num > length * 0.5f)
		{
			num -= length;
		}
		return num;
	}

	public void GetNearestCenterPosition(Vector3 point, out Vector3 position, out Vector3 direction)
	{
		m_track.centerLine.ProjectPoint(point, out position, out direction);
	}

	public float GetTargetDistance(float relativeDistance)
	{
		if (m_track == null)
		{
			return 0f;
		}
		ISpline racingLine = m_track.racingLine;
		float length = racingLine.length;
		float along;
		float across;
		Projection num = racingLine.ProjectPoint(cachedTransform.position, out along, out across);
		float num2 = along;
		if (num == Projection.Before)
		{
			num2 = 0f - across;
		}
		num2 += relativeDistance;
		if (racingLine.closed)
		{
			for (; num2 < 0f; num2 += length)
			{
			}
			while (num2 > length)
			{
				num2 -= length;
			}
		}
		else
		{
			num2 = Mathf.Clamp(num2, 0f, length);
		}
		return num2;
	}

	public float GetTarget(float relativeDistance, out Vector3 point, out Vector3 direction, out float radius)
	{
		float targetDistance = GetTargetDistance(relativeDistance);
		point = (direction = Vector3.zero);
		radius = 0f;
		if (m_track == null)
		{
			return 0f;
		}
		m_track.racingLine.SamplePoint(targetDistance, ref m_targetSampleCache, out point, out var vel, out var acc);
		radius = TrackSpline.CalcCurvature(vel, acc);
		direction = vel.normalized;
		return targetDistance;
	}

	public bool GetLeftBorderPointAndDirection(Vector3 point, out Vector3 borderPoint, out Vector3 borderDirection)
	{
		borderPoint = (borderDirection = Vector3.zero);
		bool flag = m_track.leftBorderLine != null;
		if (flag)
		{
			Projection projection = m_track.leftBorderLine.ProjectPoint(point, out borderPoint, out borderDirection);
			flag = projection == Projection.Inside;
		}
		return flag;
	}

	public bool GetRightBorderPointAndDirection(Vector3 point, out Vector3 borderPoint, out Vector3 borderDirection)
	{
		borderPoint = (borderDirection = Vector3.zero);
		bool flag = m_track.rightBorderLine != null;
		if (flag)
		{
			Projection projection = m_track.rightBorderLine.ProjectPoint(point, out borderPoint, out borderDirection);
			flag = projection == Projection.Inside;
		}
		return flag;
	}

	private void UpdatePositionIfNeeded()
	{
		if (!m_track || m_track.centerLine == null)
		{
			return;
		}
		Vector3 position = cachedTransform.position;
		if (m_prevPosition.HasValue && !(Vector3.SqrMagnitude(m_prevPosition.Value - position) > 0.001f))
		{
			return;
		}
		int clCurrPosSI = m_clCurrPosSI;
		int lbCurrPosSI = m_lbCurrPosSI;
		int rbCurrPosSI = m_rbCurrPosSI;
		int num = Mathf.Max(rbCurrPosSI, lbCurrPosSI, clCurrPosSI);
		m_track.centerLine.ProjectPoint(position, ref m_clCurrPosSI, out m_distanceAlongCenterLine, out m_distanceAcrossCenterLine, -1);
		m_track.leftBorderLine.ProjectPoint(position, ref m_lbCurrPosSI, out float along, out float across, -1);
		m_track.rightBorderLine.ProjectPoint(position, ref m_rbCurrPosSI, out float along2, out float across2, -1);
		if (m_nextCheckpointIndex != -1 || num != -1)
		{
			int num2 = Mathf.Max(m_nextCheckpointIndex, num) + 5;
			if (m_clCurrPosSI > num2 && ((clCurrPosSI <= num2 && clCurrPosSI >= 0) || m_lbCurrPosSI <= num2 || m_rbCurrPosSI <= num2))
			{
				m_clCurrPosSI = Mathf.Min(m_lbCurrPosSI, m_rbCurrPosSI, clCurrPosSI) - 5;
				m_track.centerLine.ProjectPoint(position, ref m_clCurrPosSI, out m_distanceAlongCenterLine, out m_distanceAcrossCenterLine, num2);
			}
			if (m_lbCurrPosSI > num2 && ((lbCurrPosSI <= num2 && lbCurrPosSI >= 0) || m_clCurrPosSI <= num2 || m_rbCurrPosSI <= num2))
			{
				m_lbCurrPosSI = Mathf.Min(m_clCurrPosSI, m_rbCurrPosSI, lbCurrPosSI) - 5;
				m_track.leftBorderLine.ProjectPoint(position, ref m_lbCurrPosSI, out along, out across, num2);
			}
			if (m_rbCurrPosSI > num2 && ((rbCurrPosSI <= num2 && rbCurrPosSI >= 0) || m_clCurrPosSI <= num2 || m_lbCurrPosSI <= num2))
			{
				m_rbCurrPosSI = Mathf.Min(m_clCurrPosSI, m_lbCurrPosSI, rbCurrPosSI) - 5;
				m_track.rightBorderLine.ProjectPoint(position, ref m_rbCurrPosSI, out along2, out across2, num2);
			}
		}
		m_isInsideTrack = across > -2f && across2 < 2f;
		m_prevPosition = position;
	}

	public void SetNextCheckpointIndex(int nextCheckpointIndex)
	{
		m_nextCheckpointIndex = nextCheckpointIndex;
	}

	private void Awake()
	{
		m_cachedTransform = base.transform;
	}

	private void FixedUpdate()
	{
		if (!m_track || m_track.centerLine == null)
		{
			return;
		}
		if (m_prevDistanceAlongCenterLine.HasValue)
		{
			float distanceBetweenPositions = GetDistanceBetweenPositions(distanceAlongCenterLine, m_prevDistanceAlongCenterLine.Value);
			m_totalDistanceTraveled += Mathf.Abs(distanceBetweenPositions);
			if (distanceBetweenPositions < 0f)
			{
				m_reverseDistanceTraveled += Mathf.Abs(distanceBetweenPositions);
			}
			else
			{
				m_reverseDistanceTraveled = 0f;
			}
		}
		m_prevDistanceAlongCenterLine = distanceAlongCenterLine;
	}
}
