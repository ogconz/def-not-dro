using Binding.Base;

public class TrackMetaConfigItemController : BaseItemController<TrackConfigMetaItem>
{
	public readonly Property<string> text = new Property<string>();

	protected override void Initialize()
	{
		base.Initialize();
	}

	public override void OnClick()
	{
		if (m_item != null)
		{
			m_item.OnClick();
		}
		base.OnClick();
	}

	public override void OnDoubleClick()
	{
		if (m_item != null)
		{
			m_item.OnDoubleClick();
		}
		base.OnDoubleClick();
	}

	public override void SetItem(TrackConfigMetaItem newItem)
	{
		base.SetItem(newItem);
		if (newItem != null)
		{
			text.value = newItem.text;
		}
	}
}
