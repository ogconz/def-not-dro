using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class FloatProperty : Property<float>
	{
		public FloatProperty()
			: base(0f)
		{
		}

		public FloatProperty(float startValue = 0f)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(float value)
		{
			return Mathf.Abs(GetValue() - value) > float.Epsilon;
		}
	}
}
