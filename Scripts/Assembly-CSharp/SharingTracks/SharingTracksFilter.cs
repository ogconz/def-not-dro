using System;
using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;

namespace SharingTracks
{
	public class SharingTracksFilter : ISharingFilter
	{
		private const float SCORE_SCALE = 5f;

		public int carId { get; set; }

		public int rating { get; private set; }

		public int downloadCount { get; private set; }

		public string trackName { get; private set; }

		public string authorName { get; private set; }

		public override bool Equals(object filterObj)
		{
			if (!(filterObj is SharingTracksFilter sharingTracksFilter))
			{
				return false;
			}
			if (trackName == null)
			{
				trackName = string.Empty;
			}
			if (authorName == null)
			{
				authorName = string.Empty;
			}
			if (trackName == sharingTracksFilter.trackName && authorName == sharingTracksFilter.authorName && rating == sharingTracksFilter.rating)
			{
				return downloadCount == sharingTracksFilter.downloadCount;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public ISharingFilter GetDefault()
		{
			return new SharingTracksFilter
			{
				carId = carId,
				trackName = string.Empty,
				authorName = string.Empty,
				rating = 0,
				downloadCount = 0
			};
		}

		public void SetFilterValue<K, T>(K filterType, T value)
		{
			int num = -1;
			if (value is int)
			{
				num = Convert.ToInt32(value);
			}
			if (filterType is TracksSharingFilterType)
			{
				switch ((TracksSharingFilterType)((((object)filterType) is TracksSharingFilterType) ? ((object)filterType) : null))
				{
				case TracksSharingFilterType.TrackName:
					trackName = value as string;
					break;
				case TracksSharingFilterType.AuthorName:
					authorName = value as string;
					break;
				case TracksSharingFilterType.Rating:
					rating = num;
					break;
				case TracksSharingFilterType.DownloadCount:
					downloadCount = num;
					break;
				}
			}
		}

		public T GetFilterValue<K, T>(K filterType)
		{
			T result = default(T);
			if (filterType is TracksSharingFilterType)
			{
				switch ((TracksSharingFilterType)((((object)filterType) is TracksSharingFilterType) ? ((object)filterType) : null))
				{
				case TracksSharingFilterType.TrackName:
					return (T)(object)trackName;
				case TracksSharingFilterType.AuthorName:
					return (T)(object)authorName;
				case TracksSharingFilterType.Rating:
					return (T)(object)rating;
				case TracksSharingFilterType.DownloadCount:
					return (T)(object)downloadCount;
				}
			}
			return result;
		}

		public async UniTask<bool> IsPassedThroughFilter(UGCItem item)
		{
			if (!(GetDefault() is SharingTracksFilter defaultFilter))
			{
				return true;
			}
			string title = item.title;
			bool needFilterByTrackName = string.IsNullOrEmpty(title);
			if (!string.IsNullOrEmpty(title) && trackName != defaultFilter.trackName && !string.IsNullOrEmpty(trackName))
			{
				needFilterByTrackName = !title.ToLower().Contains(trackName.ToLower());
			}
			bool needFilterByAuthorName = false;
			if (item is IUGCOwnerSystem itemWithOwner)
			{
				if (!itemWithOwner.IsValidUserName())
				{
					await itemWithOwner.RequestInfoFromServices(IUGCOwnerSystem.RequestInfo.Name);
				}
				string name = itemWithOwner.owner.name;
				if (!string.IsNullOrEmpty(name) && authorName != defaultFilter.authorName && !string.IsNullOrEmpty(authorName))
				{
					needFilterByAuthorName = !name.ToLower().Contains(authorName.ToLower());
				}
			}
			bool flag = false;
			if (item is IUGCRatable iUGCRatable && rating > 0)
			{
				float num = iUGCRatable.rate * 5f;
				flag = num < (float)rating || num > (float)(rating + 1);
			}
			bool flag2 = false;
			if (item is IUGCSubscridable iUGCSubscridable && downloadCount > 0)
			{
				flag2 = iUGCSubscridable.subscriptionsCount < (ulong)downloadCount;
			}
			return !needFilterByTrackName && !needFilterByAuthorName && !flag && !flag2;
		}
	}
}
