using System;
using GameOverlay;
using PrefsModels;
using SimpleJSON;

namespace SharingSetups
{
	public class SetupSharingListElementData
	{
		public string customSetupName { get; private set; }

		public string setup { get; private set; }

		public string customSetupId { get; private set; }

		public string code { get; private set; }

		public int engineId { get; private set; }

		public int profileId { get; private set; }

		public string publisherPlayerId { get; private set; }

		public string publisherNickname { get; private set; }

		public ProfilesInfo.Profile profileData { get; private set; }

		public DateTime createDate { get; private set; }

		public int downloadCount { get; private set; }

		public float rating { get; private set; }

		public CustomProfile customProfile { get; private set; }

		public bool isLocalCustomSetup { get; private set; }

		public bool isOfficial { get; private set; }

		public int deviceType { get; private set; }

		public SetupSharingListElementData(SdrSetupShort item)
		{
			if (item != null)
			{
				customSetupName = item.name;
				engineId = item.engineId;
				profileId = item.profileId;
				setup = item.profile;
				code = item.code;
				customSetupId = item.customSetupId;
				publisherPlayerId = item.createPlayerId;
				publisherNickname = item.createPlayerName;
				createDate = item.createDate;
				downloadCount = item.download;
				deviceType = item.deviceType;
				rating = (float)item.rating / 10f;
				isOfficial = item.official;
			}
		}

		public SetupSharingListElementData(SdrSetupFull item)
		{
			if (item != null)
			{
				customSetupName = item.name;
				engineId = item.engineId;
				profileId = item.profileId;
				setup = item.profile;
				isOfficial = item.official;
				code = item.code;
				customSetupId = item.customSetupId;
				publisherPlayerId = item.createPlayerId;
				publisherNickname = Overlay.instance.player.nickName;
				createDate = item.createDate;
				downloadCount = item.download;
				rating = (float)item.rating / 10f;
				deviceType = item.deviceType;
				SetProfileData(item);
			}
		}

		public SetupSharingListElementData(CustomProfile customProfile, string profileName)
		{
			isLocalCustomSetup = true;
			isOfficial = customProfile.isOfficial;
			code = customProfile.code;
			this.customProfile = customProfile;
			customSetupName = customProfile.name;
			customSetupId = customProfile.id;
			engineId = customProfile.engineId;
			profileId = customProfile.profile.id;
			setup = profileName;
			profileData = customProfile.profile;
		}

		public void SetNickname(string name)
		{
			publisherNickname = name;
		}

		public void SetProfileData(SdrSetupFull item)
		{
			profileData = JSON.Parse<ProfilesInfo.Profile>(item.data);
		}

		public void UpdateLocalCustomSetupStatus(bool localCustom)
		{
			isLocalCustomSetup = localCustom;
		}
	}
}
