using System;

public class ButtonListSettingsItem : BaseListSettingsItem
{
	public Action onSubmit;

	public void OnSubmit()
	{
		onSubmit.SafeCall();
	}

	public override void OnClick()
	{
		onSubmit.SafeCall();
	}

	public override string GetCurrentValueTitle()
	{
		return description;
	}
}
