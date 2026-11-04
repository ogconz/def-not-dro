using Binding;
using GameInput;

namespace UI._UGUI_
{
	[GameHotkeyBlocker(true)]
	[RaycastBlocker(true)]
	public class UIRewardReceivedDialogContext : UIBaseDialogContext
	{
		public readonly StringProperty rewardVendorDescription = new StringProperty();

		public readonly StringProperty rewardDescription = new StringProperty();

		public readonly SpriteProperty rewardSprite = new SpriteProperty();

		public readonly BoolProperty showCarReward = new BoolProperty();

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
}
