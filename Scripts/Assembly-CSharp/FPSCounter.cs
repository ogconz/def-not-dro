using System.Collections;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
	[SerializeField]
	private float m_frequency = 0.5f;

	private WaitForSeconds m_waitForFrequency;

	[SerializeField]
	private TextMeshProUGUI m_text;

	public int framesPerSec { get; protected set; }

	private void Start()
	{
		GameConsole.SetCmd("fps", delegate
		{
			SetVisible(!base.gameObject.activeSelf);
		});
		SetVisible(visible: false);
	}

	private void OnEnable()
	{
		if (m_text == null)
		{
			m_text = GetComponentInChildren<TextMeshProUGUI>();
		}
		m_waitForFrequency = new WaitForSeconds(m_frequency);
		StartCoroutine(FPS());
	}

	private void SetVisible(bool visible)
	{
		base.gameObject.SetActive(visible);
	}

	private IEnumerator FPS()
	{
		while (true)
		{
			int lastFrameCount = Time.frameCount;
			float lastTime = Time.realtimeSinceStartup;
			yield return m_waitForFrequency;
			float num = Time.realtimeSinceStartup - lastTime;
			int num2 = Time.frameCount - lastFrameCount;
			framesPerSec = Mathf.RoundToInt((float)num2 / num);
			if ((bool)m_text)
			{
				m_text.text = UIUtils.NumToTwoSignString(framesPerSec);
			}
		}
	}
}
