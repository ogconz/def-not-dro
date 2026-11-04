using Binding;
using Binding.Base;

public class FreeCamItemView : BaseItemController<FreeCamItemViewModel>
{
	public Property<string> title = new StringProperty();

	public override void SetItem(FreeCamItemViewModel newItem)
	{
		if (newItem != null)
		{
			title.value = newItem.title;
		}
		base.SetItem(newItem);
	}

	public override void OnClick()
	{
		m_item?.OnClick();
	}

	public void SelfRemove()
	{
		m_item?.SelfRemove();
	}
}
