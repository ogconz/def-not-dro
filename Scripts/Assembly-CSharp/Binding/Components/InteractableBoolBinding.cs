using UnityEngine.UI;

namespace Binding.Components
{
	public class InteractableBoolBinding : BoolBinding
	{
		private Selectable m_component;

		private void Awake()
		{
			m_component = GetComponent<Selectable>();
		}

		protected override void OnUpdateValue()
		{
			m_component.interactable = currentValue;
		}
	}
}
