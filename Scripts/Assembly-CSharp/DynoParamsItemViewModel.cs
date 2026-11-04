using System;
using CarProfile;
using CarX;
using DB;
using DI;
using Db.Meta.CarProfile;
using GameInput;
using SharingSetups;
using UnityEngine;

public class DynoParamsItemViewModel : BaseDynoParamsItemViewModel
{
	public const string FRONT_TOE = "front_toe";

	public const string REAR_TOE = "rear_toe";

	public const string FRONT_CAMBER = "front_camber";

	public const string REAR_CAMBER = "rear_camber";

	public const string FRONT_SUSPENTION_TAG = "front";

	public const string REAR_SUSPENTION_TAG = "rear";

	private bool m_initialized;

	public Action<DynoParamsItemViewModel> onValueChanged;

	private static BaseModel m_baseModel;

	private CarProfileParameterMeta m_carProfileParameterMeta;

	private float m_value;

	public int id { get; private set; }

	public int sort { get; private set; }

	public bool isLocked { get; private set; }

	public string units { get; private set; }

	public bool isRearSuspension { get; private set; }

	public override float value
	{
		get
		{
			if (!IsBoolDynoParameter())
			{
				return m_value;
			}
			return (m_value > 0.5f) ? 1 : 0;
		}
		set
		{
			if (m_value == value && m_initialized)
			{
				return;
			}
			m_value = value;
			if (base.controller != null)
			{
				if (m_initialized)
				{
					SetControllerNormalizedValue(base.controller, value);
				}
				outputValue = GetControllerOutputValue(base.controller);
				if (m_initialized)
				{
					m_value = value;
					m_dynoContext.DirtyChanges();
				}
			}
			onValueChanged.SafeCall(this);
		}
	}

	public override void RevertParameter()
	{
		if (!m_dynoContext.profileInfoCached.controllers.TryGetValue(base.controller.name, out var controllerDefValue))
		{
			controllerDefValue = GetControllerDefValue(base.controller);
		}
		value = controllerDefValue;
	}

	public override void RevertToDefault()
	{
		if (base.controller != null)
		{
			SetControllerNormalizedValue(base.controller, GetControllerDefValue(base.controller));
			value = GetControllerDefValue(base.controller);
		}
	}

	public string GetToeCamberVal()
	{
		if (m_dynoContext == null || base.controller == null)
		{
			return string.Empty;
		}
		return base.controller.name switch
		{
			"front_toe" => m_dynoContext.frontToeHint.value, 
			"rear_toe" => m_dynoContext.rearToeHint.value, 
			"front_camber" => m_dynoContext.frontCamberHint.value, 
			"rear_camber" => m_dynoContext.rearCamberHint.value, 
			_ => string.Empty, 
		};
	}

	public bool IsToeCamberItem()
	{
		if (base.controller == null)
		{
			return false;
		}
		string name = base.controller.name;
		if (!name.Equals("front_toe") && !name.Equals("rear_toe") && !name.Equals("front_camber"))
		{
			return name.Equals("rear_camber");
		}
		return true;
	}

	public bool IsRearSuspension()
	{
		if (base.controller == null)
		{
			return false;
		}
		return base.controller.name.Contains("rear");
	}

	public bool IsFrontSuspension()
	{
		if (base.controller == null)
		{
			return false;
		}
		return base.controller.name.Contains("front");
	}

	public bool IsCtrlNameEqualsKeyDiffType(CarProfileController ctrl)
	{
		return ctrl?.name.Equals("diff_type") ?? false;
	}

	public DynoParamsItemViewModel(UISetupsSharingContext context, CarProfileController ctrl, int id, int sort, bool isShortItem = false)
	{
		m_setupsSharingContext = context;
		base.controller = ctrl;
		this.id = id;
		this.sort = sort;
		if (ctrl != null)
		{
			base.title = GetControllerCaption(base.controller);
			base.description = GetControllerDescription(base.controller);
			units = GetControllerUnits(base.controller);
			value = GetControllerValue(base.controller);
			base.titleDescription = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(ctrl.name);
		}
		else
		{
			isLocked = true;
		}
		m_initialized = true;
		base.isShortItem = isShortItem;
	}

	public DynoParamsItemViewModel(UIDynostandContext context, CarProfileController ctrl, int id, int sort, bool isShortItem = false)
	{
		m_dynoContext = context;
		base.controller = ctrl;
		this.id = id;
		this.sort = sort;
		if (ctrl != null)
		{
			base.title = GetControllerCaption(base.controller);
			base.description = GetControllerDescription(base.controller);
			units = GetControllerUnits(base.controller);
			value = GetControllerValue(base.controller);
			base.titleDescription = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(ctrl.name);
		}
		else
		{
			isLocked = true;
		}
		m_initialized = true;
		m_dynoContext.navigationEvent = delegate
		{
			InputManager.PlayClickSound();
		};
		base.isShortItem = isShortItem;
	}

	public override void OnClick()
	{
		m_dynoContext.OnClick(this);
	}

	public override void OnDoubleClick()
	{
		TryShowInputDialog();
	}

	public bool CanAquireInputDialog()
	{
		if (base.controller != null && !IsCtrlNameEqualsKeyDiffType(base.controller) && !IsToeCamberItem() && !IsBoolDynoParameter())
		{
			return !GameManager.IsLinuxOsPlatform();
		}
		return false;
	}

