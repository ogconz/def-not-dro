using System.Collections.Generic;
using UnityEngine;

public class MultiplayerTops
{
	private Dictionary<int, MultiplayerTop> leaguesTops = new Dictionary<int, MultiplayerTop>();

	public void AddTop(Top top, bool next = false)
	{
		if (top == null)
		{
			Debug.LogError("MultiplayerTops::AddTop(): top is null.");
		}
		else if (!leaguesTops.ContainsKey(top.league))
		{
			leaguesTops.Add(top.league, new MultiplayerTop(top));
		}
		else
		{
			leaguesTops[top.league].Add(top, next);
		}
	}

	public void RemoveTop(int leagueId)
	{
		if (leaguesTops.ContainsKey(leagueId))
		{
			leaguesTops.Remove(leagueId);
		}
	}

	public void Clear()
	{
		leaguesTops.Clear();
	}

	public MultiplayerTop GetTop(int leagueId)
	{
		if (leaguesTops.ContainsKey(leagueId))
		{
			return leaguesTops[leagueId];
		}
		return null;
	}

	public bool UpdateNeeded(int leagueId)
	{
		if (leaguesTops.ContainsKey(leagueId))
		{
			return leaguesTops[leagueId].UpdateNeeded();
		}
		return true;
	}

	public int? GetNextPageAccessor(int leagueId)
	{
		if (!leaguesTops.ContainsKey(leagueId))
		{
			return null;
		}
		return leaguesTops[leagueId].GetNextAccessor();
	}

	public int GetSecondsBeforeNextTopUpdate(int leagueId)
	{
		if (!leaguesTops.ContainsKey(leagueId))
		{
			return 0;
		}
		return leaguesTops[leagueId].GetSecondsBeforeNextUpdate();
	}
}
