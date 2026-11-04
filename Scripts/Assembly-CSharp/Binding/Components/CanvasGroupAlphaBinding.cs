using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	[RequireComponent(typeof(CanvasGroup))]
	public class CanvasGroupAlphaBinding : BaseBinding<FloatProperty>
	{
		private CanvasGroup m_canvasGroup;

		private void Awake()
		{
			m_canvasGroup = GetComponent<CanvasGroup>();
		}

		protected override void OnUpdateValue()
		{
			m_canvasGroup.alpha = base.property.value;
		}
	}
}
