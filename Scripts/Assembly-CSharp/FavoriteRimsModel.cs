using System.Collections.Generic;
using PrefsModels;
using SimpleJSON;

public class FavoriteRimsModel : BasePrefsModel
{
	[JsonSerialize]
	private List<int> m_favoriteRims = new List<int>();

	[JsonSerialize]
	private List<(int, ulong)> m_favoriteRimsNew = new List<(int, ulong)>();

	[JsonIgnore]
	public IReadOnlyList<(int, ulong)> favoriteRims => m_favoriteRimsNew;

	public override void PostLoad()
	{
		base.PostLoad();
		if (m_favoriteRimsNew.Count > 0)
		{
			return;
		}
		foreach (int favoriteRim in m_favoriteRims)
		{
			m_favoriteRimsNew.Add((favoriteRim, 0uL));
		}
	}

	public void AddRim((int, ulong) rimId)
	{
		if (!m_favoriteRimsNew.Contains(rimId))
		{
			m_favoriteRimsNew.Add(rimId);
			Save();
		}
	}

	public void RemoveRim((int, ulong) rimId)
	{
		int num = m_favoriteRimsNew.FindIndex(delegate((int, ulong) c)
		{
			(int, ulong) tuple = c;
			(int, ulong) tuple2 = rimId;
			return tuple.Item1 == tuple2.Item1 && tuple.Item2 == tuple2.Item2;
		});
		if (num >= 0 && num < m_favoriteRimsNew.Count)
		{
			m_favoriteRimsNew.RemoveAt(num);
			Save();
		}
	}

	public bool IsFavorite((int, ulong) rimId)
	{
		return m_favoriteRimsNew.Contains(rimId);
	}

	protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
	{
		if (reserveModel is FavoriteRimsModel favoriteRimsModel)
		{
			return m_favoriteRimsNew == favoriteRimsModel.m_favoriteRimsNew;
		}
		return true;
	}

	public void ClearAll()
	{
		m_favoriteRims.Clear();
		m_favoriteRimsNew.Clear();
		Save();
	}
}
