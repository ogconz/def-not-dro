using System;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
using CodeStage.AntiCheat.ObscuredTypes;
using SyncMultiplayer;
using UnityEngine;

public class RoomData
{
	[Flags]
	public enum RoomSettingFlags : byte
	{
		NONE = 0,
		DRIFT_ALLOWED = 1,
		TIME_ALLOWED = 2,
		VOTE_ALLOWED = 4,
		CLIPPING_DUEL_ALLOWED = 8,
		COLLISIONS_ACTIVE = 0x10,
		CHAMPIONSHIP_MODE = 0x20,
		EXTERNAL_TRACK = 0x40,
		ALL = 0xF
	}

	[Flags]
	public enum AdhLimitFlags
	{
		NONE = 0,
		ADH_90 = 1,
		ADH_100 = 2,
		ADH_110 = 4,
		ADH_120 = 8,
		ALL = 0xF
	}

	[Flags]
	public enum PowerDensityLimitFlag
	{
		NONE = 0,
		PD_0_2 = 1,
		PD_0_3 = 2,
		PD_0_4 = 4,
		PD_0_5 = 8,
		PD_0_6 = 0x10,
		PD_0_7 = 0x20,
		PD_0_8 = 0x40,
		PD_0_9 = 0x80,
		PD_1_0 = 0x100,
		ALL = 0x1FF
	}

	[Flags]
	public enum WheelDriveType
	{
		NONE = 0,
		WD_RWD = 1,
		WD_FOURWD = 2,
		ALL = 3
	}

	[Flags]
	public enum CollisionTypeFilter
	{
		None = 0,
		Classic = 1,
		Dynamic = 2,
		Disabled = 3
	}

	public enum CollisionType
	{
		Disabled = 0,
		Classic = 1,
		Dynamic = 2
	}

	[Flags]
	public enum RoomTypeFlags
	{
		NONE = 0,
		Drift = 1,
		TimeAttack = 2
	}

	[Serializable]
	public class AdhIconPair
	{
		public AdhLimitFlags limit;

		public Sprite sprite;
	}

	[Serializable]
	public class RaceTypePair
	{
		public RoomSettingFlags raceType;

		public Sprite sprite;
	}

	[Serializable]
	public class PowerDensityPair
	{
		public PowerDensityLimitFlag limit;

		public Sprite sprite;
	}

	[Serializable]
	public class WheelDriveTypePair
	{
		public WheelDriveType type;

		public Sprite sprite;
	}

	[Serializable]
	public class CollisionTypePair
	{
		public CollisionType type;

		public Sprite sprite;
	}

	public bool isLocked;

	public string roomCaption;

	public string roomName;

	public string countText;

	public int flagsCount;

	public int userCount;

	public string trackName;

	public float timeLeft;

	public SmartfoxLobby.RoomLobbyInfo roomLobbyInfo;

	public string notFilteredCaption => roomOptions.NotFilteredCaption;

	public RoomSettingFlags flags => roomOptions.NewRoomSettings;

	public bool nightMode => roomOptions.RoomNightMode;

	public int adhLimit => roomOptions.RoomAdhLimit;

	public int pdensityLimit => roomOptions.RoomPowerDensityLimit;

	public int collisionType => roomOptions.CollisionType;

	public int wdlimit => roomOptions.RoomWheelDriveType;

	public ObscuredString roomId => roomLobbyInfo.roomId;

	public SmartfoxRoomOptions roomOptions => roomLobbyInfo.options;

	public ObscuredString name => roomOptions.Name;

	public bool isExternalMap => roomOptions.ExternalTrackId != 0;

	public byte ugcServiceId => roomOptions.ugcServiceId;

	public bool isModedRoom => !string.IsNullOrEmpty(roomOptions.ModName);

	public event Action<string> onFilterRoomCaption;

	public RoomData(SmartfoxLobby.RoomLobbyInfo roomLobbyInfo, string trackName)
	{
		this.roomLobbyInfo = roomLobbyInfo;
		Link(trackName);
	}

