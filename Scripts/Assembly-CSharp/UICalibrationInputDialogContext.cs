using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using GameInput;
using PrefsModels;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
public class UICalibrationInputDialogContext : BaseBindingBehaviourTarget
{
	private const float MAX_CALIBRATION_INPUT_VAL = 100f;

	private const float SCALE_RANGE_VAL = 0.001f;

	private const float CALIBRATION_SCALE_FACTOR_MOUSE_MOVE = 0.001f;

	[Dependency]
	private GamePrefs m_prefs;

	private GlobalModel m_globals;

	private UIInputBindsSettingsContext m_bindsContext;

	private InputSettingsListItem m_itemCalibration;

	public Property<string> titleElement = new StringProperty();

	public Property<bool> calibrationMode = new BoolProperty(false);

	public Property<string> calibrationModeText = new StringProperty();

	public Property<bool> invert = new BoolProperty();

	public Property<string> invertToggleText = new StringProperty();

	public Property<bool> availablePrev = new BoolProperty();

	public Property<bool> availableNext = new BoolProperty();

	public Property<float> min = new FloatProperty();

	public Property<float> max = new FloatProperty();

	public Property<float> currentValue = new FloatProperty();

	public Property<float> currentNormalizedValue = new FloatProperty();

	public System.Action onDeactivate;

	private List<ActionHintData> m_actionHintDataList = new List<ActionHintData>();

