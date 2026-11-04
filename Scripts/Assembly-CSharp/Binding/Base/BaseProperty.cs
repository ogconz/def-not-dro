using System;
using System.Collections.Generic;

namespace Binding.Base
{
	public abstract class BaseProperty
	{
		private string m_instanceName = "";

		private static Type m_basePropertyInterfaceType = typeof(IBindingTarget);

		private static Dictionary<Type, bool> m_cachedInterfaceExisting = new Dictionary<Type, bool>(50);

		public string instanceName => m_instanceName;

		public event Action OnValueChanged;

		protected void ValueChanged()
		{
			this.OnValueChanged.Call();
		}

		public void ForceValueChanged()
		{
			ValueChanged();
		}

		public abstract object GetObjectValue();

		public static bool IsHaveIBindingTargetInterface(Type type)
		{
			if (!m_cachedInterfaceExisting.TryGetValue(type, out var value))
			{
				value = m_basePropertyInterfaceType.IsAssignableFrom(type);
				m_cachedInterfaceExisting[type] = value;
			}
			return value;
		}
	}
}
