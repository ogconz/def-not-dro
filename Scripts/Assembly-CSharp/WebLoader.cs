using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BestHTTP;
using CodeStage.AntiCheat.ObscuredTypes;
using DI;
using SimpleJSON;
using UnityEngine;

public class WebLoader
{
	public enum EPlatform
	{
		Android = 0,
		Ios = 1,
		Wp8 = 2,
		AnyOrEditor = 3
	}

	public delegate void DataEvent(string key, string data, int error);

	private class RequestMeta
	{
		public DataEvent completeCallback;

		public string path;

		public int retryCount = 3;

		public RequestMeta(string path, DataEvent completeCallback)
		{
			this.path = path;
			this.completeCallback = completeCallback;
		}
	}

	private const float dataLoadTimeout = 10f;

	protected readonly Dictionary<EPlatform, ObscuredString> m_urls = new Dictionary<EPlatform, ObscuredString>();

	private string m_serializeCacheKey;

	private string m_serializeExpireTimeKey;

	private TimeSpan? m_expireInterval;

	private LongPeriodExpireTimer m_expireTime;

	public Action TimerExpiredEvent;

	private HTTPRequest m_request;

	public bool skipErrorsByLoad { get; set; }

	public bool loadInProgress { get; private set; }

	public bool expired
	{
		get
		{
			if (m_expireTime != null)
			{
				return m_expireTime.expired;
			}
			return false;
		}
	}

	public WebLoader(string serializeKey = null, TimeSpan? interval = null)
	{
		DependencyInjector.InjectObjectsInto(this);
		m_serializeCacheKey = serializeKey;
		if (serializeKey != null && interval.HasValue)
		{
			m_serializeExpireTimeKey = m_serializeCacheKey + "_expireTime";
			m_expireInterval = interval;
			m_expireTime = new LongPeriodExpireTimer();
			if (TryCatchObscuredPrefs.HasKey(m_serializeExpireTimeKey))
			{
				m_expireTime.expireTime = TryCatchObscuredPrefs.GetDouble(m_serializeExpireTimeKey);
			}
			else
			{
				m_expireTime.expireTime = 0.0;
				SaveExpireTime();
			}
			LongPeriodExpireTimer expireTime = m_expireTime;
			expireTime.timerIntervalExpireEvent = (Action)Delegate.Combine(expireTime.timerIntervalExpireEvent, new Action(ExpireTimeIntervalExpired));
		}
	}

	private static void SavePref(string key, string data, DataEvent completeCallback)
	{
		int error = 0;
		try
		{
			ObscuredPrefs.SetString(key, data);
			GameLog.Print(AlertLevel.Notify, "WebConfig", "Pref " + key + " - saved ");
			ObscuredPrefs.Save();
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error saving pref " + key + ": " + ex.Message + ", " + ex.StackTrace);
			error = 1;
		}
		completeCallback?.Invoke(key, data, error);
	}

	private static IEnumerator WaitForDataSaved(string path, string data, DataEvent completeCallback = null, bool post = true)
	{
		WWW www;
		if (post)
		{
			WWWForm wWWForm = new WWWForm();
			wWWForm.AddField("data", data);
			www = new WWW(path, wWWForm);
		}
		else
		{
			www = new WWW(path);
		}
		float timeoutTime = Time.time + 10f;
		while (!www.isDone && Time.time < timeoutTime)
		{
			yield return null;
		}
		completeCallback?.Invoke(path, www.text, (!www.isDone) ? 1 : 0);
	}

	private static void SaveUrl(string key, string data, DataEvent completeCallback)
	{
		TimeController.RunCoroutine(WaitForDataSaved(key, data, completeCallback));
	}

