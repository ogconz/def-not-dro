using System;
using DB.Meta;
using DI;
using UnityEngine;

public class TransactionManager
{
	private enum State
	{
		NotConnected = 0,
		Connecting = 1,
		Connected = 2,
		Sending = 3
	}

	public enum MoneyAddReason
	{
		None = 0,
		InAppPurchase = 1,
		WatchedVideo = 2,
		PageLike = 3,
		FBShare = 4,
		Subscription = 5,
		RaceDriftAward = 6,
		RaceDriftAwardBonus = 7,
		RaceResultAward = 8,
		RaceResultAwardBonus = 9,
		BonusCode = 10,
		MultiplayerReward = 11,
		Cheat = 12,
		Tutorial = 13,
		PushMessage = 14,
		Quest = 15
	}

	public enum UnlockType
	{
		Car = 0,
		Profile = 1,
		Track = 2,
		CarColor = 3,
		WheelRimColor = 4,
		WheelSpokeColor = 5,
		SmokeColor = 6,
		GlassColor = 7,
		CarPart = 8
	}

	public enum ConsumableType
	{
		ProfileRent = 0
	}

	public Action<UnlockType, int> onUnlockTransactionPerformed;

	[Dependency]
	private GamePrefs m_prefs;

	public bool isConnected => false;

	public TransactionManager()
	{
		DependencyInjector.InjectObjectsInto(this);
	}

	public void SetMoney(int money, ECurrencyType type)
	{
		money = Mathf.Max(0, money);
		m_prefs.money.SetMoney(type, money);
	}

	public void AddMoney(int money, ECurrencyType type, MoneyAddReason reason)
	{
		money = Mathf.Max(0, money);
		int money2 = m_prefs.money.GetMoney(type);
		SetMoney(money2 + money, type);
		if (reason != MoneyAddReason.None)
		{
			AnalyticsService.LogMoneyAdded(money, type, reason);
		}
	}

	private void SubMoney(int money, ECurrencyType type)
	{
		if (money > 0)
		{
			int money2 = m_prefs.money.GetMoney(type);
			if (money2 >= money)
			{
				SetMoney(money2 - money, type);
			}
		}
	}

	private void SubMoney(Cost cost)
	{
		SubMoney(cost.coins, ECurrencyType.Coins);
		SubMoney(cost.cash, ECurrencyType.Premium);
	}

	public int GetMoney(ECurrencyType type)
	{
		return m_prefs.money.GetMoney(type);
	}

	public void OnMoneyPurchaseHappened(int coins, int premium)
	{
		PerformMoneyGainTransaction(coins, premium, MoneyAddReason.InAppPurchase, forceSend: true);
	}

	public void OnBonusCodeMoneyReceived(int coins, int premium)
	{
		PerformMoneyGainTransaction(coins, premium, MoneyAddReason.BonusCode);
	}

	public void OnMultiplayerRewardAcquired(Cost reward)
	{
		PerformMoneyGainTransaction(reward.coins, reward.cash, MoneyAddReason.MultiplayerReward);
	}

	private void AddRaceAward(int coins, int premium, MoneyAddReason reason)
	{
		PerformMoneyGainTransaction(coins, premium, reason);
	}

	public void OnRaceEndAward(int coins)
	{
		AddRaceAward(coins, 0, MoneyAddReason.RaceResultAward);
	}

	public void OnQuestEndAward(int coins)
	{
		PerformMoneyGainTransaction(coins, 0, MoneyAddReason.Quest);
	}

	public void OnRaceWatchWideoAward(int coins, int premium)
	{
		AddRaceAward(coins, premium, MoneyAddReason.RaceResultAwardBonus);
	}

	public void OnWatchWideoFromShopAward(int coins, int premium)
	{
		AddRaceAward(coins, premium, MoneyAddReason.WatchedVideo);
	}

	public bool TryToPerformUnlockTransaction(UnlockType type, int unlockedItemId, int money, ECurrencyType currencyType)
	{
		if (!IsMoneyEnoughToAfford(money, currencyType))
		{
			return false;
		}
		try
		{
			m_prefs.locks.RecordUnlock(type, unlockedItemId);
			SubMoney(money, currencyType);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TransactionManager", "Couldn't perform unlock transaction of " + type.ToString() + " for item " + unlockedItemId + ": " + ex);
			return false;
		}
		onUnlockTransactionPerformed.SafeCall(type, unlockedItemId);
		return true;
	}

	public bool UnlockCarPart(int carId, int carPartId)
	{
		try
		{
			m_prefs.locks.RecordUnlockSpecificCarPart(carId, carPartId);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TransactionManager", "Couldn't perform unlock transaction of " + UnlockType.Car.ToString() + " for car " + carId + " (car part): " + carPartId + ": " + ex);
			return false;
		}
		onUnlockTransactionPerformed.SafeCall(UnlockType.Car, carPartId);
		return true;
	}

	public bool TryToPerformUnlockTransactions(UnlockType type, int[] unlockedItemsIds, int money, ECurrencyType currencyType)
	{
		if (!IsMoneyEnoughToAfford(money, currencyType))
		{
			return false;
		}
		int[] array;
		try
		{
			array = unlockedItemsIds;
			foreach (int unlockedItemId in array)
			{
				m_prefs.locks.RecordUnlock(type, unlockedItemId);
			}
			SubMoney(money, currencyType);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TransactionManager", "Couldn't perform unlock transaction of " + type.ToString() + " for item " + unlockedItemsIds?.ToString() + ": " + ex);
			return false;
		}
		array = unlockedItemsIds;
		foreach (int param in array)
		{
			onUnlockTransactionPerformed.SafeCall(type, param);
		}
		return true;
	}

	public bool TryToPerformConsumableTransaction(ConsumableType type, int unlockedItemId, int money, ECurrencyType currencyType)
	{
		if (!IsMoneyEnoughToAfford(money, currencyType))
		{
			return false;
		}
		try
		{
			SubMoney(money, currencyType);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TransactionManager", "Couldn't perform consumable transaction of " + type.ToString() + " for item " + unlockedItemId + ": " + ex);
			return false;
		}
		return true;
	}

	public bool IsMoneyEnoughToAfford(int spending, ECurrencyType type)
	{
		if (m_prefs.money.GetMoney(type) >= spending)
		{
			return spending >= 0;
		}
		return false;
	}

	public bool IsMoneyEnoughToAfford(Cost cost)
	{
		if (IsMoneyEnoughToAfford(cost.coins, ECurrencyType.Coins))
		{
			return IsMoneyEnoughToAfford(cost.cash, ECurrencyType.Premium);
		}
		return false;
	}

	private void PerformMoneyGainTransaction(int coins, int premium, MoneyAddReason reason, bool forceSend = false)
	{
		forceSend = forceSend || premium > 0;
		if (coins > 0 || premium > 0)
		{
			if (coins > 0)
			{
				AddMoney(coins, ECurrencyType.Coins, reason);
			}
			if (premium > 0)
			{
				AddMoney(premium, ECurrencyType.Premium, reason);
			}
		}
	}

	public static ECurrencyType AsCurrencyType(IDBCanCost canCost)
	{
		return ECurrencyType.Coins;
	}
}
