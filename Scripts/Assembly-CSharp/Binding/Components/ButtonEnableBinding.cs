using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Button))]
	public class ButtonEnableBinding : BoolBinding
	{
		private Button m_button;

		private void Awake()
		{
			m_button = GetComponent<Button>();
		}

		protected override void OnUpdateValue()
		{
			if ((bool)m_button)
			{
				m_button.enabled = currentValue;
			}
		}
	}
}
