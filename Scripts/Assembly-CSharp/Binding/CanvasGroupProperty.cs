using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class CanvasGroupProperty : Property<CanvasGroup>
	{
		public CanvasGroupProperty()
			: base((CanvasGroup)null)
		{
		}

		public CanvasGroupProperty(CanvasGroup startValue = null)
			: base(startValue)
		{
		}
	}
}
