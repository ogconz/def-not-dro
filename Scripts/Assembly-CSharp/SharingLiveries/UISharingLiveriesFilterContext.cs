using System;
using GameOverlay;
using UnityEngine;

namespace SharingLiveries
{
	[RaycastBlocker(true)]
	[GameHotkeyBlocker(true)]
	public class UISharingLiveriesFilterContext : UISharingBaseFilterContext
	{
		private enum DownloadCountType
		{
			Any = 0,
			COUNT_10 = 1,
			COUNT_100 = 2,
			COUNT_500 = 3,
			COUNT_1000 = 4
		}

		private enum LayersCountType
		{
			Any = 0,
			COUNT_0_100 = 1,
			COUNT_100_200 = 2
		}

		private SwitchListSettingsItem m_categorySwitcher;

		private SwitchListSettingsItem m_ratingSwitcher;

		private SwitchListSettingsItem m_downloadCountSwitcher;

		private SwitchListSettingsItem m_dlcSwitcher;

		private SwitchListSettingsItem m_layersCount;

		private ToggleListSettingsItem m_crossPlayToggle;

		public override void InitFilter(SharingFilterController filterController)
		{
			m_sharingController = filterController;
			m_sharingFilterSnap = filterController.sharingFilter.MemberwiseClone() as SharingLiveriesFilter;
			InitListItem(ref m_categorySwitcher, LocalizationKeys.categories.Localized(), GetCategorySwitcherStartIndex(), OnSwitchCategory, null, GetCategoryValue(SharingLiveryCategoryType.Any), GetCategoryValue(SharingLiveryCategoryType.Original), GetCategoryValue(SharingLiveryCategoryType.Replica), GetCategoryValue(SharingLiveryCategoryType.Itasha), GetCategoryValue(SharingLiveryCategoryType.Racing), GetCategoryValue(SharingLiveryCategoryType.Badges), GetCategoryValue(SharingLiveryCategoryType.Abstract), GetCategoryValue(SharingLiveryCategoryType.Colorful), GetCategoryValue(SharingLiveryCategoryType.Minimalistic), GetCategoryValue(SharingLiveryCategoryType.Artistic), GetCategoryValue(SharingLiveryCategoryType.USDM), GetCategoryValue(SharingLiveryCategoryType.EUDM), GetCategoryValue(SharingLiveryCategoryType.AUDM), GetCategoryValue(SharingLiveryCategoryType.ASDM), GetCategoryValue(SharingLiveryCategoryType.PRO), GetCategoryValue(SharingLiveryCategoryType.STREET));
			InitListItem(ref m_ratingSwitcher, LocalizationKeys.rating.Localized(), GetInitFilterValue<LiveriesSharingFilterType, int>(LiveriesSharingFilterType.Rating), OnSwitchRating, null, GetRatingStars(0), GetRatingStars(1), GetRatingStars(2), GetRatingStars(3), GetRatingStars(4), GetRatingStars(5));
			InitListItem(ref m_downloadCountSwitcher, LocalizationKeys.download_count.Localized(), GetDownloadCountStartIndex(), OnSwitchDownloadCount, null, GetDownloadCountValue(DownloadCountType.Any), GetDownloadCountValue(DownloadCountType.COUNT_10), GetDownloadCountValue(DownloadCountType.COUNT_100), GetDownloadCountValue(DownloadCountType.COUNT_500), GetDownloadCountValue(DownloadCountType.COUNT_1000));
			InitListItem(ref m_layersCount, LocalizationKeys.layers_count.Localized(), GetLayersCountSwitcherStartIndex(), OnSwitchLayersCount, null, GetLayersCountValue(LayersCountType.Any), GetLayersCountValue(LayersCountType.COUNT_0_100), GetLayersCountValue(LayersCountType.COUNT_100_200));
			InitListItem(ref m_crossPlayToggle, LocalizationKeys.crossplatform_publications.Localized(), GetInitFilterValue<LiveriesSharingFilterType, int>(LiveriesSharingFilterType.CrossMask), OnToggleCrossPlay, null);
			base.InitFilter(filterController);
		}

		private int GetCategorySwitcherStartIndex()
		{
			string initFilterValue = GetInitFilterValue<LiveriesSharingFilterType, string>(LiveriesSharingFilterType.Category);
			SharingLiveryCategoryType[] enumArray = Enum.GetValues(typeof(SharingLiveryCategoryType)) as SharingLiveryCategoryType[];
			SharingLiveryCategoryType type = GetCategoryType(initFilterValue);
			return Array.FindIndex(enumArray, 0, enumArray.Length, (SharingLiveryCategoryType el) => el.ToString() == type.ToString());
			SharingLiveryCategoryType GetCategoryType(string category)
			{
				SharingLiveryCategoryType[] array = enumArray;
				for (int i = 0; i < array.Length; i++)
				{
					SharingLiveryCategoryType result = array[i];
					if (result.ToString() == category)
					{
						return result;
					}
				}
				return SharingLiveryCategoryType.Any;
			}
		}

		private int GetDownloadCountStartIndex()
		{
			int result = 0;
			int initFilterValue = GetInitFilterValue<LiveriesSharingFilterType, int>(LiveriesSharingFilterType.DownloadCount);
			DownloadCountType[] array = Enum.GetValues(typeof(DownloadCountType)) as DownloadCountType[];
			foreach (DownloadCountType downloadCountType in array)
			{
				if (int.TryParse(GetDownloadCountValue(downloadCountType).Replace("> ", string.Empty), out var result2) && initFilterValue == result2)
				{
					result = (int)downloadCountType;
					break;
				}
			}
			return result;
		}

