using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CarProfile;
using Customization;
using DB;
using DB.Meta;
using DI;
using SimpleJSON;
using Steamworks;
using SyncMultiplayer;
using UnityEngine;
using VinylSystem;

namespace PrefsModels
{
	public class CarSettingsModel : BasePrefsModel
	{
		public class TandemWheelPressure
		{
			public float frontPressure;

			public float rearPressure;
		}

		public const string SWAP_KEY_FORMAT = "{0}_{1}";

		private BaseModel m_model;

		private Dictionary<int, CarVisualManager.VisualSettings> m_carVisualSettingsData = new Dictionary<int, CarVisualManager.VisualSettings>();

		[JsonSerialize]
		private Dictionary<int, int> m_profilePerCar = new Dictionary<int, int>
		{
			{ 3, 839 },
			{ 40, 859 },
			{ 9, 878 }
		};

		[Obsolete]
		[JsonSerialize]
		private Dictionary<int, CarSwapData> m_enginesPerCar = new Dictionary<int, CarSwapData>();

		[JsonSerialize]
		private Dictionary<string, CarCameraSettings> m_camerasSettings = new Dictionary<string, CarCameraSettings>();

		[JsonSerialize]
		private ProfilesInfo m_profilesInfo = new ProfilesInfo();

		private Dictionary<int, CarVilylsInfo> m_carVinylPreset = new Dictionary<int, CarVilylsInfo>();

		[JsonSerialize]
		private Dictionary<int, TandemWheelPressure> m_carWheelPressureForXDS = new Dictionary<int, TandemWheelPressure>();

		[JsonSerialize]
		private List<CustomProfile> m_customProfiles = new List<CustomProfile>();

		[JsonSerialize]
		private Dictionary<int, (string setupId, bool haveCustomSetup, int engineId)> m_carsWithCustomSetups = new Dictionary<int, (string, bool, int)>();

		[JsonSerialize]
		private Dictionary<string, CarSwapData> m_enginesPerCarCustomSetups = new Dictionary<string, CarSwapData>();

		private bool m_needWriteToRemote;

		private Dictionary<int, OneCarSettingsModel> m_carsModels = new Dictionary<int, OneCarSettingsModel>();

		private readonly string REMOTE_MODEL_NAME = "customizationSettings";

		private readonly string REMOTE_MODEL_EXT = ".dat";

		private StringBuilder m_sbRemoteName;

		private const char UNDERSCORE = '_';

		private string m_remoteName;

		public Dictionary<string, CarCameraSettings> camerasSettings => m_camerasSettings;

		public string remoteName => m_remoteName ?? (m_remoteName = GetRemoteName());

		public void Export(PrefsCarSlotDataModel slot)
		{
			int carId = slot.carId;
			OneCarSettingsModel value;
			bool num = !m_carsModels.TryGetValue(carId, out value) || !value.isLoaded;
			slot.customSetupId = GetInstalledCustomProfile(carId)?.id;
			slot.profileId = m_profilePerCar.GetValueOrDefault(carId, GetDefaultProfileIdByCar(carId) ?? 0);
			slot.profiles = GetProfilesForCar(carId);
			slot.swapDataByProfileId = GetSwapDataFromProfiles(carId);
			slot.visual = GetVisualForCar(carId);
			slot.wheelPressureForXDS = m_carWheelPressureForXDS.GetValueOrDefault(carId);
			if (num)
			{
				TryReleaseCarModelResources(carId);
			}
		}

		public void Import(PrefsCarSlotDataModel slot)
		{
			int carId = slot.carId;
			SetProfilePerCar(carId, slot.profileId);
			foreach (var (profileId, info) in slot.profiles)
			{
				SetProfileInfo(profileId, info);
			}
			SetHaveCustomSetupsPerCar(carId, slot.swapData.currentId, slot.customSetupId, slot.IsCustom());
			KeyValuePair<int, EngineSwapData>[] array = slot.swapData.savedEngines.ToArray();
			foreach (KeyValuePair<int, EngineSwapData> keyValuePair2 in array)
			{
				SetEngineSwapDataForCar(carId, keyValuePair2.Value);
			}
			SetEngineForCar(carId, slot.swapData.currentId);
			SetVisualForCar(carId, slot.visual);
			m_carWheelPressureForXDS[carId] = slot.wheelPressureForXDS;
		}

