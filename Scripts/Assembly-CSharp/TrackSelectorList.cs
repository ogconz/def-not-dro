public class TrackSelectorList : ListView<TrackSelectorListItem, TrackSelectorItemController>
{
	public TrackSubItem NavigationUp()
	{
		int num = ResetSelectionSubItem();
		if (num < 0)
		{
			num = 0;
		}
		selectedIndex--;
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}
		TrackSelectorListItem selectedItem = GetSelectedItem();
		if (selectedItem != null)
		{
			selectedItem.selectionIndex = num;
		}
		UpdateList();
		return selectedItem?.GetSelectedSubItem();
	}

	public TrackSubItem NavigationDown()
	{
		int num = ResetSelectionSubItem();
		if (num < 0)
		{
			num = 0;
		}
		selectedIndex++;
		if (selectedIndex >= GetCount())
		{
			selectedIndex = GetCount() - 1;
		}
		TrackSelectorListItem selectedItem = GetSelectedItem();
		if (selectedItem != null)
		{
			selectedItem.selectionIndex = num;
		}
		for (int num2 = selectedItem.selectionIndex; num2 >= 0; num2--)
		{
			selectedItem.selectionIndex = num2;
			if (selectedItem.GetSelectedSubItem() != null)
			{
				break;
			}
		}
		UpdateList();
		return selectedItem?.GetSelectedSubItem();
	}

	public TrackSubItem NavigationLeft()
	{
		TrackSelectorListItem selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			return null;
		}
		selectedItem.selectionIndex--;
		if (selectedItem.selectionIndex < 0)
		{
			selectedItem.selectionIndex = 0;
		}
		UpdateList();
		return selectedItem.GetSelectedSubItem();
	}

	public TrackSubItem NavigationRight()
	{
		TrackSelectorListItem selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			return null;
		}
		selectedItem.selectionIndex++;
		if (selectedItem.selectionIndex > 4)
		{
			selectedItem.selectionIndex = 4;
		}
		for (int num = selectedItem.selectionIndex; num >= 0; num--)
		{
			selectedItem.selectionIndex = num;
			if (selectedItem.GetSelectedSubItem() != null)
			{
				break;
			}
		}
		UpdateList();
		return selectedItem.GetSelectedSubItem();
	}

	private int ResetSelectionSubItem()
	{
		TrackSelectorListItem selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			return 0;
		}
		int selectionIndex = selectedItem.selectionIndex;
		selectedItem.selectionIndex = -1;
		UpdateList();
		return selectionIndex;
	}
}
