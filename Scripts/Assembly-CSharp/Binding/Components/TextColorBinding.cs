using Binding.Base;
using TMPro;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace Binding.Components
{
	public class TextColorBinding : BaseBinding<ColorProperty>
	{
		private Text m_text;

		private TMP_Text m_tmpText;

		private void Awake()
		{
			m_tmpText = GetComponent<TMP_Text>();
			if (m_tmpText == null)
			{
				m_text = GetComponent<Text>();
			}
		}

		protected override void OnUpdateValue()
		{
			if (m_tmpText != null)
			{
				m_tmpText.color = base.property.value;
			}
			else if (m_text != null)
			{
				m_text.SetColorOnly(base.property.value);
			}
		}
	}
}
