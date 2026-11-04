using UnityEngine;

[AddComponentMenu("Scripts/States/PleaseWait")]
public class PleaseWaitState : BaseMenuState
{
	public static void ShowPleaseWait()
	{
		if (!(States.current is PleaseWaitState))
		{
			States.Push<PleaseWaitState>();
		}
	}

	public static void HidePleaseWait()
	{
		if (States.current is PleaseWaitState)
		{
			States.Pop();
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		UIWaitDialogContext.ShowPleaseWait(base.contextOwner);
	}

	public override void OnExit()
	{
		base.OnExit();
		UIWaitDialogContext.Close(base.contextOwner);
	}

	protected override void OnProcessEscape()
	{
	}
}
