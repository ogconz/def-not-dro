using Binding.Base;

namespace Binding
{
	public class StringProperty : Property<string>
	{
		public StringProperty()
			: base((string)null)
		{
		}

		public StringProperty(string startValue = "")
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(string value)
		{
			return GetValue() != value;
		}
	}
}
