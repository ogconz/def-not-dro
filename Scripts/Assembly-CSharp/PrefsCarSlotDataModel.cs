using System;
using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using PrefsModels;
using SimpleJSON;

public class PrefsCarSlotDataModel : BasePrefsModel
{
	private const int TargetVersion = 1;

	[JsonIgnore]
	public int slotId = -1;

	public int version = 1;

	public string caption;

	public int carId;

	public int profileId;

	public string customSetupId;

	public Dictionary<int, CarSwapData> swapDataByProfileId = new Dictionary<int, CarSwapData>();

	public Dictionary<int, ProfilesInfo.Profile> profiles = new Dictionary<int, ProfilesInfo.Profile>();

	public CarVisualManager.VisualSettings visual = new CarVisualManager.VisualSettings();

	public CarSettingsModel.TandemWheelPressure wheelPressureForXDS;

	public string modsCustomData;

	private bool m_isLoaded;

	private int m_lastSaveHash;

	public CarSwapData swapData
	{
		get
		{
			if (swapDataByProfileId.TryGetValue(profileId, out var value))
			{
				return value;
			}
			return swapDataByProfileId[profileId] = new CarSwapData(0);
		}
	}

	public static PrefsCarSlotDataModel CreateLazyLoadModel(int carId, int slotId)
	{
		return new PrefsCarSlotDataModel
		{
			slotId = slotId,
			carId = carId
		};
	}

	public static PrefsCarSlotDataModel CreateEmptyModel()
	{
		return new PrefsCarSlotDataModel
		{
			m_isLoaded = true
		};
	}

