using SplineLogic;
using UnityEngine;

[AddComponentMenu("Scripts/Track/Track")]
public class Track : MonoBehaviour
{
	public enum VertexType
	{
		Central = 0,
		Left = 1,
		Right = 2
	}

	[SerializeField]
	private TrackSpline m_centerLine;

	[SerializeField]
	private TrackSpline m_racingLine;

	[SerializeField]
	private TrackSpline m_leftBorderLine;

	[SerializeField]
	private TrackSpline m_rightBorderLine;

	[SerializeField]
	private bool m_closed;

	public static Track instance;

	private ISpline m_cachedCenterSpline;

	private ISpline m_cachedRacingSpline;

	private ISpline m_cachedLeftSpline;

	private ISpline m_cachedRightSpline;

	public ISpline centerLine
	{
		get
		{
			if (m_cachedCenterSpline == null && m_centerLine != null)
			{
				m_cachedCenterSpline = m_centerLine;
			}
			return m_cachedCenterSpline;
		}
		set
		{
			m_cachedCenterSpline = value;
		}
	}

	public ISpline racingLine
	{
		get
		{
			if (m_cachedRacingSpline == null && m_racingLine != null)
			{
				m_cachedRacingSpline = m_racingLine;
			}
			return m_cachedRacingSpline;
		}
		set
		{
			m_cachedRacingSpline = value;
		}
	}

	public ISpline leftBorderLine
	{
		get
		{
			if (m_cachedLeftSpline == null && m_leftBorderLine != null)
			{
				m_cachedLeftSpline = m_leftBorderLine;
			}
			return m_cachedLeftSpline;
		}
		set
		{
			m_cachedLeftSpline = value;
		}
	}

	public ISpline rightBorderLine
	{
		get
		{
			if (m_cachedRightSpline == null && m_rightBorderLine != null)
			{
				m_cachedRightSpline = m_rightBorderLine;
			}
			return m_cachedRightSpline;
		}
		set
		{
			m_cachedRightSpline = value;
		}
	}

	public bool closed => m_closed;

	private void OnEnable()
	{
		instance = this;
	}

	private void OnDisable()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	[ContextMenu("Rebuild All Splines")]
	private void RebuildAllSplines()
	{
		if ((bool)m_centerLine)
		{
			m_centerLine.Build();
		}
		if ((bool)m_racingLine)
		{
			m_racingLine.Build();
		}
		if ((bool)m_leftBorderLine)
		{
			m_leftBorderLine.Build();
		}
		if ((bool)m_rightBorderLine)
		{
			m_rightBorderLine.Build();
		}
	}
}
