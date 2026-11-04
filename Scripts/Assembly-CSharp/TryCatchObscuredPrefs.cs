using System;
using Assets.Scripts.Services;
using CodeStage.AntiCheat.ObscuredTypes;
using SimpleJSON;

public static class TryCatchObscuredPrefs
{
	public static ObscuredPrefs.DeviceLockLevel lockToDevice
	{
		get
		{
			return ObscuredPrefs.lockToDevice;
		}
		set
		{
			ObscuredPrefs.lockToDevice = value;
		}
	}

	public static bool emergencyMode
	{
		get
		{
			return ObscuredPrefs.emergencyMode;
		}
		set
		{
			ObscuredPrefs.emergencyMode = value;
		}
	}

	public static void SetNewCryptoKey(string newKey)
	{
		ObscuredPrefs.SetNewCryptoKey(newKey);
	}

	public static bool HasKey(string key)
	{
		return ObscuredPrefs.HasKey(key);
	}

	public static void SaveObject(string key, object obj)
	{
		ObscuredPrefs.SetString(key, JSON.Serialize(obj));
		ObscuredPrefs.Save();
	}

	public static T LoadObject<T>(string key)
	{
		try
		{
			if (ObscuredPrefs.HasKey(key))
			{
				string text = ObscuredPrefs.GetString(key);
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

	public static void LoadObjectInplace(string key, object result)
	{
		try
		{
			if (ObscuredPrefs.HasKey(key))
			{
				string text = ObscuredPrefs.GetString(key);
				if (!string.IsNullOrEmpty(text))
				{
					JSON.Parse(text, result);
				}
			}
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
		}
	}

	public static double GetDouble(string key)
	{
		try
		{
			return ObscuredPrefs.GetDouble(key);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return 0.0;
		}
	}

	public static void SetDoubleAndSave(string key, double value)
	{
		ObscuredPrefs.SetDouble(key, value);
		ObscuredPrefs.Save();
	}

	public static bool GetBool(string key)
	{
		try
		{
			return ObscuredPrefs.GetBool(key);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return false;
		}
	}

	public static void SetBoolAndSave(string key, bool value)
	{
		ObscuredPrefs.SetBool(key, value);
		ObscuredPrefs.Save();
	}

	public static int GetInt(string key, int defaultValue = 0)
	{
		try
		{
			return ObscuredPrefs.GetInt(key, defaultValue);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return defaultValue;
		}
	}

	public static void SetIntAndSave(string key, int value)
	{
		ObscuredPrefs.SetInt(key, value);
		ObscuredPrefs.Save();
	}

	public static string GetString(string key, string defaultValue = "")
	{
		try
		{
			return ObscuredPrefs.GetString(key, defaultValue);
		}
		catch (Exception exc)
		{
			ErrorReporter.ReportPrefsError(exc);
			return defaultValue;
		}
	}

	public static void SetStringAndSave(string key, string value)
	{
		ObscuredPrefs.SetString(key, value);
		ObscuredPrefs.Save();
	}

	public static void Save()
	{
		ObscuredPrefs.Save();
	}
}
