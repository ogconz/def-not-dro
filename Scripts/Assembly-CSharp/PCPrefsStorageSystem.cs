using System;
using PrefsModels;
using SimpleJSON;
using SyncMultiplayer;
using UnityEngine;

public class PCPrefsStorageSystem : IStorageSystem
{
	private static bool m_needSavePlayerPrefs;

	private string m_prefix = string.Empty;

	public string prefix => m_prefix;

	public PCPrefsStorageSystem(string prefix)
	{
		m_prefix = prefix;
	}

	public PCPrefsStorageSystem()
	{
		GameLog.Print(AlertLevel.Notify, "Save", "Inited " + typeof(PCPrefsStorageSystem).Name);
	}

	public string EncodeName(string name)
	{
		if (!(m_prefix == string.Empty))
		{
			return "Prefs_" + m_prefix + "_" + name;
		}
		return "Prefs_" + name;
	}

	public void Read(BasePrefsModel model)
	{
		byte[] data = null;
		try
		{
			ReadFromPlayerPrefs(model.modelName, out data);
			if (data != null)
			{
				JSON.Parse(CustomTypes.Unzip(data), model);
			}
		}
		catch (Exception ex)
		{
			AnalyticsService.SendLoadPrefsError(ex, model.modelName, data);
			GameLog.Print(AlertLevel.Error, "System", "Data loss from prefs: cant load " + model.GetType().Name + " E:" + ex.Message);
			model.ResetByLoadError();
		}
	}

	private void ReadFromPlayerPrefs(string modelName, out byte[] data)
	{
		data = null;
		if (PlayerPrefs.HasKey(modelName))
		{
			string s = PlayerPrefs.GetString(modelName, string.Empty);
			data = Convert.FromBase64String(s);
		}
	}

	public void Save(string modelName, string data)
	{
		byte[] array = null;
		try
		{
			array = CustomTypes.Zip(data);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Save", ex.ToString());
			return;
		}
		SaveToPlayerPrefs(modelName, array);
	}

	private bool SaveToPlayerPrefs(string modelName, byte[] data)
	{
		if (data != null)
		{
			string value = Convert.ToBase64String(data);
			PlayerPrefs.SetString(modelName, value);
			m_needSavePlayerPrefs = true;
			return true;
		}
		return false;
	}

	public void FlushSave()
	{
		if (m_needSavePlayerPrefs)
		{
			m_needSavePlayerPrefs = false;
			PlayerPrefs.Save();
		}
	}

	public bool IsRemoteSupported()
	{
		return false;
	}

	public bool SkipClearProfileOnReadError()
	{
		return true;
	}

	public void WriteToRemoteStorage(string modelName, byte[] data)
	{
		throw new NotSupportedException();
	}

	public void ReadFromRemoteStorage(string modelName, out byte[] data)
	{
		throw new NotSupportedException();
	}

	public bool NeedUpdateRemote(BasePrefsModel model, string remoteName)
	{
		throw new NotSupportedException();
	}

	public int GetAvailableFreeSpaceMB()
	{
		throw new NotImplementedException();
	}

	public int GetLocalStorageSizeMB()
	{
		throw new NotImplementedException();
	}
}
