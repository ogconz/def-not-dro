using Binding;
using GameInput;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIMessageWithTitleContext : UIBaseDialogContext
{
	public readonly StringProperty messageTitle = new StringProperty();

	public readonly StringProperty messageText = new StringProperty();

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public override void OnClickOk()
	{
		base.OnClickOk();
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public override void OnClickCancel()
	{
		base.OnClickCancel();
	}
}
