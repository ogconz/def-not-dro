using TrackLayout.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace TrackLayout.Data
{
	public class BlipData : IMovable
	{
		private Vector3 m_position;

		private Quaternion m_rotation;

		private bool m_visible;

		public float crossFadeElapsedTime;

		public Vector3 position
		{
			get
			{
				return m_position;
			}
			set
			{
				m_position = value;
				wasPositionChanged = true;
			}
		}

		public Quaternion rotation
		{
			get
			{
				return m_rotation;
			}
			set
			{
				m_rotation = value;
				wasPositionChanged = true;
			}
		}

		public bool visible
		{
			get
			{
				return m_visible;
			}
			set
			{
				if (m_visible != value)
				{
					m_visible = value;
					wasVisibilityChanged = true;
				}
			}
		}

		public Transform transform { get; private set; }

		public Image image { get; private set; }

		public float crossFadeDuration { get; set; }

		public bool wasPositionChanged { get; set; }

		public bool wasVisibilityChanged { get; set; }

		public BlipData(Transform t, Image i)
		{
			transform = t;
			image = i;
		}
	}
}
