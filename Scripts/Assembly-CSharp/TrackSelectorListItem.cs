public class TrackSelectorListItem : BaseListItem
{
	private int m_selectionIndex = -1;

	public TrackSubItem[] subItems { get; } = new TrackSubItem[4];

	public int selectionIndex
	{
		get
		{
			return m_selectionIndex;
		}
		set
		{
			m_selectionIndex = value;
			for (int i = 0; i < subItems.Length; i++)
			{
				subItems[i]?.SetSelected(i == value);
			}
		}
	}

	public TrackSubItem GetSelectedSubItem()
	{
		if (selectionIndex < 0 || selectionIndex >= subItems.Length)
		{
			return null;
		}
		return subItems[selectionIndex];
	}

	public void SetSubItem(int index, TrackSubItem subItem)
	{
		if (index >= 0 && index < subItems.Length)
		{
			subItems[index] = subItem;
		}
	}

	public void LoadResources()
	{
		TrackSubItem[] array = subItems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.LoadResources();
		}
	}

	public void UnloadResources()
	{
		TrackSubItem[] array = subItems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.UnloadResources();
		}
	}

	public void UpdatePreview()
	{
		TrackSubItem[] array = subItems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.UpdatePreview();
		}
	}

	public override void OnClick()
	{
		base.OnClick();
	}
}
