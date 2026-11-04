using System;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerTop
{
	public SortedDictionary<int, MultiplayerTopPeriod> topPeriods = new SortedDictionary<int, MultiplayerTopPeriod>();

	private int? m_nextAccessor;

	private long m_nextCalculationDate;

	public MultiplayerTop(Top top)
	{
		if (top == null)
		{
			Debug.LogError("MultiplayerTop::MultiplayerTop(): top is null.");
			return;
		}
		m_nextCalculationDate = top.nextCallculationDate;
		m_nextAccessor = top.nextPageAccessor;
		UpdatePeriods(top.topRaces);
	}

	public void Add(Top top, bool next)
	{
		if (top == null)
		{
			Debug.LogError("MultiplayerTop::Add(): top is null.");
			return;
		}
		if (!next)
		{
			m_nextCalculationDate = top.nextCallculationDate;
		}
		m_nextAccessor = top.nextPageAccessor;
		UpdatePeriods(top.topRaces);
	}

	private void UpdatePeriods(List<TopReplay> replays)
	{
		if (replays == null)
		{
			Debug.LogError("MultiplayerTop::ProcessReplays(): replays is null.");
			return;
		}
		foreach (TopReplay replay in replays)
		{
			if (!topPeriods.ContainsKey(replay.calculationPeriod))
			{
				MultiplayerTopPeriod multiplayerTopPeriod = new MultiplayerTopPeriod(replay.calculationPeriod, replay.calculationDate);
				multiplayerTopPeriod.AddReplay(replay);
				topPeriods.Add(replay.calculationPeriod, multiplayerTopPeriod);
			}
			else
			{
				topPeriods[replay.calculationPeriod].AddReplay(replay);
			}
		}
	}

	public bool UpdateNeeded()
	{
		return GetSecondsBeforeNextUpdate() <= 0;
	}

	public int? GetNextAccessor()
	{
		return m_nextAccessor;
	}

	public int GetSecondsBeforeNextUpdate()
	{
		long ticks = m_nextCalculationDate.ToDateTimeFromUnixTime().ToLocalTime().Ticks - DateTime.Now.ToLocalTime().Ticks;
		return (int)new TimeSpan(ticks).TotalSeconds;
	}
}