	public void FilterRoomCaption()
	{
		if (roomOptions != null)
		{
			roomOptions.FilterCaption(delegate
			{
				InitRoomCaption();
				this.onFilterRoomCaption.Call(roomCaption);
			});
		}
	}

	private void InitRoomCaption()
	{
		if (roomOptions != null && roomCaption != roomOptions.Caption)
		{
			roomCaption = roomOptions.Caption;
			if (isModedRoom)
			{
				string text = ChatTools.WrapColor("(" + roomOptions.ModName + ")", "FF0000");
				roomCaption = roomCaption + " " + text;
			}
			roomCaption = UIUtils.ConvertNGUIColorToRich(roomCaption);
		}
	}

	public void UpdateDynamicProps()
	{
		if (roomOptions != null)
		{
			bool voteMapLocked = roomOptions.VoteMapLocked;
			isLocked = (!string.IsNullOrEmpty(roomOptions.Name) && roomOptions.Name.ToString()[0] == '@') || voteMapLocked;
			InitRoomCaption();
			int num = Mathf.Min(roomOptions.PlayerCount, roomOptions.MaxPlayers);
			bool num2 = userCount != num;
			userCount = num;
			if (num2)
			{
				countText = userCount + "/" + roomOptions.MaxPlayers;
			}
		}
		else
		{
			countText = string.Empty;
			roomCaption = string.Empty;
			isLocked = false;
		}
		flagsCount = BigInteger.BitCnt((int)flags);
	}

	public void UpdateTrackName(string trackTitle)
	{
		trackName = (isExternalMap ? trackTitle : LocalizationService.Localize(trackTitle));
	}

	public bool Update()
	{
		timeLeft -= Time.deltaTime;
		if (roomOptions != null && userCount != roomOptions.PlayerCount)
		{
			userCount = roomOptions.PlayerCount;
			countText = roomOptions.PlayerCount + "/" + roomOptions.MaxPlayers;
			return true;
		}
		return false;
	}

	private void Link(string trackTitle)
	{
		this.onFilterRoomCaption = null;
		roomName = trackTitle;
		UpdateTrackName(trackTitle);
		UpdateDynamicProps();
	}

	public static string AdhLimitToString(AdhLimitFlags limit)
	{
		switch (limit)
		{
		case AdhLimitFlags.NONE:
		case AdhLimitFlags.ALL:
			return LocalizationKeys.network_game_params_adh_all.Localized();
		case AdhLimitFlags.ADH_90:
			return "90";
		case AdhLimitFlags.ADH_100:
			return "100";
		case AdhLimitFlags.ADH_110:
			return "110";
		case AdhLimitFlags.ADH_120:
			return "120";
		default:
			Debug.LogError($"Unsupported AdhLimitFlag: '{limit}'");
			return LocalizationKeys.network_game_params_adh_all.Localized();
		}
	}

	public static string PowerDensityLimitToString(PowerDensityLimitFlag limit)
	{
		return limit switch
		{
			PowerDensityLimitFlag.NONE => LocalizationKeys.network_game_params_adh_all.Localized(), 
			PowerDensityLimitFlag.ALL => LocalizationKeys.network_game_params_adh_all.Localized(), 
			PowerDensityLimitFlag.PD_0_2 => "< 0.2", 
			PowerDensityLimitFlag.PD_0_3 => "< 0.3", 
			PowerDensityLimitFlag.PD_0_4 => "< 0.4", 
			PowerDensityLimitFlag.PD_0_5 => "< 0.5", 
			PowerDensityLimitFlag.PD_0_6 => "< 0.6", 
			PowerDensityLimitFlag.PD_0_7 => "< 0.7", 
			PowerDensityLimitFlag.PD_0_8 => "< 0.8", 
			PowerDensityLimitFlag.PD_0_9 => "< 0.9", 
			PowerDensityLimitFlag.PD_1_0 => "< 1.0", 
			_ => SetDefault(), 
		};
		string SetDefault()
		{
			Debug.LogError($"Unsupported PowerDensity: '{limit}'");
			return LocalizationKeys.network_game_params_adh_all.Localized();
		}
	}

