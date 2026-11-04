using DI;
using GameInput;

[GameHotkeyBlocker(true)]
public class UIInfoDialogContext : UIBaseDialogContext
{
	protected override void OnActivate()
	{
		base.OnActivate();
		indexNavigation.value = 0;
		BringForwardByDepth();
		NavigationInputModule.instance.enabled = false;
	}

	protected override void OnDeactivate()
	{
		NavigationInputModule.instance.enabled = true;
		base.OnDeactivate();
	}

	private void Save()
	{
		DependencyInjector.Resolve<GamePrefs>().globals.newsInfoVersion = GameVersion.version;
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public override void OnClickOk()
	{
		Save();
		Hide();
		base.OnClickOk();
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public override void OnClickCancel()
	{
		Save();
		actionCancel.SafeCall();
		Hide();
		base.OnClickCancel();
	}

	public static void Show(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIInfoDialog.Show", "owner");
		}
		else if (DependencyInjector.Resolve<GamePrefs>().globals.newsInfoVersion != GameVersion.version)
		{
			UIInfoDialogContext uIInfoDialogContext = owner.Get<UIInfoDialogContext>();
			if (!(uIInfoDialogContext != null) || !uIInfoDialogContext.gameObject.activeSelf)
			{
				uIInfoDialogContext = owner.Push<UIInfoDialogContext>();
				uIInfoDialogContext.BringForwardByDepth();
			}
		}
	}
}
