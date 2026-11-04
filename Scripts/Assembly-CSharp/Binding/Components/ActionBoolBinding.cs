using UnityEngine;
using UnityEngine.Events;

namespace Binding.Components
{
	public class ActionBoolBinding : BoolBinding
	{
		[SerializeField]
		private UnityEvent OnAction;

		protected override void OnUpdateValue()
		{
			if (currentValue && OnAction != null)
			{
				OnAction.Invoke();
			}
		}
	}
}
