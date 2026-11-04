using UnityEngine;

[AddComponentMenu("Scripts/Effects/GhostVisibility")]
[RequireComponent(typeof(RaceCar))]
public class GhostVisibility : MonoBehaviour
{
	[SerializeField]
	private Color m_color;

	[SerializeField]
	private float m_maxVisibility = 0.5f;

	private RaceCar m_target;

	private RaceCar m_car;

	private float m_maxSqrDistance;

	private float m_invMaxSqrDistance;

	private float m_prevVisibility;

	private int m_colorPropertyID;

	private Renderer[] m_renderers;

	private MaterialPropertyBlock m_propertyBlock = new MaterialPropertyBlock();

	public void SetTarget(RaceCar target)
	{
		m_target = target;
		if ((bool)m_target)
		{
			float num = (Mathf.Max(m_target.lengthFront, m_target.lengthRear) + Mathf.Max(m_car.lengthFront, m_car.lengthRear)) * 1.1f;
			m_maxSqrDistance = num * num;
			m_invMaxSqrDistance = 1f / m_maxSqrDistance;
		}
	}

	private void Awake()
	{
		m_car = GetComponent<RaceCar>();
		m_renderers = GetComponentsInChildren<Renderer>();
		m_colorPropertyID = Shader.PropertyToID("_Color");
	}

	private void Update()
	{
		float visibility = GetVisibility();
		if (visibility != m_prevVisibility)
		{
			m_prevVisibility = visibility;
			Color color = m_color;
			color.a = visibility;
			m_propertyBlock.Clear();
			m_propertyBlock.SetColor(m_colorPropertyID, color);
			for (int i = 0; i < m_renderers.Length; i++)
			{
				m_renderers[i].SetPropertyBlock(m_propertyBlock);
			}
		}
	}

	private float GetVisibility()
	{
		float num = Vector3.SqrMagnitude(m_target.getTransform.position - m_car.getTransform.position);
		if (num < m_maxSqrDistance)
		{
			float num2 = num * m_invMaxSqrDistance;
			return Mathf.Lerp(0f, m_maxVisibility, num2 * num2);
		}
		return m_maxVisibility;
	}
}