		private void SaveCarsModels()
		{
			if (m_carsModels.Count <= 0)
			{
				return;
			}
			bool flag = BasePrefsModel.m_storageSystem.IsRemoteSupported();
			foreach (KeyValuePair<int, OneCarSettingsModel> carsModel in m_carsModels)
			{
				OneCarSettingsModel value = carsModel.Value;
				if (value == null)
				{
					continue;
				}
				if (m_carVisualSettingsData.TryGetValue(value.carId, out var value2) && !value2.Equals(value.carVisualSettingsData))
				{
					if (value2.bodyColor.HasValue && value2.oldColorsFormat)
					{
						PlayerCar byId = m_model.GetById<PlayerCar>(value.carId);
						value2.TransformToNewFormat(byId);
					}
					value.carVisualSettingsData = value2.Copy();
					value.hasChanged = true;
					if (value2.colors.Count > 0 && !value.isLoaded)
					{
						value.isLoaded = true;
					}
				}
				if (m_carVinylPreset.TryGetValue(value.carId, out var value3) && !value3.Equals(value.carVinylPreset))
				{
					CarVilylsInfo carVinylPreset = value3.Copy();
					value.carVinylPreset = carVinylPreset;
					value.hasChanged = true;
				}
				if (value.isLoaded)
				{
					if (value.hasChanged)
					{
						value.hasChanged = false;
						AddQueryToSave(value);
						value.needWriteToRemote = true;
					}
					if (flag && m_needWriteToRemote && value.needWriteToRemote)
					{
						string json = JSON.Serialize(value);
						value.WriteToRemote(json);
						value.needWriteToRemote = false;
					}
				}
			}
		}

		public Dictionary<int, CarSwapData> GetSwapDataFromProfiles(int carId)
		{
			Dictionary<int, CarSwapData> dictionary = new Dictionary<int, CarSwapData>();
			foreach (PlayerCarProfile allPlayerCarProfile in GetAllPlayerCarProfiles(carId))
			{
				object arg = (allPlayerCarProfile.IsUltimate() ? null : ((object)allPlayerCarProfile.id));
				string key = $"{carId}_{arg}";
				dictionary[allPlayerCarProfile.id] = m_enginesPerCarCustomSetups.GetValueOrDefault(key, new CarSwapData(0));
			}
			return dictionary;
		}

		public Dictionary<int, ProfilesInfo.Profile> GetProfilesForCar(int carId)
		{
			Dictionary<int, ProfilesInfo.Profile> dictionary = new Dictionary<int, ProfilesInfo.Profile>();
			foreach (PlayerCarProfile allPlayerCarProfile in GetAllPlayerCarProfiles(carId))
			{
				dictionary[allPlayerCarProfile.id] = GetProfileInfo(allPlayerCarProfile.id, carId, "", forceOriginalProfile: true);
			}
			return dictionary;
		}

		public Dictionary<int, ProfilesInfo.Profile> GetEmptyProfilesForCar(int carId)
		{
			Dictionary<int, ProfilesInfo.Profile> dictionary = new Dictionary<int, ProfilesInfo.Profile>();
			foreach (PlayerCarProfile allPlayerCarProfile in GetAllPlayerCarProfiles(carId))
			{
				dictionary[allPlayerCarProfile.id] = new ProfilesInfo.Profile(allPlayerCarProfile.id);
			}
			return dictionary;
		}

		private List<PlayerCarProfile> GetAllPlayerCarProfiles(int carId)
		{
			return m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x != null && x.carId == carId);
		}

		public IEnumerable<int> GetAllCarsFromModel()
		{
			FillCarModelsIfNeed();
			foreach (KeyValuePair<int, OneCarSettingsModel> carsModel in m_carsModels)
			{
				yield return carsModel.Key;
			}
		}

		private void FillCarModelsIfNeed()
		{
			if (m_carsModels.Count != 0)
			{
				return;
			}
			foreach (PlayerCar playerCar in ResourcesWrapper.Load<StaticDictionariesAsset>("DB/StaticDictionariesAsset").holder.playerCars)
			{
				if (playerCar.enabled && !m_carsModels.TryGetValue(playerCar.id, out var value))
				{
					value = new OneCarSettingsModel
					{
						carId = playerCar.id
					};
					m_carsModels.Add(playerCar.id, value);
				}
			}
		}

		public void TryReleaseCarModelResources(int carId)
		{
			if (m_carsModels.ContainsKey(carId))
			{
				m_carVinylPreset.Remove(carId);
				m_carsModels[carId] = new OneCarSettingsModel
				{
					carId = carId
				};
				m_carVisualSettingsData[carId] = new CarVisualManager.VisualSettings();
			}
		}

