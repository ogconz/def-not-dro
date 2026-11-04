using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class Vector2Property : Property<Vector2>
	{
		public Vector2Property()
			: base(default(Vector2))
		{
		}

		public Vector2Property(Vector2 startValue = default(Vector2))
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Vector2 value)
		{
			Vector2 vector = GetValue();
			if (vector.x == value.x)
			{
				return vector.y != value.y;
			}
			return true;
		}
	}
}
