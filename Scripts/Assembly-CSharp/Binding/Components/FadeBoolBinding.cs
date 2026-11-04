using DG.Tweening;
using UnityEngine;

namespace Binding.Components
{
	public class FadeBoolBinding : BoolBinding
	{
		private CanvasGroup m_canvasGroup;

		public float fadeOutTime = 0.5f;

		public float fadeInTime = 0.5f;

		private void Awake()
		{
			m_canvasGroup = GetComponent<CanvasGroup>();
			if ((bool)m_canvasGroup)
			{
				m_canvasGroup.alpha = 0f;
			}
		}

		protected override void OnUpdateValue()
		{
			if ((bool)m_canvasGroup)
			{
				if (currentValue)
				{
					m_canvasGroup.DOKill();
					m_canvasGroup.DOFade(1f, fadeInTime);
				}
				else
				{
					m_canvasGroup.DOKill();
					m_canvasGroup.DOFade(0f, fadeOutTime);
				}
			}
		}
	}
}