	protected override void OnActivate()
	{
		base.OnActivate();
		UpdateBottomPanel();
		m_bottomPanelContext.BringForwardByDepth();
		m_globals = m_prefs.globals;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	protected override void OnDeactivate()
	{
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
		onDeactivate.SafeCall();
		base.OnDeactivate();
	}

	private void UpdateInverseParam(bool isActive)
	{
		availablePrev.value = isActive;
		availableNext.value = !isActive;
		invertToggleText.value = (isActive ? LocalizationKeys.Yes : LocalizationKeys.No).Localized();
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		if (m_bindsContext.controller.id == args.controllerId)
		{
			NavCancel();
		}
	}

	private void Update()
	{
		if (m_itemCalibration?.bind?.elementAxis == null)
		{
			return;
		}
		BindData bindData = m_itemCalibration?.bind;
		if (bindData != null)
		{
			BindAxisElement elementAxis = bindData.elementAxis;
			float num = 0f;
			num = ((!(elementAxis.elementName == "Gyroscope Horizontal Axis")) ? elementAxis.GetRAWValueAsFloat() : GameInput.InputManager.instance.lastController.TryGetDS4AccelerationX());
			bindData.setPropsMode = true;
			bindData.invert = elementAxis.invert;
			bindData.currentAxis = MathTools.Round(num * 1000f, 0.10000000149011612, MathTools.RoundMode.HalfUp);
			if (bindData.calibrationMode)
			{
				ProcessCalibrate(elementAxis, num);
			}
			bindData.minAxis = MathTools.Round(elementAxis.minValue * 1000f, 0.10000000149011612, MathTools.RoundMode.HalfUp);
			bindData.maxAxis = MathTools.Round(elementAxis.maxValue * 1000f, 0.10000000149011612, MathTools.RoundMode.HalfUp);
			bindData.currentNormalizedAxis = elementAxis.GetCurvedValue(bindData.action);
			bindData.setPropsMode = false;
			UpdateBindData();
		}
	}

	private void ProcessCalibrate(BindAxisElement axisElement, float rawSignal)
	{
		if (axisElement != null)
		{
			if (isMouseMoveBind(axisElement, out var isHorizontalMouseMove) && m_globals.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
			{
				UpdateMouseCoordAxisRangeValues(axisElement, isHorizontalMouseMove);
				return;
			}
			axisElement.minValue = Mathf.Min(axisElement.minValue, rawSignal);
			axisElement.maxValue = Mathf.Max(axisElement.maxValue, rawSignal);
		}
	}

	private bool isMouseMoveBind(BindAxisElement axisElement, out bool isHorizontalMouseMove)
	{
		isHorizontalMouseMove = false;
		if (axisElement == null)
		{
			return false;
		}
		string elementName = axisElement.elementName;
		if (string.IsNullOrEmpty(elementName))
		{
			return false;
		}
		string text = elementName.ToLower();
		isHorizontalMouseMove = text.Contains("mouse horizontal");
		ControllerMetaInfo deviceInfo = axisElement.deviceInfo;
		if (deviceInfo != null && deviceInfo.rewiredType == ControllerType.Mouse)
		{
			if (!isHorizontalMouseMove)
			{
				return text.Contains("mouse vertical");
			}
			return true;
		}
		return false;
	}

	private void UpdateMouseCoordAxisRangeValues(BindAxisElement mouseAxisElem, bool isHorizontal)
	{
		if (mouseAxisElem != null)
		{
			float num = (isHorizontal ? Screen.width : Screen.height);
			num *= 0.001f;
			Vector2 screenPosition = ReInput.controllers.Mouse.screenPosition;
			float b = (isHorizontal ? screenPosition.x : screenPosition.y) * 0.001f - num / 2f;
			float num2 = Mathf.Max(Mathf.Max(mouseAxisElem.maxValue, b), Mathf.Abs(Mathf.Min(mouseAxisElem.minValue, b)));
			mouseAxisElem.minValue = Mathf.Clamp(0f - num2, (0f - num) / 2f, 0f);
			mouseAxisElem.maxValue = Mathf.Clamp(num2, 0f, num / 2f);
			mouseAxisElem.ResetCacheInputRange();
		}
	}

	private void UpdateBottomPanel()
	{
		m_actionHintDataList.Clear();
		m_actionHintDataList.AddRange(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_calibration_inversion.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = NavInversion
			}
		});
		m_actionHintDataList.Add((!calibrationMode.value) ? new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_input_settings_calibration_begin.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = SwitchCalibration
		} : new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_input_settings_calibration_save.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = SwitchCalibration
		});
		ShowBottomPanel(m_actionHintDataList.ToArray());
	}

	public void ShowInputRangeDialog(bool isMinRange)
	{
		if (calibrationMode.value)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.input_settings_calibration_warning.Localized());
			return;
		}
		BindData bind = m_itemCalibration?.bind;
		if (bind == null)
		{
			return;
		}
		float num = (isMinRange ? bind.minAxis : bind.maxAxis);
		UIMessageBox.ShowInputDialog((isMinRange ? LocalizationKeys.input_settings_input_min_value_text : LocalizationKeys.input_settings_input_max_value_text).Localized(), num.ToString(), null, InputField.ContentType.IntegerNumber, delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate(string number)
		{
			if (!string.IsNullOrEmpty(number))
			{
				number = number.Replace(" ", "");
				if (double.TryParse(number, out var result))
				{
					float num2 = Mathf.Clamp((float)result * 0.001f, -100f, 100f);
					if (isMouseMoveBind(bind.elementAxis, out var _) && m_globals.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
					{
						float num3 = Mathf.Abs(num2);
						bind.maxAxis = num3;
						bind.minAxis = 0f - num3;
						m_itemCalibration.bind.elementAxis.maxValue = num3;
						m_itemCalibration.bind.elementAxis.minValue = 0f - num3;
						bind.elementAxis.ResetCacheInputRange();
					}
					else if (isMinRange)
					{
						bind.minAxis = num2;
						m_itemCalibration.bind.elementAxis.minValue = num2;
					}
					else
					{
						bind.maxAxis = num2;
						m_itemCalibration.bind.elementAxis.maxValue = num2;
					}
					UpdateBindData();
				}
			}
		}, LocalizationKeys.Ok.Localized());
	}

	public void SetBindData(UIInputBindsSettingsContext context, InputSettingsListItem item)
	{
		m_bindsContext = context;
		m_itemCalibration = item;
		BindData bind = m_itemCalibration.bind;
		if (bind != null)
		{
			invert.value = bind.invert;
		}
		UpdateInverseParam(invert.value);
		UpdateBindData();
	}

	private void UpdateBindData()
	{
		if (m_itemCalibration?.bind?.elementAxis != null)
		{
			BindData bindData = m_itemCalibration?.bind;
			ApplyTitleLocalization(bindData.baseElement.elementId);
			calibrationMode.value = bindData.calibrationMode;
			calibrationModeText.value = (calibrationMode.value ? LocalizationKeys.nav_hint_input_settings_calibration_save : LocalizationKeys.nav_hint_input_settings_calibration_begin).Localized();
			invert.value = bindData.invert;
			min.value = bindData.minAxis;
			max.value = bindData.maxAxis;
			currentValue.value = bindData.currentAxis;
			currentNormalizedValue.value = bindData.currentNormalizedAxis;
		}
	}

	private void ApplyTitleLocalization(string bindName)
	{
		if (!string.IsNullOrEmpty(bindName))
		{
			string text = "bind_" + bindName.Replace(' ', '_').ToLower();
			if (LocalizationService.IsKeyExist(text))
			{
				bindName = new LocalizationKeys.Key(text).Localized();
			}
			titleElement.value = bindName;
		}
	}

	private void RenameBind(ref string bind, string from, string to)
	{
		if (bind.Equals(from))
		{
			bind = to;
		}
	}

	public void SwitchCalibration()
	{
		if (m_itemCalibration?.bind != null)
		{
			m_itemCalibration.Calibration();
			UpdateBindData();
			UpdateBottomPanel();
		}
	}

	public void NavCancel()
	{
		if (calibrationMode.value)
		{
			m_itemCalibration?.RevertCalibrationToLast();
			SwitchCalibration();
		}
		Hide();
	}

	public void NavInversion()
	{
		if (m_itemCalibration != null)
		{
			BindData bind = m_itemCalibration.bind;
			bind.invert = !bind.invert;
			bind.elementAxis.invert = bind.invert;
			UpdateInverseParam(bind.invert);
		}
	}
}
