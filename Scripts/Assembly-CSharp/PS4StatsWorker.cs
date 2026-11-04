using DI;

public class PS4StatsWorker : IStatsWorker
{
	private GamePrefs m_prefs;

	public void Init()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
	}

	public void SetStat(string key, int value)
	{
		SetStat(key, (float)value);
	}

	public void SetStat(string key, float value)
	{
		float value2 = 0f;
		GetStat(key, out value2);
		if (value != value2)
		{
			m_prefs.stats.fourStats[key] = value;
			m_prefs.stats.Save();
		}
	}

	public void GetStat(string key, out int value)
	{
		GetStat(key, out float value2);
		value = (int)value2;
	}

	public void GetStat(string key, out float value)
	{
		if (!m_prefs.stats.fourStats.TryGetValue(key, out value))
		{
			value = 0f;
		}
	}

	public void ClearStats()
	{
		m_prefs.stats.fourStats.Clear();
		m_prefs.stats.Save();
	}
}
