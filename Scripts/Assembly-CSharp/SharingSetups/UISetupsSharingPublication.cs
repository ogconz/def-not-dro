using Binding;
using Binding.Base;
using GameOverlay;
using SyncMultiplayer;

namespace SharingSetups
{
	public class UISetupsSharingPublication : BaseBindingBehaviourTarget
	{
		private StringProperty m_setupName = new StringProperty();

		private StringProperty m_publicationCode = new StringProperty();

		private StringProperty m_creatorNickname = new StringProperty();

		private BoolProperty m_showPlatformIcon = new BoolProperty();

		private BoolProperty m_myCustom = new BoolProperty();

		private IntProperty m_platform = new IntProperty();

		private IntProperty m_playerInputType = new IntProperty();

		private StringProperty m_uploadDate = new StringProperty();

		private StringProperty m_downloadCount = new StringProperty();

		private StringProperty m_deviceType = new StringProperty();

		private FloatProperty m_rating = new FloatProperty();

		public void SetData(SetupSharingListElement item, bool isFilterCrossplatformEnabled)
		{
			if (item == null)
			{
				return;
			}
			m_setupName.value = item.elementName;
			m_publicationCode.value = item.code.RepeatSpacePaste(" ", 3);
			PlayerId playerId = PlayerId.Parse(item.publisherPlayerId);
			string publisherNickname = item.publisherNickname;
			UserPlatform.Id platform = playerId.platform;
			if (!string.IsNullOrEmpty(publisherNickname))
			{
				Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Nickname, platform, publisherNickname, delegate(string filteredNickname)
				{
					m_creatorNickname.value = filteredNickname;
				});
			}
			m_showPlatformIcon.value = isFilterCrossplatformEnabled && UserPlatform.IsNeedShowLocalPlatformIcon(platform);
			m_myCustom.value = playerId.Equals(Overlay.instance.player.id);
			platform = ((platform == UserPlatform.Id.MyGames) ? UserPlatform.Id.Steam : platform);
			m_platform.value = (int)platform;
			m_uploadDate.value = item.createDate.ToLocalTime().ToString();
			m_downloadCount.value = item.downloadCount.ToString();
			m_playerInputType.value = item.deviceType;
			m_deviceType.value = GetDeviceInputValue((NetPlayerInputSystem.PlayerInputDevice)item.deviceType);
			m_rating.value = item.rating;
		}

		public void ResetNames()
		{
			StringProperty setupName = m_setupName;
			string value = (m_creatorNickname.value = string.Empty);
			setupName.value = value;
			m_platform.value = -1;
		}

		private string GetDeviceInputValue(NetPlayerInputSystem.PlayerInputDevice playerInputDevice)
		{
			return playerInputDevice switch
			{
				NetPlayerInputSystem.PlayerInputDevice.Keyboard => LocalizationKeys.settings_input_keyboard.Localized(), 
				NetPlayerInputSystem.PlayerInputDevice.Wheel => LocalizationKeys.bind_wheel.Localized(), 
				NetPlayerInputSystem.PlayerInputDevice.Gamepad => LocalizationKeys.settings_input_gamepad.Localized(), 
				_ => LocalizationKeys.Unknown.Localized(), 
			};
		}
	}
}
