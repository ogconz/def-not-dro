using UnityEngine;
using UnityEngine.UI;

public class UISlideShow : MonoBehaviour
{
	public Sprite[] slides;

	public Image screen0;

	public Image screen1;

	private int m_index;

	private bool m_firstScreen = true;

	public int index => m_index;

	private void Start()
	{
		m_index = 0;
		m_firstScreen = true;
		screen0.sprite = slides[m_index];
		screen0.color = Color.white;
		screen0.gameObject.SetActive(value: true);
		screen1.color = Color.white;
		screen1.CrossFadeAlpha(0f, 0.1f, ignoreTimeScale: true);
		screen1.gameObject.SetActive(value: true);
	}

	public void ChangeSlide()
	{
		if (slides.Length >= 2 && !(screen0 == null) && !(screen1 == null))
		{
			m_index++;
			if (m_index >= slides.Length)
			{
				m_index = 0;
			}
			if (m_firstScreen)
			{
				m_firstScreen = !m_firstScreen;
				screen1.sprite = slides[m_index];
				screen0.CrossFadeAlpha(0f, 10f, ignoreTimeScale: true);
				screen1.CrossFadeAlpha(1f, 0.1f, ignoreTimeScale: true);
			}
			else
			{
				screen0.sprite = slides[m_index];
				screen0.CrossFadeAlpha(1f, 0.1f, ignoreTimeScale: true);
				screen1.CrossFadeAlpha(0f, 10f, ignoreTimeScale: true);
			}
		}
	}
}
