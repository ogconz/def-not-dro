using System;
using System.Collections.Generic;
using System.Linq;
using DB.Meta;
using Db.Meta.CarProfile;
using UnityEngine;

public static class StaticDictionariesHolderExtension
{
	[Serializable]
	public class CarDataContainer
	{
		public List<PlayerCar> playerCars = new List<PlayerCar>();

		public List<PlayerCarProfile> profiles = new List<PlayerCarProfile>();
	}

	[Serializable]
	public class GlobalGameSettingsContainer
	{
		public GlobalGameSettings GlobalGameSettings;

		public FeatuersLocksInfo FeatuersLocksInfo;
	}

	[Serializable]
	public class TracksContainer
	{
		public List<DB.Meta.Track> tracks;
	}

	[Serializable]
	public class CarColorsContainer
	{
		public List<CarColorMeta> carColors;

		public List<VinylColorMeta> vinylColors;

		public List<WheelSpokeColorMeta> wheelSpokeColors;

		public List<WheelRimColorMeta> wheelRimColors;

		public List<GlassColorMeta> glassColors;

		public List<SmokeColorMeta> smokeColors;
	}

	[Serializable]
	public class CarProfileParametersContainer
	{
		public List<CarProfileParameterMeta> carProfileParameters;
	}

	[Serializable]
	public class TrackNormsContainer
	{
		public List<TrackDriftRaceNormMeta> driftRaceNorms;

		public List<TrackTimeAttackRaceNormMeta> timeAttackRaceNorms;
	}

	[Serializable]
	public class TrackRewardsContainer
	{
		public List<TrackDriftRaceRewardMeta> driftRaceRewards;

		public List<TrackTimeAttackRaceRewardMeta> timeAttackRaceRewards;
	}

	public class DBMetaEqComparer<T> : IEqualityComparer<T> where T : IDBMeta
	{
		public bool Equals(T x, T y)
		{
			return x.id.Equals(y.id);
		}

		public int GetHashCode(T obj)
		{
			return obj.id.GetHashCode();
		}
	}

	public static TracksContainer ExtractTrackData(this StaticDictionariesHolder holder)
	{
		return new TracksContainer
		{
			tracks = holder.tracks
		};
	}

	public static List<CarDataContainer> ExtractCarsData(this StaticDictionariesHolder holder, bool sort = true)
	{
		List<CarDataContainer> list = new List<CarDataContainer>();
		int tier = 1;
		while (true)
		{
			List<PlayerCar> CarsByTier = holder.playerCars.FindAll((PlayerCar item) => item.coinsFactor == (float)tier);
			if (CarsByTier.Count == 0)
			{
				break;
			}
			list.Add(new CarDataContainer());
			List<PlayerCarProfile> list2 = holder.profiles.FindAll((PlayerCarProfile profile) => CarsByTier.Find((PlayerCar car) => car.id == profile.carId) != null);
			if (sort)
			{
				CarsByTier.Sort((PlayerCar car1, PlayerCar car2) => car1.id.CompareTo(car2.id));
				list2.Sort((PlayerCarProfile profile1, PlayerCarProfile profile2) => profile1.id.CompareTo(profile2.id));
			}
			list[tier - 1].playerCars = CarsByTier;
			list[tier - 1].profiles = list2;
			int num = tier + 1;
			tier = num;
		}
		return list;
	}

	public static CarColorsContainer ExtractCarColorData(this StaticDictionariesHolder holder)
	{
		return new CarColorsContainer
		{
			carColors = holder.carColors,
			vinylColors = holder.vinylColors,
			wheelSpokeColors = holder.wheelSpokeColors,
			wheelRimColors = holder.wheelRimColors,
			smokeColors = holder.smokeColors,
			glassColors = holder.glassColors
		};
	}

	public static CarProfileParametersContainer ExtractCarProfileParamsData(this StaticDictionariesHolder holder)
	{
		return new CarProfileParametersContainer
		{
			carProfileParameters = holder.carProfileParameters
		};
	}

	public static GlobalGameSettingsContainer ExtractGlobalSettingsData(this StaticDictionariesHolder holder)
	{
		return new GlobalGameSettingsContainer
		{
			GlobalGameSettings = holder.GlobalGameSettings,
			FeatuersLocksInfo = holder.FeatuersLocksInfo
		};
	}

	public static TrackNormsContainer ExtractTrackNorms(this StaticDictionariesHolder holder)
	{
		return new TrackNormsContainer
		{
			driftRaceNorms = holder.driftRaceNorms,
			timeAttackRaceNorms = holder.timeAttackRaceNorms
		};
	}

