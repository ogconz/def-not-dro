using System;
using System.Collections.Generic;
using Binding;
using GameInput;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIWaitDialogContext : UIBaseDialogContext
{
	public readonly StringProperty text = new StringProperty();

	protected override void OnActivate()
	{
		base.OnActivate();
		UpdateBottomPanel();
		BaseGameState.escapeHandlers = (Func<bool>)Delegate.Combine(BaseGameState.escapeHandlers, new Func<bool>(EscapeHandled));
		BringForwardByDepth();
	}

	protected override void OnDeactivate()
	{
		BaseGameState.escapeHandlers = (Func<bool>)Delegate.Remove(BaseGameState.escapeHandlers, new Func<bool>(EscapeHandled));
		base.OnDeactivate();
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	private void Empty()
	{
	}

	private bool EscapeHandled()
	{
		return true;
	}

	protected override bool OnEscape()
	{
		return false;
	}

	private void UpdateBottomPanel()
	{
		List<ActionHintData> list = new List<ActionHintData>();
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_exit.Localized(),
			type = ActionType.NavCancelBtnB
		});
		ShowBottomPanel(list.ToArray());
	}

	public static void ShowPleaseWait(ContextOwner owner)
	{
		Show(owner, LocalizationKeys.please_wait.Localized());
	}

	public static void Show(ContextOwner owner, string title = null, string text = null)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIWaitDialogContext.Show", "owner");
		}
		else if (!owner.IsActive<UIWaitDialogContext>())
		{
			UIWaitDialogContext uIWaitDialogContext = owner.Push<UIWaitDialogContext>();
			if (string.IsNullOrEmpty(title))
			{
				title = LocalizationKeys.please_wait.Localized();
			}
			uIWaitDialogContext.title.value = title;
			uIWaitDialogContext.text.value = text;
		}
	}

	public static void Close(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIWaitDialogContext.Close", "owner");
		}
		else if (owner.IsActive<UIWaitDialogContext>())
		{
			owner.Hide<UIWaitDialogContext>();
		}
	}

	public static bool IsShow(ContextOwner owner)
	{
		return owner.IsActive<UIWaitDialogContext>();
	}
}
