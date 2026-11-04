using Binding.Base;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace Binding.Components
{
	[RequireComponent(typeof(Text))]
	public class TextCSSBinding : BaseBinding<ColorStrStrProperty>
	{
		private enum CSSComponents
		{
			Color = 0,
			Str1 = 1,
			Str2 = 2
		}

		private Text m_text;

		[SerializeField]
		private CSSComponents m_elementType;

		[SerializeField]
		private string m_format = "{0}";

		private void Awake()
		{
			m_text = GetComponent<Text>();
		}

		protected override void OnUpdateValue()
		{
			if (m_elementType == CSSComponents.Color)
			{
				if (base.property.value != null)
				{
					m_text.SetColorOnly(base.property.value.color);
				}
			}
			else if (m_elementType == CSSComponents.Str1)
			{
				if (base.property.value != null)
				{
					m_text.text = string.Format(m_format, base.property.value.str1);
				}
				else
				{
					m_text.text = null;
				}
			}
			else if (base.property.value != null)
			{
				m_text.text = string.Format(m_format, base.property.value.str2);
			}
			else
			{
				m_text.text = null;
			}
		}
	}
}
