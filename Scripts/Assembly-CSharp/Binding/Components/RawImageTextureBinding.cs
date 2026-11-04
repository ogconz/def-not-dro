using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(RawImage))]
	public class RawImageTextureBinding : BaseBinding<TextureProperty>
	{
		[SerializeField]
		private bool m_clearOfImageNull;

		private RawImage m_component;

		private Color m_color;

		private void Awake()
		{
			m_component = GetComponent<RawImage>();
			UpdateColor();
		}

		protected override void OnUpdateValue()
		{
			m_component.texture = base.property.value;
			UpdateColor();
		}

		private void UpdateColor()
		{
			if (m_clearOfImageNull && !(m_component == null))
			{
				if (m_component.texture == null && m_component.color != Color.clear)
				{
					m_color = m_component.color;
					m_component.color = Color.clear;
				}
				else if (m_component.color == Color.clear && m_component.texture != null)
				{
					m_component.color = m_color;
				}
			}
		}
	}
}
