using System.Collections.Generic;

public class MinimapDBAsset : SingletonScriptableObject<MinimapDBAsset>
{
	public List<MinimapData> minimapDatas = new List<MinimapData>();

	private readonly Dictionary<int, MinimapData> m_minimapDatasCache = new Dictionary<int, MinimapData>();

	protected override void OnLoaded()
	{
		base.OnLoaded();
		m_minimapDatasCache.Clear();
		foreach (MinimapData minimapData in minimapDatas)
		{
			m_minimapDatasCache[minimapData.trackId] = minimapData;
		}
	}

	public void Set(int idTrack, MinimapData minimapData, bool save = false)
	{
		if (minimapDatas == null)
		{
			minimapDatas = new List<MinimapData>();
		}
		int num = minimapDatas.FindIndex((MinimapData value) => value.trackId == idTrack);
		if (num == -1)
		{
			minimapDatas.Add(minimapData);
		}
		else
		{
			minimapDatas[num] = minimapData;
		}
		if (save)
		{
			Save();
		}
	}

	public MinimapData Get(int idTrack)
	{
		if (!m_minimapDatasCache.TryGetValue(idTrack, out var value))
		{
			return null;
		}
		return value;
	}
}
