using Binding;
using Binding.Elements;

public class PostcombatListItem : BaseListElement
{
	public class Data : BaseListElementData
	{
		public string title;

		public string value;
	}

	public readonly StringProperty title = new StringProperty();

	public readonly StringProperty value = new StringProperty();

	protected override void OnDataChange(BaseListElementData newData)
	{
		base.OnDataChange(newData);
		if (newData is Data data)
		{
			title.value = data.title;
			value.value = data.value;
		}
	}
}
