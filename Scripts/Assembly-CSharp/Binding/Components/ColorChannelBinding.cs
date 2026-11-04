using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Graphic))]
	public class ColorChannelBinding : BaseBinding<FloatProperty>
	{
		public enum ChannelType
		{
			Red = 0,
			Green = 1,
			Blue = 2,
			Alpha = 3
		}

		[SerializeField]
		private ChannelType m_channel = ChannelType.Alpha;

		private Graphic m_graphic;

		private void Awake()
		{
			m_graphic = GetComponent<Graphic>();
		}

		protected override void OnUpdateValue()
		{
			Color color = m_graphic.color;
			switch (m_channel)
			{
			case ChannelType.Red:
				color.r = base.property.value;
				break;
			case ChannelType.Green:
				color.g = base.property.value;
				break;
			case ChannelType.Blue:
				color.b = base.property.value;
				break;
			case ChannelType.Alpha:
				color.a = base.property.value;
				break;
			}
			m_graphic.color = color;
		}
	}
}
