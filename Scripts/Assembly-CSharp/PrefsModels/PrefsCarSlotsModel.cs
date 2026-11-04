using System.Collections.Generic;
using DI;
using SimpleJSON;
using UnityEngine;
using VinylSystem;

namespace PrefsModels
{
	public class PrefsCarSlotsModel : BasePrefsModel
	{
		private class SlotsInfoHeader
		{
			public int activeSlotId = -1;

			public readonly List<int> sortedSlots = new List<int>();

			public readonly HashSet<int> usedSlots = new HashSet<int>();

			public void UseSlot(int id)
			{
				if (usedSlots.Add(id))
				{
					sortedSlots.Add(id);
				}
			}

			public void FreeSlot(int id)
			{
				if (usedSlots.Remove(id))
				{
					sortedSlots.Remove(id);
				}
			}
		}

		public const int UndefinedId = -1;

		[JsonSerialize]
		private Dictionary<int, SlotsInfoHeader> m_slotsInfo = new Dictionary<int, SlotsInfoHeader>();

		[JsonIgnore]
		private Dictionary<int, Dictionary<int, PrefsCarSlotDataModel>> m_carSlotModels = new Dictionary<int, Dictionary<int, PrefsCarSlotDataModel>>();

		private GamePrefs m_prefs;

		private CarSettingsModel m_legacyCarSettings;

		public static int GetMaxSlotCount()
		{
			return 5;
		}

		public override void PostLoad()
		{
			base.PostLoad();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_legacyCarSettings = m_prefs.legacyCarSettings;
		}

		public override void Start()
		{
			base.Start();
			FillSlotModelsForLazyLoad();
		}

		public override void Save()
		{
			m_legacyCarSettings.Save();
			SaveAllSlotModels();
			base.Save();
		}

		public override void Reset()
		{
			m_legacyCarSettings.Reset();
			ResetAllSlotModels();
			base.Reset();
		}

		public PrefsCarSlotDataModel GetActiveSlot(int carId)
		{
			if (IsCarModelCorrupted(carId))
			{
				MigrateCarModelForActiveSlot(carId);
			}
			PrefsCarSlotDataModel slotById = GetSlotById(carId, GetActiveSlotId(carId));
			UpdateExistedSlot(slotById);
			return slotById;
		}

		public PrefsCarSlotDataModel GetSlotById(int carId, int slotId)
		{
			if (!m_carSlotModels.TryGetValue(carId, out var value) || !value.TryGetValue(slotId, out var value2))
			{
				return null;
			}
			if (!value2.IsValid())
			{
				value2.Load();
			}
			if (!value2.IsValid())
			{
				value2 = PrefsCarSlotDataModel.CreateDefaultModel(carId);
				value2.slotId = slotId;
				AddSlot(value2);
			}
			return value2;
		}

		public void AddSlot(PrefsCarSlotDataModel data)
		{
			if (data == null)
			{
				return;
			}
			int carId = data.carId;
			if (!m_carSlotModels.ContainsKey(carId))
			{
				m_carSlotModels[carId] = new Dictionary<int, PrefsCarSlotDataModel>();
			}
			SlotsInfoHeader slotsInfo = GetSlotsInfo(carId);
			if (data.slotId == -1)
			{
				data.slotId = GetAvailableSlotId(slotsInfo.sortedSlots);
			}
			if (data.slotId != -1)
			{
				slotsInfo.UseSlot(data.slotId);
				m_carSlotModels[carId][data.slotId] = data;
				if (slotsInfo.activeSlotId == -1)
				{
					slotsInfo.activeSlotId = data.slotId;
				}
				Save();
			}
		}

		public void RemoveSlot(int carId, int slotId)
		{
			SlotsInfoHeader slotsInfo = GetSlotsInfo(carId);
			if (slotsInfo.usedSlots.Count <= 1 || !slotsInfo.usedSlots.Contains(slotId))
			{
				return;
			}
			GetSlotById(carId, slotId).Reset();
			if (slotsInfo.activeSlotId == slotId)
			{
				int num = slotsInfo.sortedSlots.IndexOf(slotId) - 1;
				if (num < 0)
				{
					num = 0;
				}
				slotsInfo.FreeSlot(slotId);
				slotsInfo.activeSlotId = slotsInfo.sortedSlots[num];
			}
			else
			{
				slotsInfo.FreeSlot(slotId);
			}
			m_carSlotModels[carId].Remove(slotId);
			Save();
		}

		public int GetActiveSlotId(int carId)
		{
			return GetSlotsInfo(carId).activeSlotId;
		}

