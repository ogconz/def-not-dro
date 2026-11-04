using System;
using System.Collections.Generic;
using System.IO;
using Customization;
using DI;
using GameOverlay;
using UnityEngine;

public static class IconStorageSystem
{
	private const string RootDirectoryName = "Icons";

	private const string FileExtension = ".icn";

	private const int ItemsCountLimit = 140;

	private static Dictionary<string, Texture2D> m_externalLinks = new Dictionary<string, Texture2D>();

	private static Dictionary<string, Texture2D> m_internalCache = new Dictionary<string, Texture2D>();

	private static List<string> m_clearQueue = new List<string>();

	private static GamePrefs s_prefsInstance;

	private static GamePrefs s_prefs
	{
		get
		{
			if (!(s_prefsInstance != null))
			{
				return s_prefsInstance = DependencyInjector.Resolve<GamePrefs>();
			}
			return s_prefsInstance;
		}
	}

	private static void SaveInternal(string path, Texture2D data)
	{
		try
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "SAVE: " + GetPathInfo(path));
			AddIconToCache(path, data, isExternal: true);
			path = GetAbsoluteIconPath(path);
			CreateDirectoryIfNeed(path);
			using FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
			using BinaryWriter binaryWriter = new BinaryWriter(fileStream);
			fileStream.SetLength(0L);
			binaryWriter.Write(data.EncodeToJPG());
		}
		catch (Exception arg)
		{
			GameLog.Print(AlertLevel.Error, "IconStorage", $"Failed to save {path}: {arg}");
		}
	}

	private static Texture2D ReadInternal(string path)
	{
		try
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "READ: " + GetPathInfo(path));
			if (TryGetIconFromCache(path, out var texture))
			{
				return texture;
			}
			path = GetAbsoluteIconPath(path);
			if (!File.Exists(path))
			{
				return null;
			}
			texture = CreateTextureFromData(File.ReadAllBytes(path));
			AddIconToCache(path, texture, isExternal: false);
			return texture;
		}
		catch (Exception arg)
		{
			GameLog.Print(AlertLevel.Error, "IconStorage", $"Failed to read {path}: {arg}");
			return null;
		}
	}

	private static void DeleteInternal(string path)
	{
		try
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "DELETE: " + GetPathInfo(path));
			RemoveIconFromCache(path);
			path = GetAbsoluteIconPath(path);
			if (File.Exists(path))
			{
				File.Delete(path);
				DeleteDirectoryIfNeed(path);
			}
		}
		catch (Exception arg)
		{
			GameLog.Print(AlertLevel.Error, "IconStorage", $"Failed to delete {path}: {arg}");
		}
	}

	public static void ClearStorage()
	{
		OptimizeCacheIfNeed(0);
		string storageRootPath = GetStorageRootPath();
		if (Directory.Exists(storageRootPath))
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "DELETE_DIR: " + storageRootPath);
			Directory.Delete(storageRootPath, recursive: true);
		}
	}

	private static void AddIconToCache(string path, Texture2D data, bool isExternal)
	{
		path = GetRelativeIconPath(path);
		if (isExternal)
		{
			m_externalLinks[path] = data;
			return;
		}
		AddOrUpdateIconLive(path);
		OptimizeCacheIfNeed(140);
		if (m_internalCache.ContainsKey(path) && m_internalCache[path] != null && m_internalCache[path] != data)
		{
			UnityEngine.Object.Destroy(m_internalCache[path]);
			m_internalCache[path] = null;
		}
		m_internalCache[path] = data;
	}

	private static void RemoveIconFromCache(string path)
	{
		path = GetRelativeIconPath(path);
		if (m_externalLinks.ContainsKey(path))
		{
			m_externalLinks[path] = null;
			m_externalLinks.Remove(path);
		}
		if (m_internalCache.ContainsKey(path) && m_internalCache[path] != null)
		{
			UnityEngine.Object.Destroy(m_internalCache[path]);
			m_internalCache[path] = null;
			m_internalCache.Remove(path);
		}
		m_clearQueue.Remove(path);
	}

	private static bool TryGetIconFromCache(string path, out Texture2D texture)
	{
		path = GetRelativeIconPath(path);
		if (m_externalLinks.ContainsKey(path) && m_externalLinks[path] != null)
		{
			texture = m_externalLinks[path];
			return true;
		}
		if (m_internalCache.ContainsKey(path) && m_internalCache[path] != null)
		{
			texture = m_internalCache[path];
			return true;
		}
		texture = null;
		return false;
	}

	private static void OptimizeCacheIfNeed(int itemsCountLimit)
	{
		for (int num = m_clearQueue.Count - itemsCountLimit; num > 0; num--)
		{
			RemoveIconFromCache(m_clearQueue[0]);
		}
	}

	private static void AddOrUpdateIconLive(string icon)
	{
		m_clearQueue.Remove(icon);
		m_clearQueue.Add(icon);
	}

	private static string GetStorageRootPath()
	{
		return Path.Combine(Application.persistentDataPath, "Icons", Overlay.playerId.accountId.ToString());
	}

	private static string GetAbsoluteIconPath(string path)
	{
		string storageRootPath = GetStorageRootPath();
		if (path.IndexOf(storageRootPath, StringComparison.Ordinal) == -1)
		{
			path = Path.Combine(storageRootPath, path);
		}
		if (path.IndexOf(".icn", StringComparison.Ordinal) == -1)
		{
			path += ".icn";
		}
		return path;
	}

	private static string GetRelativeIconPath(string path)
	{
		string storageRootPath = GetStorageRootPath();
		int num = path.IndexOf(storageRootPath, StringComparison.Ordinal);
		if (num != -1)
		{
			path = path.Remove(num, storageRootPath.Length + 1);
		}
		num = path.IndexOf(".icn", StringComparison.Ordinal);
		if (num != -1)
		{
			path = path.Remove(num, ".icn".Length);
		}
		return path;
	}

	private static void CreateDirectoryIfNeed(string path)
	{
		path = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "CREATE_DIR: " + GetPathInfo(path));
			Directory.CreateDirectory(path);
		}
	}

	private static void DeleteDirectoryIfNeed(string path)
	{
		path = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(path) && Directory.GetFiles(path).Length <= 0)
		{
			GameLog.Print(AlertLevel.Notify, "IconStorage", "DELETE_DIR: " + GetPathInfo(path));
			Directory.Delete(path, recursive: true);
		}
	}

	private static Texture2D CreateTextureFromData(byte[] data)
	{
		if (data == null)
		{
			return null;
		}
		Texture2D texture2D = new Texture2D(1, 1);
		try
		{
			texture2D.LoadImage(data);
			texture2D.Apply();
			return texture2D;
		}
		catch (Exception ex)
		{
			UnityEngine.Object.Destroy(texture2D);
			GameLog.Print(AlertLevel.Error, "IconStorage", $"Failed to load texture data ({data.Length}): {ex.Message}");
			return null;
		}
	}

	private static string GetPathInfo(string path)
	{
		return "cache:" + GetRelativeIconPath(path) + ", storage:" + GetAbsoluteIconPath(path);
	}

	private static string GetLiveryPreviewRelativePath(int carId, int slotId)
	{
		PrefsCarSlotDataModel slotById = s_prefs.carSettings.GetSlotById(carId, slotId);
		if (slotById == null)
		{
			return string.Empty;
		}
		string liveryId = slotById.visual?.vinylPresetLiveryId;
		if (slotById.visual != null && CarPartDatabase.IsPartsNotSupportLivery(slotById.visual.parts))
		{
			liveryId = "dlc";
		}
		return GetLiveryPreviewRelativePath(carId, slotId, liveryId);
	}

	private static string GetLiveryPreviewRelativePath(int carId, string liveryId)
	{
		return GetLiveryPreviewRelativePath(carId, s_prefs.carSettings.GetActiveSlotId(carId), liveryId);
	}

	private static string GetLiveryPreviewRelativePath(int carId, int slotId, string liveryId)
	{
		return $"LiveryPreview/{carId}_{slotId}_{liveryId}";
	}

	public static void SaveLiveryPreview(int carId, string liveryId, Texture2D data)
	{
		SaveInternal(GetLiveryPreviewRelativePath(carId, liveryId), data);
	}

	public static Texture2D ReadLiveryPreview(int carId, string liveryId)
	{
		return ReadInternal(GetLiveryPreviewRelativePath(carId, liveryId));
	}

	public static void DeleteLiveryPreview(int carId, string liveryId)
	{
		DeleteInternal(GetLiveryPreviewRelativePath(carId, liveryId));
	}

	public static void SaveLiveryPreview(int carId, int slotId, Texture2D data)
	{
		SaveInternal(GetLiveryPreviewRelativePath(carId, slotId), data);
	}

	public static Texture2D ReadLiveryPreview(int carId, int slotId)
	{
		return ReadInternal(GetLiveryPreviewRelativePath(carId, slotId));
	}

	public static void DeleteLiveryPreview(int carId, int slotId)
	{
		DeleteInternal(GetLiveryPreviewRelativePath(carId, slotId));
	}

	private static string GetGaragePreviewRelativePath(int carId, int slotId)
	{
		return $"GaragePreview/{carId}_{slotId}";
	}

	public static void SaveGaragePreview(int carId, int slotId, Texture2D data)
	{
		SaveInternal(GetGaragePreviewRelativePath(carId, slotId), data);
	}

	public static Texture2D ReadGaragePreview(int carId, int slotId)
	{
		return ReadInternal(GetGaragePreviewRelativePath(carId, slotId));
	}
}
