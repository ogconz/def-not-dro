using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	[RequireComponent(typeof(ActionElementBinding))]
	public class ActionElementProvider : BaseBinding<ActionElementBindingProperty>
	{
		private ActionElementBinding m_actionElementBinding;

		private void Awake()
		{
			m_actionElementBinding = GetComponent<ActionElementBinding>();
		}

		private void OnEnable()
		{
			OnForceUpdate();
		}

		private void OnDisable()
		{
			OnForceUpdate();
		}

		protected override void OnUpdateValue()
		{
			if (m_actionElementBinding.isActiveAndEnabled && base.property.value != m_actionElementBinding)
			{
				base.property.value = m_actionElementBinding;
			}
			if (!m_actionElementBinding.isActiveAndEnabled && base.property.value == m_actionElementBinding)
			{
				base.property.value = null;
			}
		}
	}
}
