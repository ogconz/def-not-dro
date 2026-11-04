using System;
using GameOverlay;
using SyncMultiplayer;

namespace SharingSetups
{
	[RaycastBlocker(true)]
	[GameHotkeyBlocker(true)]
	public class UISharingSetupsFilterContext : UISharingBaseFilterContext
	{
		public enum ProfileType
		{
			Any = 0,
			Racing = 1,
			Drift = 2,
			Ultimate = 3,
			UltimatePro = 4,
			Swaps = 5
		}

		private enum DownloadCountType
		{
			Any = 0,
			COUNT_10 = 10,
			COUNT_50 = 50,
			COUNT_100 = 100,
			COUNT_200 = 200,
			COUNT_500 = 500
		}

		private SwitchListSettingsItem m_profileSwitcher;

		private SwitchListSettingsItem m_adhLimitSwitcher;

		private SwitchListSettingsItem m_ratingSwitcher;

		private SwitchListSettingsItem m_downloadCountSwitcher;

		private SwitchListSettingsItem m_deviceTypeSwitcher;

		private ToggleListSettingsItem m_crossPlayToggle;

		public override void InitFilter(SharingFilterController filterController)
		{
			m_sharingController = filterController;
			m_sharingFilterSnap = filterController.sharingFilter.MemberwiseClone() as SharingSetupsFilter;
			InitListItem(ref m_profileSwitcher, LocalizationKeys.player_profile_wnd.Localized(), GetProfileSwitcherStartIndex(), OnSwitchProfile, null, GetProfileValue(ProfileType.Any), GetProfileValue(ProfileType.Racing), GetProfileValue(ProfileType.Drift), GetProfileValue(ProfileType.Ultimate), GetProfileValue(ProfileType.UltimatePro), GetProfileValue(ProfileType.Swaps));
			InitListItem(ref m_adhLimitSwitcher, LocalizationKeys.CarProfileParameterCaption.FormatLocalized("rear_tire_type"), GetAdhSwitcherStartIndex(), OnSwitchAdhLimit, null, RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ALL), RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_90), RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_100), RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_110), RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_120));
			InitListItem(ref m_ratingSwitcher, LocalizationKeys.rating.Localized(), GetInitFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.Rating), OnSwitchRating, null, GetRatingStars(0), GetRatingStars(1), GetRatingStars(2), GetRatingStars(3), GetRatingStars(4), GetRatingStars(5));
			InitListItem(ref m_downloadCountSwitcher, LocalizationKeys.download_count.Localized(), GetDownloadCountSwitcherStartIndex(), OnSwitchDownloadCount, null, GetDownloadCountValue(DownloadCountType.Any), GetDownloadCountValue(DownloadCountType.COUNT_10), GetDownloadCountValue(DownloadCountType.COUNT_50), GetDownloadCountValue(DownloadCountType.COUNT_100), GetDownloadCountValue(DownloadCountType.COUNT_200), GetDownloadCountValue(DownloadCountType.COUNT_500));
			InitListItem(ref m_deviceTypeSwitcher, LocalizationKeys.input_settings_device_column_type.Localized(), GetInitFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.DeviceType) + 1, OnSwitchDeviceType, null, GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice.Unknown), GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice.Keyboard), GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice.Wheel), GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice.Gamepad));
			InitListItem(ref m_crossPlayToggle, LocalizationKeys.crossplatform_publications.Localized(), GetInitFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.CrossMask), OnToggleCrossPlay, null);
			base.InitFilter(filterController);
		}

		private int GetProfileSwitcherStartIndex()
		{
			string initFilterValue = GetInitFilterValue<SetupSharingFilterType, string>(SetupSharingFilterType.Profile);
			return (int)GetProfileType(initFilterValue);
		}

		private int GetAdhSwitcherStartIndex()
		{
			RoomData.AdhLimitFlags adhLimitFlags = RoomData.StringToAdhLimit(GetInitFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.AdhLimit).ToString());
			return Array.IndexOf(Enum.GetValues(typeof(RoomData.AdhLimitFlags)), adhLimitFlags);
		}

		private int GetDownloadCountSwitcherStartIndex()
		{
			DownloadCountType initFilterValue = (DownloadCountType)GetInitFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.DownloadCount);
			return Array.IndexOf(Enum.GetValues(typeof(DownloadCountType)), initFilterValue);
		}

		private string GetProfileValue(ProfileType profileType)
		{
			return profileType switch
			{
				ProfileType.Racing => "Racing", 
				ProfileType.Drift => "Drift", 
				ProfileType.Ultimate => "Ultimate", 
				ProfileType.UltimatePro => "UltimatePro", 
				ProfileType.Swaps => LocalizationKeys.swaps_mode_swaps.Localized(), 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private ProfileType GetProfileType(string profile)
		{
			switch (profile)
			{
			case "Racing":
				return ProfileType.Racing;
			case "Drift":
				return ProfileType.Drift;
			case "Ultimate":
				return ProfileType.Ultimate;
			case "UltimatePro":
				return ProfileType.UltimatePro;
			default:
				if (profile == LocalizationKeys.swaps_mode_swaps.Localized())
				{
					return ProfileType.Swaps;
				}
				return ProfileType.Any;
			}
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
				DownloadCountType.COUNT_50 => "> 50", 
				DownloadCountType.COUNT_100 => "> 100", 
				DownloadCountType.COUNT_200 => "> 200", 
				DownloadCountType.COUNT_500 => "> 500", 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private string GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice playerInputDevice)
		{
			return playerInputDevice switch
			{
				NetPlayerInputSystem.PlayerInputDevice.Keyboard => LocalizationKeys.settings_input_keyboard.Localized(), 
				NetPlayerInputSystem.PlayerInputDevice.Wheel => LocalizationKeys.bind_wheel.Localized(), 
				NetPlayerInputSystem.PlayerInputDevice.Gamepad => LocalizationKeys.settings_input_gamepad.Localized(), 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private void OnSwitchProfile()
		{
			string text = m_profileSwitcher.GetCurrentValueTitle();
			if (text == LocalizationKeys.network_game_params_adh_all.Localized())
			{
				text = string.Empty;
			}
			m_sharingController.SetFilterValue(SetupSharingFilterType.Profile, text);
			UpdateParamList();
		}

		private void OnSwitchAdhLimit()
		{
			int.TryParse(m_adhLimitSwitcher.GetCurrentValueTitle(), out var result);
			m_sharingController.SetFilterValue(SetupSharingFilterType.AdhLimit, result);
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
			m_sharingController.SetFilterValue(SetupSharingFilterType.Rating, num);
			UpdateParamList();
		}

		private void OnSwitchDownloadCount()
		{
			int.TryParse(m_downloadCountSwitcher.GetCurrentValueTitle().Replace("> ", string.Empty), out var result);
			m_sharingController.SetFilterValue(SetupSharingFilterType.DownloadCount, result);
			UpdateParamList();
		}

		private void OnSwitchDeviceType()
		{
			string currentValueTitle = m_deviceTypeSwitcher.GetCurrentValueTitle();
			NetPlayerInputSystem.PlayerInputDevice val = NetPlayerInputSystem.PlayerInputDevice.Unknown;
			if (currentValueTitle.Equals(LocalizationKeys.settings_input_keyboard.Localized()))
			{
				val = NetPlayerInputSystem.PlayerInputDevice.Keyboard;
			}
			else if (currentValueTitle.Equals(LocalizationKeys.bind_wheel.Localized()))
			{
				val = NetPlayerInputSystem.PlayerInputDevice.Wheel;
			}
			else if (currentValueTitle.Equals(LocalizationKeys.settings_input_gamepad.Localized()))
			{
				val = NetPlayerInputSystem.PlayerInputDevice.Gamepad;
			}
			m_sharingController.SetFilterValue(SetupSharingFilterType.DeviceType, (int)val);
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
					m_sharingController.SetFilterValue(SetupSharingFilterType.CrossMask, (int)Overlay.instance.GetPlatform());
					UpdateParamList();
				}
				else
				{
					int val = (int)((!m_crossPlayToggle.value) ? Overlay.instance.GetPlatform() : UserPlatform.Id.Unknown);
					m_sharingController.SetFilterValue(SetupSharingFilterType.CrossMask, val);
					UpdateParamList();
				}
			});
		}
	}
}
