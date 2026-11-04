using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class RenderTextureProperty : Property<RenderTexture>
	{
		public RenderTextureProperty()
			: base((RenderTexture)null)
		{
		}

		public RenderTextureProperty(RenderTexture startValue = null)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(RenderTexture data)
		{
			return GetValue() != data;
		}
	}
}
