using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public abstract class FloatingObject : MonoBehaviour
{
	private LinkedListNode<FloatingObject> m_node;

	public virtual void OnChangeWorldOrigin(Vector3 originOffset)
	{
	}

	protected virtual void OnEnable()
	{
		if (m_node == null)
		{
			m_node = new LinkedListNode<FloatingObject>(this);
		}
		FloatingOrigin.Register(m_node);
	}

	protected virtual void OnDisable()
	{
		if (m_node != null)
		{
			FloatingOrigin.Unregister(m_node);
		}
	}
}
