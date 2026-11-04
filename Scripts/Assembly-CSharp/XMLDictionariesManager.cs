using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using DB;
using DB.Meta;
using UnityEngine;

public class XMLDictionariesManager : MonoBehaviour
{
	private bool m_initialized;

	private StaticDictionariesHolder m_dictionaries;

	public string databaseFileResourcePath;

	public string databaseFileResourcePathExtension;

	[Header("Splitted StaticDictXMLs")]
	public string dbSettingsFilePath;

	public string dbCarsFilesFolderPath;

	public string dbColorsFilePath;

	public string dbProfileParamsFilePath;

	public string dbTracksFilePath;

	public string dbTrackNormsFilePath;

	public string dbTrackRewardsFilePath;

	private StaticDictionariesHolder dictionaries
	{
		get
		{
			if (!m_initialized)
			{
				Initialize();
			}
			return m_dictionaries;
		}
	}

	public bool autoSave
	{
		get
		{
			return dictionaries.autoSave;
		}
		set
		{
			dictionaries.autoSave = value;
		}
	}

	public static T UniqueIndexGenerator<T>(List<T> list, T inObj) where T : IDBMeta
	{
		int num = -1;
		foreach (T item in list)
		{
			if (item.id > num)
			{
				num = item.id;
			}
		}
		inObj.id = num + 1;
		return inObj;
	}

	public void Load()
	{
		try
		{
			m_initialized = true;
			Stopwatch stopwatch = new Stopwatch();
			GameLog.Print(AlertLevel.Notify, "System", "Starting load db...");
			if (!StaticDictionariesHolder.AssetOnlyMode)
			{
				stopwatch.Restart();
				LoadFromXML();
				stopwatch.Stop();
				GameLog.Print(AlertLevel.Notify, "System", "Done loading XML db in " + stopwatch.Elapsed.ToString() + " sec.");
			}
			else
			{
				stopwatch.Restart();
				LoadFromAsset();
				stopwatch.Stop();
				GameLog.Print(AlertLevel.Notify, "System", "Done loading Asset db in " + stopwatch.Elapsed.ToString() + " sec.");
			}
			string text = (databaseFileResourcePathExtension.StartsWith(".") ? "" : ".") + databaseFileResourcePathExtension;
			string fileName = Path.Combine(Application.dataPath, "Editor/Resources/" + databaseFileResourcePath + text);
			m_dictionaries.fileName = fileName;
			stopwatch.Restart();
			m_dictionaries.Initialize();
			stopwatch.Stop();
			GameLog.Print(AlertLevel.Notify, "System", "Done Initialize db in " + stopwatch.Elapsed.ToString() + " sec.");
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "Counld not initialize db from resources: " + ex);
			m_dictionaries = null;
		}
	}

	private void LoadFromAsset()
	{
		StaticDictionariesAsset staticDictionariesAsset = ResourcesWrapper.Load<StaticDictionariesAsset>("DB/StaticDictionariesAsset");
		m_dictionaries = staticDictionariesAsset.holder;
	}

	private void LoadFromXML()
	{
		string text = (Resources.Load(databaseFileResourcePath) as TextAsset).text;
		m_dictionaries = DeserializeXml<StaticDictionariesHolder>(text);
	}

	private void SplitConfig()
	{
		StaticDictionariesHolder staticDictionariesHolder = DeserializeXml<StaticDictionariesHolder>((Resources.Load(databaseFileResourcePath) as TextAsset).text);
		FieldInfo[] cachedFields = staticDictionariesHolder.GetType().GetCachedFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			SaveDBObject(fieldInfo.GetValue(staticDictionariesHolder));
		}
	}

	private void SaveDBObject(object obj)
	{
		if (obj == null)
		{
			return;
		}
		string text = Path.Combine(Application.dataPath, "Editor/Resources/DB/");
		Type type = obj.GetType();
		string text2 = (type.IsGenericType ? type.GetGenericArguments()[0].Name : type.Name);
		XmlSerializer xmlSerializer = new XmlSerializer(type);
		using StreamWriter textWriter = new StreamWriter(text + text2 + ".xml");
		xmlSerializer.Serialize(textWriter, obj);
	}

	private object LoadDBObject(Type type)
	{
		string text = (type.IsGenericType ? type.GetGenericArguments()[0].Name : type.Name);
		TextAsset textAsset = Resources.Load("DB/" + text) as TextAsset;
		if (textAsset == null)
		{
			return null;
		}
		string text2 = textAsset.text;
		XmlSerializer xmlSerializer = new XmlSerializer(type);
		using StringReader textReader = new StringReader(text2);
		return xmlSerializer.Deserialize(textReader);
	}

	public void Save()
	{
		dictionaries.Save();
	}

	public void ApplyWebStore()
	{
		if (m_dictionaries != null)
		{
			m_dictionaries.ApplyWebStore();
		}
	}

	public void Initialize()
	{
		if (!m_initialized)
		{
			Load();
		}
	}

	public static T DeserializeXml<T>(string fileContent) where T : new()
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		using StringReader textReader = new StringReader(fileContent);
		return (T)xmlSerializer.Deserialize(textReader);
	}

	public T GetById<T>(int id) where T : class, IDBMeta, new()
	{
		return dictionaries.GetById<T>(id);
	}

	public T Get<T>(Func<T, bool> predicat) where T : class, new()
	{
		return dictionaries.Get(predicat);
	}

	public List<T> QueryAll<T>() where T : class, new()
	{
		return dictionaries.QueryAll<T>();
	}

	public List<T> QueryRecordsByColumnValue<T>(Func<T, bool> predicat) where T : class, new()
	{
		return dictionaries.QueryByFilter(predicat);
	}

	public T GetSingle<T>() where T : class
	{
		return dictionaries.GetSingle<T>();
	}

	private static void ReimportDbFile()
	{
		GameLog.Print(AlertLevel.Warning, "Debug", "ReimportDbFile supported only in EditorMode!");
	}

	public void EndEdit()
	{
	}

	public void UpdateRecord<T>(T newVal, Func<T, bool> predicat) where T : class
	{
		if (dictionaries.UpdateRecord(newVal, predicat) <= 0)
		{
			throw new ObjectDoesNotExistException();
		}
	}

	public void UpdateRecord<T>(Action<T> action, Func<T, bool> predicat) where T : class
	{
		if (dictionaries.UpdateRecord(action, predicat) <= 0)
		{
			throw new ObjectDoesNotExistException();
		}
	}

	public void Insert<T>(T obj, Func<List<T>, T, T> uniqIndexGenerator) where T : class
	{
		dictionaries.Insert(obj, uniqIndexGenerator);
	}

	public void Delete<T>(T obj) where T : class, IDBMeta
	{
		dictionaries.Delete(obj);
	}

	public void UpdateSingle<T>(T obj) where T : class
	{
		if (!dictionaries.UpdateSingle(obj))
		{
			throw new ObjectDoesNotExistException();
		}
	}
}
