using System;

public class CustomToggleDynoParamsItemViewModel : BaseDynoParamsItemViewModel
{
	private string m_tag;

	private bool m_defaultValue;

	private Action<bool> m_setValueAction;

	private Func<bool> m_getValueAction;

	private string m_positiveKey = LocalizationKeys.car_profile_prm_units_yes.Localized();

	private string m_negativeKey = LocalizationKeys.car_profile_prm_units_no.Localized();

	public override float value
	{
		get
		{
			float result = 0f;
			if (m_getValueAction != null)
			{
				result = (m_getValueAction() ? 1f : 0f);
			}
			return result;
		}
		set
		{
			bool obj = (double)value >= 0.5;
			if (m_setValueAction != null)
			{
				m_setValueAction(obj);
			}
		}
	}

	public override string outputValue
	{
		get
		{
			if (!m_getValueAction())
			{
				return m_negativeKey;
			}
			return m_positiveKey;
		}
	}

	public override void RevertParameter()
	{
		value = (m_defaultValue ? 1f : 0f);
	}

	public override void RevertToDefault()
	{
		RevertParameter();
	}

	public override void OnClick()
	{
		m_dynoContext.OnClick(this);
	}

	public CustomToggleDynoParamsItemViewModel(UIDynostandContext context, string tag, string localizationKey, Func<bool> getValueAction, Action<bool> setValueAction, string positiveKey = "", string negativeKey = "", bool defaultValue = true)
	{
		m_dynoContext = context;
		base.title = LocalizationKeys.carProfileParameterCaptionDyno.FormatLocalized(localizationKey);
		base.titleDescription = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(localizationKey);
		base.description = LocalizationKeys.car_profile_prm_description_.FormatLocalized(localizationKey);
		m_tag = tag;
		m_defaultValue = defaultValue;
		m_setValueAction = setValueAction;
		m_getValueAction = getValueAction;
		if (positiveKey != "" && negativeKey != "")
		{
			m_positiveKey = positiveKey;
			m_negativeKey = negativeKey;
		}
	}
}
