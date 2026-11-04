using System;
using ArrayExtension;
using DI;
using PrefsModels;
using SimpleJSON;
using UnityEngine;

public class PrefsReserveSystem
{
	protected static PCPrefsStorageSystem m_reserveSystem;

	private static readonly string backups_key = "Backups";

	public static PCPrefsStorageSystem reserveSystem => m_reserveSystem;

	[JsonIgnore]
	public bool isCompareFailed { get; private set; }

	[JsonIgnore]
	public BasePrefsModel reserveModel { get; private set; }

	public PrefsReserveSystem()
	{
		GameConsole.SetCmd("GetBackups", delegate
		{
			string[] listHistory = GetListHistory();
			for (int i = 0; i < listHistory.Length; i++)
			{
				GameConsole.Print(listHistory[i]);
			}
		});
		GameConsole.SetVar("RestoreBackup", "");
		GameConsole.SetCmd("RestoreBackup", delegate
		{
			DependencyInjector.Resolve<GamePrefs>().RestoreBackup(GameConsole.String["RestoreBackup"]);
		});
		GameConsole.SetVar("MakeBackup", "");
		GameConsole.SetCmd("MakeBackup", delegate
		{
			DependencyInjector.Resolve<GamePrefs>().MakeBackup(GameConsole.String["MakeBackup"]);
		});
	}

	public string[] GetListHistory()
	{
		return PlayerPrefsX.GetStringArray(backups_key);
	}

	public void Restore(DateTime date, BasePrefsModel model)
	{
		Restore(date.ToShortDateString(), model);
	}

	public void Restore(string date, BasePrefsModel model)
	{
		if (m_reserveSystem == null || m_reserveSystem.prefix != date)
		{
			m_reserveSystem = new PCPrefsStorageSystem(date);
		}
		BasePrefsModel basePrefsModel;
		try
		{
			basePrefsModel = Activator.CreateInstance(model.GetType()) as BasePrefsModel;
			m_reserveSystem.Read(basePrefsModel);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Save", "Fail loading reverse prefs " + model.modelName + ": " + ex.Message);
			basePrefsModel = null;
		}
		if (basePrefsModel == null)
		{
			GameLog.Print(AlertLevel.Warning, "GamePrefs", "Restore data for " + model.GetType().Name + " is empty");
			return;
		}
		GameLog.Print(AlertLevel.Notify, "GamePrefs", "Restore for " + model.GetType().Name);
		try
		{
			JSON.Parse(JSON.Serialize(basePrefsModel), model);
		}
		catch (Exception ex2)
		{
			Debug.LogError($"Error restore {GetType()}: E: {ex2.Message}");
		}
		model.PostLoad();
		model.Start();
	}

	public void Save(DateTime date, BasePrefsModel model)
	{
		Save(date.ToShortDateString(), model);
	}

	public void Save(string date, BasePrefsModel model)
	{
		string[] listHistory = GetListHistory();
		if (!listHistory.Contains(date))
		{
			PlayerPrefsX.SetStringArray(backups_key, listHistory.Add(date));
		}
		if (m_reserveSystem == null || m_reserveSystem.prefix != date)
		{
			m_reserveSystem = new PCPrefsStorageSystem(date);
		}
		m_reserveSystem.Save(model.modelName, JSON.Serialize(model));
	}

	public void OptimizeBackups()
	{
	}
}
