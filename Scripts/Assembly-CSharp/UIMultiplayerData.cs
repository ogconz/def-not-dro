using System;
using System.Collections.Generic;
using DI;
using SimpleJSON;
using UnityEngine;

public class UIMultiplayerData
{
	public struct DivisionRow
	{
		public int id;

		public int elo;
	}

	public readonly List<DivisionRow> divisionTable = new List<DivisionRow>();

	public int lastViewedGhostLog;

	public int currentSeason;

	public readonly int?[] _dpInLastRaces = new int?[15];

	public int averageResultPrevDay;

	public int dayNumber;

	public int recordDp;

	public int bestQualificationDp;

	public bool leagueListIsViewed;

	public string editorNickname = "default";

	public int? playerId;

	[Dependency]
	[JsonIgnore]
	private GamePrefs m_prefs;

	public UIMultiplayerData()
	{
		DependencyInjector.InjectObjectsInto(this);
	}

	public void MarkLeagueListAsViewed()
	{
		leagueListIsViewed = true;
		Save();
	}

	public void NewSeason(int seasonNumber, bool leagueListIsViewed)
	{
		currentSeason = seasonNumber;
		bestQualificationDp = 0;
		lastViewedGhostLog = 0;
		recordDp = 0;
		averageResultPrevDay = 0;
		divisionTable.Clear();
		for (int i = 0; i < _dpInLastRaces.Length; i++)
		{
			_dpInLastRaces[i] = null;
		}
		this.leagueListIsViewed = leagueListIsViewed;
		Save();
	}

	public void NewRaceResult(int driftPoints, bool isQualify = false)
	{
		if (isQualify)
		{
			bestQualificationDp = Mathf.Max(bestQualificationDp, driftPoints);
		}
		recordDp = Mathf.Max(driftPoints, recordDp);
		for (int i = 0; i < _dpInLastRaces.Length - 1; i++)
		{
			_dpInLastRaces[i] = _dpInLastRaces[i + 1];
		}
		_dpInLastRaces[_dpInLastRaces.Length - 1] = driftPoints;
		Save();
	}

	public int GetAverageDp()
	{
		int num = 0;
		int num2 = 0;
		int?[] dpInLastRaces = _dpInLastRaces;
		for (int i = 0; i < dpInLastRaces.Length; i++)
		{
			int? num3 = dpInLastRaces[i];
			if (num3.HasValue)
			{
				num++;
				num2 += num3.Value;
			}
		}
		if (num == 0)
		{
			return 0;
		}
		return Convert.ToInt32((float)num2 / (float)num);
	}

	public int GetDpDiffWithPrevDay()
	{
		int multiplayerDayNumber = DateTime.Now.GetMultiplayerDayNumber();
		if (multiplayerDayNumber != dayNumber)
		{
			dayNumber = multiplayerDayNumber;
			averageResultPrevDay = GetAverageDp();
		}
		Save();
		return GetAverageDp() - averageResultPrevDay;
	}

	public void Save()
	{
		m_prefs.UIMultiplayer.data = this;
	}

	public string ToJson()
	{
		return JSON.Serialize(this);
	}
}
