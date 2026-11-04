using Binding.Base;

namespace Binding
{
	public class BoolProperty : Property<bool>
	{
		public BoolProperty()
			: base(false)
		{
		}

		public BoolProperty(bool startValue = false)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(bool value)
		{
			return GetValue() != value;
		}
	}
}
