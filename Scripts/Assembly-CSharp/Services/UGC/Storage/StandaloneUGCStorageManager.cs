using System;
using System.IO;
using Services.UGC.Interfaces;
using Services.UGC.Platforms;
using UnityEngine;

namespace Services.UGC.Storage
{
	public class StandaloneUGCStorageManager : IUGCStorageManager
	{
		private DriveInfo m_plsDriveInfo;

		private UGCService<IUGCManager> m_service;

		public StandaloneUGCStorageManager(UGCService<IUGCManager> service)
		{
			InitializePlsDriveInfo();
			m_service = service;
		}

		public bool IsReady()
		{
			return m_plsDriveInfo != null;
		}

		private void InitializePlsDriveInfo()
		{
			try
			{
				FileInfo fileInfo = new FileInfo(Application.persistentDataPath);
				m_plsDriveInfo = new DriveInfo(Path.GetPathRoot(fileInfo.FullName));
			}
			catch (Exception message)
			{
				m_plsDriveInfo = null;
				Debug.LogError(message);
			}
		}

		public ulong GetAvailableSizeBytes()
		{
			return (ulong)m_plsDriveInfo.AvailableFreeSpace;
		}

		public ulong GetSizeBytes()
		{
			return (ulong)m_plsDriveInfo.TotalSize;
		}

		public bool IsEnoughSpaceToInstall(IUGCMeta meta)
		{
			return meta.sizeBytes + meta.archievedSizeBytes + m_service.GetRequestedQueueItemsTotalSizeBytes() < (ulong)m_plsDriveInfo.AvailableFreeSpace;
		}
	}
}
