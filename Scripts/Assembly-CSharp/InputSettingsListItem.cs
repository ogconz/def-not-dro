using GameInput;
using Rewired;

public class InputSettingsListItem : BaseListItem
{
	private const float RECT_WIDTH = 840f;

	private const float RECT_HEIGHT = 58f;

	public UIInputBindsSettingsContext context;

	public Action action;

	public string title;

	public ButtonDisplayMode m_flags;

	public BindData bind;

	public BindData lastBindData = new BindData();

	public bool useWideViewItem;

	public bool updatedNames;

	public InputSettingsListItem(UIInputBindsSettingsContext context, Action action, string title, ButtonDisplayMode displayMode = ButtonDisplayMode.ALL, bool useWideViewItem = false)
	{
		this.context = context;
		this.action = action;
		this.title = LocalizationKeys.input_action_.AddLocalized(title.ToLower());
		m_flags = displayMode;
		this.useWideViewItem = useWideViewItem;
		width = 840f;
		height = 58f;
		UpdateBindsName();
	}

	public BindElement GetElement()
	{
		return bind?.baseElement;
	}

	public string GetElementName()
	{
		BindElement element = GetElement();
		if (element == null)
		{
			return "";
		}
		return element.elementId;
	}

	public void UpdateBindsName()
	{
		bind = null;
		if (action.activeBinds.Count == 0)
		{
			return;
		}
		foreach (BindElement activeBind in action.activeBinds)
		{
			if (!IsBindDisplayable(activeBind))
			{
				continue;
			}
			Controller rewiredController = activeBind.deviceInfo.rewiredController;
			if (context.controller.type != ControllerType.Keyboard)
			{
				if (rewiredController.type != context.controller.type || rewiredController.hardwareIdentifier != context.controller.hardwareIdentifier)
				{
					continue;
				}
			}
			else if (rewiredController.type != ControllerType.Mouse && rewiredController.type != ControllerType.Keyboard)
			{
				continue;
			}
			BindAxisElement bindAxisElement = activeBind as BindAxisElement;
			BindData bindData = new BindData
			{
				action = action,
				baseElement = activeBind,
				elementButton = (activeBind as BindButtonElement),
				elementAxis = bindAxisElement,
				calibrationMode = false,
				currentAxis = 0f,
				currentNormalizedAxis = 0f,
				invert = (bindAxisElement?.invert ?? false),
				minAxis = (bindAxisElement?.minValue ?? 0f),
				maxAxis = (bindAxisElement?.maxValue ?? 0f)
			};
			bind = bindData;
			break;
		}
		updatedNames = true;
	}

	public void Calibration()
	{
		if (bind?.elementAxis != null)
		{
			bind.calibrationMode = !bind.calibrationMode;
			if (bind.calibrationMode)
			{
				bind.CopyTo(lastBindData);
				bind.elementAxis.minValue = 0f;
				bind.elementAxis.maxValue = 0f;
			}
			else
			{
				CancelCalibration();
			}
		}
	}

	public bool CancelCalibration()
	{
		bool result = false;
		if (bind != null && bind.calibrationMode)
		{
			bind.calibrationMode = false;
			result = true;
		}
		return result;
	}

	public void RevertCalibrationToLast()
	{
		lastBindData.CopyTo(bind);
		bind.UpdateAxisMinMax();
	}

	public bool IsCalibration()
	{
		if (bind != null)
		{
			return bind.calibrationMode;
		}
		return false;
	}

	private bool IsBindDisplayable(BindElement bind)
	{
		if (!(bind is BindButtonElement bindButtonElement))
		{
			return true;
		}
		if (action.valueType == ActionValueType.Full)
		{
			if ((m_flags & ButtonDisplayMode.EXCLUDE_NEGATIVE) != ButtonDisplayMode.ALL && bindButtonElement.bindType == BindButtonType.Negative)
			{
				return false;
			}
			if ((m_flags & ButtonDisplayMode.EXCLUDE_POSITIVE) != ButtonDisplayMode.ALL && bindButtonElement.bindType == BindButtonType.Positive)
			{
				return false;
			}
		}
		return true;
	}

	public void PollBind()
	{
		ControllerPollingInfo pollInfo = ReInput.controllers.polling.PollControllerForFirstButtonDown(ControllerType.Mouse, ReInput.controllers.Mouse.id);
		if (pollInfo.success)
		{
			context.TryBind(this, pollInfo);
		}
		else
		{
			context.PollItem(this);
		}
	}

	public void ShowCalibration()
	{
		context?.ShowCalibrationDialog(this);
	}

	public void ClearBind()
	{
		context?.RemoveBind(this);
	}

	public bool IsNegative()
	{
		return (m_flags & ButtonDisplayMode.EXCLUDE_POSITIVE) != 0;
	}
}
