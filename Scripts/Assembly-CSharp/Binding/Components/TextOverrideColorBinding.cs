using Binding.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace Binding.Components
{
	public class TextOverrideColorBinding : BaseBinding<BoolProperty>
	{
		private Text m_text;

		private TMP_Text m_tmpText;

		[SerializeField]
		private Color m_overrideColor;

		private Color m_originalColor = Color.white;

		private void Awake()
		{
			m_tmpText = GetComponent<TMP_Text>();
			if (m_tmpText == null)
			{
				m_text = GetComponent<Text>();
				if (m_text != null)
				{
					m_originalColor = m_text.color;
				}
			}
			else
			{
				m_originalColor = m_tmpText.color;
			}
		}

		protected override void OnUpdateValue()
		{
			if (m_tmpText != null)
			{
				m_tmpText.color = (base.property.value ? m_overrideColor : m_originalColor);
			}
			else if (m_text != null)
			{
				m_text.SetColorOnly(base.property.value ? m_overrideColor : m_originalColor);
			}
		}
	}
}
