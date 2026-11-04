using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;
using UnityEngine;

public static class LocalizationService
{
	public class VersionInfo
	{
		public string hash;

		public ulong timestamp;
	}

	public const string LanguagesJSONDir = "Assets/Localization/";

	public const string LanguagesAssetDir = "Assets/Resources_moved/Localization/";

	public const string ResourceAssetPath = "Localization";

	public const string LanguagesMetafileName = "LanguagesMeta";

	public const SystemLanguage DefaultLanguage = SystemLanguage.English;

	private static readonly List<LocalizationLanguageMeta> m_allLanguagesMeta = new List<LocalizationLanguageMeta>();

	private static LocalizationLanguageMeta m_languageMeta;

	private static Dictionary<string, string> m_languageData;

	private static bool m_isInitialized;

	private static SystemLanguage m_systemLanguage = SystemLanguage.Unknown;

	private static SystemLanguage? m_prevLanguage;

	private static Dictionary<string, LocalizationLanguage> m_localizationsCache = new Dictionary<string, LocalizationLanguage>();

	public static Action onLanguageChanged;

	public static bool languageMetaResolved { get; private set; }

	public static bool languageMetaRequested { get; private set; }

	public static SystemLanguage currentLanguage => m_languageMeta.languageType;

	public static IList<LocalizationLanguageMeta> allLanguagesMeta
	{
		get
		{
			InitializeIfNeed();
			return m_allLanguagesMeta;
		}
	}

	public static LocalizationLanguageMeta languageMeta
	{
		get
		{
			InitializeIfNeed();
			return m_languageMeta;
		}
	}

	public static SystemLanguage? languageType => languageMeta?.languageType;

	private static T LoadFromAsset<T>(string fileName) where T : class
	{
		return LoadFromJsonAsset<T>("Localization/" + fileName);
	}

	private static LocalizationLanguage LoadFromInnerAsset(string fileName)
	{
		if (m_localizationsCache.ContainsKey(fileName))
		{
			return m_localizationsCache[fileName];
		}
		string path = "Localization/" + fileName;
		LocalizationLanguage result = null;
		LanguageAsset languageAsset = ResourcesWrapper.Load<LanguageAsset>(path);
		if (languageAsset != null)
		{
			result = new LocalizationLanguage
			{
				data = languageAsset.data.dictionary
			};
			if (!m_localizationsCache.ContainsKey(fileName))
			{
				m_localizationsCache.Add(fileName, result);
			}
			return result;
		}
		return result;
	}

	public static T LoadObjFromJsonFile<T>(string fileName)
	{
		try
		{
			string aJSON;
			using (StreamReader streamReader = new StreamReader(fileName))
			{
				aJSON = streamReader.ReadToEnd();
				streamReader.Close();
			}
			return JSON.Parse<T>(aJSON);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error loading flie " + fileName + ": " + ex.Message + ", " + ex.StackTrace);
		}
		return default(T);
	}

	public static void SaveObjToJsonFile(string fileName, object obj, bool formatJson = false)
	{
		try
		{
			string text = JSON.Serialize(obj);
			if (formatJson)
			{
				text = JSON.Format(text);
			}
			using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
			{
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				fileStream.Write(bytes, 0, bytes.Length);
				fileStream.Close();
			}
			GameLog.Print(AlertLevel.Notify, "WebConfig", "Pref " + fileName + " - saved ");
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error saving file " + fileName + ": " + ex.Message + ", " + ex.StackTrace);
		}
	}

	public static T LoadFromJsonAsset<T>(string path) where T : class
	{
		TextAsset textAsset = ResourcesWrapper.Load<TextAsset>(path);
		if (textAsset != null)
		{
			try
			{
				return JSON.Parse<T>(textAsset.text);
			}
			catch (Exception e)
			{
				GameLog.LogException(e);
			}
			finally
			{
				ResourcesWrapper.UnloadAsset(textAsset);
			}
		}
		return null;
	}

	private static bool IsLanguagesMetaFileName(string fileName)
	{
		return string.Equals(fileName, "LanguagesMeta");
	}

	private static bool IsInLanguagesMeta(SystemLanguage langType)
	{
		for (int i = 0; i < m_allLanguagesMeta.Count; i++)
		{
			if (m_allLanguagesMeta[i].languageType == langType)
			{
				return true;
			}
		}
		return false;
	}

	private static LocalizationLanguageMeta FindLanguageMeta(SystemLanguage langType)
	{
		for (int i = 0; i < m_allLanguagesMeta.Count; i++)
		{
			LocalizationLanguageMeta localizationLanguageMeta = m_allLanguagesMeta[i];
			if (localizationLanguageMeta.languageType == langType)
			{
				return localizationLanguageMeta;
			}
		}
		return null;
	}

