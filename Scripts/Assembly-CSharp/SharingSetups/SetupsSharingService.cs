using System;
using System.Collections.Generic;
using CarProfile;
using CarX;
using DB.Meta;
using GameInput;
using PrefsModels;
using SimpleJSON;
using SyncMultiplayer;

namespace SharingSetups
{
	public class SetupsSharingService : BaseServerSharingService
	{
		private const string MAIN_ROUTE = "setup";

		private Dictionary<string, string> m_argsDict = new Dictionary<string, string>();

		protected override string m_publishEndpoint => "setup/setupPublic";

		protected override string m_fetchShortEndpoint => "setup/setupLobby";

		protected override string m_fetchFullEndpoint => "setup/setupList";

		protected override string m_fetchSingleEndpoint => "setup/setupSingle";

		protected override string m_markDownloadEndpoint => "setup/setupMarkDownload";

		protected override string m_makeRatingEndpoint => "setup/setupRating";

		protected override Dictionary<string, byte[]> PrepareFileParams()
		{
			return null;
		}

		protected override bool IsServerImplemented()
		{
			return true;
		}

		protected override bool PrepareUploadRequestParams<T>(out Dictionary<string, string> args, out string endPoint, object data)
		{
			m_argsDict.Clear();
			args = m_argsDict;
			endPoint = string.Empty;
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(SdrRestSetupPublic))
			{
				endPoint = m_publishEndpoint;
				return PrepareUploadRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestSetupLobby))
			{
				endPoint = m_fetchShortEndpoint;
				return PrepareShortRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestSetupList))
			{
				endPoint = m_fetchFullEndpoint;
				return PrepareFullRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestSetupSingle))
			{
				endPoint = m_fetchSingleEndpoint;
				return PrepareSingleRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestSetupMarkDownload))
			{
				endPoint = m_markDownloadEndpoint;
				return PrepareSingleRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestSetupRating))
			{
				endPoint = m_makeRatingEndpoint;
				return PrepareRatingRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrNickname))
			{
				endPoint = base.m_fetchNicknameEndPoint;
				return PrepareNicknameRequestParams(data);
			}
			return false;
		}

		private bool PrepareUploadRequestParams(object data)
		{
			var (customProfile, playerCarProfile, deviceType) = ((CustomProfile, PlayerCarProfile, DeviceType))data;
			if (customProfile == null || playerCarProfile == null)
			{
				return false;
			}
			int num = (int)ExtractInputDevice(deviceType);
			int carId = playerCarProfile.carId;
			int engineId = customProfile.engineId;
			string value = ((engineId > 0) ? LocalizationKeys.swaps_mode_swaps.Localized() : playerCarProfile.name);
			string value2 = (GetRearAdh() * 100f).ToString();
			m_argsDict.Add("customSetupId", customProfile.id);
			m_argsDict.Add("name", customProfile.name);
			m_argsDict.Add("profile", value);
			m_argsDict.Add("profileId", customProfile.profile.id.ToString());
			m_argsDict.Add("carId", carId.ToString());
			m_argsDict.Add("engineId", engineId.ToString());
			m_argsDict.Add("adhLimit", value2);
			m_argsDict.Add("crossMask", customProfile.mask.ToString());
			m_argsDict.Add("physicsVersion", Simulator.version.ToString());
			m_argsDict.Add("deviceType", num.ToString());
			m_argsDict.Add("data", JSON.Serialize(customProfile.profile));
			return true;
			float GetRearAdh()
			{
				if (!customProfile.profile.values.TryGetValue("rear_tire_type", out var value3))
				{
					List<CarProfileController> controllers = playerCarProfile.dynoInfo.controllers;
					for (int i = 0; i < controllers.Count; i++)
					{
						CarProfileController carProfileController = controllers[i];
						if (carProfileController != null && carProfileController.name == "rear_tire_type")
						{
							return carProfileController.changers[0].defaultValue;
						}
					}
				}
				return value3;
			}
		}

		private bool PrepareShortRequestParams(object data)
		{
			if (!(data is SharingSetupsFilter sharingSetupsFilter))
			{
				throw new NullReferenceException("NRE at SetupsSharingService > PrepareShortRequestParams >>> need define setupsFilter");
			}
			m_argsDict.Add("carId", sharingSetupsFilter.carId.ToString());
			m_argsDict.Add("profile", sharingSetupsFilter.profile ?? string.Empty);
			m_argsDict.Add("dataId", sharingSetupsFilter.dataId.ToString());
			m_argsDict.Add("deviceType", sharingSetupsFilter.deviceType.ToString());
			m_argsDict.Add("adhLimit", sharingSetupsFilter.adhLimit.ToString());
			m_argsDict.Add("rating", sharingSetupsFilter.rating.ToString());
			m_argsDict.Add("download", sharingSetupsFilter.downloadCount.ToString());
			m_argsDict.Add("crossMask", sharingSetupsFilter.crossmask.ToString());
			return true;
		}

		private bool PrepareFullRequestParams(object data)
		{
			if (data is int num)
			{
				m_argsDict.Add("carId", num.ToString());
			}
			return true;
		}

		private bool PrepareSingleRequestParams(object data)
		{
			string value = data as string;
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			m_argsDict.Add("code", value);
			return true;
		}

		private bool PrepareRatingRequestParams(object data)
		{
			var (value, num) = ((string, int))data;
			if (string.IsNullOrEmpty(value) || num < 0)
			{
				return false;
			}
			m_argsDict.Add("code", value);
			m_argsDict.Add("rating", num.ToString());
			return true;
		}

		private bool PrepareNicknameRequestParams(object data)
		{
			if (data is string value)
			{
				m_argsDict.Add("platformUserId", value);
				return true;
			}
			return false;
		}

		private NetPlayerInputSystem.PlayerInputDevice ExtractInputDevice(DeviceType deviceType)
		{
			switch (deviceType)
			{
			case DeviceType.Wheel:
			case DeviceType.Pedals:
				return NetPlayerInputSystem.PlayerInputDevice.Wheel;
			case DeviceType.Keyboard:
			case DeviceType.Mouse:
				return NetPlayerInputSystem.PlayerInputDevice.Keyboard;
			case DeviceType.Gamepad:
				return NetPlayerInputSystem.PlayerInputDevice.Gamepad;
			default:
				return NetPlayerInputSystem.PlayerInputDevice.Keyboard;
			}
		}
	}
}