	public static PrefsCarSlotDataModel CreateDefaultModel(int carId)
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		PrefsCarSlotDataModel prefsCarSlotDataModel = CreateEmptyModel();
		prefsCarSlotDataModel.carId = carId;
		prefsCarSlotDataModel.profileId = gamePrefs.legacyCarSettings.GetDefaultProfileIdByCar(carId) ?? 0;
		prefsCarSlotDataModel.profiles = gamePrefs.legacyCarSettings.GetEmptyProfilesForCar(carId);
		PlayerCar byId = DependencyInjector.Resolve<BaseModel>().GetById<PlayerCar>(carId);
		prefsCarSlotDataModel.visual.Reset(byId);
		return prefsCarSlotDataModel;
	}

	protected override string GetModelName()
	{
		return $"{BasePrefsModel.storageSystem.EncodeName(GetType().Name)}_{carId}_{slotId}";
	}

	public override void Reset()
	{
		base.Reset();
		m_isLoaded = false;
		version = 0;
		caption = null;
		carId = 0;
		profileId = 0;
		customSetupId = null;
		swapDataByProfileId = null;
		profiles = null;
		visual = null;
		wheelPressureForXDS = null;
		modsCustomData = null;
		m_lastSaveHash = 0;
	}

	public override void Load()
	{
		base.Load();
		if (visual != null)
		{
			m_lastSaveHash = GetHashCode();
			m_isLoaded = true;
		}
	}

	public override void Save()
	{
		if (IsValid())
		{
			int hashCode = GetHashCode();
			if (m_lastSaveHash != hashCode)
			{
				m_lastSaveHash = hashCode;
				AddQueryToSave(this);
			}
		}
	}

	protected override void DoRealSave(string json)
	{
		if (IsValid())
		{
			base.DoRealSave(json);
		}
	}

	public bool IsValid()
	{
		if (m_isLoaded && profileId != 0)
		{
			return profiles.Count > 0;
		}
		return false;
	}

	public bool IsUpdateRequired()
	{
		return version < 1;
	}

	public void PullVersion()
	{
		version = 1;
	}

	public bool IsCustom()
	{
		return !string.IsNullOrEmpty(customSetupId);
	}

	public PrefsCarSlotDataModel Copy()
	{
		PrefsCarSlotDataModel prefsCarSlotDataModel = CreateEmptyModel();
		prefsCarSlotDataModel.version = version;
		prefsCarSlotDataModel.caption = caption;
		prefsCarSlotDataModel.carId = carId;
		prefsCarSlotDataModel.profileId = profileId;
		prefsCarSlotDataModel.customSetupId = customSetupId;
		prefsCarSlotDataModel.swapDataByProfileId = new Dictionary<int, CarSwapData>();
		prefsCarSlotDataModel.profiles = new Dictionary<int, ProfilesInfo.Profile>();
		prefsCarSlotDataModel.visual = visual.Copy();
		prefsCarSlotDataModel.modsCustomData = modsCustomData;
		int key;
		foreach (KeyValuePair<int, CarSwapData> item in swapDataByProfileId)
		{
			item.Deconstruct(out key, out var value);
			int key2 = key;
			CarSwapData data = value;
			prefsCarSlotDataModel.swapDataByProfileId[key2] = new CarSwapData(data);
		}
		foreach (KeyValuePair<int, ProfilesInfo.Profile> profile2 in profiles)
		{
			profile2.Deconstruct(out key, out var value2);
			int key3 = key;
			ProfilesInfo.Profile profile = value2;
			prefsCarSlotDataModel.profiles[key3] = profile.Copy();
		}
		return prefsCarSlotDataModel;
	}

	public override bool Equals(object obj)
	{
		if (obj is PrefsCarSlotDataModel prefsCarSlotDataModel)
		{
			return GetHashCode() == prefsCarSlotDataModel.GetHashCode();
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(version);
		hashCode.Add(caption);
		hashCode.Add(carId);
		hashCode.Add(profileId);
		hashCode.Add(customSetupId);
		hashCode.Add(visual);
		hashCode.Add(wheelPressureForXDS?.frontPressure);
		hashCode.Add(wheelPressureForXDS?.rearPressure);
		hashCode.Add(modsCustomData);
		if (profiles == null)
		{
			return hashCode.ToHashCode();
		}
		int key;
		foreach (KeyValuePair<int, ProfilesInfo.Profile> profile2 in profiles)
		{
			profile2.Deconstruct(out key, out var value);
			int value2 = key;
			ProfilesInfo.Profile profile = value;
			hashCode.Add(value2);
			hashCode.Add(profile.id);
			hashCode.Add(profile.forceDefault);
			string key2;
			float value3;
			foreach (KeyValuePair<string, float> controller in profile.controllers)
			{
				controller.Deconstruct(out key2, out value3);
				string value4 = key2;
				float value5 = value3;
				hashCode.Add(value4);
				hashCode.Add(value5);
			}
			foreach (KeyValuePair<string, float> value12 in profile.values)
			{
				value12.Deconstruct(out key2, out value3);
				string value6 = key2;
				float value7 = value3;
				hashCode.Add(value6);
				hashCode.Add(value7);
			}
		}
		if (swapDataByProfileId == null)
		{
			return hashCode.ToHashCode();
		}
		foreach (KeyValuePair<int, CarSwapData> item in swapDataByProfileId)
		{
			item.Deconstruct(out key, out var value8);
			int value9 = key;
			CarSwapData carSwapData = value8;
			hashCode.Add(value9);
			hashCode.Add(carSwapData.currentId);
			foreach (KeyValuePair<int, EngineSwapData> savedEngine in carSwapData.savedEngines)
			{
				savedEngine.Deconstruct(out key, out var value10);
				int value11 = key;
				EngineSwapData engineSwapData = value10;
				hashCode.Add(value11);
				hashCode.Add(engineSwapData.id);
				if (engineSwapData.controllers == null)
				{
					continue;
				}
				foreach (EngineSwapData.SwapController controller2 in engineSwapData.controllers)
				{
					hashCode.Add(controller2.key);
					hashCode.Add(controller2.value);
				}
			}
		}
		return hashCode.ToHashCode();
	}
}
