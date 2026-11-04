using System;
using Binding.Base;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI._UGUI_.Lists
{
	public class SubquestConditionListElement : BaseBindingBehaviourTarget
	{
		[SerializeField]
		private float m_fadeAnimationDuration;

		[SerializeField]
		private TextMeshProUGUI m_typeText;

		[SerializeField]
		private TextMeshProUGUI m_valueText;

		protected override void OnDeactivate()
		{
			StopAnimations();
			base.OnDeactivate();
		}

		public void ShowView()
		{
			base.gameObject.SetActive(value: true);
		}

		public void HideView()
		{
			StopAnimations();
			ResetColorsToDefault();
			base.gameObject.SetActive(value: false);
		}

		public void SetConditionText(string type, string value, Action callback = null)
		{
			m_typeText.DOFade(0f, m_fadeAnimationDuration).SetUpdate(isIndependentUpdate: true).OnComplete(delegate
			{
				m_typeText.DOFade(1f, m_fadeAnimationDuration).SetUpdate(isIndependentUpdate: true).OnComplete(callback.SafeCall);
				m_typeText.SetText(type);
			});
			m_valueText.DOFade(0f, m_fadeAnimationDuration).SetUpdate(isIndependentUpdate: true).OnComplete(delegate
			{
				m_valueText.DOFade(1f, m_fadeAnimationDuration).SetUpdate(isIndependentUpdate: true).OnComplete(callback.SafeCall);
				m_valueText.SetText(value);
			});
		}

		public void ResetColorsToDefault()
		{
			m_typeText.DOFade(0f, 0f).SetUpdate(isIndependentUpdate: true);
			m_valueText.DOFade(0f, 0f).SetUpdate(isIndependentUpdate: true);
		}

		private void StopAnimations()
		{
			m_typeText.DOKill();
			m_valueText.DOKill();
		}
	}
}
