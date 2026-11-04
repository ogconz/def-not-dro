using System;
using System.Collections.Generic;
using DB.Meta;

public class ListToDictionary<TValue> : IListToDictionary where TValue : class, new()
{
	private readonly Type m_valueType = typeof(TValue);

	private Dictionary<int, TValue> m_indicies = new Dictionary<int, TValue>();

	public List<TValue> valuesList = new List<TValue>();

	public Type valueType => m_valueType;

	public ListToDictionary(List<TValue> baseList)
	{
		valuesList = baseList;
		m_indicies.Clear();
		if (baseList.Count <= 0 || !(baseList[0] is IDBMeta))
		{
			return;
		}
		foreach (TValue @base in baseList)
		{
			m_indicies.Add((@base as IDBMeta).id, @base);
		}
	}

	public object Get<T>(Func<T, bool> predicat) where T : class
	{
		if (valueType == typeof(T))
		{
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				TValue val = valuesList[i];
				if (predicat(val as T))
				{
					return val;
				}
			}
		}
		return null;
	}

	public List<T> QueryAll<T>() where T : class
	{
		List<T> list = null;
		if (valueType == typeof(T))
		{
			list = new List<T>();
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				TValue val = valuesList[i];
				list.Add(val as T);
			}
		}
		return list;
	}

	public List<T> QueryByFilter<T>(Func<T, bool> predicat) where T : class
	{
		List<T> list = null;
		if (valueType == typeof(T))
		{
			list = new List<T>();
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				T val = valuesList[i] as T;
				if (predicat(val))
				{
					list.Add(val);
				}
			}
		}
		return list;
	}

	public int UpdateRecord<T>(T newVal, Func<T, bool> predicat) where T : class
	{
		int num = 0;
		if (valueType == typeof(T))
		{
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				T val = valuesList[i] as T;
				if (predicat(val))
				{
					BaseXMLDictionaryHolder.UpdateObjectFields(val, newVal);
					num++;
				}
			}
		}
		return num;
	}

	public int UpdateRecord<T>(Action<T> action, Func<T, bool> predicat) where T : class
	{
		int num = 0;
		if (valueType == typeof(T))
		{
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				T val = valuesList[i] as T;
				if (predicat(val))
				{
					action(val);
					num++;
				}
			}
		}
		return num;
	}

	public void Insert<T>(T obj, Func<List<T>, T, T> uniqIndexGenerator) where T : class
	{
		if (valueType == typeof(T))
		{
			List<T> list = new List<T>();
			int i = 0;
			for (int count = valuesList.Count; i < count; i++)
			{
				TValue val = valuesList[i];
				list.Add(val as T);
			}
			T val2 = uniqIndexGenerator(list, obj);
			valuesList.Add(val2 as TValue);
			if (val2 is IDBMeta)
			{
				m_indicies.Add((obj as IDBMeta).id, val2 as TValue);
			}
		}
	}

	public void Delete<T>(T obj) where T : class, IDBMeta
	{
		if (!(valueType == typeof(T)))
		{
			return;
		}
		foreach (TValue values in valuesList)
		{
			T val = values as T;
			if (obj.id == val.id)
			{
				valuesList.Remove(values);
				if (values is IDBMeta)
				{
					m_indicies.Remove((values as IDBMeta).id);
				}
				break;
			}
		}
	}

	T IListToDictionary.GetById<T>(int id)
	{
		if (m_indicies.TryGetValue(id, out var value))
		{
			return value as T;
		}
		return null;
	}
}
