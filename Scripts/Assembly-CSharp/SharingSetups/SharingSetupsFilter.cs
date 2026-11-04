using System;
using SimpleJSON;

namespace SharingSetups
{
	public class SharingSetupsFilter : ISharingFilter
	{
		public const string ANY_PROFILE_TAG = "ANY_PROFILE: ";

		public int carId { get; set; }

		public int dataId { get; set; }

		public string profile { get; private set; }

		public int deviceType { get; private set; }

		public int adhLimit { get; private set; }

		public int rating { get; private set; }

		public int downloadCount { get; private set; }

		public int crossmask { get; private set; }

		[JsonIgnore]
		public Func<string> currentProfilesCallback { get; private set; }

		[JsonIgnore]
		public Func<string, int> profileIdCallback { get; private set; }

		public void UpdateCarId(int carId)
		{
			this.carId = carId;
		}

		public override bool Equals(object filterObj)
		{
			if (!(filterObj is SharingSetupsFilter sharingSetupsFilter))
			{
				return false;
			}
			if (profile == null)
			{
				string text = (profile = string.Empty);
			}
			if (carId == sharingSetupsFilter.carId && dataId == sharingSetupsFilter.dataId && profile == sharingSetupsFilter.profile && deviceType == sharingSetupsFilter.deviceType && adhLimit == sharingSetupsFilter.adhLimit && rating == sharingSetupsFilter.rating && downloadCount == sharingSetupsFilter.downloadCount)
			{
				return crossmask == sharingSetupsFilter.crossmask;
			}
			return false;
		}

		public void RefreshProfilesIfNeeded()
		{
			if (profile != null && profile.Contains("ANY_PROFILE: "))
			{
				SharingSetupsFilter sharingSetupsFilter = (SharingSetupsFilter)GetDefault();
				profile = sharingSetupsFilter.profile;
			}
			if (dataId != 0)
			{
				SharingSetupsFilter sharingSetupsFilter2 = (SharingSetupsFilter)GetDefault();
				dataId = sharingSetupsFilter2.dataId;
			}
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public ISharingFilter GetDefault()
		{
			return new SharingSetupsFilter
			{
				currentProfilesCallback = currentProfilesCallback,
				profileIdCallback = profileIdCallback,
				carId = carId,
				profile = currentProfilesCallback?.Invoke(),
				dataId = (profileIdCallback?.Invoke(profile) ?? 0),
				deviceType = -1,
				adhLimit = 0,
				rating = 0,
				downloadCount = 0,
				crossmask = 0
			};
		}

		public void SetCurrentProfilesCallBack(Func<string> callback)
		{
			currentProfilesCallback = callback;
		}

		public void SetProfileIdCallBack(Func<string, int> callback)
		{
			profileIdCallback = callback;
		}

		public void SetFilterValue<K, T>(K filterType, T value)
		{
			int num = -1;
			if (!(value is string))
			{
				num = Convert.ToInt32(value);
			}
			if (filterType is SetupSharingFilterType)
			{
				switch ((SetupSharingFilterType)((((object)filterType) is SetupSharingFilterType) ? ((object)filterType) : null))
				{
				case SetupSharingFilterType.Profile:
					profile = value as string;
					dataId = profileIdCallback?.Invoke(profile) ?? 0;
					break;
				case SetupSharingFilterType.DeviceType:
					deviceType = num;
					break;
				case SetupSharingFilterType.AdhLimit:
					adhLimit = num;
					break;
				case SetupSharingFilterType.Rating:
					rating = num;
					break;
				case SetupSharingFilterType.DownloadCount:
					downloadCount = num;
					break;
				case SetupSharingFilterType.CrossMask:
					crossmask = num;
					break;
				}
			}
		}

		public T GetFilterValue<K, T>(K filterType)
		{
			T result = default(T);
			if (filterType is SetupSharingFilterType)
			{
				switch ((SetupSharingFilterType)((((object)filterType) is SetupSharingFilterType) ? ((object)filterType) : null))
				{
				case SetupSharingFilterType.Profile:
					return (T)(object)profile;
				case SetupSharingFilterType.DeviceType:
					return (T)(object)deviceType;
				case SetupSharingFilterType.AdhLimit:
					return (T)(object)adhLimit;
				case SetupSharingFilterType.Rating:
					return (T)(object)rating;
				case SetupSharingFilterType.DownloadCount:
					return (T)(object)downloadCount;
				case SetupSharingFilterType.CrossMask:
					return (T)(object)crossmask;
				}
			}
			return result;
		}
	}
}
