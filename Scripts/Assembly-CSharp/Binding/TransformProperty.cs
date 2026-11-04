using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class TransformProperty : Property<Transform>
	{
		public TransformProperty()
			: base((Transform)null)
		{
		}

		public TransformProperty(Transform startValue = null)
			: base(startValue)
		{
		}
	}
}
