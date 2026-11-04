using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	public class ScaleBinding : BaseBinding<Vector2Property>
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
				if (value.x != m_rectTransform.localScale.x && value.y != m_rectTransform.localScale.y)
				{
					m_rectTransform.localScale = new Vector3(value.x, value.y, m_rectTransform.localScale.z);
				}
				return;
			}
			if (base.property.value == default(Vector2))
			{
				base.property.value = m_rectTransform.localScale;
			}
			else if (value.x != m_rectTransform.localScale.x && value.y != m_rectTransform.localScale.y)
			{
				m_rectTransform.localScale = new Vector3(value.x, value.y, m_rectTransform.localScale.z);
			}
			m_inited = true;
		}
	}
}
