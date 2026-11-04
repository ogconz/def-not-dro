using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

public class UIWheelAnglePanel : BaseBindingBehaviourTarget
{
	private const float STEER_SLIDER_STEP = 0.05f;

	[SerializeField]
	private UISliderChangerContext m_sliderChanger;

	[SerializeField]
	private UpDownPointerHandler m_upDownPointHandler;

	private DynoSteeringModel m_dynoSteeeringModel;

	private readonly Property<string> m_angleValueLeft = new Property<string>();

	private readonly Property<string> m_angleValueRight = new Property<string>();

	private readonly Property<bool> m_steeringResetHintVisibility = new BoolProperty();

	private readonly Property<bool> m_steerSliderSelected = new BoolProperty();

	public bool isSelected => m_steerSliderSelected.value;

	public new bool isActive => base.gameObject.activeInHierarchy;

	private void Start()
	{
		m_upDownPointHandler = m_sliderChanger.slider.GetComponent<UpDownPointerHandler>();
		SliderStartListenKeys();
		m_sliderChanger.smoothSlideTrait.step = 0.05f;
		m_sliderChanger.slider.onValueChanged.AddListener(UpdateSteeringResetHitVisibility);
		m_upDownPointHandler.onUpHandler.AddListener(OnSliderUpHandler);
		m_upDownPointHandler.onDownHandler.AddListener(OnSliderDownHandler);
	}

	private void SliderStartListenKeys()
	{
		m_sliderChanger.StartListenKeys(ActionType.NavAdditionalPrevBtnL2, ActionType.NavAdditionalNextBtnR2);
	}

	private void OnSliderUpHandler()
	{
		UpdateSteeringResetHitVisibility(m_sliderChanger.slider.value);
	}

	public void OnSliderDownHandler()
	{
		SetSelected(isSelected: true);
		if (States.current is BaseGameState baseGameState)
		{
			UIDynostandContext uIDynostandContext = baseGameState.contextOwner.Get<UIDynostandContext>();
			if (uIDynostandContext != null)
			{
				uIDynostandContext.ResetListsSelection();
			}
		}
	}

	public void SetDynoSteeringModel(DynoSteeringModel dynoSteeringModel)
	{
		m_dynoSteeeringModel = dynoSteeringModel;
	}

	private void UpdateSteeringResetHitVisibility(float sliderValue)
	{
		m_steeringResetHintVisibility.value = m_steerSliderSelected.value && Math.Abs(sliderValue) > 0f;
	}

	public void ResetSteeringValue()
	{
		m_sliderChanger.slider.value = 0f;
		HideResetHint();
	}

	public void SetSelected(bool isSelected)
	{
		if (m_steerSliderSelected.value != isSelected)
		{
			m_steerSliderSelected.value = isSelected;
			OnSliderUpHandler();
		}
	}

	private void HideResetHint()
	{
		m_steeringResetHintVisibility.value = false;
	}

	public void IncSteering()
	{
		if (m_steerSliderSelected.value)
		{
			SliderStartListenKeys();
			OnSliderUpHandler();
			m_sliderChanger.smoothSlideTrait.IncValue();
		}
	}

	public void DecSteering()
	{
		if (m_steerSliderSelected.value)
		{
			SliderStartListenKeys();
			OnSliderUpHandler();
			m_sliderChanger.smoothSlideTrait.DecValue();
		}
	}

	private void Update()
	{
		if (m_dynoSteeeringModel != null)
		{
			m_dynoSteeeringModel.ManualSteerUpdate(m_sliderChanger.slider.value);
			UpdateAngleValues();
			m_sliderChanger.smoothSlideTrait?.FixedUpdate();
		}
	}

	private void UpdateAngleValues()
	{
		m_angleValueLeft.value = $"{m_dynoSteeeringModel.GetFrontWheelAnglesTuple().Item1:N2}{'°'}";
		m_angleValueRight.value = $"{m_dynoSteeeringModel.GetFrontWheelAnglesTuple().Item2:N2}{'°'}";
	}

	protected override void OnDestroy()
	{
		m_upDownPointHandler.onUpHandler.RemoveAllListeners();
		m_upDownPointHandler.onDownHandler.RemoveAllListeners();
		m_sliderChanger.slider.onValueChanged.RemoveAllListeners();
	}
}
