using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class RoomUILine : BaseBindingBehaviourTarget
{
	private int m_currentMask;

	public RectTransform rectTransform;

	private RoomData m_roomData;

	public Property<bool> selected = new BoolProperty(false);

	public Property<bool> isLocked = new BoolProperty();

	public Property<bool> isEmpty = new BoolProperty();

	public Property<string> roomCaption = new StringProperty();

	public Property<string> roomName = new StringProperty();

	public Property<string> countText = new StringProperty();

	public Property<string> trackName = new StringProperty();

	public Property<int> flagsCount = new IntProperty();

	public Property<int> userCount = new IntProperty();

	public Property<bool> nightModeAllowed = new BoolProperty();

	public Property<int> adhesionLimitIndex = new IntProperty();

	public Property<int> pdensityLimitIndex = new IntProperty();

	public Property<int> wheelDriveLimitIndex = new IntProperty();

	public Property<int> collisionTypeIndex = new IntProperty();

	public Property<bool> adhesionUnlimited = new BoolProperty();

	public Property<bool> pdensityUnlimited = new BoolProperty();

	public Property<bool> wdriveUnlimited = new BoolProperty();

	public Property<bool> driftAllowed = new BoolProperty();

	public Property<bool> raceTypeActive = new BoolProperty();

	public Property<bool> clippingDuelAllowed = new BoolProperty();

	public Property<bool> voteAllowed = new BoolProperty();

	public Property<bool> collisionsActive = new BoolProperty();

	public Property<bool> assistAllowed = new BoolProperty();

	public Property<bool> tractionControlAllowed = new BoolProperty();

	public IntProperty ugcTrackServiceId = new IntProperty();

	public BoolProperty showCrossPlatform = new BoolProperty(startValue: true);

	public BoolProperty showSteam = new BoolProperty();

	public BoolProperty showPS4 = new BoolProperty();

	public BoolProperty showXbox = new BoolProperty();

	public BoolProperty showSwitch = new BoolProperty();

	public IntProperty showGenericConsole = new IntProperty(0);

	public BoolProperty friendInRoom = new BoolProperty();

	public UINetworkRoomsContext context;

	private int m_lastFreendUpdateHash;

	public RoomData roomData => m_roomData;

	public void OnClick()
	{
		context.OnClickRoomLine(this);
	}

	private void RefreshRoomCaption(string caption)
	{
		roomCaption.value = (NetworkController.IsRoomCreatorBlocked(m_roomData.roomOptions.RoomCreatorId) ? LocalizationKeys.network_game_servername_default.Localized() : caption);
	}

	public void SetItem(RoomData roomData)
	{
		if (m_roomData != null)
		{
			m_roomData.onFilterRoomCaption -= RefreshRoomCaption;
		}
		m_roomData = roomData;
		isEmpty.value = roomData == null;
		if (!isEmpty.value)
		{
			RefreshRoomCaption(roomData.roomCaption);
			roomData.onFilterRoomCaption -= RefreshRoomCaption;
			roomData.onFilterRoomCaption += RefreshRoomCaption;
			roomData.FilterRoomCaption();
			isLocked.value = roomData.isLocked;
			roomName.value = roomData.roomName;
			trackName.value = roomData.trackName;
			countText.value = roomData.countText;
			flagsCount.value = roomData.flagsCount;
			userCount.value = roomData.userCount;
			showCrossPlatform.value = NetworkController.InstanceGame.IsCrossPlatformGloballyEnabled();
			AddPlatformIcons(roomData.roomOptions);
			raceTypeActive.value = true;
			nightModeAllowed.value = roomData.nightMode;
			adhesionLimitIndex.value = GetAdhLimitIndex((RoomData.AdhLimitFlags)roomData.adhLimit);
			wheelDriveLimitIndex.value = GetWDLimitType((RoomData.WheelDriveType)roomData.wdlimit);
			pdensityLimitIndex.value = roomData.pdensityLimit;
			adhesionUnlimited.value = adhesionLimitIndex.value != 0;
			pdensityUnlimited.value = pdensityLimitIndex.value != 0;
			wdriveUnlimited.value = wheelDriveLimitIndex.value != 0;
			ugcTrackServiceId.value = roomData.ugcServiceId;
			driftAllowed.value = (int)(roomData.flags & RoomData.RoomSettingFlags.DRIFT_ALLOWED) > 0;
			clippingDuelAllowed.value = (int)(roomData.flags & RoomData.RoomSettingFlags.CLIPPING_DUEL_ALLOWED) > 0;
			voteAllowed.value = (int)(roomData.flags & RoomData.RoomSettingFlags.VOTE_ALLOWED) > 0;
			assistAllowed.value = roomData.roomOptions.AssistVersion < 1;
			tractionControlAllowed.value = roomData.roomOptions.RoomTractionControlMode < 1;
			collisionsActive.value = roomData.roomOptions.CollisionType > 0;
			collisionTypeIndex.value = roomData.roomOptions.CollisionType;
			friendInRoom.value = IsFriendInRoom(roomData);
		}
	}

	private static int GetAdhLimitIndex(RoomData.AdhLimitFlags limit)
	{
		switch (limit)
		{
		case RoomData.AdhLimitFlags.NONE:
		case RoomData.AdhLimitFlags.ALL:
			return 0;
		case RoomData.AdhLimitFlags.ADH_90:
			return 1;
		case RoomData.AdhLimitFlags.ADH_100:
			return 2;
		case RoomData.AdhLimitFlags.ADH_110:
			return 3;
		case RoomData.AdhLimitFlags.ADH_120:
			return 4;
		default:
			Debug.LogError($"Unsupported AdhLimitFlag flag, unable to get sprite index for: '{limit}'");
			return 0;
		}
	}

	public static int GetWDLimitType(RoomData.WheelDriveType limit)
	{
		switch (limit)
		{
		case RoomData.WheelDriveType.NONE:
		case RoomData.WheelDriveType.ALL:
			return 0;
		case RoomData.WheelDriveType.WD_RWD:
			return 1;
		case RoomData.WheelDriveType.WD_FOURWD:
			return 2;
		default:
			Debug.LogError($"Unsupported WheelDrive flag, unable to get sprite index for: '{limit}'");
			return 0;
		}
	}

	private void AddPlatformIcons(SmartfoxRoomOptions roomInfo)
	{
		_ = m_currentMask;
		m_currentMask = roomInfo.CurrentCrossPlayMask;
		ResetLayout();
		IEnumerable<UserPlatform.Id> platformsFromMask = UserPlatform.GetPlatformsFromMask(roomInfo.CurrentCrossPlayMask & NetworkController.InstanceGame.GetCurrentCrossPlayMask());
		showGenericConsole.value = 0;
		foreach (UserPlatform.Id item in platformsFromMask)
		{
			switch (item)
			{
			case UserPlatform.Id.Steam:
			case UserPlatform.Id.MyGames:
			case UserPlatform.Id.EpicGames:
				showSteam.value = true;
				break;
			case UserPlatform.Id.PS4:
				showPS4.value = true;
				break;
			case UserPlatform.Id.Xbox:
				showXbox.value = true;
				break;
			case UserPlatform.Id.Switch:
				showSwitch.value = true;
				break;
			}
		}
	}

	private void ResetLayout()
	{
		showSteam.value = false;
		showPS4.value = false;
		showXbox.value = false;
		showSwitch.value = false;
	}

	private bool IsFriendInRoom(RoomData item)
	{
		string roomPlayersList = item.roomOptions.RoomPlayersList;
		if (string.IsNullOrEmpty(roomPlayersList))
		{
			return false;
		}
		List<IPlayer> friends = Overlay.instance.friends;
		int num = roomPlayersList.GetHashCode() ^ friends.Count;
		if (m_lastFreendUpdateHash == num)
		{
			return friendInRoom.value;
		}
		m_lastFreendUpdateHash = num;
		foreach (IPlayer item2 in friends)
		{
			if (roomPlayersList.Contains(item2.id.ToString()))
			{
				return true;
			}
		}
		return false;
	}

	public void SetFocus(bool flag)
	{
		selected.value = flag;
	}

	public void UpdateFrom(RoomData roomData)
	{
		base.gameObject.SetActive(value: true);
		SetItem(roomData);
		SetFocus(flag: false);
	}
}
