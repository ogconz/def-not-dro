using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine.UI;

public class DialogButton : BaseBindingBehaviourTarget
{
	private UIBaseDialogContext m_context;

	public Property<string> text = new StringProperty();

	public Property<bool> selected = new BoolProperty();

	private System.Action m_clickAction;

	private Image m_buttonImage;

	private void Awake()
	{
		Button componentInChildren = GetComponentInChildren<Button>(includeInactive: true);
		if (componentInChildren != null)
		{
			m_buttonImage = componentInChildren.image;
		}
	}

	public void SetData(UIBaseDialogContext context, DialogButtonData data)
	{
		m_context = context;
		text.value = data.text;
		m_clickAction = data.action;
		selected.value = false;
	}

	public void OnClick()
	{
		if ((bool)InputManager.instance)
		{
			InputManager.instance.PlayClickEffect(m_buttonImage, OnAction, skipPauseToAction: false);
		}
		else
		{
			OnAction();
		}
	}

	public void OnAction()
	{
		m_clickAction.SafeCall();
	}
}
