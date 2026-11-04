using System;
using Binding.Base;
using GameInput;
using UnityEngine.UI;

public class UISliderChangerContext : BaseBindingBehaviourTarget
{
	private UISmoothSlideTrait m_smoothSlideTrait;

	public Slider slider { get; private set; }

	public UISmoothSlideTrait smoothSlideTrait => m_smoothSlideTrait;

	private void Awake()
	{
		slider = GetComponentInChildren<Slider>(includeInactive: true);
	}

	private void Start()
	{
		if (!slider)
		{
			slider = GetComponentInChildren<Slider>(includeInactive: true);
		}
	}

	public void AddSmoothSlideTrait(Func<float> getter, Action<float> setter)
	{
		m_smoothSlideTrait = new UISmoothSlideTrait(getter, setter);
	}

	public void StartListenKeys(ActionType actionLeft = ActionType.NavLeft, ActionType actionRight = ActionType.NavRight, bool multiplyInputs = false)
	{
		if (slider == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "UISliderChangerContext.StartListenKeys - slider == null");
			return;
		}
		if (m_smoothSlideTrait == null)
		{
			m_smoothSlideTrait = new UISmoothSlideTrait(() => slider.normalizedValue, delegate(float value)
			{
				slider.normalizedValue = value;
			});
		}
		UISmoothSlideTrait.activeInstance = m_smoothSlideTrait;
		m_smoothSlideTrait.actionLeft = actionLeft;
		m_smoothSlideTrait.actionRight = actionRight;
		m_smoothSlideTrait.multiInputMode = multiplyInputs;
		m_smoothSlideTrait.useSmooth = !slider.wholeNumbers;
	}

	public void FixedUpdate()
	{
		m_smoothSlideTrait?.FixedUpdate();
	}
}
