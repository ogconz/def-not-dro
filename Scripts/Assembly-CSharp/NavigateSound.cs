using UnityEngine;

public class NavigateSound : MonoBehaviour
{
	private float m_lastPlayTime;

	public SoundSample sample = SoundSample.HoverClip;

	[Range(0f, 1f)]
	public float volume = 1f;

	public void PlayHover()
	{
		float unscaledTime = Time.unscaledTime;
		float num = unscaledTime - m_lastPlayTime;
		if (!(num < 0.2f) || num == 0f)
		{
			m_lastPlayTime = unscaledTime;
			SoundController.Play2DUISound(sample, 0f, volume);
		}
	}
}
