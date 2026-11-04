using System.Collections;
using UnityEngine;

public class MapParticlesActivator : MonoBehaviour
{
	public float timeToDie = 10f;

	private ParticleSystem m_ps;

	private void Awake()
	{
		m_ps = GetComponent<ParticleSystem>();
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)m_ps)
		{
			StopAllCoroutines();
			if (!m_ps.isPlaying)
			{
				m_ps.Play(withChildren: true);
			}
			StartCoroutine(StopParitcles());
		}
	}

	private IEnumerator StopParitcles()
	{
		yield return new WaitForSeconds(timeToDie);
		if ((bool)m_ps)
		{
			m_ps.Stop();
		}
	}
}
