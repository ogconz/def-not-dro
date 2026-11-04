using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	public class CanvasGroupBinding : BaseBinding<CanvasGroupProperty>
	{
		protected void Awake()
		{
			Init();
			if (base.property != null)
			{
				base.property.value = GetComponent<CanvasGroup>();
			}
		}

		protected override void Start()
		{
			base.Start();
			if (base.property != null)
			{
				base.property.value = GetComponent<CanvasGroup>();
			}
		}
	}
}