	public void TryShowInputDialog()
	{
		if (CanAquireInputDialog())
		{
			UIMessageBox.ShowInputDialog(LocalizationKeys.dyno_input_value_text.LocalizedFormat(base.titleDescription, units), GetControllerOutputValue(base.controller, forceTrimOutput: true), null, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.Ok.Localized());
			m_dynoContext.UpdateLists();
		}
	}

	private void OnReceiveInputText(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			text = text.Replace(" ", "").Replace(",", ".");
			if (float.TryParse(text, out var result))
			{
				SetControllerOriginalValue(base.controller, result);
				value = GetControllerValue(base.controller);
				SetValue(value);
			}
		}
	}

	public override int CompareTo(BaseDynoParamsItemViewModel other)
	{
		return sort.CompareTo((other as DynoParamsItemViewModel)?.sort);
	}

	private void SetValue(float newValue)
	{
		SetControllerNormalizedValue(base.controller, newValue);
		outputValue = GetControllerOutputValue(base.controller);
		m_dynoContext.DirtyChanges();
	}

	private string GetControllerCaption(CarProfileController profileController)
	{
		return LocalizationKeys.carProfileParameterCaptionDyno.FormatLocalized(profileController.name);
	}

	private string GetControllerDescription(CarProfileController profileController)
	{
		if (profileController.type == CarProfileControllerType.Transmission && profileController.name.StartsWith("gearbox"))
		{
			return string.Empty;
		}
		return LocalizationKeys.car_profile_prm_description_.FormatLocalized(profileController.name);
	}

	private string GetControllerUnits(CarProfileController profileController)
	{
		if (profileController.isGroup || profileController.isPercent)
		{
			return "%";
		}
		if (profileController.changers[0].parameter.units != "none")
		{
			return LocalizationKeys.car_profile_prm_units_.FormatLocalized(profileController.changers[0].parameter.units);
		}
		return string.Empty;
	}

	private float GetControllerDefValue(CarProfileController profileController)
	{
		return profileController.changers[0].EvaluatePhase(profileController.changers[0].defaultValue + profileController.changers[0].shift);
	}

	private float GetControllerValue(CarProfileController profileController)
	{
		ProfilesInfo.Profile profile = null;
		if (m_dynoContext != null)
		{
			profile = m_dynoContext.profileInfoPreview;
		}
		else if (m_setupsSharingContext != null)
		{
			profile = m_setupsSharingContext.profileInfoPreview;
		}
		if (profile == null)
		{
			return GetControllerDefValue(profileController);
		}
		if (!profile.controllers.TryGetValue(profileController.name, out var result))
		{
			return GetControllerDefValue(profileController);
		}
		return result;
	}

	public void SetControllerNormalizedValue(CarProfileController profileController, float normalizedValue)
	{
		m_dynoContext.profileInfoPreview.controllers[profileController.name] = normalizedValue;
	}

	private void SetControllerOriginalValue(CarProfileController profileController, float newValue)
	{
		if (profileController.isGroup || profileController.isPercent)
		{
			SetControllerNormalizedValue(profileController, newValue * 0.01f);
			return;
		}
		float num = newValue / base.controller.changers[0].multiplier + base.controller.changers[0].shift;
		SetControllerNormalizedValue(base.controller, base.controller.changers[0].EvaluatePhase(num));
	}

	public string GetControllerOutputValue(CarProfileController profileController, bool forceTrimOutput = false, float? overrideValue = null)
	{
		if (profileController == null)
		{
			return string.Empty;
		}
		float num = (overrideValue.HasValue ? overrideValue.Value : GetControllerValue(profileController));
		if (IsCtrlNameEqualsKeyDiffType(profileController))
		{
			DiffType diffType = (DiffType)profileController.changers[0].EvaluateValue(num);
			return LocalizationService.Localize(string.Format(LocalizationKeys.car_profile_diff_type.KeyValue, diffType.ToString().ToLower()));
		}
		if (profileController.changers[0].parameter.id == 63)
		{
			return DrivetrainSwitchDynoParamViewModel.GetDrivetrainTitle(Mathf.RoundToInt(profileController.changers[0].EvaluateValue(num)));
		}
		if (profileController.changers[0].parameter.id == 65)
		{
			if (!(num > 0.5f))
			{
				return LocalizationKeys.car_profile_standart_transmission.Localized();
			}
			return LocalizationKeys.car_profile_seq_transmission.Localized();
		}
		if (profileController.isGroup || profileController.isPercent)
		{
			return (num * 100f).ToString("#,0", UIUtils.numbersFormatter);
		}
		if (IsBoolDynoParameter())
		{
			if (!(num > 0.5f))
			{
				return LocalizationKeys.car_profile_prm_units_no.Localized();
			}
			return LocalizationKeys.car_profile_prm_units_yes.Localized();
		}
		float num2 = profileController.changers[0].EvaluateValue(num);
		string format = profileController.changers[0].parameter.format;
		string text = (num2 * profileController.changers[0].multiplier).ToString(format, UIUtils.numbersFormatter);
		if (!forceTrimOutput)
		{
			return text;
		}
		return text?.Replace(" ", "");
	}

	public bool IsBoolDynoParameter()
	{
		if (base.controller == null)
		{
			return false;
		}
		if (m_baseModel == null)
		{
			m_baseModel = DependencyInjector.Resolve<BaseModel>();
		}
		if (m_carProfileParameterMeta == null)
		{
			m_carProfileParameterMeta = m_baseModel.GetById<CarProfileParameterMeta>(base.controller.changers[0].parameterId);
		}
		return m_carProfileParameterMeta.isBool;
	}
}
