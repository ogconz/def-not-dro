using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(ScrollRect))]
	public class ScrollRectBinding : BaseBinding<ScrollRectProperty>
	{
		protected void Awake()
		{
			Init();
			if (base.property != null)
			{
				base.property.value = GetComponent<ScrollRect>();
			}
		}
	}
}
