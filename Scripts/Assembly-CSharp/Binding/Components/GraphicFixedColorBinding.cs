using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Graphic))]
	public class GraphicFixedColorBinding : BoolBinding
	{
		private Graphic m_graphic;

		[SerializeField]
		private Color m_colorOnTrue = Color.white;

		[SerializeField]
		private Color m_colorOnFalse = Color.white;

		private void Awake()
		{
			m_graphic = GetComponent<Graphic>();
		}

		protected override void OnUpdateValue()
		{
			m_graphic.color = (currentValue ? m_colorOnTrue : m_colorOnFalse);
		}
	}
}
