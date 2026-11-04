public class ToggleListSettingsItem : BaseListSettingsItem
{
	private bool m_InitialValue;

	private LocalizationKeys.Key m_trueValueTitle = LocalizationKeys.control_on;

	private LocalizationKeys.Key m_falseValueTitle = LocalizationKeys.control_off;

	public bool value { get; set; }

	public bool isEnableToEditing { get; private set; } = true;

	private void CheckSwitcher()
	{
		base.canNext = !value;
		base.canPrev = value;
	}

	public override float GetTrackValue()
	{
		if (!value)
		{
			return 0f;
		}
		return 1f;
	}

	public override string GetCurrentValueTitle()
	{
		if (isEnableToEditing)
		{
			CheckSwitcher();
		}
		return (value ? m_trueValueTitle : m_falseValueTitle).Localized();
	}

	public override void NextValue()
	{
		if (isEnableToEditing)
		{
			value = true;
			CheckSwitcher();
			base.NextValue();
		}
	}

	public override void PrevValue()
	{
		if (isEnableToEditing)
		{
			value = false;
			CheckSwitcher();
			base.PrevValue();
		}
	}

	public override void StoreInitialValue()
	{
		m_InitialValue = value;
	}

	public override void ReturnToInitialValue()
	{
		value = m_InitialValue;
	}

	public bool GetInitialValue()
	{
		return m_InitialValue;
	}

	public void SetEditingPermission(bool isPermitted)
	{
		isEnableToEditing = isPermitted;
		if (!isEnableToEditing)
		{
			base.canNext = false;
			base.canPrev = false;
		}
		else
		{
			CheckSwitcher();
		}
	}
}
