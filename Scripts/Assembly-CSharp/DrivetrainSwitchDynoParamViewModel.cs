using CarProfile;
using CarX;
using UnityEngine;

public class DrivetrainSwitchDynoParamViewModel : BaseDynoParamsItemViewModel
{
	private float m_value;

	public override float value
	{
		get
		{
			return m_value;
		}
		set
		{
			m_value = value;
			SetControllerNormalizedValue(base.controller, m_value);
			m_dynoContext.DirtyChanges();
		}
	}

	public override string outputValue => GetDrivetrainTitle(Mathf.RoundToInt(GetOutputValue(base.controller)));

	public DrivetrainSwitchDynoParamViewModel(UIDynostandContext context, CarProfileController ctrl)
	{
		m_dynoContext = context;
		base.controller = ctrl;
		base.title = LocalizationKeys.carProfileParameterCaptionDyno.FormatLocalized(base.controller.name);
		base.description = LocalizationKeys.car_profile_prm_description_.FormatLocalized(base.controller.name);
		base.titleDescription = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(base.controller.name);
		value = GetControllerValue(base.controller);
	}

	public static GearType GetDrivetrainType(int type)
	{
		return type switch
		{
			1 => GearType.GEAR_AWD, 
			2 => GearType.GEAR_4WD, 
			_ => GearType.GEAR_RWD, 
		};
	}

	public static string GetDrivetrainTitle(int type)
	{
		return type switch
		{
			1 => "AWD", 
			2 => "4WD", 
			_ => "RWD", 
		};
	}

	public override void OnClick()
	{
		m_dynoContext.OnClick(this);
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

	private float GetControllerDefValue(CarProfileController profileController)
	{
		return profileController.changers[0].EvaluatePhase(profileController.changers[0].defaultValue + profileController.changers[0].shift);
	}

	private void SetControllerNormalizedValue(CarProfileController profileController, float normalizedValue)
	{
		m_dynoContext.profileInfoPreview.controllers[profileController.name] = normalizedValue;
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

	public float GetOutputValue(CarProfileController profileController)
	{
		float controllerValue = GetControllerValue(profileController);
		return profileController.changers[0].EvaluateValue(controllerValue);
	}

	public bool IsIndexValid(float index)
	{
		if (index >= base.controller.changers[0].values[0])
		{
			return index <= base.controller.changers[0].values[1];
		}
		return false;
	}

	public override int CompareTo(BaseDynoParamsItemViewModel other)
	{
		return -1;
	}
}
