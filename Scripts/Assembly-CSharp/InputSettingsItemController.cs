using Binding;
using Binding.Base;
using Binding.Components;
using Rewired;
using UnityEngine;

public class InputSettingsItemController : BaseItemController<InputSettingsListItem>
{
	[SerializeField]
	private RectTransform m_controllerRect;

	public Property<string> title = new StringProperty();

	public Property<string> bindingName = new StringProperty();

	public Property<Sprite> bindingSprite = new SpriteProperty();

	public Property<int> selectedIndex = new IntProperty(-1);

	public Property<bool> mouseAdditional = new BoolProperty(false);

	public Property<bool> standaloneBuild = new BoolProperty(false);

	public Property<bool> showBindingSprite = new BoolProperty(false);

	public Property<bool> showBindingText = new BoolProperty(false);

	public Property<bool> showWiderBG = new BoolProperty(false);

	public Property<bool> showRemoveBtn = new BoolProperty();

	private SettingsInputList m_controllerInput;

	public readonly Property<bool> isAxisBind = new BoolProperty();

	public SettingsInputList controllerInput
	{
		get
		{
			if (m_controllerInput == null && m_controller != null)
			{
				m_controllerInput = m_controller as SettingsInputList;
			}
			return m_controllerInput;
		}
	}

	protected override void Start()
	{
		base.Start();
		standaloneBuild.value = true;
	}

	public override void SetItem(InputSettingsListItem newItem)
	{
		if (newItem == null)
		{
			m_item = null;
			return;
		}
		if (newItem.useWideViewItem)
		{
			SetWideSettingsItem(newItem);
		}
		base.SetItem(newItem);
		title.value = newItem.title;
		bindingName.value = newItem.GetElementName();
		Sprite spriteBindElement = ActionElementBinding.GetSpriteBindElement(newItem.GetElement());
		bindingSprite.value = spriteBindElement;
		if (m_item.context.controller.type == ControllerType.Keyboard && !string.IsNullOrEmpty(bindingName.value))
		{
			showBindingText.value = true;
			showBindingSprite.value = false;
		}
		else
		{
			showBindingSprite.value = spriteBindElement != null;
			showBindingText.value = !showBindingSprite.value;
		}
		mouseAdditional.value = m_item.context.controller.type == ControllerType.Mouse;
		isAxisBind.value = m_item.bind?.elementAxis != null;
	}

	public override void SetSelected(bool value)
	{
		base.SetSelected(value);
		UpdateRemoveBtnVisibility();
	}

	private void UpdateRemoveBtnVisibility()
	{
		showRemoveBtn.value = (!string.IsNullOrEmpty(bindingName.value) || showBindingSprite.value) && selected.value;
	}

	public void PollBind()
	{
		if (m_item != null)
		{
			if (controllerInput != null)
			{
				controllerInput.selectedIndex = base.index;
			}
			m_item.PollBind();
		}
	}

	public void ShowCalibration()
	{
		m_item?.ShowCalibration();
	}

	public void ClearBind()
	{
		m_item?.ClearBind();
	}

	public override void OnClick()
	{
		base.OnClick();
		if (m_item != null && m_item.context != null)
		{
			m_item.context.OnClickSettingsItem();
		}
	}

	public override void OnDoubleClick()
	{
		base.OnDoubleClick();
		PollBind();
	}

	private void RenameBind(string from, string to)
	{
		if (bindingName.value.Equals(from))
		{
			bindingName.value = to;
		}
	}

	private void SetWideSettingsItem(InputSettingsListItem newItem)
	{
		showWiderBG.value = true;
		m_controllerRect.SetWidth(controllerInput.consoleContainerPresetWidth - 10f);
		newItem.width = controllerInput.consoleContainerPresetWidth - 10f;
	}
}
