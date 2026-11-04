using System;

public interface IBaseListController
{
	Action<int> onClick { get; set; }

	Action<IBaseListItem> onAddItem { get; set; }

	Action<IBaseListItem> onRemoveItem { get; set; }

	Action onChangeList { get; set; }

	void Init();

	int GetCount();

	IBaseListItem GetItem(int index);
}
