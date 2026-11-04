using System;
using System.Collections;
using UnityEngine;

[AddComponentMenu("Scripts/Generic/Util/ScreenFadeEffect")]
public class ScreenFadeEffect : BaseLoggingMonobeh
{
	[SerializeField]
	private Color m_fadeColor = Color.black;

	[SerializeField]
	private float m_defaultFadeDuration = 1f;

	private Texture2D m_fadeTexture;

	private bool m_fadeOutState;

	private float m_alpha;

	private float m_time;

	public float defaultFadeDuration
	{
		get
		{
			return m_defaultFadeDuration;
		}
		set
		{
			m_defaultFadeDuration = value;
		}
	}

	public void StartFadeOut()
	{
		StartFadeOut(m_defaultFadeDuration);
	}

	public void StartFadeIn()
	{
		StartFadeIn(m_defaultFadeDuration);
	}

	public void StartFadeOut(Action onFinish)
	{
		StartFadeOut(m_defaultFadeDuration, onFinish);
	}

	public void StartFadeIn(Action onFinish)
	{
		StartFadeIn(m_defaultFadeDuration, onFinish);
	}

	public void StartFadeOut(float duration, Action onFinish = null)
	{
		m_fadeOutState = true;
		if (Mathf.Approximately(duration, 0f))
		{
			m_alpha = 0f;
			onFinish?.Invoke();
		}
		else
		{
			m_alpha = 1f;
			StartCoroutine(DidUpdateFade(1f / duration, onFinish));
		}
	}

	public void StartFadeIn(float duration, Action onFinish = null)
	{
		m_fadeOutState = false;
		if (Mathf.Approximately(duration, 0f))
		{
			m_alpha = 1f;
			onFinish?.Invoke();
		}
		else
		{
			m_alpha = 0f;
			StartCoroutine(DidUpdateFade(1f / duration, onFinish));
		}
	}

	private IEnumerator DidUpdateFade(float fadeSpeed, Action onFinish)
	{
		m_time = Time.realtimeSinceStartup;
		while (true)
		{
			float num = Mathf.Min(Time.realtimeSinceStartup - m_time, 0.05f);
			if (m_fadeOutState)
			{
				m_alpha -= fadeSpeed * num;
				if (m_alpha <= 0f)
				{
					m_alpha = 0f;
					break;
				}
			}
			else
			{
				m_alpha += fadeSpeed * num;
				if (m_alpha >= 1f)
				{
					m_alpha = 1f;
					break;
				}
			}
			m_time = Time.realtimeSinceStartup;
			yield return null;
		}
		onFinish?.Invoke();
	}

	private Texture2D GetFadeTexture()
	{
		if (m_fadeTexture == null)
		{
			m_fadeTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
			m_fadeTexture.SetPixels(new Color[1] { m_fadeColor });
			m_fadeTexture.Apply();
		}
		return m_fadeTexture;
	}

	protected override void AwakeHandler()
	{
		base.useGUILayout = false;
	}

	private void OnDestroy()
	{
		if ((bool)m_fadeTexture)
		{
			UnityEngine.Object.Destroy(m_fadeTexture);
		}
	}

	private void OnGUI()
	{
		if (Event.current.type == EventType.Repaint && m_alpha > 0f)
		{
			Color color = GUI.color;
			color.a = m_alpha;
			GUI.color = color;
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), GetFadeTexture(), ScaleMode.StretchToFill, alphaBlend: true);
		}
	}
}
