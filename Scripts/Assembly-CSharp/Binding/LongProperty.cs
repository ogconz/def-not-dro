using Binding.Base;

namespace Binding
{
	public class LongProperty : Property<long>
	{
		public LongProperty()
			: base(0L)
		{
		}

		public LongProperty(int startValue = 0)
			: base((long)startValue)
		{
		}

		protected override bool IsValueDifferent(long value)
		{
			return GetValue() != value;
		}
	}
}
