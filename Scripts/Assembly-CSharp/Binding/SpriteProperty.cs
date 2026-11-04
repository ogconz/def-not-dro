using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class SpriteProperty : Property<Sprite>
	{
		public SpriteProperty()
			: base((Sprite)null)
		{
		}

		public SpriteProperty(Sprite startValue = null)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Sprite data)
		{
			return GetValue() != data;
		}
	}
}