	public static TrackRewardsContainer ExtractTrackRewards(this StaticDictionariesHolder holder)
	{
		return new TrackRewardsContainer
		{
			driftRaceRewards = holder.driftRaceRewards,
			timeAttackRaceRewards = holder.timeAttackRaceRewards
		};
	}

	public static void LoadGlobalSettings(this StaticDictionariesHolder holder, string path)
	{
		GlobalGameSettingsContainer globalGameSettingsContainer = LoadDataContainerFromXML<GlobalGameSettingsContainer>(path);
		holder.GlobalGameSettings = globalGameSettingsContainer.GlobalGameSettings;
		holder.FeatuersLocksInfo = globalGameSettingsContainer.FeatuersLocksInfo;
	}

	public static void LoadTracks(this StaticDictionariesHolder holder, string path)
	{
		holder.tracks = LoadDataContainerFromXML<TracksContainer>(path).tracks;
	}

	public static void LoadCarColors(this StaticDictionariesHolder holder, string path)
	{
		CarColorsContainer carColorsContainer = LoadDataContainerFromXML<CarColorsContainer>(path);
		holder.carColors = carColorsContainer.carColors;
		holder.vinylColors = carColorsContainer.vinylColors;
		holder.wheelRimColors = carColorsContainer.wheelRimColors;
		holder.wheelSpokeColors = carColorsContainer.wheelSpokeColors;
		holder.glassColors = carColorsContainer.glassColors;
		holder.smokeColors = carColorsContainer.smokeColors;
	}

	public static void LoadCarsAndProfiles(this StaticDictionariesHolder holder, string folderPath, bool sort = true)
	{
		List<CarDataContainer> list = LoadAllDataContainersFromXML<CarDataContainer>(folderPath);
		List<PlayerCar> list2 = new List<PlayerCar>();
		List<PlayerCarProfile> list3 = new List<PlayerCarProfile>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.AddRange(list[i].playerCars);
			list3.AddRange(list[i].profiles);
		}
		if (sort)
		{
			list2.Sort(CompareCars);
			list3.Sort(CompareProfiles);
		}
		holder.playerCars = list2;
		holder.profiles = list3;
		static int CompareCars(PlayerCar car1, PlayerCar car2)
		{
			if (car1.id >= car2.id)
			{
				if (car1.id <= car2.id)
				{
					return 0;
				}
				return 1;
			}
			return -1;
		}
		static int CompareProfiles(PlayerCarProfile profile1, PlayerCarProfile profile2)
		{
			if (profile1.id >= profile2.id)
			{
				if (profile1.id <= profile2.id)
				{
					return 0;
				}
				return 1;
			}
			return -1;
		}
	}

	public static void LoadCarProfileParams(this StaticDictionariesHolder holder, string path)
	{
		holder.carProfileParameters = LoadDataContainerFromXML<CarProfileParametersContainer>(path).carProfileParameters;
	}

	public static void LoadTrackNorms(this StaticDictionariesHolder holder, string path)
	{
		TrackNormsContainer trackNormsContainer = LoadDataContainerFromXML<TrackNormsContainer>(path);
		holder.driftRaceNorms = trackNormsContainer.driftRaceNorms;
		holder.timeAttackRaceNorms = trackNormsContainer.timeAttackRaceNorms;
	}

	public static void LoadTrackRewards(this StaticDictionariesHolder holder, string path)
	{
		TrackRewardsContainer trackRewardsContainer = LoadDataContainerFromXML<TrackRewardsContainer>(path);
		holder.driftRaceRewards = trackRewardsContainer.driftRaceRewards;
		holder.timeAttackRaceRewards = trackRewardsContainer.timeAttackRaceRewards;
	}

	public static List<T> GetDBMetaDiffList<T>(this StaticDictionariesHolder holder, List<T> oldDBMetaList, List<T> newDBMetaList) where T : IDBMeta
	{
		return newDBMetaList.Except(oldDBMetaList, new DBMetaEqComparer<T>()).ToList();
	}

	private static T LoadDataContainerFromXML<T>(string resourcePath) where T : new()
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			throw new ArgumentNullException("The given path is empty or null.");
		}
		TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
		if (textAsset == null)
		{
			throw new ArgumentException("Unable to load text asset at the given path '" + resourcePath + "'.");
		}
		return XMLDictionariesManager.DeserializeXml<T>(textAsset.text);
	}

	private static List<T> LoadAllDataContainersFromXML<T>(string resourceFolderPath) where T : new()
	{
		TextAsset[] array = Resources.LoadAll<TextAsset>(resourceFolderPath);
		if (array == null)
		{
			return null;
		}
		List<T> list = new List<T>();
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			T item = XMLDictionariesManager.DeserializeXml<T>(array[i].text);
			list.Add(item);
		}
		return list;
	}
}
