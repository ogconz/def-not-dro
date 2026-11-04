using System;
using Binding.Base;
using Binding.Components;

namespace Binding
{
	public class ActionElementBindingProperty : Property<ActionElementBinding>
	{
		private Action m_actionForBind;

		public ActionElementBindingProperty()
			: base((ActionElementBinding)null)
		{
		}

		public ActionElementBindingProperty(ActionElementBinding startValue = null)
			: base(startValue)
		{
		}

		public void BindAction(Action action)
		{
			m_actionForBind = action;
			base.OnValueChanged += DelayedSetupAction;
		}

		public void ReleaseAction()
		{
			base.OnValueChanged -= DelayedSetupAction;
			m_actionForBind = null;
		}

		private void DelayedSetupAction()
		{
			if (base.value != null && m_actionForBind != null)
			{
				base.value.SetData(base.value.hintText, base.value.actionType, m_actionForBind, base.value.isRepeatAction);
			}
		}
	}
}
