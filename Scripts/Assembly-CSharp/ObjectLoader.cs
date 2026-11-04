using System;

public class ObjectLoader<T> : WebLoader where T : class
{
	public delegate T InitDefaultHandler();

	public InitDefaultHandler InitDefaultCallback;

	public Action<T> LoadedEvent;

	public T Item { get; private set; }

	public ObjectLoader(string serializeKey = null, TimeSpan? interval = null)
		: base(serializeKey, interval)
	{
	}

	private void SetObj(T obj)
	{
		Item = obj;
		SaveToCache(Item);
	}

	private T InitDefault()
	{
		if (InitDefaultCallback != null)
		{
			return InitDefaultCallback();
		}
		return null;
	}

	private void LoadFromServer()
	{
		if (m_urls.Count == 0)
		{
			return;
		}
		ConnectionUtils.IsInternetAccessAvailable(delegate(bool internetAvailable)
		{
			if (internetAvailable)
			{
				WebConfigManager.CheckWebConfig(delegate
				{
					Load(delegate(T obj, bool isErrorExists)
					{
						if (!isErrorExists)
						{
							SetObj(obj);
							LoadedEvent.Call(obj);
						}
					});
				});
			}
		});
	}

	public T Load()
	{
		if (Item == null)
		{
			Item = LoadFromCache<T>();
			if (Item == null)
			{
				SetObj(InitDefault());
				LoadFromServer();
				return Item;
			}
		}
		if (base.expired)
		{
			LoadFromServer();
		}
		return Item;
	}
}
