using UnityEngine;

public class ShakeObjFx : MonoBehaviour
{
	private Vector3 m_offset = new Vector3(0f, 0f, 0.06f);

	private float m_force = 4f;

	private float m_startDelay = 1f;

	private Vector3 m_startPosition;

	private float m_timer;

	private void Start()
	{
		m_startDelay = Random.value;
		m_startPosition = base.transform.localPosition;
	}

	private void Update()
	{
		if ((m_timer += Time.deltaTime) > m_startDelay)
		{
			base.transform.localPosition = m_startPosition + m_offset * Mathf.Sin(Time.time * m_force);
		}
	}
}
