using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class LocalizationBinding : MonoBehaviour
	{
		public string key;

		[Tooltip("If empty, formating skipped, default {0}")]
		[SerializeField]
		private string m_format = string.Empty;

		[SerializeField]
		private bool m_forceUpperCase = true;

		[SerializeField]
		private string m_param = string.Empty;

		private Text m_label;

		private TMP_Text m_tmpLabel;

		private void LocalizeText()
		{
			string text = ((!string.IsNullOrEmpty(m_param)) ? LocalizationService.LocalizeFormat(key, m_param) : LocalizationService.Localize(key));
			if (!string.IsNullOrEmpty(m_format) && m_format != "{0}")
			{
				text = string.Format(m_format, text);
			}
			if (m_forceUpperCase)
			{
				text = text.ToUpper();
			}
			SetLabelText(text);
		}

		private void Awake()
		{
			m_label = GetComponent<Text>();
			m_tmpLabel = GetComponent<TMP_Text>();
			SetLabelText(string.Empty);
			LocalizationService.onLanguageChanged = (Action)Delegate.Combine(LocalizationService.onLanguageChanged, new Action(OnLocalizationChanged));
		}

		private void Start()
		{
			LocalizeText();
		}

		private void SetLabelText(string labelText)
		{
			if ((bool)m_tmpLabel)
			{
				m_tmpLabel.text = labelText;
			}
			if ((bool)m_label)
			{
				m_label.text = labelText;
			}
		}

		private void OnDestroy()
		{
			LocalizationService.onLanguageChanged = (Action)Delegate.Remove(LocalizationService.onLanguageChanged, new Action(OnLocalizationChanged));
		}

		private void OnLocalizationChanged()
		{
			LocalizeText();
		}
	}
}
