using EpicUnity;
using UnityEngine;

namespace Services.Stats.StatsWorkers
{
	public class EOSStatsWorker : IStatsWorker
	{
		public void Init()
		{
			EOSManager instance = EOSManager.instance;
			if (!instance.IsShutdown)
			{
				instance.QueryStats();
			}
		}

		public void SetStat(string key, int value)
		{
			GetStat(key, out int value2);
			value -= value2;
			EOSManager eosManager = EOSManager.instance;
			if (!eosManager.IsShutdown)
			{
				eosManager.IngestStat(key, value, delegate
				{
					eosManager.QueryStats();
				});
			}
		}

		public void SetStat(string key, float value)
		{
			GetStat(key, out float value2);
			value -= value2;
			EOSManager eosManager = EOSManager.instance;
			if (!eosManager.IsShutdown)
			{
				eosManager.IngestStat(key, Mathf.FloorToInt(value), delegate
				{
					eosManager.QueryStats();
				});
			}
		}

		public void GetStat(string key, out int value)
		{
			EOSManager instance = EOSManager.instance;
			int stats;
			if (instance.IsShutdown)
			{
				value = 0;
			}
			else if (instance.TryGetStat(key, out stats))
			{
				value = stats;
			}
			else
			{
				value = 0;
			}
		}

		public void GetStat(string key, out float value)
		{
			EOSManager instance = EOSManager.instance;
			int stats;
			if (instance.IsShutdown)
			{
				value = 0f;
			}
			else if (instance.TryGetStat(key, out stats))
			{
				value = stats;
			}
			else
			{
				value = 0f;
			}
		}

		public void ClearStats()
		{
		}
	}
}
