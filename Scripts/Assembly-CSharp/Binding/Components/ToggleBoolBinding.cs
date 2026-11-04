using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Toggle))]
	public class ToggleBoolBinding : BaseBinding<BoolProperty>
	{
		[SerializeField]
		private bool m_changePropertyValue = true;

		[SerializeField]
		private bool m_invert;

		private Toggle m_toogle;

		private void Awake()
		{
			m_toogle = GetComponent<Toggle>();
			m_toogle.onValueChanged.AddListener(ToggleValueChangeHandler);
		}

		protected override void OnUpdateValue()
		{
			m_toogle.isOn = (m_invert ? (!base.property.value) : base.property.value);
		}

		private void ToggleValueChangeHandler(bool value)
		{
			if (m_changePropertyValue)
			{
				base.property.value = value;
			}
		}
	}
}
