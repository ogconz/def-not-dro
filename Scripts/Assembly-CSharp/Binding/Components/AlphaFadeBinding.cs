using System.Collections;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace Binding.Components
{
	public class AlphaFadeBinding : BaseBinding<AlphaFadeProperty>
	{
		private CanvasGroup m_canvasGroup;

		private Graphic m_graphic;

		private float m_alpha;

		private IEnumerator m_coroutine;

		private void Awake()
		{
			m_canvasGroup = GetComponent<CanvasGroup>();
			m_graphic = GetComponent<Graphic>();
		}

		private void OnDestroy()
		{
			Stop();
		}

		private void Stop()
		{
			TimeController.RemoveCoroutine(m_coroutine);
		}

		protected override void OnValueChanged()
		{
			Stop();
			if (base.property.fadeTime == 0f || base.property.force)
			{
				SetAlpha(base.property.value);
			}
			else
			{
				m_coroutine = TimeController.RunCoroutine(FadeCoroutine());
			}
		}

		protected override void OnUpdateValue()
		{
			SetAlpha(base.property.value);
		}

		private void SetAlpha(float alpha)
		{
			if (m_canvasGroup != null)
			{
				m_canvasGroup.alpha = alpha;
			}
			if (m_graphic != null)
			{
				m_graphic.SetAlpha(alpha);
			}
			m_alpha = alpha;
		}

		private IEnumerator FadeCoroutine()
		{
			bool fade = m_alpha > base.property.value;
			float targetAlpha = base.property.value;
			float delta = Mathf.Abs(targetAlpha - m_alpha);
			while (delta > 0f)
			{
				float alpha = (fade ? (targetAlpha + delta) : (targetAlpha - delta));
				SetAlpha(alpha);
				delta = ((base.property.fadeTime != 0f) ? (delta - Time.deltaTime * (1f / base.property.fadeTime)) : 0f);
				yield return null;
			}
			SetAlpha(targetAlpha);
			base.property.Finish();
		}
	}
}