		private void LoadCarModels(int id)
		{
			if (!m_carsModels.TryGetValue(id, out var value))
			{
				FillCarModelsIfNeed();
				if (!m_carsModels.TryGetValue(id, out value))
				{
					return;
				}
			}
			if (value.isLoaded)
			{
				return;
			}
			base.isLastLoadFailed = false;
			bool flag = isSaveAllowed;
			isSaveAllowed = false;
			try
			{
				BasePrefsModel.storageSystem.Read(value);
				FixCarIdModelIfNeed(value, id);
				CarVisualManager.VisualSettings carVisualSettingsData = value.carVisualSettingsData;
				if ((carVisualSettingsData != null && (carVisualSettingsData.colorId.HasValue || carVisualSettingsData.colors.Count > 0)) || value.carVinylPreset != null)
				{
					value.isLoaded = true;
					value.hasChanged = false;
				}
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "System", "Data loss: cant load " + GetType().Name + " E:" + ex.Message);
				ResetByLoadError();
			}
			isSaveAllowed = flag;
		}

		private void FixCarIdModelIfNeed(OneCarSettingsModel carModel, int targetToLoadId)
		{
			if (carModel.carId != targetToLoadId)
			{
				GameLog.Print(AlertLevel.Warning, "System", $"Fixing car prefs model id => CarModelCarId: [{carModel.carId}] != targetToLoadCarId [{targetToLoadId}]");
				carModel.carId = targetToLoadId;
			}
		}

		public void SetTandemPressure(int carId, float fPressure, float rPressure)
		{
			m_carWheelPressureForXDS[carId] = new TandemWheelPressure
			{
				frontPressure = fPressure,
				rearPressure = rPressure
			};
		}

		public CarVilylsInfo GetVinylsInfoForCar(int carId)
		{
			if (!m_carVinylPreset.TryGetValue(carId, out var value))
			{
				value = new CarVilylsInfo();
				m_carVinylPreset.Add(carId, value);
				m_carsModels[carId].carVinylPreset = value;
			}
			return value;
		}

		public void TryLoadCarModelIfNeeded(int carId)
		{
			if (m_carsModels.TryGetValue(carId, out var value) && value.isLoaded)
			{
				return;
			}
			FillCarModelsIfNeed();
			if (value == null)
			{
				value = m_carsModels[carId];
			}
			if (!BasePrefsModel.m_storageSystem.IsRemoteSupported() || !ReadFromRemoteIfNeed(value))
			{
				LoadCarModels(carId);
				if (!m_carsModels.TryGetValue(carId, out value) || !value.isLoaded)
				{
					value.isLoaded = true;
				}
				PostLoadCarModel(carId);
			}
		}

		private bool ReadFromRemoteIfNeed(OneCarSettingsModel carModel)
		{
			if (BasePrefsModel.m_storageSystem.NeedUpdateRemote(carModel, carModel.remoteName))
			{
				byte[] data = null;
				try
				{
					BasePrefsModel.m_storageSystem.ReadFromRemoteStorage(carModel.remoteName, out data);
				}
				catch (Exception ex)
				{
					GameLog.Print(AlertLevel.Error, "Save", ex.ToString());
					data = null;
				}
				if (data != null)
				{
					try
					{
						OneCarSettingsModel oneCarSettingsModel = CustomTypes.DeserializeByJSON<OneCarSettingsModel>(data);
						if (oneCarSettingsModel != null && oneCarSettingsModel.carVisualSettingsData != null)
						{
							m_carsModels[carModel.carId] = oneCarSettingsModel;
							m_carsModels[carModel.carId].isLoaded = true;
							m_carsModels[carModel.carId].hasChanged = true;
						}
						else
						{
							Save();
						}
					}
					catch (Exception ex2)
					{
						GameLog.Print(AlertLevel.Error, "Save", ex2.ToString());
						data = null;
					}
				}
				PostLoadCarModel(carModel.carId);
				return true;
			}
			return false;
		}

		public CarVisualManager.VisualSettings GetVisualForCar(int carId)
		{
			if (m_carVisualSettingsData.ContainsKey(carId))
			{
				TryLoadCarModelIfNeeded(carId);
				CarVisualManager.VisualSettings visualSettings = m_carVisualSettingsData[carId];
				if (visualSettings.colors.Count <= 0)
				{
					PlayerCar byId = m_model.GetById<PlayerCar>(carId);
					if (visualSettings.oldColorsFormat)
					{
						visualSettings.TransformToNewFormat(byId);
					}
					else
					{
						visualSettings.ResetColors(byId);
					}
				}
				return visualSettings;
			}
			Debug.LogError($"GetVisual wrong carId:{carId} for carVisualSettingsData[{m_carVisualSettingsData.Count}]");
			return null;
		}

		public void SetVisualForCar(int carId, CarVisualManager.VisualSettings settings, bool vinylsHasChanged = false)
		{
			m_carsModels[carId].hasChanged = vinylsHasChanged;
			m_carVisualSettingsData[carId] = settings;
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			if (gamePrefs != null)
			{
				gamePrefs.globals.carsVisualUpdate.Add(carId);
			}
			m_needWriteToRemote = false;
			Save();
		}

		public void SetVisualWithoutSave(int carId, CarVisualManager.VisualSettings settings)
		{
			m_carVisualSettingsData[carId] = settings;
		}

		public void SetProfilePerCar(int carId, int profileId)
		{
			if (m_profilePerCar[carId] != profileId)
			{
				m_profilePerCar[carId] = profileId;
				Save();
			}
		}

		[Obsolete]
		private string MakeLegacyInstalledCustomSetupKey(int carId)
		{
			return $"{carId}_{GetInstalledCustomProfile(carId)?.id}";
		}

		private string MakeInstalledCustomSetupKey(int carId)
		{
			CustomProfile installedCustomProfile = GetInstalledCustomProfile(carId);
			if (installedCustomProfile != null)
			{
				return $"{carId}_{installedCustomProfile.id}";
			}
			PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(m_profilePerCar.GetValueOrDefault(carId));
			object arg = ((byId == null || byId.IsUltimate()) ? null : ((object)byId.id));
			return $"{carId}_{arg}";
		}

		public bool SwapAvailableForCar(int carId)
		{
			string key = MakeInstalledCustomSetupKey(carId);
			if (m_enginesPerCarCustomSetups.TryGetValue(key, out var value))
			{
				return value.savedEngines.Count > 0;
			}
			return false;
		}

		public int GetEngineIdForCar(int carId, string customSetupId = "")
		{
			string key = MakeInstalledCustomSetupKey(carId);
			if (!string.IsNullOrEmpty(customSetupId))
			{
				key = $"{carId}_{customSetupId}";
			}
			if (!m_enginesPerCarCustomSetups.TryGetValue(key, out var value))
			{
				return 0;
			}
			return value.currentId;
		}

		public void SetEngineForCar(int carId, int engineId)
		{
			string key = MakeInstalledCustomSetupKey(carId);
			if (m_enginesPerCarCustomSetups.TryGetValue(key, out var value))
			{
				value.currentId = engineId;
				if (!value.savedEngines.ContainsKey(engineId))
				{
					value.savedEngines.Add(engineId, CarSwapData.GetDefaultMeta(engineId));
				}
				Save();
			}
			else
			{
				m_enginesPerCarCustomSetups[key] = new CarSwapData(engineId);
				Save();
			}
		}

		public void ClearEnginesPerCarCustomSetupsCache(int carId, int engineId, string customSetupId)
		{
			string key = $"{carId}_{customSetupId}";
			if (m_enginesPerCarCustomSetups.TryGetValue(key, out var value) && value.savedEngines.ContainsKey(engineId))
			{
				value.savedEngines.Remove(engineId);
			}
		}

		public EngineSwapData GetEngineSwapDataForCar(int carId, int engineId = -1, string customSetupId = "")
		{
			if (engineId == -1)
			{
				engineId = GetEngineIdForCar(carId);
			}
			string key = MakeInstalledCustomSetupKey(carId);
			if (!string.IsNullOrEmpty(customSetupId))
			{
				key = $"{carId}_{customSetupId}";
			}
			if (m_enginesPerCarCustomSetups.TryGetValue(key, out var value) && value.savedEngines.TryGetValue(engineId, out var value2))
			{
				return value2.Copy();
			}
			return CarSwapData.GetDefaultMeta(engineId);
		}

		public void SetEngineSwapDataForCar(int carId, EngineSwapData swap, string overrideKey = null)
		{
			if (swap != null)
			{
				string key = overrideKey ?? MakeInstalledCustomSetupKey(carId);
				if (!m_enginesPerCarCustomSetups.TryGetValue(key, out var value))
				{
					m_enginesPerCarCustomSetups[key] = new CarSwapData(swap);
					Save();
					return;
				}
				Dictionary<int, EngineSwapData> savedEngines = value.savedEngines;
				value.currentId = swap.id;
				savedEngines[swap.id] = swap.Copy();
				Save();
			}
		}

		public void SaveEngineSwapDataForCar(int carId, EngineSwapData swap)
		{
			string key = MakeInstalledCustomSetupKey(carId);
			if (!m_enginesPerCarCustomSetups.TryGetValue(key, out var value))
			{
				CarSwapData value2 = new CarSwapData(swap)
				{
					currentId = 0
				};
				m_enginesPerCarCustomSetups[key] = value2;
				Save();
			}
			else
			{
				value.savedEngines[swap.id] = swap.Copy();
				Save();
			}
		}

		public List<CustomProfile> GetCustomProfilesByCarId(int carId)
		{
			return m_customProfiles.FindAll((CustomProfile cp) => cp.carId == carId && IsCustomValid(cp));
		}

		public (string setupId, bool haveInstalledCustom, int engineId) GetHaveCustomSetupsPerCar(int carId)
		{
			if (m_carsWithCustomSetups.TryGetValue(carId, out (string, bool, int) value))
			{
				return value;
			}
			return (setupId: string.Empty, haveInstalledCustom: false, engineId: 0);
		}

		public void RemoveCustomSetup(int carId, string setupId)
		{
			if (m_carsWithCustomSetups.ContainsKey(carId))
			{
				m_carsWithCustomSetups.Remove(carId);
			}
			CustomProfile customProfile = m_customProfiles.Find((CustomProfile el) => el.id == setupId);
			if (customProfile != null)
			{
				m_customProfiles.Remove(customProfile);
			}
			Save();
		}

		public CustomProfile GetInstalledCustomProfile(int carId)
		{
			(string setupId, bool haveInstalledCustom, int engineId) haveCustomSetupsPerCar = GetHaveCustomSetupsPerCar(carId);
			var (text, _, _) = haveCustomSetupsPerCar;
			if (haveCustomSetupsPerCar.haveInstalledCustom)
			{
				foreach (CustomProfile customProfile in m_customProfiles)
				{
					if (customProfile.id == text && IsCustomValid(customProfile))
					{
						return customProfile;
					}
				}
			}
			return null;
		}

		public void SetHaveCustomSetupsPerCar(int carId, int engineId, string setupId, bool isHaveCustomSetup)
		{
			m_carsWithCustomSetups[carId] = (setupId, isHaveCustomSetup, engineId);
			Save();
		}

		private bool IsCustomValid(CustomProfile customProfile)
		{
			if (customProfile == null || customProfile.profile == null)
			{
				return false;
			}
			return PlayerCarProfileMetaExtensions.IsProfileExist(customProfile.profile.id);
		}

		public CustomProfile FindCustomById(string customId)
		{
			return m_customProfiles.Find((CustomProfile cp) => cp.id == customId && IsCustomValid(cp));
		}

		public ProfilesInfo.Profile GetProfileInfo(int profileId, int carId, string customSetupId = "", bool forceOriginalProfile = false)
		{
			(string, bool, int) haveCustomSetupsPerCar = GetHaveCustomSetupsPerCar(carId);
			if ((!string.IsNullOrEmpty(customSetupId) || haveCustomSetupsPerCar.Item2) && !forceOriginalProfile)
			{
				string text;
				if (string.IsNullOrEmpty(customSetupId))
				{
					(text, _, _) = haveCustomSetupsPerCar;
				}
				else
				{
					text = customSetupId;
				}
				string customId = text;
				CustomProfile customProfile = FindCustomById(customId);
				if (customProfile != null)
				{
					return customProfile.profile;
				}
			}
			return GetOriginalProfile(profileId);
		}

		public ProfilesInfo.Profile GetOriginalProfile(int profileId)
		{
			if (!m_profilesInfo.profiles.TryGetValue(profileId, out var value))
			{
				value = new ProfilesInfo.Profile(profileId);
				m_profilesInfo.profiles.Add(profileId, value);
			}
			return value;
		}

		public bool CheckHaveCustomSetup(out CustomProfile customProfile, int carId, string customName, int profileId, int engineId)
		{
			customProfile = m_customProfiles.Find((CustomProfile p) => p.carId == carId && p.name.Equals(customName, StringComparison.OrdinalIgnoreCase) && p.profile.id == profileId && p.engineId == engineId && IsCustomValid(p));
			return customProfile != null;
		}

		public void SetCustomProfileInfo(out CustomSetupsSaveResult reason, out CustomProfile customProfile, string customName, int profileId, int carId, int engineId, ProfilesInfo.Profile info, string customSetupId = "")
		{
			reason = CustomSetupsSaveResult.Fail;
			customProfile = null;
			List<CustomProfile> customProfilesByCarId = GetCustomProfilesByCarId(carId);
			CheckHaveCustomSetup(out var customProfile2, carId, customName, profileId, engineId);
			string setupId = customSetupId;
			int num = m_customProfiles.IndexOf(customProfile2);
			if (num >= 0)
			{
				setupId = customProfile2.id;
				m_customProfiles.RemoveAt(num);
			}
			else if (customProfilesByCarId != null && customProfilesByCarId.Count >= 16)
			{
				reason = CustomSetupsSaveResult.SlotExceed;
				return;
			}
			customProfile = new CustomProfile(customName, carId, engineId, info, setupId);
			if (num >= 0)
			{
				m_customProfiles.Insert(num, customProfile);
				reason = CustomSetupsSaveResult.Rewrite;
			}
			else
			{
				m_customProfiles.Add(customProfile);
				reason = CustomSetupsSaveResult.Success;
			}
			SetProfileOriginalValues(profileId, info);
			Save();
		}

		public void SetProfileInfo(int profileId, ProfilesInfo.Profile info)
		{
			m_profilesInfo.profiles.Remove(profileId);
			info.forceDefault = info.controllers.Count == 0;
			m_profilesInfo.profiles.Add(profileId, info);
			Save();
		}

		public CarSettingsModel()
		{
			m_model = DependencyInjector.Resolve<BaseModel>();
		}

		public string SerializeBase()
		{
			return Convert.ToBase64String(CustomTypes.SerializeByJSON<CarSettingsModel>(this));
		}

		public byte[] SerializeByJSON()
		{
			return CustomTypes.SerializeByJSON<CarSettingsModel>(this);
		}

		private void DeserializeAndMerge(byte[] bytes)
		{
			CarSettingsModel carSettingsModel = null;
			try
			{
				carSettingsModel = CustomTypes.DeserializeByJSON<CarSettingsModel>(bytes);
			}
			catch
			{
				carSettingsModel = null;
			}
			if (carSettingsModel == null)
			{
				return;
			}
			bool flag = isSaveAllowed;
			if (carSettingsModel.m_carVisualSettingsData != null)
			{
				foreach (KeyValuePair<int, CarVisualManager.VisualSettings> carVisualSettingsDatum in carSettingsModel.m_carVisualSettingsData)
				{
					m_carVisualSettingsData[carVisualSettingsDatum.Key] = carVisualSettingsDatum.Value;
				}
			}
			if (carSettingsModel.m_profilePerCar != null)
			{
				foreach (KeyValuePair<int, int> item in carSettingsModel.m_profilePerCar)
				{
					m_profilePerCar[item.Key] = item.Value;
				}
			}
			if (carSettingsModel.m_profilesInfo != null)
			{
				foreach (KeyValuePair<int, ProfilesInfo.Profile> profile in carSettingsModel.m_profilesInfo.profiles)
				{
					m_profilesInfo.profiles[profile.Key] = profile.Value;
				}
			}
			if (carSettingsModel.m_carVinylPreset != null)
			{
				foreach (KeyValuePair<int, CarVilylsInfo> item2 in carSettingsModel.m_carVinylPreset)
				{
					m_carVinylPreset[item2.Key] = item2.Value;
				}
			}
			isSaveAllowed = true;
			Save();
			isSaveAllowed = flag;
			PostLoad();
			Start();
		}

		public void DeserializeAndMergeBase(string codebase)
		{
			if (!string.IsNullOrEmpty(codebase))
			{
				byte[] bytes = Convert.FromBase64String(codebase);
				DeserializeAndMerge(bytes);
			}
		}

		public override void PostLoad()
		{
			InitVisual();
			InitProfiles();
			RestoreValues();
			InitNewCustomProfilesFieldsIfNeed();
			MergeEngineSwapInfoIfNeed();
			PostLoadCarModel();
		}

		private void InitNewCustomProfilesFieldsIfNeed()
		{
			bool flag = false;
			foreach (CustomProfile customProfile in m_customProfiles)
			{
				if (customProfile.code == null)
				{
					customProfile.code = string.Empty;
					customProfile.isOfficial = false;
					flag = true;
				}
			}
			if (flag)
			{
				Save();
			}
		}

		private void MergeEngineSwapInfoIfNeed()
		{
			if (m_enginesPerCarCustomSetups.Count > 0)
			{
				return;
			}
			foreach (KeyValuePair<int, CarSwapData> item in m_enginesPerCar)
			{
				string key = $"{item.Key.ToString()}_{string.Empty}";
				m_enginesPerCarCustomSetups[key] = item.Value;
			}
		}

		public override void Reset()
		{
			base.Reset();
			try
			{
				int num = 0;
				if (m_carsModels != null && m_carsModels.Count != 0)
				{
					foreach (KeyValuePair<int, OneCarSettingsModel> carsModel in m_carsModels)
					{
						OneCarSettingsModel value = carsModel.Value;
						if (value == null)
						{
							continue;
						}
						int carId = value.carId;
						value = new OneCarSettingsModel
						{
							carId = carId,
							carVisualSettingsData = new CarVisualManager.VisualSettings(),
							carVinylPreset = new CarVilylsInfo(),
							isLoaded = false,
							hasChanged = false
						};
						if (m_carVisualSettingsData.ContainsKey(carId))
						{
							m_carVisualSettingsData[carId] = value.carVisualSettingsData.Copy();
						}
						if (m_carVinylPreset.ContainsKey(carId))
						{
							m_carVinylPreset[carId] = value.carVinylPreset.Copy();
						}
						num++;
						AddQueryToSave(value);
						if (BasePrefsModel.m_storageSystem.IsRemoteSupported())
						{
							if (SteamRemoteStorage.FileExists(value.remoteName))
							{
								SteamRemoteStorage.FileDelete(value.remoteName);
							}
							if (SteamRemoteStorage.FileExists(remoteName))
							{
								SteamRemoteStorage.FileDelete(remoteName);
							}
						}
					}
				}
				num = Mathf.CeilToInt((float)num / 50f);
				for (int i = 0; i < num; i++)
				{
					BasePrefsModel.SaveAllQueries(inOneFrame: true);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error reset {GetType()}: E: {ex.Message}");
			}
		}

		private void PostLoadCarModel(int id = -1)
		{
			if (id == -1)
			{
				foreach (KeyValuePair<int, OneCarSettingsModel> carsModel in m_carsModels)
				{
					PostLoad(carsModel.Value);
				}
				return;
			}
			if (m_carsModels.TryGetValue(id, out var value) && value.isLoaded)
			{
				PostLoad(value);
			}
			void PostLoad(OneCarSettingsModel carModel)
			{
				if (carModel != null && carModel.isLoaded)
				{
					int carId = carModel.carId;
					CarVisualManager.VisualSettings carVisualSettingsData = carModel.carVisualSettingsData;
					if (carVisualSettingsData != null && (carVisualSettingsData.colors.Count > 0 || carVisualSettingsData.colorId.HasValue))
					{
						m_carVisualSettingsData[carId] = carVisualSettingsData.Copy();
					}
					CarVilylsInfo carVinylPreset = carModel.carVinylPreset;
					if (carVinylPreset != null)
					{
						if (carVinylPreset.MergeInternalDataIfNeed(carVisualSettingsData))
						{
							SetVisualForCar(carId, carVisualSettingsData.Copy(), vinylsHasChanged: true);
						}
						m_carVinylPreset[carId] = carVinylPreset.Copy();
					}
				}
			}
		}

		public override void Load()
		{
			base.Load();
		}

		public override void Save()
		{
			StoreValues();
			SaveCarsModels();
			base.Save();
		}

		private void RestoreValues()
		{
			foreach (KeyValuePair<int, ProfilesInfo.Profile> profile in m_profilesInfo.profiles)
			{
				PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(profile.Key);
				if (byId == null)
				{
					continue;
				}
				foreach (string item in new List<string>(profile.Value.controllers.Keys))
				{
					if (item == "front_suspension_stiffness")
					{
						float value = profile.Value.controllers["front_suspension_stiffness"];
						profile.Value.controllers["front_fast_bump"] = value;
						profile.Value.controllers["front_fast_rebound"] = value;
						profile.Value.controllers["front_slow_bump"] = value;
						profile.Value.controllers["front_slow_rebound"] = value;
						profile.Value.controllers.Remove("front_suspension_stiffness");
					}
					else if (item == "rear_suspension_stiffness")
					{
						float value2 = profile.Value.controllers["rear_suspension_stiffness"];
						profile.Value.controllers["rear_fast_bump"] = value2;
						profile.Value.controllers["rear_fast_rebound"] = value2;
						profile.Value.controllers["rear_slow_bump"] = value2;
						profile.Value.controllers["rear_slow_rebound"] = value2;
						profile.Value.controllers.Remove("rear_suspension_stiffness");
					}
				}
				foreach (KeyValuePair<string, float> val in profile.Value.values)
				{
					float value3 = val.Value;
					CarProfileController carProfileController = byId.dynoInfo.controllers.Find((CarProfileController x) => x.name.Equals(val.Key));
					if (carProfileController != null && carProfileController.changers != null && carProfileController.changers.Count > 0)
					{
						float value4 = carProfileController.changers[0].EvaluatePhase(value3, carProfileController.isPercent);
						profile.Value.controllers[val.Key] = value4;
					}
				}
			}
		}

		private void StoreValues()
		{
			foreach (KeyValuePair<int, ProfilesInfo.Profile> profile in m_profilesInfo.profiles)
			{
				SetProfileOriginalValues(profile.Key, profile.Value);
			}
		}

		private void SetProfileOriginalValues(int profileId, ProfilesInfo.Profile info)
		{
			info.values.Clear();
			PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(profileId);
			if (byId == null)
			{
				return;
			}
			foreach (KeyValuePair<string, float> val in info.controllers)
			{
				CarProfileController carProfileController = byId.dynoInfo.controllers.Find((CarProfileController x) => x.name.Equals(val.Key));
				if (carProfileController != null && carProfileController.changers != null && carProfileController.changers.Count > 0)
				{
					float value = carProfileController.changers[0].EvaluateOriginalValue(val.Value, carProfileController.isPercent);
					info.values.Add(val.Key, value);
				}
			}
		}

		private void InitVisual()
		{
			CarPartDatabase.Load();
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (!m_carVisualSettingsData.ContainsKey(item.id))
				{
					m_carVisualSettingsData[item.id] = new CarVisualManager.VisualSettings();
				}
			}
			foreach (KeyValuePair<int, CarVisualManager.VisualSettings> carVisualSettingsDatum in m_carVisualSettingsData)
			{
				PlayerCar byId = m_model.GetById<PlayerCar>(carVisualSettingsDatum.Key);
				if (byId == null)
				{
					continue;
				}
				CarVisualManager.VisualSettings value = carVisualSettingsDatum.Value;
				if (!value.IsCorrectForCar(byId, m_model))
				{
					GameLog.Print(AlertLevel.Error, "Prefs", "Wrong visual settings for the car " + carVisualSettingsDatum.Key + ". Resetting.");
					value.Reset();
				}
				if (value.vinylLayers == null)
				{
					GameLog.Print(AlertLevel.Error, "Prefs", "Wrong visual settings for the car " + carVisualSettingsDatum.Key + ". Empty layer list.");
					value.vinylLayers = new List<VinylLayer>();
					continue;
				}
				int count = value.vinylLayers.Count;
				value.vinylLayers.RemoveAll((VinylLayer l) => l == null);
				int count2 = value.vinylLayers.Count;
				if (count != count2)
				{
					GameLog.Print(AlertLevel.Error, "Prefs", "Wrong visual settings for the car " + carVisualSettingsDatum.Key + ". Null layers removed.");
				}
			}
		}

		protected override void DoRealSave(string json)
		{
			if (m_needWriteToRemote)
			{
				WriteToRemote(json);
				m_needWriteToRemote = false;
			}
			base.DoRealSave(json);
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is CarSettingsModel carSettingsModel))
			{
				return true;
			}
			if (m_carVisualSettingsData == carSettingsModel.m_carVisualSettingsData && m_carVinylPreset == carSettingsModel.m_carVinylPreset && m_profilesInfo == carSettingsModel.m_profilesInfo)
			{
				return m_profilePerCar == carSettingsModel.m_profilePerCar;
			}
			return false;
		}

		private string GetRemoteName()
		{
			if (m_sbRemoteName == null)
			{
				m_sbRemoteName = new StringBuilder();
			}
			m_sbRemoteName.Clear();
			m_sbRemoteName.Append(REMOTE_MODEL_NAME);
			m_sbRemoteName.Append(REMOTE_MODEL_EXT);
			return m_sbRemoteName.ToString();
		}

		private void WriteToRemote(string json)
		{
			if (BasePrefsModel.m_storageSystem.IsRemoteSupported())
			{
				byte[] array = null;
				try
				{
					array = CustomTypes.Zip(json);
				}
				catch (Exception ex)
				{
					GameLog.Print(AlertLevel.Error, "Save", ex.ToString());
					array = null;
				}
				if (array != null)
				{
					BasePrefsModel.m_storageSystem.WriteToRemoteStorage(remoteName, array);
				}
			}
		}

		public void SaveAndWriteToRemote()
		{
			m_needWriteToRemote = true;
			Save();
		}

		public void ReadFromRemote()
		{
			if (BasePrefsModel.m_storageSystem.IsRemoteSupported() && BasePrefsModel.m_storageSystem.NeedUpdateRemote(this, remoteName))
			{
				byte[] data = null;
				try
				{
					BasePrefsModel.m_storageSystem.ReadFromRemoteStorage(remoteName, out data);
				}
				catch (Exception ex)
				{
					GameLog.Print(AlertLevel.Error, "Save", ex.ToString());
					data = null;
				}
				if (data != null)
				{
					DeserializeAndMerge(data);
				}
			}
		}

		private void InitProfiles()
		{
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (!m_profilePerCar.ContainsKey(item.id))
				{
					int? defaultProfileIdByCar = GetDefaultProfileIdByCar(item.id);
					if (!defaultProfileIdByCar.HasValue)
					{
						throw new Exception($"can't find default profile for car id - {item.id}");
					}
					m_profilePerCar.Add(item.id, defaultProfileIdByCar.Value);
				}
			}
			foreach (KeyValuePair<int, ProfilesInfo.Profile> profile in m_profilesInfo.profiles)
			{
				ProfilesInfo.Profile value = profile.Value;
				int key = profile.Key;
				if (key != value.id)
				{
					Debug.LogWarning($"Wrong profile for id: {key} =! {value.id}, fixed!");
					value.FixId(key);
				}
			}
		}

		public int? GetDefaultProfileIdByCar(int carId)
		{
			List<PlayerCarProfile> list = m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId);
			foreach (PlayerCarProfile item in list)
			{
				if (!item.defaultLocked)
				{
					return item.id;
				}
			}
			using (List<PlayerCarProfile>.Enumerator enumerator = list.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.id;
				}
			}
			return null;
		}
	}
}