		public void SetActiveSlotId(int carId, int slotId)
		{
			SlotsInfoHeader slotsInfo = GetSlotsInfo(carId);
			if (slotsInfo.usedSlots.Contains(slotId) && slotsInfo.activeSlotId != slotId)
			{
				slotsInfo.activeSlotId = slotId;
				Save();
			}
		}

		public List<int> GetUsedSlots(int carId)
		{
			return GetSlotsInfo(carId).sortedSlots;
		}

		private SlotsInfoHeader GetSlotsInfo(int carId)
		{
			if (!m_slotsInfo.ContainsKey(carId))
			{
				m_slotsInfo[carId] = new SlotsInfoHeader();
			}
			return m_slotsInfo[carId];
		}

		public CarVisualManager.VisualSettings GetVisualForCar(int carId)
		{
			m_legacyCarSettings.TryLoadCarModelIfNeeded(carId);
			return GetActiveSlot(carId).visual;
		}

		public void SetVisualForCar(int carId, CarVisualManager.VisualSettings settings, bool vinylsHasChanged = false)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			activeSlot.visual = settings;
			activeSlot.Save();
			m_legacyCarSettings.SetVisualForCar(carId, settings, vinylsHasChanged);
			if (vinylsHasChanged)
			{
				SyncVisualBetweenSlots(carId, settings);
			}
		}

		public void SetVisualWithoutSave(int carId, CarVisualManager.VisualSettings settings)
		{
			GetActiveSlot(carId).visual = settings;
			m_legacyCarSettings.SetVisualWithoutSave(carId, settings);
		}

		private void SyncVisualBetweenSlots(int carId, CarVisualManager.VisualSettings visual)
		{
			SlotsInfoHeader slotsInfo = GetSlotsInfo(carId);
			foreach (int usedSlot in slotsInfo.usedSlots)
			{
				if (usedSlot != slotsInfo.activeSlotId)
				{
					PrefsCarSlotDataModel slotById = GetSlotById(carId, usedSlot);
					if (slotById.visual.vinylPresetLiveryId == visual.vinylPresetLiveryId)
					{
						slotById.visual.vinylLayers = new List<VinylLayer>(visual.vinylLayers);
						slotById.Save();
						IconStorageSystem.DeleteLiveryPreview(carId, usedSlot);
					}
				}
			}
		}