	private static SystemLanguage ParseSystemLanguage(string text)
	{
		return (SystemLanguage)Enum.Parse(typeof(SystemLanguage), text);
	}

	private static string GetWebUrlByFileName(string fileName)
	{
		if (IsLanguagesMetaFileName(fileName))
		{
			string localizationRoot = WebConfigManager.LocalizationRoot;
			languageMetaRequested = !string.IsNullOrEmpty(localizationRoot);
			return localizationRoot;
		}
		try
		{
			return FindLanguageMeta(ParseSystemLanguage(fileName)).webUrl;
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		return null;
	}

	private static void LoadFileFromFileSystem<T>(string fileName, Action<bool, T> loaded) where T : class
	{
		Debug.Log(">> LoadFileFromFileSystem: " + fileName + " ");
		T val = null;
		if (val == null)
		{
			val = LoadFromAsset<T>(fileName);
		}
		loaded.Call(val != null, val);
	}

	private static void LoadFileFromFileSystem(string fileName, Action<bool, LocalizationLanguage> loaded)
	{
		LocalizationLanguage localizationLanguage = LoadFromInnerAsset(fileName);
		loaded.Call(localizationLanguage != null, localizationLanguage);
	}

	public static string GetLanguageTranslate(SystemLanguage langType, string key, params object[] args)
	{
		LocalizationLanguage localizationLanguage = LoadFromInnerAsset(langType.ToString());
		if (GetText(key, out var text, localizationLanguage.data) && args != null)
		{
			try
			{
				text = string.Format(text, args);
				return text;
			}
			catch
			{
				Debug.LogWarning("LocalizationService.Localized: could not format localization key '" + key + "'");
			}
		}
		return text;
	}

	private static void LoadFileFromAsset<T>(string fileName, Action<bool, T> loaded) where T : class
	{
		T val = LoadFromAsset<T>(fileName);
		loaded.Call(val != null, val);
	}

	private static void LoadLanguage(SystemLanguage langType, Action<bool, LocalizationLanguage> loaded)
	{
		LoadFileFromFileSystem(langType.ToString(), delegate(bool successful, LocalizationLanguage language)
		{
			loaded.Call(successful, language);
		});
	}

	private static void SaveSelectedLanguageType(SystemLanguage langType)
	{
		TryCatchPrefs.SetString("LocalizationServiceData", langType.ToString());
	}

	public static bool IsAutoSelectLanguage()
	{
		if (!TryCatchPrefs.HasKey("LocalizationServiceIsAuto"))
		{
			TryCatchPrefs.SetBoolAndSave("LocalizationServiceIsAuto", value: true);
			return true;
		}
		return TryCatchPrefs.GetBool("LocalizationServiceIsAuto");
	}

	public static string GetAutoSelectLanguageTitle()
	{
		LocalizationLanguageMeta localizationLanguageMeta = FindLanguageMetaOrDefault(GetSystemLanguage());
		if (localizationLanguageMeta != null)
		{
			return localizationLanguageMeta.title;
		}
		return "---";
	}

	private static void SaveAutoSelectLanguage(bool value)
	{
		TryCatchPrefs.SetBoolAndSave("LocalizationServiceIsAuto", value);
	}

	private static LocalizationLanguageMeta FindLanguageMetaOrDefault(SystemLanguage langType)
	{
		LocalizationLanguageMeta localizationLanguageMeta = FindLanguageMeta(langType);
		if (localizationLanguageMeta == null)
		{
			localizationLanguageMeta = FindLanguageMeta(SystemLanguage.English);
		}
		return localizationLanguageMeta;
	}

	private static void RizeLanguageChanged(SystemLanguage langType)
	{
		if (m_prevLanguage.HasValue && m_prevLanguage.Value != langType)
		{
			onLanguageChanged.SafeCall();
		}
		m_prevLanguage = langType;
	}

	private static void SetLanguageByType(SystemLanguage langType)
	{
		LocalizationLanguageMeta meta = FindLanguageMetaOrDefault(langType);
		if (meta == null)
		{
			Debug.LogError("Localization language meta not found - " + langType);
			return;
		}
		LoadFileFromFileSystem(meta.languageType.ToString(), delegate(bool successful, LocalizationLanguage language)
		{
			if (!successful)
			{
				Debug.LogWarning("Localization language data not found - " + meta.languageType);
			}
			else
			{
				m_languageMeta = meta;
				m_languageData = language.data;
				if (m_systemLanguage == SystemLanguage.Unknown || m_systemLanguage != meta.languageType)
				{
					SaveSelectedLanguageType(meta.languageType);
				}
				RizeLanguageChanged(langType);
			}
		});
	}

	private static void InitSelectedLanguage()
	{
		SystemLanguage languageByType = (IsAutoSelectLanguage() ? GetSystemLanguage() : (TryCatchPrefs.HasKey("LocalizationServiceData") ? ParseSystemLanguage(TryCatchPrefs.GetString("LocalizationServiceData", SystemLanguage.English.ToString())) : GetSystemLanguage()));
		SetLanguageByType(languageByType);
	}

	private static void InitMetaFile()
	{
		m_allLanguagesMeta.Clear();
		LoadFileFromAsset("LanguagesMeta", delegate(bool successful, List<LocalizationLanguageMeta> methas)
		{
			if (successful)
			{
				foreach (LocalizationLanguageMeta m in methas)
				{
					if (m_allLanguagesMeta.Find((LocalizationLanguageMeta i) => i.languageType == m.languageType) == null)
					{
						m_allLanguagesMeta.Add(m);
					}
				}
			}
		});
	}

	public static SystemLanguage GetSystemLanguage()
	{
		if (m_systemLanguage == SystemLanguage.Unknown)
		{
			m_systemLanguage = Application.systemLanguage;
			if (m_systemLanguage == SystemLanguage.Chinese)
			{
				m_systemLanguage = SystemLanguage.ChineseSimplified;
			}
		}
		return m_systemLanguage;
	}

	public static void ClearData()
	{
		m_isInitialized = false;
		m_allLanguagesMeta.Clear();
		m_languageData = new Dictionary<string, string>();
		m_languageMeta = null;
	}

	private static void InitializeIfNeed()
	{
		if (!m_isInitialized)
		{
			InitMetaFile();
			InitSelectedLanguage();
			Debug.Log($"Init localization: {m_languageMeta.languageType}");
			m_isInitialized = true;
		}
	}

	public static void SetLanguage(SystemLanguage languageType, Action<bool> done, bool restorePreviousLanguage = false)
	{
		InitializeIfNeed();
		if (m_languageMeta.languageType == languageType && !restorePreviousLanguage)
		{
			done.Call(param: true);
			return;
		}
		LoadLanguage(languageType, delegate(bool successful, LocalizationLanguage language)
		{
			if (successful)
			{
				SaveSelectedLanguageType(languageType);
				SaveAutoSelectLanguage(value: false);
			}
			InitSelectedLanguage();
			if (successful)
			{
				RizeLanguageChanged(languageType);
			}
			done.SafeCall(successful);
		});
	}

	public static void SetLanguageByAuto(bool value)
	{
		InitializeIfNeed();
		SaveAutoSelectLanguage(value);
	}

	public static bool IsKeyExist(string key)
	{
		if (m_languageData == null)
		{
			return false;
		}
		return m_languageData.ContainsKey(key);
	}

	public static List<string> GetAllKeysWithPrefix(string prefixKeyName)
	{
		List<string> list = new List<string>();
		foreach (string key in m_languageData.Keys)
		{
			if (key.StartsWith(prefixKeyName))
			{
				list.Add(key);
			}
		}
		return list;
	}

	private static bool GetText(string key, out string text, Dictionary<string, string> langDataParam = null)
	{
		Dictionary<string, string> dictionary = langDataParam;
		if (langDataParam == null)
		{
			dictionary = m_languageData;
		}
		text = null;
		if (dictionary != null && key != null && dictionary.ContainsKey(key))
		{
			text = dictionary[key];
			return true;
		}
		return false;
	}

	public static string Localize(string key)
	{
		InitializeIfNeed();
		if (GetText(key, out var text))
		{
			return text;
		}
		return "[" + key + "]";
	}

	public static string LocalizeFormat(string key, params object[] args)
	{
		string text = Localize(key);
		if (args != null)
		{
			try
			{
				text = string.Format(text, args);
			}
			catch
			{
				Debug.LogWarning("LocalizationService.Localized: could not format localization key '" + key + "'");
			}
		}
		return text;
	}

	public static string FormatLocalize(string key, params object[] args)
	{
		string text = key;
		if (args != null)
		{
			try
			{
				text = string.Format(text, args);
			}
			catch
			{
				Debug.LogWarning("LocalizationService.Localized: could not format localization key '" + key + "'");
			}
		}
		return Localize(text);
	}
}
