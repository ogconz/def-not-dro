using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	public class RectSizeBinding : BaseBinding<Vector2Property>
	{
		private RectTransform m_rectTransform;

		private bool m_inited;

		private void Awake()
		{
			m_rectTransform = base.transform as RectTransform;
			m_inited = false;
		}

		protected override void OnUpdateValue()
		{
			Vector2 value = base.property.value;
			if (m_inited)
			{
				if (value != m_rectTransform.sizeDelta)
				{
					m_rectTransform.sizeDelta = value;
				}
				return;
			}
			if (base.property.value == default(Vector2))
			{
				base.property.value = m_rectTransform.sizeDelta;
			}
			else if (value != m_rectTransform.sizeDelta)
			{
				m_rectTransform.sizeDelta = value;
			}
			m_inited = true;
		}
	}
}
