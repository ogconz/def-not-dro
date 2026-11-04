namespace Binding
{
	public abstract class BaseIdentifiedListElementData : BaseListElementData
	{
		protected int m_id;

		public virtual int id => m_id;
	}
}
