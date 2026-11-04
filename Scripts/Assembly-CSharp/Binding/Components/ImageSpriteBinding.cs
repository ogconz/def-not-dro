using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Image))]
	public class ImageSpriteBinding : BaseBinding<SpriteProperty>
	{
		private Image m_component;

		public bool fadeMode = true;

		private Color m_backUp;

		private Color m_target;

		private PhaseTimeController m_phaseController = new PhaseTimeController(PhaseControllerTimerMode.UnscaledTime, PhaseControllerWorkMode.Once, 0.7f, suspended: true);

		private void Awake()
		{
			m_component = GetComponent<Image>();
			m_backUp = m_component.color;
		}

		protected override void Start()
		{
			base.Start();
			if ((bool)GetComponent<ColorToggleBinding>())
			{
				fadeMode = false;
			}
		}

		protected override void OnUpdateValue()
		{
			m_component.sprite = base.property.value;
			if (fadeMode)
			{
				if ((bool)base.property.value)
				{
					m_target = m_backUp;
				}
				else
				{
					m_target = Color.clear;
					m_component.color = Color.clear;
				}
				m_phaseController.Reset();
				m_phaseController.Play();
				base.enabled = true;
			}
			else
			{
				m_phaseController.IsFinished = true;
				base.enabled = false;
			}
		}

		private void Update()
		{
			if (fadeMode && m_phaseController.IsActive)
			{
				m_component.color = Color.Lerp(m_component.color, m_target, m_phaseController.getLaxitySlideFValue(1f));
			}
			else
			{
				base.enabled = false;
			}
		}
	}
}
