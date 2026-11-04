using System.Collections.Generic;
using Binding;
using GameInput;
using UnityEngine;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIPrivacyPolicyBaseDialogContext : UIBaseDialogContext
{
	[SerializeField]
	protected Transform m_buttonsLinksContainer;

	protected BoolProperty m_visibleButtonsLink = new BoolProperty(false);

	protected StringProperty m_titleText = new StringProperty();

	protected StringProperty m_contentText = new StringProperty();

	protected List<DialogButtonData> m_buttonsDialog = new List<DialogButtonData>();

	protected List<DialogButtonData> m_buttonsDialogLinks = new List<DialogButtonData>();

	protected bool m_isLinkButtonsPlaced => m_buttonsDialogLinks.Count > 0;

	public void SetDialogData(string title, string content, List<DialogButtonData> buttons, List<DialogButtonData> links)
	{
		m_buttonsDialog.Clear();
		m_buttonsDialogLinks.Clear();
		m_visibleButtonsLink.value = links.Count > 0;
		m_titleText.value = title;
		m_contentText.value = content;
		m_buttonsDialog.AddRange(buttons);
		m_buttonsDialogLinks.AddRange(links);
		CreateDialog();
	}

	protected void CreateDialog()
	{
		if (m_buttonsDialog.Count > 0)
		{
			BuildDialog(m_buttonsDialog.ToArray());
		}
		if (m_buttonsDialogLinks.Count > 0)
		{
			AddButtonsDialog(m_buttonsDialogLinks.ToArray(), m_buttonsLinksContainer);
		}
		indexNavigation.value = 0;
	}

	private void AddButtonsDialog(DialogButtonData[] data, Transform container)
	{
		for (int num = container.childCount - 1; num >= 0; num--)
		{
			Object.Destroy(container.GetChild(num).gameObject);
		}
		GameObject original = ResourcesWrapper.Load<GameObject>("Prefabs/UI/Common/DialogButton");
		foreach (DialogButtonData data2 in data)
		{
			DialogButton component = Object.Instantiate(original, container).GetComponent<DialogButton>();
			if (!(component == null))
			{
				component.SetData(this, data2);
				base.buttons.Add(component);
			}
		}
		BringForwardByDepth();
		UpdateSelection();
	}

	[GameHotkey(ActionType.NavUp, false, false)]
	[GameHotkey(ActionType.NavDPadUp, false, false)]
	public virtual void NavigateUp()
	{
		if (m_isLinkButtonsPlaced && indexNavigation.value < m_buttonsDialog.Count)
		{
			indexNavigation.value = Mathf.Clamp(indexNavigation.value + m_buttonsDialog.Count, 0, base.buttons.Count - 1);
		}
	}

	[GameHotkey(ActionType.NavDown, false, false)]
	[GameHotkey(ActionType.NavDPadDown, false, false)]
	public virtual void NavigateDown()
	{
		if (m_isLinkButtonsPlaced && indexNavigation.value >= m_buttonsDialog.Count)
		{
			indexNavigation.value = Mathf.Clamp(indexNavigation.value - m_buttonsDialogLinks.Count, 0, base.buttons.Count - 1);
		}
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	[GameHotkey(ActionType.NavDPadRight, false, false)]
	public override void NavigateRight()
	{
		if (indexNavigation.value != m_buttonsDialog.Count - 1)
		{
			base.NavigateRight();
		}
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	[GameHotkey(ActionType.NavDPadLeft, false, false)]
	public override void NavigateLeft()
	{
		if (indexNavigation.value != base.buttons.Count - m_buttonsDialogLinks.Count)
		{
			base.NavigateLeft();
		}
	}

	[GameHotkey(ActionType.NavSelectBtnA, true, false)]
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
