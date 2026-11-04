using System;
using GameOverlay;
using UnityEngine.UI;

namespace SharingTracks
{
	[RaycastBlocker(true)]
	[GameHotkeyBlocker(true)]
	public class UISharingTracksFilterContext : UISharingBaseFilterContext
	{
		private enum DownloadCountType
		{
			Any = 0,
			COUNT_500 = 500,
			COUNT_1_000 = 1000,
			COUNT_5_000 = 5000,
			COUNT_10_000 = 10000,
			COUNT_25_000 = 25000,
			COUNT_50_000 = 50000
		}

		private const int MAX_INPUT_LENGTH = 22;

		public Action onOpenInputDialog;

		public Action onClosedInputDialog;

		private SwitchListSettingsItem m_ratingSwitcher;

		private SwitchListSettingsItem m_downloadCountSwitcher;

		private HeaderListSettingsItem m_filterByTrack;

		private HeaderListSettingsItem m_filterByAuthor;

		private bool m_needSnap = true;

		private int m_lastSelected;

		protected override void OnDeactivate()
		{
			m_needSnap = true;
			base.OnDeactivate();
		}

		public override void InitFilter(SharingFilterController filterController)
		{
			m_sharingController = filterController;
			if (m_needSnap)
			{
				m_sharingFilterSnap = filterController.sharingFilter.MemberwiseClone() as SharingTracksFilter;
			}
			InitListItem(ref m_filterByAuthor, LocalizationKeys.creator.Localized(), 0, null, OnClickFilterByAuthor, GetInitVal(GetInitFilterValue<TracksSharingFilterType, string>(TracksSharingFilterType.AuthorName)));
			InitListItem(ref m_ratingSwitcher, LocalizationKeys.rating.Localized(), GetInitFilterValue<TracksSharingFilterType, int>(TracksSharingFilterType.Rating), OnSwitchRating, null, GetRatingStars(0), GetRatingStars(1), GetRatingStars(2), GetRatingStars(3), GetRatingStars(4), GetRatingStars(5));
			InitListItem(ref m_downloadCountSwitcher, LocalizationKeys.download_count.Localized(), GetDownloadCountStartIndex(), OnSwitchDownloadCount, null, GetDownloadCountValue(DownloadCountType.Any), GetDownloadCountValue(DownloadCountType.COUNT_500), GetDownloadCountValue(DownloadCountType.COUNT_1_000), GetDownloadCountValue(DownloadCountType.COUNT_5_000), GetDownloadCountValue(DownloadCountType.COUNT_10_000), GetDownloadCountValue(DownloadCountType.COUNT_25_000), GetDownloadCountValue(DownloadCountType.COUNT_50_000));
			base.InitFilter(filterController);
			if (!m_needSnap)
			{
				m_paramsList.selectedIndex = m_lastSelected;
			}
		}

		private string GetInitVal(string val)
		{
			if (string.IsNullOrEmpty(val))
			{
				val = LocalizationKeys.placeholder_input_text.Localized();
			}
			return val;
		}

		private void OnClickFilterByTrack()
		{
			Hide();
			UIMessageBox.ShowInputDialog(LocalizationKeys.input_track_name.Localized(), m_filterByTrack.GetCurrentValueTitle(), LocalizationKeys.input_track_name.Localized(), InputField.ContentType.Standard, OnNavigationCloseInputDialog, LocalizationKeys.Cancel.Localized(), OnReceiveTrackResult, LocalizationKeys.nav_hint_apply.Localized(), 22);
			void OnReceiveTrackResult(string result)
			{
				OnRecieveInputText(TracksSharingFilterType.TrackName, result);
			}
		}

		private void OnClickFilterByAuthor()
		{
			onOpenInputDialog.SafeCall();
			Hide();
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.ShowOverlayKeyboard(LocalizationKeys.input_publication_code.Localized(), string.Empty, 22);
			}
			else
			{
				ShowInputDialog();
			}
			void OnReceiveAuthorResult(string result)
			{
				m_lastSelected = 1;
				OnRecieveInputText(TracksSharingFilterType.AuthorName, result);
				onClosedInputDialog.SafeCall();
			}
			void ShowInputDialog()
			{
				UIMessageBox.ShowInputDialog(LocalizationKeys.input_author_name.Localized(), m_filterByAuthor.GetCurrentValueTitle(), LocalizationKeys.input_author_name.Localized(), InputField.ContentType.Standard, OnNavigationCloseInputDialog, LocalizationKeys.Cancel.Localized(), OnReceiveAuthorResult, LocalizationKeys.nav_hint_apply.Localized(), 22);
			}
		}

		private void OnNavigationCloseInputDialog()
		{
			NavigationClose();
			onClosedInputDialog.SafeCall();
		}

		private void OnRecieveInputText(TracksSharingFilterType tracksSharingFilterType, string result)
		{
			m_needSnap = false;
			if (result == null || result == LocalizationKeys.placeholder_input_text.Localized())
			{
				result = string.Empty;
			}
			m_sharingController.SetFilterValue(tracksSharingFilterType, result);
			base.owner.Push<UISharingTracksFilterContext>().InitFilter(m_sharingController);
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
			m_sharingController.SetFilterValue(TracksSharingFilterType.Rating, num);
			UpdateParamList();
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

		private int GetDownloadCountStartIndex()
		{
			int result = 0;
			int initFilterValue = GetInitFilterValue<TracksSharingFilterType, int>(TracksSharingFilterType.DownloadCount);
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

		private string GetDownloadCountValue(DownloadCountType downloadCountType)
		{
			return downloadCountType switch
			{
				DownloadCountType.COUNT_500 => "> " + 500, 
				DownloadCountType.COUNT_1_000 => "> " + 1000, 
				DownloadCountType.COUNT_5_000 => "> " + 5000, 
				DownloadCountType.COUNT_10_000 => "> " + 10000, 
				DownloadCountType.COUNT_25_000 => "> " + 25000, 
				DownloadCountType.COUNT_50_000 => "> " + 50000, 
				_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
			};
		}

		private void OnSwitchDownloadCount()
		{
			int.TryParse(m_downloadCountSwitcher.GetCurrentValueTitle().Replace("> ", string.Empty), out var result);
			m_sharingController.SetFilterValue(TracksSharingFilterType.DownloadCount, result);
			UpdateParamList();
		}
	}
}
