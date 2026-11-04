public class BaseListItem : IBaseListItem
{
	public float offsetIndex;

	public float defaultWidth;

	public float defaultHeight;

	public float width;

	public float height;

	public virtual void OnClick()
	{
	}

	public virtual void OnDoubleClick()
	{
	}
}
