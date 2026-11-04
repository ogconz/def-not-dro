using System;
using System.Collections.Generic;
using DB.Meta;

public interface IListToDictionary
{
	Type valueType { get; }

	T GetById<T>(int id) where T : class, IDBMeta;

	object Get<T>(Func<T, bool> predicat) where T : class;

	List<T> QueryAll<T>() where T : class;

	List<T> QueryByFilter<T>(Func<T, bool> predicat) where T : class;

	int UpdateRecord<T>(T newVal, Func<T, bool> predicat) where T : class;

	int UpdateRecord<T>(Action<T> action, Func<T, bool> predicat) where T : class;

	void Insert<T>(T obj, Func<List<T>, T, T> uniqIndexGenerator) where T : class;

	void Delete<T>(T obj) where T : class, IDBMeta;
}
