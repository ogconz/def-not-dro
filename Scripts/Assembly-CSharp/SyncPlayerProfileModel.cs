using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using PrefsModels;
using SimpleJSON;
using UnityEngine;

public class SyncPlayerProfileModel
{
	public int exp;

	public int gold;

	public Dictionary<int, LocksModel.CarLocksDescriptor> carLocks = new Dictionary<int, LocksModel.CarLocksDescriptor>();

	public HashSet<int> availableCars;

	public HashSet<int> availableTracks;

	public HashSet<int> availableProfiles;

	public int driftRacesCount;

	public int timeRacesCount;

	public int mpRacesCount;

	public float maxPointsPerRace;

	public float maxPointsPerDrift;

	public float averagePoints;

	public int cups1;

	public int cups2;

	public int cups3;

	public bool isDLCMoneyApplied;

	public bool isOldCarsRestored;

	public long timeInGame;

	public string customization;

	public static string GetJSONString()
	{
		return new SyncPlayerProfileModel().ToJSON();
	}

	public static void ClearGamePrefs()
	{
		GameLog.Print(AlertLevel.Notify, "GamePrefs", "Call clear game prefs");
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		GlobalGameSettings single = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>();
		gamePrefs.DisableSaveForAll();
		gamePrefs.globals.lastCarId = single.startFocusedCar;
		gamePrefs.playerProfile.exp = 0;
		gamePrefs.money.Reset();
		gamePrefs.money.coins = single.startCoins;
		gamePrefs.money.isDLCMoneyApplied = false;
		gamePrefs.money.ApplyDLCMoney();
		gamePrefs.inventory.ClearAll();
		gamePrefs.favoriteCars.ClearAll();
		gamePrefs.favoriteTrack.ClearAll();
		gamePrefs.favoriteRims.ClearAll();
		gamePrefs.compensationModel.ClearAll();
		gamePrefs.locks.Reset();
		gamePrefs.stats.Reset();
		gamePrefs.slotLock.Reset();
		gamePrefs.slotProgression.Reset();
		gamePrefs.carSettings.Reset();
		gamePrefs.globals.lastLevelNotify = 1;
		gamePrefs.globals.waypoints.Clear();
		gamePrefs.EnableSaveForAll();
		gamePrefs.SaveAll();
	}

	private static int FindDefaultUnlockedCarId()
	{
		foreach (PlayerCar item in DependencyInjector.Resolve<BaseModel>().QueryAll<PlayerCar>())
		{
			if (!item.defaultLocked)
			{
				return item.id;
			}
		}
		return -1;
	}

	public void MergeToGamePrefs()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.DisableSaveForAll();
		gamePrefs.playerProfile.exp = Mathf.Max(gamePrefs.playerProfile.exp, exp);
		gamePrefs.money.coins = Mathf.Max(gamePrefs.money.coins, gold);
		gamePrefs.money.isDLCMoneyApplied = isDLCMoneyApplied;
		gamePrefs.money.ApplyDLCMoney();
		gamePrefs.locks.isOldCarsRestored = isOldCarsRestored;
		gamePrefs.locks.MergeLocks(TransactionManager.UnlockType.Car, availableCars);
		gamePrefs.locks.MergeLocks(TransactionManager.UnlockType.Track, availableTracks);
		gamePrefs.locks.MergeLocks(TransactionManager.UnlockType.Profile, availableProfiles);
		gamePrefs.locks.carLocks = carLocks;
		gamePrefs.locks.ApplyStylishDriftAchievements();
		gamePrefs.stats.driftRacesPlayed = Mathf.Max(gamePrefs.stats.driftRacesPlayed, driftRacesCount);
		gamePrefs.stats.maxPointsPerRace = Mathf.Max(gamePrefs.stats.maxPointsPerRace, maxPointsPerRace);
		gamePrefs.stats.maxPointsPerDrift = Mathf.Max(gamePrefs.stats.maxPointsPerDrift, maxPointsPerDrift);
		gamePrefs.stats.averagePointsPerRace = Mathf.Max(gamePrefs.stats.averagePointsPerRace, averagePoints);
		gamePrefs.stats.timeAttackRacesPlayed = Mathf.Max(gamePrefs.stats.timeAttackRacesPlayed, timeRacesCount);
		gamePrefs.stats.MPRacesPlayed = Mathf.Max(gamePrefs.stats.MPRacesPlayed, mpRacesCount);
		gamePrefs.stats.cups1 = Mathf.Max(gamePrefs.stats.cups1, cups1);
		gamePrefs.stats.cups2 = Mathf.Max(gamePrefs.stats.cups2, cups2);
		gamePrefs.stats.cups3 = Mathf.Max(gamePrefs.stats.cups3, cups3);
		if (timeInGame > gamePrefs.stats.timeInGame)
		{
			gamePrefs.stats.timeInGame = timeInGame;
		}
		if (string.IsNullOrEmpty(customization))
		{
			gamePrefs.carSettings.ReadFromRemote();
		}
		else
		{
			gamePrefs.carSettings.DeserializeAndMergeBase(customization);
		}
		gamePrefs.EnableSaveForAll();
		gamePrefs.SaveAll();
	}

	private string ToJSON()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		exp = gamePrefs.playerProfile.exp;
		gold = gamePrefs.money.coins;
		isDLCMoneyApplied = gamePrefs.money.isDLCMoneyApplied;
		isOldCarsRestored = gamePrefs.locks.isOldCarsRestored;
		carLocks = gamePrefs.locks.carLocks;
		availableCars = gamePrefs.locks.availableCars;
		availableTracks = gamePrefs.locks.availableTracks;
		availableProfiles = gamePrefs.locks.availableProfiles;
		driftRacesCount = gamePrefs.stats.driftRacesPlayed;
		maxPointsPerRace = gamePrefs.stats.maxPointsPerRace;
		maxPointsPerDrift = gamePrefs.stats.maxPointsPerDrift;
		averagePoints = gamePrefs.stats.averagePointsPerRace;
		timeRacesCount = gamePrefs.stats.timeAttackRacesPlayed;
		mpRacesCount = gamePrefs.stats.MPRacesPlayed;
		cups1 = gamePrefs.stats.cups1;
		cups2 = gamePrefs.stats.cups2;
		cups3 = gamePrefs.stats.cups3;
		timeInGame = gamePrefs.stats.timeInGame;
		customization = string.Empty;
		return JSON.Serialize(this);
	}
}
