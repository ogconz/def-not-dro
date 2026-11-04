using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(RawImage))]
	public class RawImageRenderTextureBinding : BaseBinding<RenderTextureProperty>
	{
		private RawImage m_component;

		private void Awake()
		{
			m_component = GetComponent<RawImage>();
		}

		protected override void OnUpdateValue()
		{
			if (m_component != null && base.property != null)
			{
				m_component.texture = base.property.value;
			}
		}
	}
}
