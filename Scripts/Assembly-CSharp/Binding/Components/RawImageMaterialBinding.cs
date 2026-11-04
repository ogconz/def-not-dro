using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(RawImage))]
	public class RawImageMaterialBinding : BaseBinding<MaterialProperty>
	{
		private RawImage m_component;

		private void Awake()
		{
			m_component = GetComponent<RawImage>();
		}

		protected override void OnUpdateValue()
		{
			m_component.material = base.property.value;
		}
	}
}
