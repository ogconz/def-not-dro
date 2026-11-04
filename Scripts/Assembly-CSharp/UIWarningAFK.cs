using TMPro;
using UnityEngine;

public class UIWarningAFK : MonoBehaviour
{
	private readonly PhaseTimeController m_alphaController = new PhaseTimeController(PhaseControllerTimerMode.UnscaledTime, PhaseControllerWorkMode.Once, 0.3f, suspended: true);

	private static UIWarningAFK m_instance;

	[SerializeField]
	private CanvasGroup m_group;

	[SerializeField]
	private TextMeshProUGUI m_label;

	private void Awake()
	{
		m_instance = this;
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (m_alphaController.IsActive)
		{
			float laxitySlideFValue = m_alphaController.getLaxitySlideFValue(1f);
			m_group.alpha = laxitySlideFValue;
		}
	}

	public static void ShowText(string text)
	{
		text = UIUtils.ConvertNGUIColorToRich(text);
		m_instance.m_label.text = text;
		if (m_instance.m_alphaController.IsReversed)
		{
			ResetAndPlayAlphaController();
		}
		if (!m_instance.gameObject.activeSelf)
		{
			ResetAndPlayAlphaController();
			m_instance.gameObject.SetActive(value: true);
		}
	}

	public static void HideText()
	{
		m_instance.gameObject.SetActive(value: false);
	}

	private static void ResetAndPlayAlphaController()
	{
		m_instance.m_alphaController.IsReversed = false;
		m_instance.m_alphaController.Reset();
		m_instance.m_alphaController.Play();
	}
}
