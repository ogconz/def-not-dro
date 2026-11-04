using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Graphics))]
	public class VisibilityFadeBoolBinding : BoolBinding
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
		private Color m_trueColor;

		[SerializeField]
		private Color m_falseColor;

		private List<Graphic> m_graphicsList = new List<Graphic>();

		protected override void Start()
		{
			GetComponents(m_graphicsList);
			base.Start();
		}

		protected override void OnUpdateValue()
		{
			if (base.gameObject == null || !base.gameObject.activeSelf)
			{
				return;
			}
			if (currentValue)
			{
				foreach (Graphic gr in m_graphicsList)
				{
					if (gr == null || !gr.gameObject.activeSelf)
					{
						break;
					}
					DOTween.To(() => (gr == null || !gr.gameObject.activeSelf) ? m_falseColor : gr.color, delegate(Color x)
					{
						if (!(gr == null) && gr.gameObject.activeSelf)
						{
							gr.color = x;
						}
					}, m_trueColor, m_time).Play().SetEase(m_trueEase)
						.SetUpdate(m_independentUpdate);
				}
				return;
			}
			foreach (Graphic gr2 in m_graphicsList)
			{
				DOTween.To(() => (gr2 == null || !gr2.gameObject.activeSelf) ? m_falseColor : gr2.color, delegate(Color x)
				{
					if (!(gr2 == null) && gr2.gameObject.activeSelf)
					{
						gr2.color = x;
					}
				}, m_falseColor, m_time).Play().SetEase(m_falseEase)
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
