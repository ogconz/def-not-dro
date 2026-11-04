using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Toggle))]
	public class ToggleBinding : BoolBinding
	{
		[SerializeField]
		private bool m_changePropertyValue = true;

		private Toggle m_toogle;

		private void Awake()
		{
			m_toogle = GetComponent<Toggle>();
			m_toogle.onValueChanged.AddListener(ToggleValueChangeHandler);
		}

		protected override void OnUpdateValue()
		{
			m_toogle.isOn = currentValue;
		}

		private void ToggleValueChangeHandler(bool value)
		{
			if (m_changePropertyValue)
			{
				currentValue = value;
			}
		}
	}
}