		public void SetWinterTiresForcar(int carId, bool winterTiresEnabled)
		{
			CarVisualManager.VisualSettings visualForCar = GetVisualForCar(carId);
			if (visualForCar.winterTires != winterTiresEnabled)
			{
				visualForCar.winterTires = winterTiresEnabled;
				if (visualForCar.winterTires)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.tires_changed_to_winter_type.Localized());
				}
				SetVisualForCar(carId, visualForCar);
			}
		}

		public CarVilylsInfo GetVinylsInfoForCar(int carId)
		{
			m_legacyCarSettings.TryLoadCarModelIfNeeded(carId);
			return m_legacyCarSettings.GetVinylsInfoForCar(carId);
		}

		public int GetProfileIdForCar(int carId)
		{
			FixCarProfileIfNeed(carId);
			return GetActiveSlot(carId).profileId;
		}

		public void SetProfilePerCar(int carId, int profileId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (activeSlot.profileId != profileId)
			{
				activeSlot.profileId = profileId;
				activeSlot.Save();
				m_legacyCarSettings.SetProfilePerCar(carId, profileId);
			}
		}

		private void FixCarProfileIfNeed(int carId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			int profileId = activeSlot.profileId;
			PlayerCarProfileMetaExtensions.FixProfileIdIfNeed(carId, ref profileId);
			if (activeSlot.profileId != profileId)
			{
				if (activeSlot.IsCustom())
				{
					(string, bool, int) haveCustomSetupsPerCar = GetHaveCustomSetupsPerCar(carId);
					SetHaveCustomSetupsPerCar(carId, haveCustomSetupsPerCar.Item3, haveCustomSetupsPerCar.Item1, isHaveCustomSetup: false);
				}
				SetEngineForCar(carId, 0);
				SetProfilePerCar(carId, profileId);
			}
		}

		public bool SwapAvailableForCar(int carId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (activeSlot.IsCustom())
			{
				return m_legacyCarSettings.SwapAvailableForCar(carId);
			}
			return activeSlot.swapData.savedEngines.Count > 0;
		}

		public int GetEngineIdForCar(int carId, string customSetupId = "")
		{
			return GetActiveSlot(carId).swapData.currentId;
		}

		public void SetEngineForCar(int carId, int engineId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			activeSlot.swapData.currentId = engineId;
			if (!activeSlot.swapData.savedEngines.ContainsKey(engineId))
			{
				activeSlot.swapData.savedEngines.Add(engineId, CarSwapData.GetDefaultMeta(engineId));
			}
			activeSlot.Save();
			m_legacyCarSettings.SetEngineForCar(carId, engineId);
		}

		public EngineSwapData GetEngineSwapDataForCar(int carId, int engineId = -1, string customSetupId = "")
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (activeSlot.IsCustom())
			{
				return m_legacyCarSettings.GetEngineSwapDataForCar(carId, engineId, customSetupId);
			}
			if (engineId == -1)
			{
				engineId = GetEngineIdForCar(carId);
			}
			if (activeSlot.swapData.savedEngines.TryGetValue(engineId, out var value))
			{
				return value.Copy();
			}
			return CarSwapData.GetDefaultMeta(engineId);
		}

		public void SetEngineSwapDataForCar(int carId, EngineSwapData swap, string overrideKey = null)
		{
			if (swap != null)
			{
				PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
				if (!activeSlot.IsCustom())
				{
					activeSlot.swapData.currentId = swap.id;
					activeSlot.swapData.savedEngines[swap.id] = swap.Copy();
					activeSlot.Save();
				}
				m_legacyCarSettings.SetEngineSwapDataForCar(carId, swap, overrideKey);
			}
		}

		public void SaveEngineSwapDataForCar(int carId, EngineSwapData swap)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (!activeSlot.IsCustom())
			{
				activeSlot.swapData.savedEngines[swap.id] = swap.Copy();
				activeSlot.Save();
			}
			m_legacyCarSettings.SaveEngineSwapDataForCar(carId, swap);
		}

		public void ClearEnginesPerCarCustomSetupsCache(int carId, int engineId, string customSetupId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (!activeSlot.IsCustom())
			{
				activeSlot.swapData.savedEngines.Remove(engineId);
				activeSlot.Save();
			}
			m_legacyCarSettings.ClearEnginesPerCarCustomSetupsCache(carId, engineId, customSetupId);
		}

		public List<CustomProfile> GetCustomProfilesByCarId(int carId)
		{
			return m_legacyCarSettings.GetCustomProfilesByCarId(carId);
		}

		public (string setupId, bool haveInstalledCustom, int engineId) GetHaveCustomSetupsPerCar(int carId)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (activeSlot.IsCustom())
			{
				return (setupId: activeSlot.customSetupId, haveInstalledCustom: true, engineId: activeSlot.swapData.currentId);
			}
			return (setupId: string.Empty, haveInstalledCustom: false, engineId: 0);
		}

		public void SetHaveCustomSetupsPerCar(int carId, int engineId, string setupId, bool isHaveCustomSetup)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			activeSlot.customSetupId = (isHaveCustomSetup ? setupId : null);
			if (!isHaveCustomSetup)
			{
				activeSlot.swapData.currentId = engineId;
			}
			activeSlot.Save();
			m_legacyCarSettings.SetHaveCustomSetupsPerCar(carId, engineId, setupId, isHaveCustomSetup);
		}

		public void RemoveCustomSetup(int carId, string setupId)
		{
			m_legacyCarSettings.RemoveCustomSetup(carId, setupId);
		}

		public CustomProfile GetInstalledCustomProfile(int carId)
		{
			(string setupId, bool haveInstalledCustom, int engineId) haveCustomSetupsPerCar = GetHaveCustomSetupsPerCar(carId);
			var (customId, _, _) = haveCustomSetupsPerCar;
			if (!haveCustomSetupsPerCar.haveInstalledCustom)
			{
				return null;
			}
			return m_legacyCarSettings.FindCustomById(customId);
		}

		public CustomProfile FindCustomById(string customId)
		{
			return m_legacyCarSettings.FindCustomById(customId);
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
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			if (!activeSlot.profiles.ContainsKey(profileId))
			{
				activeSlot.profiles[profileId] = new ProfilesInfo.Profile(profileId);
				Save();
			}
			return activeSlot.profiles[profileId];
		}

		public void SetProfileInfo(int carId, int profileId, ProfilesInfo.Profile info)
		{
			info.forceDefault = info.controllers.Count == 0;
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			activeSlot.profiles[profileId] = info;
			activeSlot.Save();
			m_legacyCarSettings.SetProfileInfo(profileId, info);
		}

		public void SetCustomProfileInfo(out CustomSetupsSaveResult reason, out CustomProfile customProfile, string customName, int profileId, int carId, int engineId, ProfilesInfo.Profile info, string customSetupId = "")
		{
			m_legacyCarSettings.SetCustomProfileInfo(out reason, out customProfile, customName, profileId, carId, engineId, info, customSetupId);
		}

		public bool CheckHaveCustomSetup(out CustomProfile customProfile, int carId, string customName, int profileId, int engineId)
		{
			return m_legacyCarSettings.CheckHaveCustomSetup(out customProfile, carId, customName, profileId, engineId);
		}

		public CarSettingsModel.TandemWheelPressure GetTandemWheelPressure(int carId)
		{
			return GetActiveSlot(carId).wheelPressureForXDS;
		}

		public void SetTandemPressure(int carId, float fPressure, float rPressure)
		{
			PrefsCarSlotDataModel activeSlot = GetActiveSlot(carId);
			activeSlot.wheelPressureForXDS = new CarSettingsModel.TandemWheelPressure
			{
				frontPressure = fPressure,
				rearPressure = rPressure
			};
			activeSlot.Save();
			m_legacyCarSettings.SetTandemPressure(carId, fPressure, rPressure);
		}

		public CarCameraSettings GetCameraSettings(string id)
		{
			return m_legacyCarSettings.camerasSettings.GetValueOrDefault(id);
		}

		public void SetCameraSettings(string id, CarCameraSettings settings)
		{
			m_legacyCarSettings.camerasSettings[id] = settings;
			m_legacyCarSettings.Save();
		}

		public void DeserializeAndMergeBase(string codebase)
		{
			m_legacyCarSettings.DeserializeAndMergeBase(codebase);
		}

		public void SaveAndWriteToRemote()
		{
			m_legacyCarSettings.SaveAndWriteToRemote();
		}

		public void ReadFromRemote()
		{
			m_legacyCarSettings.ReadFromRemote();
		}

		public int GetAvailableSlotId(List<int> slotIds)
		{
			for (int i = 0; i < GetMaxSlotCount(); i++)
			{
				if (!slotIds.Contains(i))
				{
					return i;
				}
			}
			return -1;
		}

		private void UpdateExistedSlot(PrefsCarSlotDataModel slot)
		{
			if (slot != null)
			{
				MigrateSlotFromLegacyVersion(slot);
				slot.PullVersion();
			}
		}

		private void MigrateSlotFromLegacyVersion(PrefsCarSlotDataModel slot)
		{
			if (slot.IsUpdateRequired() && m_prefs.globals.carSlotVersion <= 0)
			{
				m_legacyCarSettings.Export(slot);
			}
		}

		private void MigrateCarModelForActiveSlot(int carId)
		{
			PrefsCarSlotDataModel prefsCarSlotDataModel = PrefsCarSlotDataModel.CreateEmptyModel();
			prefsCarSlotDataModel.carId = carId;
			prefsCarSlotDataModel.slotId = GetActiveSlotId(carId);
			m_legacyCarSettings.Export(prefsCarSlotDataModel);
			AddSlot(prefsCarSlotDataModel);
		}

		private bool IsCarModelCorrupted(int carId)
		{
			SlotsInfoHeader slotsInfo = GetSlotsInfo(carId);
			if (slotsInfo.activeSlotId == -1)
			{
				return true;
			}
			if (!slotsInfo.usedSlots.Contains(slotsInfo.activeSlotId))
			{
				return true;
			}
			if (slotsInfo.usedSlots.Count <= 0)
			{
				return true;
			}
			return false;
		}

		public void TryReleaseCarModelResources(int carId)
		{
			m_legacyCarSettings.TryReleaseCarModelResources(carId);
			m_carSlotModels.Remove(carId);
			FillSlotModelsForLazyLoad();
		}

		private void FillSlotModelsForLazyLoad()
		{
			foreach (var (num2, slotsInfoHeader2) in m_slotsInfo)
			{
				foreach (int usedSlot in slotsInfoHeader2.usedSlots)
				{
					if (!m_carSlotModels.ContainsKey(num2) || !m_carSlotModels[num2].ContainsKey(usedSlot))
					{
						AddSlot(PrefsCarSlotDataModel.CreateLazyLoadModel(num2, usedSlot));
					}
				}
			}
		}

		private void SaveAllSlotModels()
		{
			foreach (Dictionary<int, PrefsCarSlotDataModel> value in m_carSlotModels.Values)
			{
				foreach (PrefsCarSlotDataModel value2 in value.Values)
				{
					value2.Save();
				}
			}
		}

		private void ResetAllSlotModels()
		{
			int num = 0;
			foreach (Dictionary<int, PrefsCarSlotDataModel> value in m_carSlotModels.Values)
			{
				foreach (PrefsCarSlotDataModel value2 in value.Values)
				{
					value2.Reset();
					value2.Save();
					num++;
				}
			}
			num = Mathf.CeilToInt((float)num / 50f);
			for (int i = 0; i < num; i++)
			{
				BasePrefsModel.SaveAllQueries(inOneFrame: true);
			}
		}
	}
}
