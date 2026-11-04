using UnityEngine;

public class LoadingProgressBar : MonoBehaviour
{
	[SerializeField]
	private float m_minProgressStep = 0.01f;

	private float m_curValue;

	public float progress
	{
		get
		{
			return m_curValue;
		}
		set
		{
			UpdateProgressStep(value);
		}
	}

	private void UpdateProgressStep(float v)
	{
		float num = v - m_curValue;
		if (num < m_minProgressStep && m_curValue + m_minProgressStep < 1f)
		{
			num = m_minProgressStep;
		}
		m_curValue += num;
		OnChangeProgress();
	}

	public virtual void Show()
	{
		m_curValue = 0f;
		OnChangeProgress();
	}

	protected virtual void OnChangeProgress()
	{
		GameLog.Print(AlertLevel.Notify, "Debug", "Loading progress: " + progress.ToString("P0"));
	}

	public virtual void Done()
	{
	}
}
