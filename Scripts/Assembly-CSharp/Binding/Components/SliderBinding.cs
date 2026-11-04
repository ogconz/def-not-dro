using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Slider))]
	public class SliderBinding : BaseBinding<FloatProperty>
	{
		private Slider m_slider;

		private void Awake()
		{
			m_slider = GetComponent<Slider>();
			m_slider.onValueChanged.AddListener(SliderValueChangeHandler);
		}

		protected override void OnUpdateValue()
		{
			if (base.property != null)
			{
				m_slider.value = base.property.value;
			}
		}

		private void SliderValueChangeHandler(float value)
		{
			if (base.property != null)
			{
				base.property.value = value;
			}
		}
	}
}
