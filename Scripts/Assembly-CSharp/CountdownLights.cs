using UnityEngine;

public class CountdownLights : MonoBehaviour
{
	private const float MIN_COOLDOWN = 5f;

	[SerializeField]
	private GameObject[] m_prepareSections;

	[SerializeField]
	private GameObject m_readySection;

	[SerializeField]
	[Range(0f, 30f)]
	internal float cooldown;

	internal float time;

	internal float endTime = float.MinValue;

	internal float readyDelay;

	public void Update()
	{
		UpdateSections();
		UpdateCooldown();
	}

	public void SetTime(float duration, float delay)
	{
		endTime = time + duration;
		readyDelay = delay;
	}

	private void UpdateCooldown()
	{
		if (!(cooldown < 5f) && time > endTime + 5f)
		{
			SetTime(cooldown, Random.Range(1f, 3f));
		}
	}

	private void UpdateSections()
	{
		float num = endTime - readyDelay - (float)m_prepareSections.Length;
		bool flag = time > endTime;
		for (int i = 0; i < m_prepareSections.Length; i++)
		{
			bool flag2 = time > num + (float)(i + 1);
			m_prepareSections[i].SetActive(flag2 && !flag);
		}
		m_readySection.SetActive(flag);
	}
}
