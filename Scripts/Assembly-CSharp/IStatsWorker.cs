public interface IStatsWorker
{
	void Init();

	void SetStat(string key, int value);

	void SetStat(string key, float value);

	void GetStat(string key, out int value);

	void GetStat(string key, out float value);

	void ClearStats();
}
