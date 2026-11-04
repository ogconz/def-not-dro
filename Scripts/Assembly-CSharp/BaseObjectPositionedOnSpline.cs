using SplineLogic;
using UnityEngine;

[AddComponentMenu("")]
public abstract class BaseObjectPositionedOnSpline : MonoBehaviour
{
	[SerializeField]
	private TrackSpline m_spline;

	[SerializeField]
	private float m_distance;

	private float? m_prevDistance;

	[SerializeField]
	private int m_segmentIndex = -1;

	private ISpline m_cachedSpline;

	public int segmentIndex
	{
		get
		{
			if (m_segmentIndex == -1)
			{
				Track instance = Track.instance;
				if ((bool)instance)
				{
					instance.centerLine.ProjectPoint(base.transform.position, ref m_segmentIndex, out float _, out float _, -1);
				}
			}
			return m_segmentIndex;
		}
	}

	public ISpline spline
	{
		get
		{
			if (m_cachedSpline == null && m_spline != null)
			{
				m_cachedSpline = m_spline;
			}
			return m_cachedSpline;
		}
		set
		{
			m_cachedSpline = value;
		}
	}

	public float distance
	{
		get
		{
			return m_distance;
		}
		set
		{
			m_distance = value;
			OnValidate();
		}
	}

	private void OnValidate()
	{
		if ((bool)m_spline)
		{
			m_distance = Mathf.Repeat(m_distance, m_spline.length);
		}
		if (!m_prevDistance.HasValue || m_distance != m_prevDistance.Value)
		{
			m_prevDistance = m_distance;
			Reposition();
		}
	}

	protected void Reposition()
	{
		if ((bool)m_spline)
		{
			m_spline.SamplePoint(m_distance, out var pnt, out var vel, out var _);
			base.transform.localPosition = pnt;
			base.transform.localRotation = Quaternion.LookRotation(vel);
		}
	}
}
