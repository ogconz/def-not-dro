using Binding.Base;
using UnityEngine.UI;

namespace Binding
{
	public class ScrollRectProperty : Property<ScrollRect>
	{
		public ScrollRectProperty()
			: base((ScrollRect)null)
		{
		}

		public ScrollRectProperty(ScrollRect startValue = null)
			: base(startValue)
		{
		}
	}
}
