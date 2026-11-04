using System.Collections.Generic;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching
{
	public class HashSetHolder<T> : IValueHolder<HashSet<T>>
	{
		private HashSet<T> m_values = new HashSet<T>();

		public HashSet<T> value => m_values;

		public bool Add(HashSet<T> values)
		{
			if (!CanAdd(values))
			{
				return false;
			}
			m_values.UnionWith(values);
			return true;
		}

		public virtual bool CanAdd(HashSet<T> value)
		{
			return true;
		}
	}
}
