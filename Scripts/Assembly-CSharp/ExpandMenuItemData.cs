using Binding;

public class ExpandMenuItemData : BaseListElementData
{
	protected readonly IntProperty m_voteRate = new IntProperty();

	private readonly StringProperty m_title = new StringProperty();

	public ExpandMenuItemData(string title)
	{
		m_title.value = title;
	}
}
