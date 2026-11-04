using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using DB.Meta;
using DB.XML;

public abstract class BaseXMLDictionaryHolder
{
	[XmlIgnore]
	protected List<IListToDictionary> allLists = new List<IListToDictionary>();

	[XmlIgnore]
	private readonly Dictionary<Type, object> m_allSingles = new Dictionary<Type, object>();

	[NonSerialized]
	[XmlIgnore]
	public string fileName;

	[NonSerialized]
	[XmlIgnore]
	public bool autoSave;

	protected void AddSingle(object obj)
	{
		m_allSingles[obj.GetType()] = obj;
	}

	protected abstract void OnInitialize();

	public void Initialize()
	{
		OnInitialize();
	}

	public void ApplyWebStore()
	{
		foreach (IListToDictionary allList in allLists)
		{
			if (allList is ICachedWebStore)
			{
				((ICachedWebStore)allList).ApplyWebStore();
			}
		}
	}

	protected abstract void OnSave();

	public void Save()
	{
		OnSave();
	}

	protected void SaveXMLData<T>(T data, string fileName)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		using StreamWriter textWriter = new StreamWriter(fileName);
		xmlSerializer.Serialize(textWriter, data);
	}

	public T GetById<T>(int id) where T : class, IDBMeta
	{
		T val = null;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				val = allList.GetById<T>(id);
				if (val != null)
				{
					return val;
				}
			}
		}
		return val;
	}

	public T Get<T>(Func<T, bool> predicat) where T : class
	{
		T val = null;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				val = allList.Get(predicat) as T;
				if (val != null)
				{
					return val;
				}
			}
		}
		return val;
	}

	public List<T> QueryAll<T>() where T : class
	{
		List<T> list = new List<T>();
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				list = allList.QueryAll<T>();
				if (list != null && list.Count > 0)
				{
					return list;
				}
			}
		}
		return list;
	}

	public List<T> QueryByFilter<T>(Func<T, bool> predicat) where T : class
	{
		List<T> list = new List<T>();
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				list = allList.QueryByFilter(predicat);
				if (list != null && list.Count > 0)
				{
					return list;
				}
			}
		}
		return list;
	}

	internal static void UpdateObjectFields(object oldVal, object newVal)
	{
		FieldInfo[] cachedFields = oldVal.GetType().GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			fieldInfo.SetValue(oldVal, fieldInfo.GetValue(newVal));
		}
		PropertyInfo[] cachedProperties = oldVal.GetType().GetCachedProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in cachedProperties)
		{
			if (propertyInfo.CanWrite && propertyInfo.CanRead)
			{
				propertyInfo.SetValue(oldVal, propertyInfo.GetValue(newVal, null), null);
			}
		}
	}

	public int UpdateRecord<T>(T newVal, Func<T, bool> predicat) where T : class
	{
		int num = 0;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				num += allList.UpdateRecord(newVal, predicat);
			}
		}
		if (autoSave && num > 0)
		{
			Save();
		}
		return num;
	}

	public int UpdateRecord<T>(Action<T> action, Func<T, bool> predicat) where T : class
	{
		int num = 0;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				num += allList.UpdateRecord(action, predicat);
			}
		}
		if (autoSave && num > 0)
		{
			Save();
		}
		return num;
	}

	public void Insert<T>(T obj, Func<List<T>, T, T> uniqIndexGenerator) where T : class
	{
		bool flag = false;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				allList.Insert(obj, uniqIndexGenerator);
				flag = true;
			}
		}
		if (autoSave && flag)
		{
			Save();
		}
	}

	public void Delete<T>(T obj) where T : class, IDBMeta
	{
		bool flag = false;
		foreach (IListToDictionary allList in allLists)
		{
			if (allList.valueType == typeof(T))
			{
				allList.Delete(obj);
				flag = true;
			}
		}
		if (autoSave && flag)
		{
			Save();
		}
	}

	public T GetSingle<T>() where T : class
	{
		if (m_allSingles.TryGetValue(typeof(T), out var value))
		{
			return (T)value;
		}
		return null;
	}

	public bool UpdateSingle<T>(T obj) where T : class
	{
		Type typeFromHandle = typeof(T);
		if (m_allSingles.ContainsKey(typeFromHandle))
		{
			UpdateObjectFields(m_allSingles[typeFromHandle], obj);
			if (autoSave)
			{
				Save();
			}
			return true;
		}
		return false;
	}
}
