public interface IListView<T> where T : BaseListItem
{
	void Reselect(T item);

	int GetTopIndex();

	int GetCount();

	void Clear();

	T GetItem(int index);

	T GetSelectedItem();

	int GetSelectedIndex();

	int IndexOf(T item);

	bool OnClick(int index);

	void OnDoubleClick(int index);

	float GetScroll();

	void ScrollTo(int index);

	void ScrollTo(T item);

	void ClearSelection();

	void RecalcList();
}
