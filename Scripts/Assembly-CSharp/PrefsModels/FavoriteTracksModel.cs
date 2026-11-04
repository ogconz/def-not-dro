using System.Collections.Generic;
using SimpleJSON;

namespace PrefsModels
{
	public class FavoriteTracksModel : BasePrefsModel
	{
		[JsonSerialize]
		private List<ulong> m_favoriteTracks = new List<ulong>();

		[JsonIgnore]
		public IReadOnlyList<ulong> favoriteTracks => m_favoriteTracks;

		public void AddTrack(ulong trackId)
		{
			if (!m_favoriteTracks.Contains(trackId))
			{
				m_favoriteTracks.Add(trackId);
				Save();
			}
		}

		public void RemoveTrack(ulong trackId)
		{
			int num = m_favoriteTracks.FindIndex((ulong c) => c == trackId);
			if (num >= 0 && num < m_favoriteTracks.Count)
			{
				m_favoriteTracks.RemoveAt(num);
				Save();
			}
		}

		public bool IsFavorite(ulong trackId)
		{
			return m_favoriteTracks.Contains(trackId);
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (reserveModel is FavoriteTracksModel favoriteTracksModel)
			{
				return m_favoriteTracks == favoriteTracksModel.m_favoriteTracks;
			}
			return true;
		}

		public void ClearAll()
		{
			m_favoriteTracks.Clear();
			Save();
		}
	}
}
