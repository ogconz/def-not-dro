using Binding.Base;
using TMPro;
using UnityEngine;

namespace Binding.Components
{
	public class TMProTextBinding : BaseBinding<Property>
	{
		private TMP_Text m_text;

		private TMP_InputField m_inputField;

		[SerializeField]
		[Tooltip("If empty, formating skipped, default {0}")]
		private string m_format = string.Empty;

		private void Awake()
		{
			m_text = GetComponent<TMP_Text>();
			if (m_text == null)
			{
				m_inputField = GetComponent<TMP_InputField>();
			}
			else
			{
				m_text.raycastTarget = false;
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
			SetText(text);
		}

		private void SetText(string newText)
		{
			if (m_text != null)
			{
				m_text.text = newText;
			}
			else if (m_inputField != null)
			{
				m_inputField.text = newText;
			}
		}
	}
}
