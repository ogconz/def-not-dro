using UnityEngine;

public class UITextScroller : MonoBehaviour
{
	private const float ScrollError = 0.1f;

	[SerializeField]
	private Transform[] m_items;

	[SerializeField]
	private float m_distance = 50f;

	[SerializeField]
	private float m_interval = 10f;

	[SerializeField]
	private float m_speed = 1f;

	private float m_position;

	private float m_startPosition;

	private float m_endPosition;

	private float m_lastScrollTime;

	private int m_previousIndex;

	private int m_currentIndex;

	private bool m_freezed;

	private bool m_active;

	public void StartScrolling()
	{
		if (!m_active)
		{
			m_position = m_items[0].localPosition.y;
			m_startPosition = m_position + m_distance;
			m_endPosition = m_position - m_distance;
			m_lastScrollTime = Time.time;
			m_previousIndex = -1;
			m_currentIndex = 0;
			m_freezed = false;
			m_active = true;
			for (int i = 1; i < m_items.Length; i++)
			{
				MoveItem(i, m_endPosition);
			}
		}
	}

	public void StopScrolling()
	{
		m_active = false;
	}

	public void SetFreezing(bool freezed)
	{
		m_freezed = freezed;
	}

	public void ScrollTo(int index)
	{
		if (index >= 0 && index < m_items.Length && index != m_currentIndex)
		{
			m_previousIndex = m_currentIndex;
			m_currentIndex = index;
			MoveItem(m_currentIndex, m_startPosition);
			m_lastScrollTime = Time.time;
		}
	}

	private void Update()
	{
		if (m_active)
		{
			if (!m_freezed && Time.time > m_lastScrollTime + m_interval)
			{
				m_previousIndex = m_currentIndex;
				m_currentIndex = ((m_currentIndex < m_items.Length - 1) ? (m_currentIndex + 1) : 0);
				MoveItem(m_previousIndex, m_position);
				MoveItem(m_currentIndex, m_startPosition);
				m_lastScrollTime = Time.time;
			}
			ScrollItem(m_previousIndex, m_endPosition);
			ScrollItem(m_currentIndex, m_position);
		}
	}

	private void ScrollItem(int index, float position)
	{
		if (index < 0)
		{
			return;
		}
		Transform transform = m_items[index];
		if (transform.localPosition.y != position)
		{
			if (Mathf.Abs(transform.localPosition.y - position) < 0.1f)
			{
				MoveItem(transform, position);
			}
			else
			{
				MoveItem(transform, Mathf.Lerp(transform.localPosition.y, position, Time.deltaTime * m_speed));
			}
		}
	}

	private void MoveItem(int index, float position)
	{
		if (index >= 0)
		{
			MoveItem(m_items[index], position);
		}
	}

	private void MoveItem(Transform item, float position)
	{
		item.localPosition = new Vector3(item.localPosition.x, position, item.localPosition.z);
	}
}
