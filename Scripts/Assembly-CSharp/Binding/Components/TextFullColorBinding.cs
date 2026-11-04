using Binding.Base;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace Binding.Components
{
	[RequireComponent(typeof(Text))]
	public class TextFullColorBinding : BaseBinding<ColorProperty>
	{
		private Text m_text;

		private void Awake()
		{
			m_text = GetComponent<Text>();
		}

		protected override void OnUpdateValue()
		{
			float a = base.property.value.a;
			m_text.SetColorOnly(base.property.value);
			m_text.SetAlpha(a);
		}
	}
}
