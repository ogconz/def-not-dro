using Binding.Base;

namespace Binding
{
	public class ColorStrStrProperty : Property<ColorStrStr>
	{
		public ColorStrStrProperty()
			: base((ColorStrStr)null)
		{
		}

		public ColorStrStrProperty(ColorStrStr startValue)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(ColorStrStr other)
		{
			ColorStrStr colorStrStr = GetValue();
			return (byte)(0u | ((colorStrStr.color != other.color) ? 1u : 0u) | ((colorStrStr.str1 != other.str1) ? 1u : 0u) | ((colorStrStr.str2 != other.str2) ? 1u : 0u)) != 0;
		}
	}
}
