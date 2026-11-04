using System;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public static class StatsManager
{
	public enum EStats
	{
		None = 0,
		Travel = 1,
		DriftDistance = 2,
		DriftScore = 3,
		CupsBronze = 4,
		CupsSilver = 5,
		CupsGold = 6,
		CupsAllGold = 7,
		ProfileLevel = 8,
		MpWins = 9,
		MpFriendWins = 10,
		ATSpeed = 11,
		ATSideChange = 12,
		ATAngle = 13,
		ATAccuracy = 14,
		ATQuality = 15,
		ATBackward = 16,
		ATClippingZone = 17,
		ATClippingPoint = 18,
		StylishDrift = 19,
		WinterDrift = 20,
		WeatherHindrance = 21,
		Voodoo = 22,
		DriftGuardian = 23,
		QuantityQuality = 24,
		HalloweenCountPumpkins2019 = 25,
		HalloweenWin = 26
	}

	private const float MAX_F_INCREMENT = 10000000f;

	private const int MAX_I_INCREMENT = 1000000;

	private static IStatsWorker m_statsWorker;

	private static int m_winterTrackId = -1;

	private static int m_winterTrackRevercedId = -1;

	private static int m_voodoCarId = -1;

	private static int m_hornerCarId = -1;

	private static GamePrefs m_prefs;

	public static void Init()
	{
		m_statsWorker = Overlay.instance.CreateStatsWorker();
		m_statsWorker.Init();
		GameConsole.SetCmd("clear_stats", ClearStats, GameConsole.Flags.Hidden);
		GameConsole.SetCmd("show_stats", DebugAllStats, GameConsole.Flags.Hidden);
		FindIds();
	}

	private static void FindIds()
	{
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		foreach (DB.Meta.Track item in baseModel.QueryAll<DB.Meta.Track>())
		{
			if (item.scene.ToLowerInvariant().Equals("winterfell"))
			{
				if (item.configuration == "Reversed")
				{
					m_winterTrackRevercedId = item.id;
				}
				else
				{
					m_winterTrackId = item.id;
				}
			}
		}
		if (m_winterTrackId == -1)
		{
			Debug.LogError("Not found Winter track!");
		}
		if (m_winterTrackRevercedId == -1)
		{
			Debug.LogError("Not found Winter reversed track!");
		}
		foreach (PlayerCar item2 in baseModel.QueryAll<PlayerCar>())
		{
			if (string.Equals(item2.name, "voodoo", StringComparison.OrdinalIgnoreCase))
			{
				m_voodoCarId = item2.id;
			}
			if (string.Equals(item2.name, "hornet gt", StringComparison.OrdinalIgnoreCase))
			{
				m_hornerCarId = item2.id;
			}
		}
		if (m_voodoCarId == -1)
		{
			Debug.LogError("Not found car Voodoo!");
		}
		if (m_hornerCarId == -1)
		{
			Debug.LogError("Not found car Hornet GT!");
		}
	}

	public static bool IsWinterfallTrackActive()
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		int lastTrackId = m_prefs.globals.lastTrackId;
		if (lastTrackId != m_winterTrackId)
		{
			return lastTrackId == m_winterTrackRevercedId;
		}
		return true;
	}

	public static bool IsVoodoActive()
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		return m_prefs.globals.lastCarId == m_voodoCarId;
	}

	public static bool IsCamaroActive()
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		return m_prefs.globals.lastCarId == m_hornerCarId;
	}

	public static void ClearStats()
	{
		m_statsWorker.ClearStats();
	}

	public static void GetStat(EStats stat, out int value)
	{
		m_statsWorker.GetStat(stat.ToString(), out value);
	}

	public static void GetStat(EStats stat, out float value)
	{
		m_statsWorker.GetStat(stat.ToString(), out value);
	}

	public static string GetStatAsString(EStats stat, string format = "N")
	{
		GetStat(stat, out float value);
		GetStat(stat, out int value2);
		string empty = string.Empty;
		empty = ((value > (float)value2) ? value.ToString(format, UIUtils.numbersFormatter) : value2.ToString(format, UIUtils.numbersFormatter));
		Log(stat.ToString() + " : " + empty);
		return empty;
	}

	public static void SetStat(EStats stat, int value)
	{
		if (m_statsWorker != null)
		{
			m_statsWorker.SetStat(stat.ToString(), ValidateInt(value));
			Log("Set stat: " + stat.ToString() + " : " + value);
		}
	}

	public static void SetStat(EStats stat, float value)
	{
		if (m_statsWorker != null)
		{
			m_statsWorker.SetStat(stat.ToString(), ValidateFloat(value));
			Log("Set stat: " + stat.ToString() + " : " + value);
		}
	}

	public static void IncStat(EStats stat)
	{
		GetStat(stat, out int value);
		SetStat(stat, ValidateInt(value) + 1);
	}

	public static void AddToStat(EStats stat, int added)
	{
		if (added != 0 && IsIntValid(added) && Mathf.Abs(added) <= 1000000)
		{
			GetStat(stat, out int value);
			SetStat(stat, ValidateInt(value) + added);
		}
	}

	public static void AddToStat(EStats stat, float added)
	{
		if (IsFloatValid(added) && added != 0f && !(Mathf.Abs(added) > 10000000f))
		{
			GetStat(stat, out float value);
			SetStat(stat, ValidateFloat(value) + added);
		}
	}

	public static void TrySetMaxStat(EStats stat, int value)
	{
		if (m_statsWorker != null && IsIntValid(value))
		{
			GetStat(stat, out int value2);
			value2 = ValidateInt(value2);
			if (value > value2)
			{
				SetStat(stat, value);
			}
		}
	}

	public static void TrySetMaxStat(EStats stat, float value)
	{
		if (IsFloatValid(value))
		{
			GetStat(stat, out float value2);
			value2 = ValidateFloat(value2);
			if (value > value2)
			{
				SetStat(stat, value);
			}
		}
	}

	private static bool IsFloatValid(float value)
	{
		if (float.IsNaN(value))
		{
			return false;
		}
		if (float.IsInfinity(value))
		{
			return false;
		}
		return true;
	}

	private static float ValidateFloat(float value)
	{
		if (float.IsNaN(value))
		{
			return 0f;
		}
		if (float.IsInfinity(value))
		{
			return 0f;
		}
		return value;
	}

	private static bool IsIntValid(int value)
	{
		if (value < 0)
		{
			return false;
		}
		return true;
	}

	private static int ValidateInt(int value)
	{
		if (value < 0)
		{
			return 0;
		}
		return value;
	}

	private static void Log(object obj)
	{
		GameLog.Print(AlertLevel.Notify, "Achievements", obj.ToString());
	}

	public static void DebugAllStats()
	{
		foreach (EStats value3 in Enum.GetValues(typeof(EStats)))
		{
			GetStat(value3, out float value);
			GetStat(value3, out int value2);
			Debug.Log(value3.ToString() + " : " + Mathf.Max(value, value2));
		}
		foreach (Achievement achievement2 in SteamUserStats.Achievements)
		{
			bool achievement = SteamServerStats.GetAchievement(SteamClient.SteamId, achievement2.Name);
			Debug.Log(achievement2.Name + " " + achievement);
		}
		SteamUserStats.StoreStats();
	}
}
