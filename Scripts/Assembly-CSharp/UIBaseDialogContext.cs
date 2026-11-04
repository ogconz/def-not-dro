using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
public class UIBaseDialogContext : BaseBindingBehaviourTarget
{
	public System.Action actionCancel;

	public readonly Property<string> title = new Property<string>();

	public readonly Property<bool> allowClose = new Property<bool>(startValue: false);

	public readonly Property<int> indexNavigation = new IntProperty();

	protected int m_defaultIndexNavigation;

	public Transform btnsContainer;

	public List<DialogButton> buttons { get; } = new List<DialogButton>();

	public void BuildDialog(DialogButtonData[] data, Transform container = null)
	{
		if (container == null)
		{
			container = btnsContainer;
		}
		if (container == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "Not found btns container for dialog " + base.gameObject.name);
			return;
		}
		if (data == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "Data is empty for " + base.gameObject.name);
			return;
		}
		buttons.Clear();
		DestroyButtonsContainer();
		GameObject original = ResourcesWrapper.Load<GameObject>("Prefabs/UI/Common/DialogButton");
		foreach (DialogButtonData data2 in data)
		{
			DialogButton component = UnityEngine.Object.Instantiate(original, container).GetComponent<DialogButton>();
			if (!(component == null))
			{
				component.SetData(this, data2);
				buttons.Add(component);
			}
		}
		indexNavigation.OnValueChanged -= UpdateSelection;
		indexNavigation.OnValueChanged += UpdateSelection;
		indexNavigation.value = -1;
		BringForwardByDepth();
	}

	public void DestroyButtonsContainer()
	{
		if (!(this == null) && !(base.gameObject == null) && !(btnsContainer == null))
		{
			for (int num = btnsContainer.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(btnsContainer.GetChild(num).gameObject);
			}
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		TimeController.StartNow(DefferedUpdateVisualSelection(m_defaultIndexNavigation));
	}

	private IEnumerator DefferedUpdateVisualSelection(int index)
	{
		yield return new WaitForEndOfFrame();
		indexNavigation.value = index;
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		m_gameManager.isUnityUIActive = true;
	}

	protected void UpdateSelection()
	{
		if (indexNavigation.value >= 0 && indexNavigation.value < buttons.Count)
		{
			ResetSelection();
			buttons[indexNavigation.value].selected.value = true;
		}
	}

	protected void ResetSelection()
	{
		foreach (DialogButton button in buttons)
		{
			button.selected.value = false;
		}
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	[GameHotkey(ActionType.NavDPadLeft, false, false)]
	public virtual void NavigateLeft()
	{
		indexNavigation.value--;
		if (indexNavigation.value < 0)
		{
			indexNavigation.value = 0;
		}
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	[GameHotkey(ActionType.NavDPadRight, false, false)]
	public virtual void NavigateRight()
	{
		indexNavigation.value++;
		if (indexNavigation.value >= buttons.Count)
		{
			indexNavigation.value = buttons.Count - 1;
		}
	}

	[GameHotkey(ActionType.NavSelectBtnA, true, false)]
	public virtual void OnClickOk()
	{
		if (indexNavigation.value >= 0 && indexNavigation.value < buttons.Count)
		{
			buttons[indexNavigation.value].OnClick();
		}
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public virtual void OnClickCancel()
	{
		actionCancel.SafeCall();
	}
}
