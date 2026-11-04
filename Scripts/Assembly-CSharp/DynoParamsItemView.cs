using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarProfile;
using GameInput;
using UnityEngine;
using UnityEngine.UI;

public class DynoParamsItemView : BaseItemController<BaseDynoParamsItemViewModel>
{
	public enum ChangerMode
	{
		Slider = 0,
		Toggle = 1
	}

	public enum ScrollDirection
	{
		None = 0,
		Left = 1,
		Right = 2
	}

	public const float AUTO_SCROLL_SPEED = 17f;

	public const float WIDE_SLIDER_WIDTH = 697f;

	public const float SHORT_SLIDER_WIDTH = 393f;

	public const float WIDE_TITLE_WIDTH = 492f;

	public const float SHORT_TITLE_WIDTH = 292f;

	public Property<string> title = new StringProperty();

	public Property<string> description = new StringProperty();

	public Property<string> units = new StringProperty();

	public Property<float> value = new FloatProperty();

	public Property<string> additionalOutputValueLeft = new StringProperty();

	public Property<string> outputValue = new StringProperty();

	public Property<string> additionalOutputValueRight = new StringProperty();

	public Property<bool> isLocked = new BoolProperty();

	private BoolProperty m_needShowText = new BoolProperty();

	private IntProperty m_changerMode = new IntProperty();

	private BoolProperty m_canPrev = new BoolProperty();

	private BoolProperty m_canNext = new BoolProperty();

	[SerializeField]
	private Slider m_slider;

	[SerializeField]
	private RectTransform m_valueContainer;

	[SerializeField]
	private RectTransform m_valueBorders;

	[SerializeField]
	private RectTransform m_outputValueRect;

	[SerializeField]
	private RectTransform m_titleRect;

	[SerializeField]
	private Button m_sliderHoverButton;

	[SerializeField]
	private Button m_switcherHoverButton;

	private bool m_isLeftSideAlignmentSetted;

	private UISliderChangerContext m_sliderChanger;

	private List<float> m_values;

	private ContextOwner m_contextOwner;

	public bool enableAutoScrollValue { get; private set; }

