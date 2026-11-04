using Binding.Base;

namespace Binding
{
	public class IntProperty : Property<int>
	{
		public IntProperty()
			: base(0)
		{
		}

		public IntProperty(int startValue = 0)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(int value)
		{
			return GetValue() != value;
		}
	}
}
