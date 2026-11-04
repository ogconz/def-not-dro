using System;
using Assets.Scripts.Services;
using SimpleJSON;
using UnityEngine;

public static class TryCatchPrefs
{
	public static bool HasKey(string key)
	{
		return PlayerPrefs.HasKey(key);
	}

	public static void SaveObject(string key, object obj)
	{
		PlayerPrefs.SetString(key, JSON.Serialize(obj));
		PlayerPrefs.Save();
	}

	public static T LoadObject<T>(string key)
	{
		try
		{
			if (PlayerPrefs.HasKey(key))
			{
				string text = PlayerPrefs.GetString(key);
				if (!string.IsNullOrEmpty(text))
				{
					return JSON.Parse<T>(text);
				}
			}
			return default(T);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return default(T);
		}
	}

	public static T LoadObjectOrCreateDefault<T>(string key) where T : class, new()
	{
		T val = LoadObject<T>(key);
		if (val == null)
		{
			val = new T();
		}
		return val;
	}

	public static bool GetBool(string key, bool defaultValue = false)
	{
		try
		{
			return PlayerPrefs.GetInt(key) == 1;
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return false;
		}
	}

	public static void SetBoolAndSave(string key, bool value)
	{
		PlayerPrefs.SetInt(key, value ? 1 : 0);
		PlayerPrefs.Save();
	}

	public static void SetString(string key, string value)
	{
		PlayerPrefs.SetString(key, value);
		PlayerPrefs.Save();
	}

	public static string GetString(string key, string defaultValue = "")
	{
		try
		{
			return PlayerPrefs.GetString(key, defaultValue);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return defaultValue;
		}
	}
}
