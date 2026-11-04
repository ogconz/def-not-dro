using Binding;
using GameInput;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UINewItemDialogContext : UIBaseDialogContext
{
	public readonly StringProperty itemName = new StringProperty();

	public readonly StringProperty itemsCount = new StringProperty();

	public readonly SpriteProperty itemSprite = new SpriteProperty();

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
