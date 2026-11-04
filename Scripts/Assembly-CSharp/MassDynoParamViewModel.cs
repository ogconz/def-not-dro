using CarProfile;

public class MassDynoParamViewModel : BaseDynoParamsItemViewModel
{
	private float m_value;

	private RaceCar m_car;

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

	public override string outputValue => GetOutputValue(base.controller);

	public string units { get; private set; }

	public MassDynoParamViewModel(UIDynostandContext context, CarProfileController ctrl, RaceCar car)
	{
		m_dynoContext = context;
		base.controller = ctrl;
		m_car = car;
		base.title = LocalizationKeys.carProfileParameterCaptionDyno.FormatLocalized(base.controller.name);
		base.description = LocalizationKeys.car_profile_prm_description_.FormatLocalized(base.controller.name);
		base.titleDescription = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(base.controller.name);
		units = LocalizationKeys.car_profile_prm_units_.FormatLocalized(base.controller.changers[0].parameter.units);
		value = GetControllerValue(base.controller);
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

	public string GetOutputValue(CarProfileController profileController)
	{
		if (m_car == null)
		{
			return string.Empty;
		}
		float num = profileController.changers[0].EvaluateOriginalValue(value);
		if (!m_car.swap.isStockEngine)
		{
			num = num - m_car.metaInfo.stockEngineWeight + m_car.swap.meta.weight;
		}
		return num.ToString("#", UIUtils.numbersFormatter);
	}

	public override int CompareTo(BaseDynoParamsItemViewModel other)
	{
		return -1;
	}
}
