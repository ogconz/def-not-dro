using System;
using System.Collections.Generic;
using DB.Meta;

namespace DB.XML
{
	public abstract class CachedWebStore<TValue, TOwerrided> : ListToDictionary<TValue>, ICachedWebStore where TValue : class, IDBMeta, new() where TOwerrided : IDBMeta
	{
		private ObjectLoader<List<TOwerrided>> m_loader;

		protected CachedWebStore(List<TValue> baseList)
			: base(baseList)
		{
		}

		private void SetLoader(ObjectLoader<List<TOwerrided>> loader)
		{
			m_loader = loader;
			ObjectLoader<List<TOwerrided>> loader2 = m_loader;
			loader2.LoadedEvent = (Action<List<TOwerrided>>)Delegate.Combine(loader2.LoadedEvent, new Action<List<TOwerrided>>(ApplyFilter));
			ObjectLoader<List<TOwerrided>> loader3 = m_loader;
			loader3.TimerExpiredEvent = (Action)Delegate.Combine(loader3.TimerExpiredEvent, new Action(NeedApplyFilter));
			ApplyFilter();
		}

		protected abstract void ApplyFilter(TValue item, TOwerrided overridedItem);

		private void ApplyFilter(List<TOwerrided> list)
		{
			if (list == null)
			{
				return;
			}
			Dictionary<int, TOwerrided> dictionary = Convert(list);
			foreach (TValue values in valuesList)
			{
				if (dictionary.ContainsKey(values.id))
				{
					TOwerrided overridedItem = dictionary[values.id];
					ApplyFilter(values, overridedItem);
				}
			}
		}

		private void ApplyFilter()
		{
			ApplyFilter(m_loader.Load());
		}

		protected abstract ObjectLoader<List<TOwerrided>> MakeLoader();

		public void ApplyWebStore()
		{
			if (m_loader == null)
			{
				SetLoader(MakeLoader());
			}
			else
			{
				ApplyFilter();
			}
		}

		private void NeedApplyFilter()
		{
			if (GameManager.isInGarage)
			{
				ApplyFilter();
			}
		}

		public static Dictionary<int, T> Convert<T>(List<T> list) where T : IDBMeta
		{
			Dictionary<int, T> dictionary = new Dictionary<int, T>();
			foreach (T item in list)
			{
				dictionary.Add(item.id, item);
			}
			return dictionary;
		}
	}
}
