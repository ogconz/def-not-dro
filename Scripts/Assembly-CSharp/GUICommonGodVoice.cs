using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUICommonGodVoice : MonoBehaviour
{
	public Image iconImage;

	private static GUICommonGodVoice m_Instance = null;

	private static CanvasGroup m_group = null;

	private static TextMeshProUGUI m_label = null;

	private static float m_HideTime = 0f;

	private static PhaseTimeController m_AlphaController = new PhaseTimeController(PhaseControllerTimerMode.UnscaledTime, PhaseControllerWorkMode.Once, 0.3f, suspended: true);

	public static int CurrentLabelHash => m_label.GetHashCode();

	private void Awake()
	{
		if (m_Instance == null)
		{
			m_Instance = this;
			m_group = GetComponent<CanvasGroup>();
			m_label = GetComponentInChildren<TextMeshProUGUI>();
		}
		base.gameObject.SetActive(value: false);
	}

	public static void ShowText(string text, float time = 10f, Sprite iconSprite = null, bool silence = false)
	{
		text = UIUtils.ConvertNGUIColorToRich(text);
		if (!silence)
		{
			GameLog.Print(AlertLevel.Notify, "System", "GodVoice: " + text);
		}
		m_Instance.iconImage.gameObject.SetActive(iconSprite != null);
		if (iconSprite != null)
		{
			m_Instance.iconImage.sprite = iconSprite;
		}
		m_label.text = text;
		m_HideTime = Time.unscaledTime + time;
		if (m_AlphaController.IsReversed)
		{
			m_AlphaController.IsReversed = false;
			m_AlphaController.Reset();
			m_AlphaController.Play();
		}
		if (!m_Instance.gameObject.activeSelf)
		{
			m_AlphaController.IsReversed = false;
			m_AlphaController.Reset();
			m_AlphaController.Play();
			m_Instance.gameObject.SetActive(value: true);
		}
	}

	public static void HideText()
	{
		m_Instance.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (m_AlphaController.IsActive)
		{
			float laxitySlideFValue = m_AlphaController.getLaxitySlideFValue(1f);
			m_group.alpha = laxitySlideFValue;
		}
		else if (!m_AlphaController.IsReversed && Time.unscaledTime > m_HideTime - m_AlphaController.Length - 0.1f)
		{
			m_AlphaController.IsReversed = true;
			m_AlphaController.Reset();
			m_AlphaController.Play();
		}
		if (Time.unscaledTime > m_HideTime)
		{
			m_AlphaController.Reset();
			base.gameObject.SetActive(value: false);
		}
	}

	public static void HideTextWithHash(int labelHash)
	{
		if (m_Instance.gameObject.activeInHierarchy && CurrentLabelHash == labelHash)
		{
			HideText();
		}
	}
}
