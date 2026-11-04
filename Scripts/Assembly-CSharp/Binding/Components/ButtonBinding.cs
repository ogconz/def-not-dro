using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Button))]
	public class ButtonBinding : BaseBinding<ButtonProperty>
	{
		protected void Awake()
		{
			Init();
			if (base.property != null)
			{
				base.property.value = GetComponent<Button>();
			}
		}
	}
}
