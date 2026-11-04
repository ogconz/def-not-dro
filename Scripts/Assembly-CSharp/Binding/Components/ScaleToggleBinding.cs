using DG.Tweening;
using UnityEngine;

namespace Binding.Components
{
	public class ScaleToggleBinding : BoolBinding
	{
		public float trueScale = 1f;

		public float falseScale = 1f;

		public bool useTween;

		private bool m_firstUpdate = true;

		protected override void OnUpdateValue()
		{
			base.OnUpdateValue();
			if (useTween && !m_firstUpdate)
			{
				base.transform.DOScale(currentValue ? trueScale : falseScale, 0.2f);
			}
			else
			{
				base.transform.localScale = Vector3.one * (currentValue ? trueScale : falseScale);
			}
			m_firstUpdate = false;
		}
	}
}