	public ScrollDirection currentScrollDirection { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		if (m_slider != null)
		{
			m_slider.onValueChanged.AddListener(OnChangedValue);
			m_slider.interactable = false;
			ColorBlock colors = m_slider.colors;
			colors.disabledColor = colors.normalColor;
			m_slider.colors = colors;
		}
		m_sliderChanger = GetComponentInChildren<UISliderChangerContext>();
		if (m_sliderChanger != null && m_sliderChanger.slider != null)
		{
			m_sliderChanger.AddSmoothSlideTrait(() => m_sliderChanger.slider.value, OnChangedValue);
		}
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState != null)
		{
			m_contextOwner = baseGameState.contextOwner;
		}
	}

	protected override void OnEnable()
	{
		selected.OnValueChanged += OnSliderValueChangeHandler;
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onTopContextChanged, new Action<BaseContext>(HandleSlidersInteractable));
		}
	}

	private void Update()
	{
		if (enableAutoScrollValue)
		{
			HandleMovingText();
		}
	}

	private void HandleMovingText()
	{
		TrySetLeftAlignment();
		ScrollToAndChangeDirectionIfNeed(currentScrollDirection);
	}

	private void ScrollToAndChangeDirectionIfNeed(ScrollDirection direction)
	{
		int num;
		switch (direction)
		{
		case ScrollDirection.None:
			return;
		default:
			num = -1;
			break;
		case ScrollDirection.Right:
			num = 1;
			break;
		}
		int num2 = num;
		float x = m_outputValueRect.anchoredPosition.x;
		m_outputValueRect.anchoredPosition += num2 * new Vector2(Time.unscaledDeltaTime * 17f, 0f);
		if (direction == ScrollDirection.Right)
		{
			if (x - m_outputValueRect.rect.width / 2f > (0f - m_valueBorders.rect.width) / 2f)
			{
				currentScrollDirection = ScrollDirection.Left;
			}
		}
		else if (x + m_outputValueRect.rect.width / 2f < m_valueBorders.rect.width / 2f)
		{
			currentScrollDirection = ScrollDirection.Right;
		}
	}

	private void TrySetLeftAlignment()
	{
		if (!m_isLeftSideAlignmentSetted)
		{
			float x = Mathf.Abs(m_outputValueRect.anchoredPosition.x - m_outputValueRect.rect.width / 2f - (0f - m_valueBorders.rect.width) / 2f);
			m_outputValueRect.anchoredPosition = new Vector2(x, m_outputValueRect.anchoredPosition.y);
			m_isLeftSideAlignmentSetted = true;
			currentScrollDirection = ScrollDirection.Left;
		}
	}

	private void InitIfNeedValueBlockMoving()
	{
		float num = m_valueBorders.rect.width;
		bool flag = m_outputValueRect.rect.width > num;
		if (enableAutoScrollValue && !flag)
		{
			m_outputValueRect.anchoredPosition = new Vector2(0f, m_outputValueRect.anchoredPosition.y);
			currentScrollDirection = ScrollDirection.None;
			m_isLeftSideAlignmentSetted = false;
		}
		enableAutoScrollValue = flag;
	}

	private void OnSliderValueChangeHandler()
	{
		if (selected.value)
		{
			StartListenerHotKeys();
		}
		m_slider.interactable = selected.value;
	}

	private void HandleSlidersInteractable(BaseContext cntx)
	{
		if (m_contextOwner != null)
		{
			m_slider.interactable = !(m_contextOwner.topContext is UIBaseDialogContext);
		}
	}

	protected override void OnDisable()
	{
		selected.OnValueChanged -= OnSliderValueChangeHandler;
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onTopContextChanged, new Action<BaseContext>(HandleSlidersInteractable));
		}
	}

	private void SetItemValueContainerWidePreset(bool isShort)
	{
		m_valueContainer.SetWidth(isShort ? 393f : 697f);
		m_titleRect.SetWidth(isShort ? 292f : 492f);
	}

	public override void SetItem(BaseDynoParamsItemViewModel newItem)
	{
		base.SetItem(newItem);
		if (newItem == null)
		{
			return;
		}
		SetItemValueContainerWidePreset(newItem.isShortItem);
		if (!(newItem is DynoParamsItemViewModel { value: var num } dynoParamsItemViewModel))
		{
			if (!(newItem is CustomToggleDynoParamsItemViewModel customToggleDynoParamsItemViewModel))
			{
				if (!(newItem is DrivetrainSwitchDynoParamViewModel drivetrainSwitchDynoParamViewModel))
				{
					if (newItem is MassDynoParamViewModel massDynoParamViewModel)
					{
						value.value = massDynoParamViewModel.value;
						outputValue.value = massDynoParamViewModel.outputValue;
						m_needShowText.value = true;
						if (m_slider != null)
						{
							m_slider.value = massDynoParamViewModel.value;
						}
						title.value = massDynoParamViewModel.title;
						description.value = massDynoParamViewModel.description;
						units.value = massDynoParamViewModel.units;
						isLocked.value = false;
						m_changerMode.value = 0;
						m_switcherHoverButton.enabled = false;
						m_sliderHoverButton.enabled = true;
						if (selected.value)
						{
							UpdateSliderStep();
						}
					}
				}
				else
				{
					m_changerMode.value = 1;
					m_sliderHoverButton.enabled = false;
					m_switcherHoverButton.enabled = true;
					m_canNext.value = drivetrainSwitchDynoParamViewModel.IsIndexValid(drivetrainSwitchDynoParamViewModel.GetOutputValue(drivetrainSwitchDynoParamViewModel.controller) + 1f);
					m_canPrev.value = drivetrainSwitchDynoParamViewModel.IsIndexValid(drivetrainSwitchDynoParamViewModel.GetOutputValue(drivetrainSwitchDynoParamViewModel.controller) - 1f);
					m_needShowText.value = true;
					title.value = drivetrainSwitchDynoParamViewModel.title;
					description.value = drivetrainSwitchDynoParamViewModel.description;
					units.value = string.Empty;
					UpdateSliderStep();
					outputValue.value = drivetrainSwitchDynoParamViewModel.outputValue;
					if (m_slider != null)
					{
						m_slider.value = drivetrainSwitchDynoParamViewModel.value;
					}
				}
			}
			else
			{
				m_changerMode.value = 1;
				m_sliderHoverButton.enabled = false;
				m_switcherHoverButton.enabled = true;
				m_canNext.value = customToggleDynoParamsItemViewModel.value <= 0.5f;
				m_canPrev.value = !m_canNext.value;
				m_needShowText.value = true;
				title.value = customToggleDynoParamsItemViewModel.title;
				description.value = customToggleDynoParamsItemViewModel.description;
				units.value = string.Empty;
				UpdateSliderStep();
				outputValue.value = customToggleDynoParamsItemViewModel.outputValue;
				if (m_slider != null)
				{
					m_slider.value = customToggleDynoParamsItemViewModel.value;
				}
			}
		}
		else
		{
			value.value = num;
			if (!dynoParamsItemViewModel.IsToeCamberItem())
			{
				outputValue.value = dynoParamsItemViewModel.outputValue;
			}
			m_needShowText.value = !dynoParamsItemViewModel.IsRearSuspension() && !dynoParamsItemViewModel.isLocked;
			bool flag = dynoParamsItemViewModel.IsCtrlNameEqualsKeyDiffType(dynoParamsItemViewModel.controller);
			if (flag)
			{
				dynoParamsItemViewModel.onValueChanged = (Action<DynoParamsItemViewModel>)Delegate.Remove(dynoParamsItemViewModel.onValueChanged, new Action<DynoParamsItemViewModel>(SliderDiscretization));
				dynoParamsItemViewModel.onValueChanged = (Action<DynoParamsItemViewModel>)Delegate.Combine(dynoParamsItemViewModel.onValueChanged, new Action<DynoParamsItemViewModel>(SliderDiscretization));
			}
			if (m_slider != null)
			{
				m_slider.value = num;
			}
			title.value = dynoParamsItemViewModel.title;
			description.value = dynoParamsItemViewModel.description;
			units.value = dynoParamsItemViewModel.units;
			isLocked.value = dynoParamsItemViewModel.isLocked;
			if (dynoParamsItemViewModel.IsBoolDynoParameter() || flag)
			{
				m_changerMode.value = 1;
				m_sliderHoverButton.enabled = false;
				m_switcherHoverButton.enabled = true;
				m_canNext.value = num != 1f;
				m_canPrev.value = num != 0f;
			}
			else
			{
				m_changerMode.value = 0;
				m_switcherHoverButton.enabled = false;
				m_sliderHoverButton.enabled = true;
			}
			if (selected.value)
			{
				UpdateSliderStep();
			}
		}
		InitIfNeedValueBlockMoving();
	}

	private void UpdateSliderStep()
	{
		UISmoothSlideTrait smoothSlideTrait = m_sliderChanger.smoothSlideTrait;
		if (smoothSlideTrait == null)
		{
			return;
		}
		float step = 0.008f;
		BaseDynoParamsItemViewModel baseDynoParamsItemViewModel = m_item;
		if (!(baseDynoParamsItemViewModel is DrivetrainSwitchDynoParamViewModel) && !(baseDynoParamsItemViewModel is MassDynoParamViewModel) && !(baseDynoParamsItemViewModel is DynoParamsItemViewModel))
		{
			if (baseDynoParamsItemViewModel is CustomToggleDynoParamsItemViewModel)
			{
				step = 1f;
			}
		}
		else
		{
			CarProfileChanger carProfileChanger = m_item.controller.changers[0];
			m_values = carProfileChanger.values;
			float num = m_values[0] * carProfileChanger.multiplier;
			float num2 = m_values[m_values.Count - 1] * carProfileChanger.multiplier;
			if (m_values != null && m_values.Count >= 2)
			{
				float num3 = carProfileChanger.step * carProfileChanger.multiplier;
				if (num3 != 0f)
				{
					float num4 = num2 - num;
					step = num3 / num4;
				}
				else if (m_values.Count > 2 || m_item.controller.name == "turbo_charged")
				{
					step = 1f / (float)(m_values.Count - 1);
				}
				else
				{
					float num5 = num2 - num;
					if (carProfileChanger.parameter.format == "F2")
					{
						num5 *= 100f;
					}
					else if (carProfileChanger.parameter.format == "F1")
					{
						num5 *= 10f;
					}
					step = 1f / num5;
				}
			}
		}
		smoothSlideTrait.step = step;
	}

	public void StartListenerHotKeys()
	{
		m_sliderChanger?.StartListenKeys(ActionType.NavAdditionalPrevBtnL2, ActionType.NavAdditionalNextBtnR2);
	}

	public void IncValue()
	{
		if (!selected.value)
		{
			OnClick();
			StartListenerHotKeys();
		}
		UpdateSliderStep();
		if (m_changerMode.value == 1)
		{
			TimeController.AddCallback(0f, delegate
			{
				ChangerInc();
			});
		}
		else
		{
			ChangerInc();
		}
		void ChangerInc()
		{
			m_sliderChanger.smoothSlideTrait.IncValue();
		}
	}

	public void DecValue()
	{
		if (!selected.value)
		{
			OnClick();
			StartListenerHotKeys();
		}
		UpdateSliderStep();
		if (m_changerMode.value == 1)
		{
			TimeController.AddCallback(0f, delegate
			{
				ChangerDec();
			});
		}
		else
		{
			ChangerDec();
		}
		void ChangerDec()
		{
			m_sliderChanger.smoothSlideTrait.DecValue();
		}
	}

	public override void OnClick()
	{
		base.OnClick();
		m_item?.OnClick();
	}

	public override void OnUpMouse()
	{
		m_item?.OnUpMouse();
	}

	public void OnChangedValue(float newValue)
	{
		if (selected.value && m_item != null && m_slider != null)
		{
			m_item.value = newValue;
			SetItem(m_item);
		}
	}

	private void SliderDiscretization(DynoParamsItemViewModel dynoParamsItemViewModel)
	{
		if (selected.value)
		{
			float num = (float)Mathf.RoundToInt(dynoParamsItemViewModel.value * dynoParamsItemViewModel.controller.changers[0].values[1]) / dynoParamsItemViewModel.controller.changers[0].values[1];
			m_slider.value = num;
		}
	}
}
