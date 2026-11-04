using System;
using UnityEngine;

namespace SharingLiveries
{
	public class SharingLiveriesFilter : ISharingFilter
	{
		public int carId { get; set; }

		public string category { get; private set; }

		public int rating { get; private set; }

		public int downloadCount { get; private set; }

		public int dlcMask { get; private set; }

		public int crossmask { get; private set; }

		public Vector2 layers { get; private set; }

		public void UpdateCarId(int carId)
		{
			this.carId = carId;
		}

		public override bool Equals(object filterObj)
		{
			if (!(filterObj is SharingLiveriesFilter sharingLiveriesFilter))
			{
				return false;
			}
			if (category == null)
			{
				string text = (category = string.Empty);
			}
			if (carId == sharingLiveriesFilter.carId && category == sharingLiveriesFilter.category && rating == sharingLiveriesFilter.rating && downloadCount == sharingLiveriesFilter.downloadCount && dlcMask == sharingLiveriesFilter.dlcMask && crossmask == sharingLiveriesFilter.crossmask)
			{
				return layers == sharingLiveriesFilter.layers;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public ISharingFilter GetDefault()
		{
			return new SharingLiveriesFilter
			{
				carId = carId,
				category = string.Empty,
				rating = 0,
				downloadCount = 0,
				dlcMask = 0,
				crossmask = 0,
				layers = Vector2.zero
			};
		}

		public void SetFilterValue<K, T>(K filterType, T value)
		{
			int num = -1;
			if (value is int)
			{
				num = Convert.ToInt32(value);
			}
			if (!(filterType is LiveriesSharingFilterType))
			{
				return;
			}
			switch ((LiveriesSharingFilterType)((((object)filterType) is LiveriesSharingFilterType) ? ((object)filterType) : null))
			{
			case LiveriesSharingFilterType.Category:
				category = value as string;
				break;
			case LiveriesSharingFilterType.Rating:
				rating = num;
				break;
			case LiveriesSharingFilterType.DownloadCount:
				downloadCount = num;
				break;
			case LiveriesSharingFilterType.DLC:
				dlcMask = num;
				break;
			case LiveriesSharingFilterType.CrossMask:
				crossmask = num;
				break;
			case LiveriesSharingFilterType.LayersCount:
				if (value is Vector2 vector)
				{
					layers = vector;
				}
				break;
			}
		}

		public T GetFilterValue<K, T>(K filterType)
		{
			T result = default(T);
			if (filterType is LiveriesSharingFilterType)
			{
				switch ((LiveriesSharingFilterType)((((object)filterType) is LiveriesSharingFilterType) ? ((object)filterType) : null))
				{
				case LiveriesSharingFilterType.Category:
					return (T)(object)category;
				case LiveriesSharingFilterType.Rating:
					return (T)(object)rating;
				case LiveriesSharingFilterType.DownloadCount:
					return (T)(object)downloadCount;
				case LiveriesSharingFilterType.DLC:
					return (T)(object)dlcMask;
				case LiveriesSharingFilterType.CrossMask:
					return (T)(object)crossmask;
				case LiveriesSharingFilterType.LayersCount:
					return (T)(object)layers;
				}
			}
			return result;
		}
	}
}
