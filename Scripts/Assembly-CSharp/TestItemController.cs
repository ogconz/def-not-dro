using Binding.Base;
using UnityEngine;

public class TestItemController : BaseItemController<TestListItem>
{
	public Property<string> title = new Property<string>("");

	public Property<string> sizeText = new Property<string>("");

	private TestListItem _currentListItem;

	public override void SetItem(TestListItem listItem)
	{
		_currentListItem = listItem;
		title.value = listItem.title;
		sizeText.value = listItem.width + ":" + listItem.height;
		base.SetItem(listItem);
	}

	public override void OnClick()
	{
		if (_currentListItem != null)
		{
			Vector2 vector = new Vector2(_currentListItem.width, (_currentListItem.height > 41f) ? 40f : 80f);
			sizeText.value = vector.x + ":" + vector.y;
		}
	}
}
