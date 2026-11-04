using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class ColorProperty : Property<Color>
	{
		public ColorProperty()
			: base(default(Color))
		{
		}

		public ColorProperty(Color startValue = default(Color))
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Color data)
		{
			return GetValue() != data;
		}
	}
}
