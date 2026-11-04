using Binding;
using Binding.Base;

public class ButtonItemController : BaseItemController<ButtonListItem>
{
	public Property<string> title = new StringProperty();

	public Property<bool> isPressed = new BoolProperty();

	public Property<bool> needHide = new BoolProperty();

	private ButtonListItem m_prevItem;

	public override void OnClick()
	{
		base.OnClick();
		if (m_item != null)
		{
			m_item.OnSelectButton();
		}
	}

	public override void SetItem(ButtonListItem listItem)
	{
		base.SetItem(listItem);
		if (m_prevItem != base.item)
		{
			m_prevItem = base.item;
		}
		if (listItem != null)
		{
			title.value = listItem.title;
			isPressed.value = listItem.isPressed;
			needHide.value = listItem.needHide;
		}
	}
}
