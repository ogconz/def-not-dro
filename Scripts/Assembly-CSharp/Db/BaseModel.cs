using System;
using System.Collections.Generic;
using System.Reflection;
using DB.Meta;
using UnityEngine;

namespace DB
{
	public class BaseModel
	{
		private XMLDictionariesManager m_xmlDBManager;

		private XMLDictionariesManager xmlDBManager
		{
			get
			{
				if (m_xmlDBManager == null)
				{
					m_xmlDBManager = UnityEngine.Object.FindObjectOfType<XMLDictionariesManager>();
				}
				return m_xmlDBManager;
			}
		}

		public bool autoSave
		{
			get
			{
				return xmlDBManager.autoSave;
			}
			set
			{
				xmlDBManager.autoSave = value;
			}
		}

		public T GetById<T>(int id) where T : class, IDBMeta, new()
		{
			return xmlDBManager.GetById<T>(id);
		}

		public T Get<T>(Func<T, bool> predicat) where T : class, new()
		{
			return xmlDBManager.Get(predicat);
		}

		public List<T> QueryAll<T>() where T : class, new()
		{
			return xmlDBManager.QueryAll<T>();
		}

		public List<T> QueryRecordsByColumnValue<T>(string column, object value, Func<T, bool> predicat) where T : class, new()
		{
			return xmlDBManager.QueryRecordsByColumnValue(predicat);
		}

		public T GetSingle<T>() where T : class
		{
			return xmlDBManager.GetSingle<T>();
		}

		public void EndEdit()
		{
			xmlDBManager.EndEdit();
		}

		public void UpdateRecord<T>(T obj) where T : class, IDBMeta, new()
		{
			xmlDBManager.UpdateRecord(obj, (T dbObj) => dbObj.id == obj.id);
		}

		public void UpdateRecord<T>(Action<T> action, Func<T, bool> predicat) where T : class, new()
		{
			xmlDBManager.UpdateRecord(action, predicat);
		}

		public void Insert<T>(T obj) where T : class, IDBMeta
		{
			xmlDBManager.Insert(obj, XMLDictionariesManager.UniqueIndexGenerator);
		}

		public void Delete<T>(T obj) where T : class, IDBMeta
		{
			xmlDBManager.Delete(obj);
		}

		public void UpdateSingle<T>(T obj) where T : class
		{
			xmlDBManager.UpdateSingle(obj);
		}

		public void ApplyWebStore()
		{
			xmlDBManager.ApplyWebStore();
		}

		public void Reload(bool downloadRemoteData = false)
		{
			xmlDBManager.Load();
			if (downloadRemoteData)
			{
				ApplyWebStore();
			}
		}

		public void Save()
		{
			xmlDBManager.Save();
		}

		public string GetNameById<T>(int? id) where T : class, IDBMeta, new()
		{
			if (!id.HasValue)
			{
				return "";
			}
			T byId = GetById<T>(id.Value);
			PropertyInfo property = byId.GetType().GetProperty("name");
			if (!(property == null))
			{
				return property.GetValue(byId, null) as string;
			}
			return "";
		}
	}
}
