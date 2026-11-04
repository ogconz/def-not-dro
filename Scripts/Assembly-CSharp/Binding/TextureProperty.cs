using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class TextureProperty : Property<Texture>
	{
		public TextureProperty()
			: base((Texture)null)
		{
		}

		public TextureProperty(Texture startValue = null)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Texture data)
		{
			return GetValue() != data;
		}
	}
}
