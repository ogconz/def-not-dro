using Binding.Base;
using UnityEngine.UI;

namespace Binding
{
	public class ButtonProperty : Property<Button>
	{
		public ButtonProperty()
			: base((Button)null)
		{
		}

		public ButtonProperty(Button startValue = null)
			: base(startValue)
		{
		}
	}
}
