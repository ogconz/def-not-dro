using System.Collections.Generic;
using UnityEngine;

public class MultiplayerTopPeriod
{
	public int calculationPeriod;

	public long calculationDate;

	private List<TopReplay> m_replays = new List<TopReplay>();

	public MultiplayerTopPeriod(int calculationPeriod, long calculationDate)
	{
		this.calculationPeriod = calculationPeriod;
		this.calculationDate = calculationDate;
	}

	public void AddReplay(TopReplay replay)
	{
		if (replay == null)
		{
			Debug.LogError("TopPeriod::AddReplay(): replay is null.");
		}
		else if (replay.calculationPeriod != calculationPeriod)
		{
			Debug.LogError("TopPeriod::AddReplay(): incorrect calculationPeriod.");
		}
		else
		{
			m_replays.Add(replay);
		}
	}

	public TopReplay[] GetReplays()
	{
		return m_replays.ToArray();
	}
}