	public static string RoomTypeToString(RoomTypeFlags type)
	{
		return type switch
		{
			RoomTypeFlags.Drift => LocalizationKeys.network_game_params_drift_mode.Localized(), 
			RoomTypeFlags.TimeAttack => LocalizationKeys.network_game_params_timeattack_mode.Localized(), 
			_ => LocalizationKeys.network_game_params_adh_all.Localized(), 
		};
	}

	public static AdhLimitFlags StringToAdhLimit(string limit)
	{
		return limit switch
		{
			"90" => AdhLimitFlags.ADH_90, 
			"100" => AdhLimitFlags.ADH_100, 
			"110" => AdhLimitFlags.ADH_110, 
			"120" => AdhLimitFlags.ADH_120, 
			_ => AdhLimitFlags.ALL, 
		};
	}

	public static WheelDriveType StringToWDLimit(string limit)
	{
		return limit switch
		{
			"RWD" => WheelDriveType.WD_RWD, 
			"AWD" => WheelDriveType.WD_FOURWD, 
			"4WD" => WheelDriveType.WD_FOURWD, 
			_ => WheelDriveType.ALL, 
		};
	}

	public static PowerDensityLimitFlag StringToPDLimit(string limit)
	{
		return limit switch
		{
			"0.2" => PowerDensityLimitFlag.PD_0_2, 
			"0.3" => PowerDensityLimitFlag.PD_0_3, 
			"0.4" => PowerDensityLimitFlag.PD_0_4, 
			"0.5" => PowerDensityLimitFlag.PD_0_5, 
			"0.6" => PowerDensityLimitFlag.PD_0_6, 
			"0.7" => PowerDensityLimitFlag.PD_0_7, 
			"0.8" => PowerDensityLimitFlag.PD_0_8, 
			"0.9" => PowerDensityLimitFlag.PD_0_9, 
			"1.0" => PowerDensityLimitFlag.PD_1_0, 
			_ => PowerDensityLimitFlag.ALL, 
		};
	}

	public static float AdhLimitAsFloat(AdhLimitFlags limit)
	{
		return limit switch
		{
			AdhLimitFlags.ADH_90 => 0.9f, 
			AdhLimitFlags.ADH_100 => 1f, 
			AdhLimitFlags.ADH_110 => 1.1f, 
			AdhLimitFlags.ADH_120 => 1.2f, 
			_ => 1.2f, 
		};
	}

	public static float PowerDensityAsFloat(PowerDensityLimitFlag limit)
	{
		return limit switch
		{
			PowerDensityLimitFlag.PD_0_2 => 0.2f, 
			PowerDensityLimitFlag.PD_0_3 => 0.3f, 
			PowerDensityLimitFlag.PD_0_4 => 0.4f, 
			PowerDensityLimitFlag.PD_0_5 => 0.5f, 
			PowerDensityLimitFlag.PD_0_6 => 0.6f, 
			PowerDensityLimitFlag.PD_0_7 => 0.7f, 
			PowerDensityLimitFlag.PD_0_8 => 0.8f, 
			PowerDensityLimitFlag.PD_0_9 => 0.9f, 
			PowerDensityLimitFlag.PD_1_0 => 1f, 
			_ => 0f, 
		};
	}

	public static CarVisualManager.TyreProtectorGripType AdhLimitToProtectorType(AdhLimitFlags limit)
	{
		return limit switch
		{
			AdhLimitFlags.ADH_90 => CarVisualManager.TyreProtectorGripType.Worn, 
			AdhLimitFlags.ADH_100 => CarVisualManager.TyreProtectorGripType.LowGrip, 
			AdhLimitFlags.ADH_110 => CarVisualManager.TyreProtectorGripType.NormalGrip, 
			AdhLimitFlags.ADH_120 => CarVisualManager.TyreProtectorGripType.HighestGrip, 
			_ => CarVisualManager.TyreProtectorGripType.LowGrip, 
		};
	}
}
