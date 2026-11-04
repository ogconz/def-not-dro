using System;

namespace Binding.Base
{
	public abstract class Property : BaseProperty
	{
		public abstract string ValueToString();
	}
	public class Property<T> : Property
	{
		private T m_value;

		private Type m_type;

		private bool m_changing;

		private readonly bool m_isValue;

		public T value
		{
			get
			{
				return GetValue();
			}
			set
			{
				SetValue(value);
			}
		}

		public Property(T startValue = default(T))
		{
			m_value = startValue;
			m_type = typeof(T);
			m_isValue = m_type.IsValueType;
		}

		protected virtual void SetValue(T value)
		{
			if (!m_changing)
			{
				m_changing = true;
				bool flag = false;
				if ((!m_isValue) ? ((value == null && m_value != null) || (value != null && m_value == null) || (m_value != null && IsClassDifferent(value))) : IsValueDifferent(value))
				{
					m_value = value;
					ValueChanged();
				}
				m_changing = false;
			}
		}

		public override object GetObjectValue()
		{
			return GetValue();
		}

		protected virtual T GetValue()
		{
			return m_value;
		}

		protected virtual bool IsValueDifferent(T value)
		{
			return !m_value.Equals(value);
		}

		private bool IsClassDifferent(T value)
		{
			return !m_value.Equals(value);
		}

		public override string ValueToString()
		{
			if (value != null)
			{
				return value.ToString();
			}
			return null;
		}
	}
}
