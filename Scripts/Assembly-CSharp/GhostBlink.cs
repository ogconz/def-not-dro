using UnityEngine;

public class GhostBlink : MonoBehaviour
{
	private MeshRenderer[] m_renderers;

	private void Start()
	{
		m_renderers = GetComponentsInChildren<MeshRenderer>();
	}

	private void Update()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
		_ = other.gameObject.layer;
		LayerMask.NameToLayer("Car");
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Car"))
		{
			MeshRenderer[] renderers = m_renderers;
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].enabled = true;
			}
		}
	}
}
