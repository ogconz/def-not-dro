using UnityEngine;

[AddComponentMenu("Scripts/FloatingSpace/FloatingAnchor")]
public class FloatingAnchor : MonoBehaviour
{
	private Transform m_cachedTransform;

	public Transform cachedTransform
	{
		get
		{
			if (m_cachedTransform == null)
			{
				m_cachedTransform = base.transform;
			}
			return m_cachedTransform;
		}
	}
}
