using System;
using System.Collections.Generic;
using DI;
using UnityEngine;

public class StatisticsService
{
	private enum EStatistic
	{
		MulitiplayerEnter = 0,
		MultiplayerQualifyEnd = 1,
		MultiplayerTrainingEnd = 2,
		MultiplayerBattleEnd = 3,
		MultiplayeCashPurchase = 4
	}

	private class StatisticItem
	{
		public virtual void OnDayChanged()
		{
		}
	}

	private class MultiplayerEnterItem : StatisticItem
	{
		public int totalEnterCount;

		public int dayEnterCount;

		public override void OnDayChanged()
		{
			base.OnDayChanged();
			dayEnterCount = 0;
		}
	}

	private class MultiplayerQualifyEndItem : StatisticItem
	{
		public int totalQualifyCount;

		public int qualifyCountInSeason;
	}

	private class MultiplayerBattleEndItem : StatisticItem
	{
		public int totalBattleCount;

		public int dayBattleCount;

		public override void OnDayChanged()
		{
			base.OnDayChanged();
			dayBattleCount = 0;
		}
	}

	private class MultiplayerTrainingEndItem : StatisticItem
	{
		public int totalTrainingCount;

		public int dayTrainingCount;

		public override void OnDayChanged()
		{
			base.OnDayChanged();
			dayTrainingCount = 0;
		}
	}

	private class StatisticsServiceData
	{
		public double lastDay;

		public double? multiplayerFirstEnterDay;

		public int multiplayerSeasonId;

		public Dictionary<EStatistic, StatisticItem> items = new Dictionary<EStatistic, StatisticItem>();
	}

	private StatisticsServiceData m_serviceData;

	public StatisticsService()
	{
		Load();
	}

	private void CheckDayChanged()
	{
		double num = TimerUtils.DateTimeToSeconds(TimerUtils.GetDayBegin());
		if (!(Math.Abs(m_serviceData.lastDay - num) > (double)Mathf.Epsilon))
		{
			return;
		}
		m_serviceData.lastDay = num;
		foreach (KeyValuePair<EStatistic, StatisticItem> item in m_serviceData.items)
		{
			item.Value.OnDayChanged();
		}
		Save();
	}

	private void Save()
	{
		TryCatchPrefs.SaveObject("StatisticsServiceData", m_serviceData);
	}

	private void Load()
	{
		m_serviceData = TryCatchPrefs.LoadObjectOrCreateDefault<StatisticsServiceData>("StatisticsServiceData");
		CheckDayChanged();
	}

	private T GetItemOrDefault<T>(EStatistic statistic) where T : StatisticItem, new()
	{
		if (m_serviceData.items.ContainsKey(statistic))
		{
			return (T)m_serviceData.items[statistic];
		}
		T val = new T();
		m_serviceData.items.Add(statistic, val);
		return val;
	}

	private int GetDayAfterFirstMpEnter()
	{
		if (m_serviceData.multiplayerFirstEnterDay.HasValue)
		{
			DateTime dateTime = TimerUtils.SecondsToDateTime(m_serviceData.multiplayerFirstEnterDay.Value);
			int days = (TimerUtils.GetDayBegin() - dateTime).Days;
			if (days > 0)
			{
				return days;
			}
		}
		return 0;
	}

	private static void GetDeivisionAndLeague(out int placeInDivision, out string leagueName)
	{
		placeInDivision = 0;
		leagueName = "LEAGUE";
		MultiplayerManager multiplayerManager = DependencyInjector.Resolve<MultiplayerManager>();
		if (multiplayerManager != null)
		{
			if (multiplayerManager.currentUser != null && multiplayerManager.currentUser.position.HasValue)
			{
				placeInDivision = multiplayerManager.currentUser.position.Value;
			}
			if (multiplayerManager.currentLeague != null)
			{
				leagueName = leagueName + " " + multiplayerManager.currentLeague.LeagueNumber().ToRoman();
			}
		}
	}

	public void MultiplayerEnter()
	{
		CheckDayChanged();
		if (!m_serviceData.multiplayerFirstEnterDay.HasValue)
		{
			m_serviceData.multiplayerFirstEnterDay = TimerUtils.DateTimeToSeconds(TimerUtils.GetDayBegin());
		}
		MultiplayerEnterItem itemOrDefault = GetItemOrDefault<MultiplayerEnterItem>(EStatistic.MulitiplayerEnter);
		itemOrDefault.totalEnterCount++;
		itemOrDefault.dayEnterCount++;
		Save();
	}

	public void MultiplayerQualificationEnd(int dp, int seasonId)
	{
		CheckDayChanged();
		MultiplayerQualifyEndItem itemOrDefault = GetItemOrDefault<MultiplayerQualifyEndItem>(EStatistic.MultiplayerQualifyEnd);
		itemOrDefault.totalQualifyCount++;
		if (m_serviceData.multiplayerSeasonId != seasonId)
		{
			m_serviceData.multiplayerSeasonId = seasonId;
			itemOrDefault.qualifyCountInSeason = 0;
		}
		itemOrDefault.qualifyCountInSeason++;
		Save();
		GetDeivisionAndLeague(out var _, out var _);
	}

	public void MultiplayerTrainingEnd(int dp)
	{
		CheckDayChanged();
		MultiplayerTrainingEndItem itemOrDefault = GetItemOrDefault<MultiplayerTrainingEndItem>(EStatistic.MultiplayerTrainingEnd);
		itemOrDefault.totalTrainingCount++;
		itemOrDefault.dayTrainingCount++;
		Save();
		GetDeivisionAndLeague(out var _, out var _);
	}

	public void MultiplayerBattleEnd(bool isWin, int dp, int rating, string carName)
	{
		CheckDayChanged();
		MultiplayerBattleEndItem itemOrDefault = GetItemOrDefault<MultiplayerBattleEndItem>(EStatistic.MultiplayerBattleEnd);
		itemOrDefault.totalBattleCount++;
		itemOrDefault.dayBattleCount++;
		Save();
		GetDeivisionAndLeague(out var _, out var _);
	}
}
