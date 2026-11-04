using System;
using System.IO;
using System.Text;
using ArrayExtension;
using CarX.Platform;
using GameOverlay;
using PrefsModels;
using SimpleJSON;
using UnityEngine;
using Utlis;

public abstract class PCStorageSystem : IStorageSystem
{
	private const string allowed_chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

	protected const string SaveExtension = ".sav";

	protected const string TempPostfix = "_temp";

	protected static string m_savePath;

	protected bool m_loadedOldSaves;

	protected StringBuilder m_sbSavePath;

	private string m_newFilePath;

	private string m_newSavePath => m_newFilePath ?? (m_newFilePath = GetNewSavePath());

	protected PCStorageSystem()
	{
		string persistentDataPath = Application.persistentDataPath;
		char altDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
		m_savePath = persistentDataPath + altDirectorySeparatorChar + "Cache";
		if (!Directory.Exists(m_savePath))
		{
			Directory.CreateDirectory(m_savePath);
		}
		string savePath = m_savePath;
		altDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
		m_savePath = savePath + altDirectorySeparatorChar;
		GameLog.Print(AlertLevel.Notify, "Save", "Inited PCStorageSystem: " + m_savePath);
	}

	protected virtual string GetNewSavePath()
	{
		if (m_sbSavePath == null)
		{
			m_sbSavePath = new StringBuilder();
		}
		m_sbSavePath.Clear();
		if (Overlay.instance.localUniqId.IsInvalid)
		{
			return string.Empty;
		}
		m_sbSavePath.Append(m_savePath);
		m_sbSavePath.Append(Overlay.instance.localUniqId.ToString());
		string path = m_sbSavePath.ToString();
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		m_sbSavePath.Append(Path.AltDirectorySeparatorChar);
		return m_sbSavePath.ToString();
	}

	public string EncodeName(string name)
	{
		byte[] bytes = BitConverter.GetBytes(name.GetHashCode());
		AccountId localUniqId = Overlay.instance.localUniqId;
		object obj = localUniqId.format switch
		{
			UidFormat.UInt32 => bytes.Add(BitConverter.GetBytes(localUniqId.ToUInt32())), 
			UidFormat.UInt64 => bytes.Add(BitConverter.GetBytes(localUniqId.ToUInt64())), 
			UidFormat.Hex128BString => bytes.Add(BitConverter.GetBytes(localUniqId.ToHexString().GetHashCode())), 
			_ => bytes.Add(BitConverter.GetBytes(localUniqId.GetHashCode())), 
		};
		string text = "";
		byte[] array = (byte[])obj;
		foreach (byte b in array)
		{
			text += "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"[b % "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".Length];
		}
		return text;
	}

	public void Read(BasePrefsModel model)
	{
		byte[] array = null;
		string text = null;
		try
		{
			string text2 = null;
			try
			{
				string text3 = m_savePath + model.modelName;
				string text4 = m_newSavePath + model.modelName + ".sav";
				if (File.Exists(text4))
				{
					text = text4;
					array = File.ReadAllBytes(text);
					text2 = SymmetricAlgorithmTextCrypter.DecryptZip(array);
				}
				else if (File.Exists(text3))
				{
					m_loadedOldSaves = true;
					text = text3;
					array = File.ReadAllBytes(text);
					text2 = SymmetricAlgorithmTextCrypter.Decrypt(array);
				}
			}
			catch (Exception ex)
			{
				AnalyticsService.SendLoadPrefsError(ex, text, array);
				GameLog.Print(AlertLevel.Error, "System", "Fail read data: " + ex.Message);
				model.ResetByLoadError();
				return;
			}
			if (!string.IsNullOrEmpty(text2))
			{
				JSON.Parse(text2, model);
				if (model.accountId != Overlay.playerId.accountId)
				{
					model.ResetByLoadError();
				}
			}
		}
		catch (Exception ex2)
		{
			AnalyticsService.SendLoadPrefsError(ex2, text, array);
			GameLog.Print(AlertLevel.Error, "System", "Data loss: cant load " + model.GetType().Name + " E:" + ex2.Message);
			model.ResetByLoadError();
		}
	}

	public void Save(string modelName, string data)
	{
		if (string.IsNullOrEmpty(m_savePath))
		{
			throw new Exception("Invalid save path");
		}
		byte[] array = SymmetricAlgorithmTextCrypter.CryptZip(data);
		string text = m_newSavePath + modelName + "_temp.sav";
		string text2 = m_newSavePath + modelName + ".sav";
		try
		{
			bool flag = File.Exists(text2);
			using (FileStream fileStream = new FileStream(flag ? text : text2, FileMode.OpenOrCreate))
			{
				fileStream.Write(array, 0, array.Length);
				fileStream.SetLength(array.Length);
			}
			if (flag)
			{
				File.Replace(text, text2, null, ignoreMetadataErrors: false);
			}
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "System", "Failed to save " + modelName + ": " + ex.Message);
		}
	}

	public void FlushSave()
	{
	}

	public abstract bool SkipClearProfileOnReadError();

	public abstract bool IsRemoteSupported();

	public abstract void WriteToRemoteStorage(string modelName, byte[] data);

	public abstract void ReadFromRemoteStorage(string modelName, out byte[] data);

	public abstract bool NeedUpdateRemote(BasePrefsModel model, string remoteName);
}
