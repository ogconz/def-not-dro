using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class MaterialProperty : Property<Material>
	{
		public MaterialProperty()
			: base((Material)null)
		{
		}

		public MaterialProperty(Material startValue = null)
			: base(startValue)
		{
		}

		protected override bool IsValueDifferent(Material data)
		{
			return GetValue() != data;
		}
	}
}
