using System;
using System.Collections;
using System.Collections.Generic;
using CarX.Platform;
using GameOverlay;
using SimpleJSON;
using UnityEngine;

namespace PrefsModels
{
	public abstract class BasePrefsModel
	{
		[JsonIgnore]
		public bool isSaveAllowed;

		private bool m_loadingFromResetByError;

		private bool m_isLastLoadFailed;

		protected static IStorageSystem m_storageSystem;

		protected static PrefsReserveSystem m_reserveSystem;

		private static List<BasePrefsModel> m_queriesToSave = new List<BasePrefsModel>();

		private static float m_timeToNextRealSave;

		private static int m_saveQueryCounter;

		private static bool m_lockSaves;

		private static string m_savePath = null;

		private static int m_writeAccessCount;

		private static float m_lastResetWriteAccessTime;

		public ulong pid;

		public ulong pid1;

		public UidFormat format = UidFormat.UInt64;

		private string m_modelName;

		[JsonIgnore]
		public bool isLastLoadFailed
		{
			get
			{
				return m_isLastLoadFailed;
			}
			set
			{
				m_isLastLoadFailed = value;
			}
		}

		public static IStorageSystem storageSystem => m_storageSystem ?? InitSaveSystem();

		public static PrefsReserveSystem reserveSystem => m_reserveSystem;

		public AccountId accountId => new AccountId(pid, pid1, format);

		public string modelName => m_modelName ?? (m_modelName = GetModelName());

		public virtual void Reset()
		{
			GameLog.Print(AlertLevel.Notify, "GamePrefs", "Call reset for " + GetType().Name);
			try
			{
				JSON.Parse(JSON.Serialize(Activator.CreateInstance(GetType()) as BasePrefsModel), this);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error reset {GetType()}: E: {ex.Message}");
			}
			PostLoad();
			Start();
		}

		public void ResetByLoadError()
		{
			if (m_loadingFromResetByError)
			{
				isLastLoadFailed = true;
				m_loadingFromResetByError = false;
				GameLog.Print(AlertLevel.FatalError, "System", "PrefsModel total failure. Data loss!");
				PostLoad();
				Start();
				return;
			}
			try
			{
				m_loadingFromResetByError = true;
				Reset();
			}
			finally
			{
				m_loadingFromResetByError = false;
				isLastLoadFailed = true;
			}
		}

		public virtual void Load()
		{
			isLastLoadFailed = false;
			bool flag = isSaveAllowed;
			isSaveAllowed = false;
			try
			{
				storageSystem.Read(this);
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "System", "Data loss: cant load " + GetType().Name + " E:" + ex.Message);
				ResetByLoadError();
			}
			isSaveAllowed = flag;
		}

		public virtual void Restore(string date)
		{
			reserveSystem.Restore(date, this);
		}

		public virtual void MakeBackup(string date)
		{
			reserveSystem.Save(date, this);
		}

		public static IStorageSystem InitSaveSystem()
		{
			m_storageSystem = Overlay.CreateStorageSystem();
			m_reserveSystem = new PrefsReserveSystem();
			return m_storageSystem;
		}

		public virtual void PostLoad()
		{
		}

		public virtual void Start()
		{
		}

		public virtual void Save()
		{
			try
			{
				if (isSaveAllowed)
				{
					AddQueryToSave(this);
					GameSync.SetNeedSync();
				}
			}
			catch
			{
				GameLog.Print(AlertLevel.Error, "System", "Data loss: cant save " + GetType().Name);
			}
		}

		private void DoRealSave()
		{
			try
			{
				pid = Overlay.playerId.accountId.id0;
				pid1 = Overlay.playerId.accountId.id1;
				format = Overlay.playerId.accountId.format;
				string json = JSON.Serialize(this);
				DoRealSave(json);
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.FatalError, "System", $"{GetType().Name} Save error: {arg}");
			}
		}

		protected virtual void DoRealSave(string json)
		{
			storageSystem.Save(modelName, json);
		}

		public void AddQueryToSave(BasePrefsModel item)
		{
			if (m_queriesToSave.IndexOf(item) == -1)
			{
				m_queriesToSave.Add(item);
			}
			m_saveQueryCounter++;
			UpdateNextRealSaveTime();
		}

		private static void UpdateNextRealSaveTime()
		{
			m_timeToNextRealSave = Time.unscaledTime + 1f;
		}

		private static void UpdateWritingAccessCooldown(float currTime)
		{
			if (!(currTime < m_lastResetWriteAccessTime + 1f))
			{
				m_lastResetWriteAccessTime = currTime;
				m_writeAccessCount = 0;
			}
		}

		public static void UpdateDefferedSaves()
		{
			float unscaledTime = Time.unscaledTime;
			UpdateWritingAccessCooldown(unscaledTime);
			if (!m_lockSaves && m_queriesToSave.Count > 0 && unscaledTime >= m_timeToNextRealSave)
			{
				TimeController.StartNow(PerformSaveCoroutine(ignoreWriteLimit: false));
			}
		}

		public static Coroutine SaveAllQueries(bool inOneFrame)
		{
			if (m_queriesToSave.Count > 0)
			{
				if (!inOneFrame)
				{
					return TimeController.StartNow(PerformSaveCoroutine(ignoreWriteLimit: false));
				}
				IEnumerator enumerator = PerformSaveCoroutine(ignoreWriteLimit: true);
				while (enumerator.MoveNext())
				{
				}
			}
			return null;
		}

		private static IEnumerator PerformSaveCoroutine(bool ignoreWriteLimit)
		{
			if (m_queriesToSave.Count == 0)
			{
				yield break;
			}
			int queriesCount = 0;
			if (m_queriesToSave.Count > 12)
			{
				queriesCount = m_queriesToSave.Count;
				StartState.m_saveEnded = false;
			}
			while (m_queriesToSave.Count > 0)
			{
				if (!ignoreWriteLimit && IsWriteAccessExceeded())
				{
					yield break;
				}
				m_writeAccessCount++;
				BasePrefsModel basePrefsModel = m_queriesToSave.Last();
				m_queriesToSave.RemoveAt(m_queriesToSave.Count - 1);
				basePrefsModel.DoRealSave();
				yield return null;
			}
			yield return null;
			storageSystem.FlushSave();
			yield return null;
			m_saveQueryCounter = 0;
			if (queriesCount != 0)
			{
				StartState.m_saveEnded = true;
			}
		}

		public static void LockSaves(bool flag)
		{
			m_lockSaves = flag;
			if (flag)
			{
				UpdateNextRealSaveTime();
			}
			GameLog.Print(AlertLevel.Notify, "Save", $"Set GLOBAL Lock: {m_lockSaves}");
		}

		protected virtual string GetModelName()
		{
			return storageSystem.EncodeName(GetType().Name);
		}

		protected virtual bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			return true;
		}

		private static bool IsWriteAccessExceeded()
		{
			return m_writeAccessCount > 50;
		}
	}
}
