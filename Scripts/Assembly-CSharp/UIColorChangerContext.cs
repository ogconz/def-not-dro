using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine.UI;

public class UIColorChangerContext : BaseBindingBehaviourTarget
{
	public readonly Property<bool> selected = new BoolProperty();

	public Action<int> clickAction;

	private const float m_stepslider = 0.01f;

	private UISmoothSlideTrait m_smoothSlideTrait;

	private UpDownPointerHandler m_pointer;

	public Slider slider { get; private set; }

	public int id { get; set; }

	private void Awake()
	{
		slider = GetComponentInChildren<Slider>(includeInactive: true);
		if (slider.TryGetComponent<UpDownPointerHandler>(out m_pointer))
		{
			m_pointer.onDownHandler.AddListener(OnClickHandler);
		}
	}

	private void Start()
	{
		if (!slider)
		{
			slider = GetComponentInChildren<Slider>(includeInactive: true);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)m_pointer)
		{
			m_pointer.onDownHandler.RemoveListener(OnClickHandler);
		}
	}

	public void AddSmoothSlideTrait(Func<float> getter, Action<float> setter)
	{
		m_smoothSlideTrait = new UISmoothSlideTrait(getter, setter);
	}

	public void OnClickHandler()
	{
		clickAction.SafeCall(id);
	}

	public void StartListenKeys(ActionType actionLeft = ActionType.NavLeft, ActionType actionRight = ActionType.NavRight, bool multiplyInputs = false)
	{
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
	}

	public static void DetachListener()
	{
		UISmoothSlideTrait.activeInstance = null;
	}

	public void Update()
	{
		m_smoothSlideTrait?.FixedUpdate();
	}
}
