using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Scripts/FloatingSpace/FloatingOrigin")]
public class FloatingOrigin : MonoBehaviour
{
	[SerializeField]
	private float m_threshold = 100f;

	[SerializeField]
	private FloatingAnchor m_anchor;

	private LinkedList<FloatingObject> m_floatingObjects;

	private static FloatingOrigin m_instance;

	public FloatingAnchor anchor
	{
		get
		{
			return m_anchor;
		}
		set
		{
			m_anchor = value;
		}
	}

	private static FloatingOrigin instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = Object.FindObjectOfType(typeof(FloatingOrigin)) as FloatingOrigin;
				if ((bool)m_instance)
				{
					m_instance.Init();
				}
			}
			return m_instance;
		}
	}

	public static void Register(LinkedListNode<FloatingObject> node)
	{
		if ((bool)instance)
		{
			instance.m_floatingObjects.AddLast(node);
		}
	}

	public static void Unregister(LinkedListNode<FloatingObject> node)
	{
		if ((bool)instance)
		{
			instance.m_floatingObjects.Remove(node);
		}
	}

	private void Init()
	{
		m_anchor = Object.FindObjectOfType(typeof(FloatingAnchor)) as FloatingAnchor;
		m_floatingObjects = new LinkedList<FloatingObject>();
	}

	private void LateUpdate()
	{
		if (!m_anchor)
		{
			return;
		}
		Vector3 position = m_anchor.cachedTransform.position;
		position.y = 0f;
		if (position.sqrMagnitude > m_threshold * m_threshold)
		{
			foreach (FloatingObject floatingObject in m_floatingObjects)
			{
				floatingObject.OnChangeWorldOrigin(position);
			}
		}
		m_anchor.cachedTransform.position -= position;
	}
}
