using DG.Tweening;
using UnityEngine;

namespace Binding.Components
{
	public class ScaleBoolBinding : BoolBinding
	{
		[SerializeField]
		private bool m_independentUpdate;

		[SerializeField]
		private float m_time;

		[SerializeField]
		private Ease m_trueEase;

		[SerializeField]
		private Ease m_falseEase;

		[SerializeField]
		private Vector3 m_trueScale;

		[SerializeField]
		private Vector3 m_falseScale;

		protected override void OnUpdateValue()
		{
			if (!(base.transform == null) && base.transform.gameObject.activeSelf)
			{
				Vector3 endValue = (currentValue ? m_trueScale : m_falseScale);
				Ease ease = (currentValue ? m_trueEase : m_falseEase);
				DOTween.To(() => base.transform.localScale, delegate(Vector3 x)
				{
					base.transform.localScale = x;
				}, endValue, m_time).Play().SetEase(ease)
					.SetUpdate(m_independentUpdate);
			}
		}

		private void OnEnable()
		{
			if (base.initialized)
			{
				UpdateValue();
			}
		}
	}
}