		private int GetLayersCountSwitcherStartIndex()
		{
			Vector2 initFilterValue = GetInitFilterValue<LiveriesSharingFilterType, Vector2>(LiveriesSharingFilterType.LayersCount);
			int num = 0;
			foreach (LayersCountType value in Enum.GetValues(typeof(LayersCountType)))
			{
				string layersCountValue = GetLayersCountValue(value);
				if (initFilterValue == GetLayersMinMaxByTitle(layersCountValue))
				{
					break;
				}
				num++;
			}
			return num;
		}

		private void OnSwitchCategory()
		{
			string text = m_categorySwitcher.GetCurrentValueTitle();
			if (text == LocalizationKeys.network_game_params_adh_all.Localized())
			{
				text = string.Empty;
			}
			m_sharingController.SetFilterValue(LiveriesSharingFilterType.Category, text);
			UpdateParamList();
		}

		private void OnSwitchRating()
		{
			char[] array = m_ratingSwitcher.GetCurrentValueTitle().ToCharArray();
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == '⭐')
				{
					num++;
				}
			}
			m_sharingController.SetFilterValue(LiveriesSharingFilterType.Rating, num);
			UpdateParamList();
		}

		private void OnSwitchDownloadCount()
		{
			int.TryParse(m_downloadCountSwitcher.GetCurrentValueTitle().Replace("> ", string.Empty), out var result);
			m_sharingController.SetFilterValue(LiveriesSharingFilterType.DownloadCount, result);
			UpdateParamList();
		}

		private void OnSwitchDLC()
		{
			string currentValueTitle = m_dlcSwitcher.GetCurrentValueTitle();
			SharingLiveryDLCFlags sharingLiveryDLCFlags = SharingLiveryDLCFlags.Any;
			if (currentValueTitle != LocalizationKeys.network_game_params_adh_all.Localized())
			{
				foreach (SharingLiveryDLCFlags value in Enum.GetValues(typeof(SharingLiveryDLCFlags)))
				{
					if (value != SharingLiveryDLCFlags.Any && Enum.TryParse<DLCCode>(value.ToString(), ignoreCase: true, out var result) && Overlay.instance.IsDLCInstalled(result))
					{
						sharingLiveryDLCFlags |= value;
					}
				}
			}
			m_sharingController.SetFilterValue(LiveriesSharingFilterType.DLC, (int)sharingLiveryDLCFlags);
			UpdateParamList();
		}

		private void OnSwitchLayersCount()
		{
			string currentValueTitle = m_layersCount.GetCurrentValueTitle();
			m_sharingController.SetFilterValue(LiveriesSharingFilterType.LayersCount, GetLayersMinMaxByTitle(currentValueTitle));
			UpdateParamList();
		}

		private void OnToggleCrossPlay()
		{
			Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool restrictedCrossMp)
			{
				if (restrictedCrossMp)
				{
					Overlay.instance.ShowCrossPlayRestrictionDialog();
					m_crossPlayToggle.value = false;
					m_sharingController.SetFilterValue(LiveriesSharingFilterType.CrossMask, (int)Overlay.instance.GetPlatform());
					UpdateParamList();
				}
				else
				{
					int val = (int)((!m_crossPlayToggle.value) ? Overlay.instance.GetPlatform() : UserPlatform.Id.Unknown);
					m_sharingController.SetFilterValue(LiveriesSharingFilterType.CrossMask, val);
					UpdateParamList();
				}
			});
		}

		private string GetCategoryValue(SharingLiveryCategoryType profileType)
		{
			if (profileType == SharingLiveryCategoryType.Any)
			{
				return LocalizationKeys.network_game_params_adh_all.Localized();
			}
			return profileType.ToString();
		}

		private string GetRatingStars(int rating)
		{
			string text = string.Empty;
			for (int i = 0; i < 5; i++)
			{
				text = ((i >= rating) ? (text + "✫") : (text + "⭐"));
			}
			if (!text.Contains('⭐'.ToString()))
			{
				return LocalizationKeys.network_game_params_adh_all.Localized();
			}
			return text;
		}

		private string GetDownloadCountValue(DownloadCountType downloadCountType)
		{
			return downloadCountType switch
			{
				DownloadCountType.COUNT_10 => "> 10", 
				DownloadCountType.COUNT_100 => "> 100", 
				DownloadCountType.COUNT_500 => "> 500", 
				DownloadCountType.COUNT_1000 => "> 1000", 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private string GetLayersCountValue(LayersCountType layersCountType)
		{
			return layersCountType switch
			{
				LayersCountType.COUNT_0_100 => "0-100", 
				LayersCountType.COUNT_100_200 => "100-200", 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private Vector2 GetLayersMinMaxByTitle(string title)
		{
			return GetLayersCountTypeFromTitle() switch
			{
				LayersCountType.COUNT_0_100 => new Vector2(0f, 100f), 
				LayersCountType.COUNT_100_200 => new Vector2(100f, 200f), 
				_ => Vector2.zero, 
			};
			LayersCountType GetLayersCountTypeFromTitle()
			{
				SharingLiveryDLCFlags[] array = Enum.GetValues(typeof(SharingLiveryDLCFlags)) as SharingLiveryDLCFlags[];
				for (int i = 0; i < array.Length; i++)
				{
					LayersCountType layersCountType = (LayersCountType)array[i];
					if (title == GetLayersCountValue(layersCountType))
					{
						return layersCountType;
					}
				}
				return LayersCountType.Any;
			}
		}
	}
}
