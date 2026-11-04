public class VoidStatsWorker : IStatsWorker
{
	public void Init()
	{
	}

	public void SetStat(string key, int value)
	{
	}

	public void SetStat(string key, float value)
	{
	}

	public void GetStat(string key, out int value)
	{
		value = 0;
	}

	public void GetStat(string key, out float value)
	{
		value = 0f;
	}

	public void ClearStats()
	{
	}
}
