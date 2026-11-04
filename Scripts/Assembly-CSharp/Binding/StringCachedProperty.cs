using System.Collections.Generic;

namespace Binding
{
	public class StringCachedProperty<T> : StringProperty
	{
		private T m_cachedValue;

		public void SetCached(T value)
		{
			if (!EqualityComparer<T>.Default.Equals(m_cachedValue, value))
			{
				m_cachedValue = value;
				SetValue(value.ToString());
			}
		}

		public bool UpdateCache(T value)
		{
			if (EqualityComparer<T>.Default.Equals(m_cachedValue, value))
			{
				return false;
			}
			m_cachedValue = value;
			return true;
		}
	}
}
