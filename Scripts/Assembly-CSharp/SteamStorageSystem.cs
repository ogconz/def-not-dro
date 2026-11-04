using System;
using System.IO;
using System.Text;
using GameOverlay;
using PrefsModels;
using Steamworks;

public class SteamStorageSystem : PCStorageSystem
{
	public override bool IsRemoteSupported()
	{
		return false;
	}

	protected override string GetNewSavePath()
	{
		if (m_sbSavePath == null)
		{
			m_sbSavePath = new StringBuilder();
		}
		m_sbSavePath.Clear();
		if (!(Overlay.instance is SteamOverlay { SteamId: var steamId }))
		{
			return string.Empty;
		}
		m_sbSavePath.Append(PCStorageSystem.m_savePath);
		m_sbSavePath.Append(steamId);
		string path = m_sbSavePath.ToString();
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		m_sbSavePath.Append(Path.AltDirectorySeparatorChar);
		return m_sbSavePath.ToString();
	}

	public override void ReadFromRemoteStorage(string modelName, out byte[] data)
	{
		int num = SteamRemoteStorage.FileSize(modelName);
		if (num > 0)
		{
			data = new byte[num];
			byte[] array = SteamRemoteStorage.FileRead(modelName);
			if (array != null && array.Length == num)
			{
				data = array;
				return;
			}
		}
		data = null;
	}

	public override bool NeedUpdateRemote(BasePrefsModel model, string remoteName)
	{
		if (m_loadedOldSaves)
		{
			return false;
		}
		string path = PCStorageSystem.m_savePath + model.modelName + ".sav";
		if (!SteamRemoteStorage.FileExists(remoteName))
		{
			return false;
		}
		if (!File.Exists(path))
		{
			return true;
		}
		DateTime dateTime = SteamRemoteStorage.FileTime(remoteName);
		DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
		return (float)(dateTime - lastWriteTimeUtc).TotalSeconds > 180f;
	}

	public override void WriteToRemoteStorage(string modelName, byte[] data)
	{
		if (SteamRemoteStorage.FileExists(modelName))
		{
			SteamRemoteStorage.FileDelete(modelName);
		}
		SteamRemoteStorage.FileWrite(modelName, data);
	}

	public override bool SkipClearProfileOnReadError()
	{
		return false;
	}
}
