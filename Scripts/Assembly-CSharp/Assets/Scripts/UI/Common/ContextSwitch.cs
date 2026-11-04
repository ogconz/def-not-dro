namespace Assets.Scripts.UI.Common
{
	public class ContextSwitch
	{
		private readonly ContextOwner m_contextOwner;

		private BaseContext m_currentContext;

		public ContextSwitch(ContextOwner contextOwner)
		{
			m_contextOwner = contextOwner;
		}

		public T Push<T>() where T : BaseContext
		{
			if (m_currentContext != null)
			{
				m_currentContext.Hide();
			}
			m_currentContext = m_contextOwner.Push<T>();
			return (T)m_currentContext;
		}
	}
}
