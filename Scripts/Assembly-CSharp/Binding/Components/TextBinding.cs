using Binding.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class TextBinding : BaseBinding<Property>
	{
		private Text m_text;

		private InputField m_inputField;

		private TMP_Text m_textTMPro;

		private TMP_InputField m_inputFieldTMPro;

		[SerializeField]
		[Tooltip("If empty, formating skipped, default {0}")]
		private string m_format = string.Empty;

		[SerializeField]
		private bool m_forceUpperCase;

		private void Awake()
		{
			m_text = GetComponent<Text>();
			if (m_text == null)
			{
				m_textTMPro = GetComponent<TMP_Text>();
			}
			if (m_textTMPro == null && m_text == null)
			{
				m_inputField = GetComponent<InputField>();
			}
			if (m_textTMPro == null && m_text == null && m_inputField == null)
			{
				m_inputFieldTMPro = GetComponent<TMP_InputField>();
			}
			SetText(string.Empty);
		}

		protected override void OnUpdateValue()
		{
			string text = string.Empty;
			object objectValue = base.property.GetObjectValue();
			if (!string.IsNullOrEmpty(m_format) && m_format != "{0}")
			{
				text = string.Format(m_format, objectValue);
			}
			else if (objectValue != null)
			{
				text = objectValue.ToString();
			}
			if (m_forceUpperCase)
			{
				text = text.ToUpper();
			}
			SetText(text);
		}

		private void SetText(string newText)
		{
			if (m_text != null)
			{
				m_text.text = newText;
			}
			else if (m_textTMPro != null)
			{
				m_textTMPro.text = newText;
			}
			else if (m_inputField != null)
			{
				m_inputField.text = newText;
			}
			else if (m_inputFieldTMPro != null)
			{
				m_inputFieldTMPro.text = newText;
			}
		}
	}
}