	private static void SaveFile(string fileName, string data, DataEvent completeCallback)
	{
		int error = 0;
		try
		{
			using (FileStream fileStream = new FileStream(fileName + ".json", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				byte[] bytes = Encoding.UTF8.GetBytes(data);
				fileStream.Write(bytes, 0, bytes.Length);
				fileStream.Close();
			}
			GameLog.Print(AlertLevel.Notify, "WebConfig", "Pref " + fileName + " - saved ");
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error saving file " + fileName + ": " + ex.Message + ", " + ex.StackTrace);
			error = 1;
		}
		completeCallback?.Invoke(fileName, data, error);
	}

	public static void SaveData(string path, DataLocation location, string data, DataEvent completeCallback = null)
	{
		switch (location)
		{
		case DataLocation.File:
			SaveFile(path, data, completeCallback);
			break;
		case DataLocation.Prefs:
			SavePref(path, data, completeCallback);
			break;
		case DataLocation.Url:
			SaveUrl(path, data, completeCallback);
			break;
		case DataLocation.Resource:
			break;
		}
	}

	private static string LoadFile(string fileName, DataEvent completeCallback)
	{
		int error = 0;
		string text = "";
		try
		{
			using StreamReader streamReader = new StreamReader(fileName + ".json");
			text = streamReader.ReadToEnd();
			streamReader.Close();
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error loading flie " + fileName + ": " + ex.Message + ", " + ex.StackTrace);
			error = 1;
		}
		completeCallback?.Invoke(fileName, text, error);
		return text;
	}

	private static string LoadPref(string key, DataEvent completeCallback)
	{
		int error = 0;
		string text = "";
		try
		{
			text = ObscuredPrefs.GetString(key);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Error loading pref " + key + ": " + ex.Message + ", " + ex.StackTrace);
			error = 1;
		}
		completeCallback?.Invoke(key, text, error);
		return text;
	}

	private static HTTPRequest LoadFromWeb(string path, DataEvent completeCallback)
	{
		RequestMeta meta = new RequestMeta(path, completeCallback);
		GameLog.Print(AlertLevel.Notify, "WebConfig", "WebLoader::LoadFromWeb path = " + path);
		return Send(meta);
	}

	private static HTTPRequest Send(RequestMeta meta)
	{
		try
		{
			HTTPRequest hTTPRequest = new HTTPRequest(new Uri(meta.path), HTTPMethods.Get, isKeepAlive: false, disableCache: true, OnResponse);
			hTTPRequest.Tag = meta;
			hTTPRequest.Send();
			return hTTPRequest;
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", ex.Message);
			meta.completeCallback(meta.path, "", 1);
			return null;
		}
	}

	private static void OnResponse(HTTPRequest req, HTTPResponse resp)
	{
		RequestMeta meta = req.Tag as RequestMeta;
		Action<string> action = delegate(string msg)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", msg);
			meta.completeCallback(meta.path, "", 1);
		};
		if (resp == null)
		{
			action("Null response for the request to " + meta.path);
		}
		else if (resp.StatusCode >= 502 && resp.StatusCode <= 504 && meta.retryCount > 0)
		{
			GameLog.Print(AlertLevel.Error, "WebConfig", "Failed: " + meta.path + ". Try count = " + meta.retryCount);
			meta.retryCount--;
			TimeController.AddCallback(2.5f, delegate
			{
				Send(meta);
			});
		}
		else if (resp.StatusCode / 100 != 2)
		{
			action("Code " + resp.StatusCode + " returned for the request to " + meta.path);
		}
		else
		{
			string data = Encoding.UTF8.GetString(resp.Data);
			meta.completeCallback(meta.path, data, 0);
		}
	}

	public static string LoadData(string path, DataLocation location, DataEvent completeCallback = null)
	{
		switch (location)
		{
		case DataLocation.File:
			return LoadFile(path, completeCallback);
		case DataLocation.Prefs:
			return LoadPref(path, completeCallback);
		case DataLocation.Url:
			LoadFromWeb(path, completeCallback);
			break;
		}
		return "";
	}

	protected virtual void ExpireTimeIntervalExpired()
	{
		TimerExpiredEvent.SafeCall();
	}

