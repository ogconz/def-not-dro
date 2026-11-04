using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class QuaternionProperty : Property<Quaternion>
	{
		public QuaternionProperty()
			: base(default(Quaternion))
		{
		}

		public QuaternionProperty(Quaternion startValue = default(Quaternion))
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Quaternion value)
		{
			return GetValue() != value;
		}
	}
}