	private Dictionary<EPlatform, ObscuredString> GetUrls()
	{
		return m_urls;
	}

	public void Abort()
	{
		lock (m_request)
		{
			if (m_request != null)
			{
				m_request.Callback = null;
				m_request.Abort();
			}
		}
	}

	public void AddUrl(EPlatform platform, string url)
	{
		GetUrls().Add(platform, url);
	}

	public static string PlatformToUrlString(EPlatform platform)
	{
		return platform switch
		{
			EPlatform.Android => "Android", 
			EPlatform.Ios => "IOS", 
			EPlatform.Wp8 => "WP8", 
			EPlatform.AnyOrEditor => "Android", 
			_ => throw new NotImplementedException(), 
		};
	}

	public void AddUrl(string urlDir, string urlVersion, EPlatform platform, string svLandscape)
	{
		string arg = PlatformToUrlString(platform);
		string arg2 = $"{arg}.{urlVersion}";
		string url = string.Format(urlDir, arg2, svLandscape);
		AddUrl(platform, url);
	}

	public void AddUrlSet(string urlDir, string urlVersion, string language, string svLandscape)
	{
		foreach (EPlatform value in Enum.GetValues(typeof(EPlatform)))
		{
			string arg = PlatformToUrlString(value);
			string arg2 = $"{arg}.{urlVersion}";
			string url = string.Format(urlDir, arg2, language, svLandscape);
			AddUrl(value, url);
		}
	}

	private void SaveExpireTime()
	{
		if (!string.IsNullOrEmpty(m_serializeExpireTimeKey))
		{
			TryCatchObscuredPrefs.SetDoubleAndSave(m_serializeExpireTimeKey, m_expireTime.expireTime);
		}
	}

	public static void GetPlatformUrlParams(out EPlatform platform)
	{
		platform = EPlatform.AnyOrEditor;
	}

	public string GetPlatformUrl(bool addTicks = true)
	{
		GetPlatformUrlParams(out var platform);
		Dictionary<EPlatform, ObscuredString> urls = GetUrls();
		if (!urls.ContainsKey(platform))
		{
			throw new InvalidOperationException($"platform - {platform} not defined");
		}
		ObscuredString obscuredString = urls[platform];
		if (addTicks)
		{
			obscuredString = $"{obscuredString}?{DateTime.Now.Ticks}";
		}
		return obscuredString;
	}

	public void SaveToCache(object obj)
	{
		if (!string.IsNullOrEmpty(m_serializeCacheKey))
		{
			TryCatchObscuredPrefs.SaveObject(m_serializeCacheKey, obj);
		}
	}

	public T LoadFromCache<T>()
	{
		if (!string.IsNullOrEmpty(m_serializeCacheKey))
		{
			return TryCatchObscuredPrefs.LoadObject<T>(m_serializeCacheKey);
		}
		return default(T);
	}

	public void Load<T>(Action<T, bool> complete, bool addTicks = true) where T : class
	{
		if (m_urls.Count == 0 || loadInProgress)
		{
			return;
		}
		loadInProgress = true;
		string platformUrl = GetPlatformUrl(addTicks);
		m_request = LoadFromWeb(platformUrl, delegate(string key, string data, int error)
		{
			bool param = false;
			T val;
			if (error == 0)
			{
				try
				{
					val = ((!skipErrorsByLoad) ? JSON.Parse<T>(data) : JSON.ParseAndSkipErrors<T>(data));
					SaveToCache(val);
				}
				catch (Exception e)
				{
					GameLog.LogException(e);
					val = LoadFromCache<T>();
					param = true;
				}
			}
			else
			{
				val = LoadFromCache<T>();
				param = true;
			}
			loadInProgress = false;
			if (m_expireTime != null && m_expireInterval.HasValue)
			{
				m_expireTime.SetExpireTimeByInterval(m_expireInterval.Value);
				SaveExpireTime();
			}
			complete.Call(val, param);
		});
	}
}
