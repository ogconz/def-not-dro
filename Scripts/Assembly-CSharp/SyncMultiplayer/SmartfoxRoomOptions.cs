using System;
using System.Collections.Generic;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using GameOverlay;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using UnityEngine;

namespace SyncMultiplayer
{
	public class SmartfoxRoomOptions
	{
		public class NullRoomVariable : RoomVariable, Variable
		{
			public bool IsPrivate { get; set; }

			public bool IsPersistent { get; set; }

			public string Name { get; set; }

			public VariableType Type { get; set; }

			public object Value { get; set; }

			public bool GetBoolValue()
			{
				return false;
			}

			public double GetDoubleValue()
			{
				return 0.0;
			}

			public int GetIntValue()
			{
				return -1;
			}

			public ISFSArray GetSFSArrayValue()
			{
				return null;
			}

			public ISFSObject GetSFSObjectValue()
			{
				return null;
			}

			public string GetStringValue()
			{
				return "";
			}

			public bool IsNull()
			{
				return true;
			}

			public ISFSArray ToSFSArray()
			{
				return null;
			}
		}

		public const string NAME_KEY = "n";

		public const string ROOM_CAPTION = "c";

		public const string ROOM_VERSION = "k";

		public const string CROSS_PLAY_MASK = "x";

		public const string ROOM_MODNAME = "j";

		public const string MAX_USERS_KEY = "m";

		public const string TRACK_NAME_KEY = "tn";

		public const string TRACK_ID_KEY = "2";

		public const string EXTERNAL_TRACK_ID_KEY = "et";

		public const string SPAWN_POINT_INDEX_KEY = "sp";

		public const string SPAWNS_COUNT_KEY = "spawns";

		public const string ASSIST_VERSION_KEY = "as";

		public const string NEW_ROOM_SETTINGS = "s2";

		public const string OLD_ROOM_SETTINGS = "s";

		public const string VOTEMAP_LOCKROOM = "l";

		public const string ROOM_PLAYERS_LIST = "o";

		public const string XBOX_ROOM_GUID = "g";

		public const string ROOM_CREATOR_ID = "3";

		public const string ROOM_NIGHTMODE = "nm";

		public const string ROOM_LIGHT_MODE = "lm";

		public const string BANNED_USERS_LIST = "bu";

		public const string ROOM_ADHESION_LIMIT = "al";

		public const string CHAMP_MODE = "ch";

		public const string UGCServiceIdKey = "usid";

		public const string PowerDensityKey = "pd";

		public const string WheelDriveKey = "wd";

		public const string TractionControlKey = "tc";

		public const string CollisionTypeKey = "ct";

		private string m_Version;

		private int m_crossPlayMask;

		private string m_ModName;

		private int m_PlayerCount;

		private ObscuredString m_Name;

		private string m_Caption;

		private string m_filteredCaption;

		private string m_roomPlayersList;

		private int m_MaxPlayers;

		private string m_bannedUsersList = string.Empty;

		private string m_TrackName;

		private int m_TrackID;

		private ulong m_externalTrackId;

		private byte m_ugcServiceId;

		private int m_spawnPointIndex;

		private int m_assistVersion;

		private int m_tractionControlMode;

		private RoomData.RoomSettingFlags m_newRoomSettings;

		private RoomData.RoomSettingFlags m_OldRoomSettings;

		private bool m_VoteMapLocked;

		private HashSet<string> m_changedProps = new HashSet<string>();

		private static RoomVariable m_zeroSettings = new NullRoomVariable();

		private string m_xboxRoomGuid;

		private string m_roomCreatorId;

		private bool m_roomNightMode;

		private string m_roomLightPreset;

		private int m_roomAdhLimit;

		private int m_powerDensityLimit;

		private int m_wheelDriveType;

		private int m_collisionsType;

		private bool m_champModeRoom;

		private SFSObject m_settingsSFSObject = new SFSObject();

		public Room Room { get; private set; }

		public string Version
		{
			get
			{
				return m_Version;
			}
			set
			{
				m_Version = value;
			}
		}

		private int CrossPlayMask
		{
			get
			{
				return m_crossPlayMask;
			}
			set
			{
				if (m_crossPlayMask != value)
				{
					m_changedProps.Add("x");
					m_crossPlayMask = value;
				}
			}
		}

		public int InitialCrossPlayMask
		{
			get
			{
				return m_crossPlayMask >> 16;
			}
			set
			{
				value &= UserPlatform.AllMask;
				CrossPlayMask = value | (value << 16);
			}
		}

		public int CurrentCrossPlayMask
		{
			get
			{
				return m_crossPlayMask & 0xFFFF;
			}
			set
			{
				value &= UserPlatform.AllMask;
				CrossPlayMask = (m_crossPlayMask & 0x7FFF0000) | value;
			}
		}

		public string ModName
		{
			get
			{
				if (!string.IsNullOrEmpty(m_ModName))
				{
					return m_ModName;
				}
				return "";
			}
			set
			{
				m_ModName = value;
			}
		}

		public int PlayerCount => Mathf.Max(1, (Room == null) ? m_PlayerCount : Room.UserCount);

		public ObscuredString Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
				m_changedProps.Add("n");
			}
		}

		public string NotFilteredCaption => m_Caption;

		public string Caption
		{
			get
			{
				return m_filteredCaption;
			}
			set
			{
				m_Caption = value;
				InitFilterCaption();
				m_changedProps.Add("c");
			}
		}

		public string RoomPlayersList
		{
			get
			{
				return m_roomPlayersList;
			}
			set
			{
				m_roomPlayersList = value;
				m_changedProps.Add("o");
			}
		}

		public string BannedUsersList
		{
			get
			{
				return m_bannedUsersList;
			}
			set
			{
				m_bannedUsersList = value;
				m_changedProps.Add("bu");
			}
		}

		public int MaxPlayers
		{
			get
			{
				return m_MaxPlayers;
			}
			set
			{
				if (m_MaxPlayers != value)
				{
					m_changedProps.Add("m");
				}
				m_MaxPlayers = value;
			}
		}

		public string TrackName
		{
			get
			{
				return m_TrackName;
			}
			set
			{
				if (m_TrackName != value)
				{
					m_changedProps.Add("tn");
				}
				m_TrackName = value;
			}
		}

		public int TrackID
		{
			get
			{
				return m_TrackID;
			}
			set
			{
				if (m_TrackID != value)
				{
					m_changedProps.Add("2");
				}
				m_TrackID = value;
			}
		}

		public ulong ExternalTrackId
		{
			get
			{
				return m_externalTrackId;
			}
			set
			{
				if (m_externalTrackId != value)
				{
					m_changedProps.Add("et");
				}
				m_externalTrackId = value;
			}
		}

		public byte ugcServiceId
		{
			get
			{
				return m_ugcServiceId;
			}
			set
			{
				if (m_ugcServiceId != value)
				{
					m_changedProps.Add("usid");
				}
				m_ugcServiceId = value;
			}
		}

		public int SpawnPointIndex
		{
			get
			{
				return m_spawnPointIndex;
			}
			set
			{
				if (m_spawnPointIndex != value)
				{
					m_changedProps.Add("sp");
				}
				m_spawnPointIndex = value;
			}
		}

		public int AssistVersion
		{
			get
			{
				return m_assistVersion;
			}
			set
			{
				if (m_assistVersion != value)
				{
					m_changedProps.Add("as");
				}
				m_assistVersion = value;
			}
		}

		public int RoomTractionControlMode
		{
			get
			{
				return m_tractionControlMode;
			}
			set
			{
				if (m_tractionControlMode != value)
				{
					m_changedProps.Add("tc");
				}
				m_tractionControlMode = value;
			}
		}

		public RoomData.RoomSettingFlags NewRoomSettings
		{
			get
			{
				return m_newRoomSettings;
			}
			set
			{
				if (m_newRoomSettings != value)
				{
					m_changedProps.Add("s2");
				}
				m_newRoomSettings = value;
			}
		}

		public RoomData.RoomSettingFlags OldRoomSettings
		{
			get
			{
				return m_OldRoomSettings;
			}
			set
			{
				m_changedProps.Add("s");
				m_OldRoomSettings = value;
			}
		}

		public bool VoteMapLocked
		{
			get
			{
				return m_VoteMapLocked;
			}
			set
			{
				if (m_VoteMapLocked != value)
				{
					m_changedProps.Add("l");
				}
				m_VoteMapLocked = value;
			}
		}

		public string XboxRoomGuid
		{
			get
			{
				if (!(m_xboxRoomGuid == "null"))
				{
					return m_xboxRoomGuid;
				}
				return string.Empty;
			}
			set
			{
				if (m_xboxRoomGuid != value)
				{
					m_xboxRoomGuid = value;
					m_changedProps.Add("g");
				}
			}
		}

		public string RoomCreatorId
		{
			get
			{
				return m_roomCreatorId;
			}
			set
			{
				if (m_roomCreatorId != value)
				{
					m_changedProps.Add("3");
					m_roomCreatorId = value;
				}
			}
		}

		public bool RoomNightMode
		{
			get
			{
				return m_roomNightMode;
			}
			set
			{
				if (m_roomNightMode != value)
				{
					m_changedProps.Add("nm");
					m_roomNightMode = value;
				}
			}
		}

		public string RoomLightPreset
		{
			get
			{
				return m_roomLightPreset;
			}
			set
			{
				if (m_roomLightPreset != value)
				{
					m_changedProps.Add("lm");
					m_roomLightPreset = value;
				}
			}
		}

		public int RoomAdhLimit
		{
			get
			{
				return m_roomAdhLimit;
			}
			set
			{
				if (m_roomAdhLimit != value)
				{
					m_changedProps.Add("al");
					m_roomAdhLimit = value;
				}
			}
		}

		public int RoomPowerDensityLimit
		{
			get
			{
				return m_powerDensityLimit;
			}
			set
			{
				if (m_powerDensityLimit != value)
				{
					m_changedProps.Add("pd");
					m_powerDensityLimit = value;
				}
			}
		}

		public int RoomWheelDriveType
		{
			get
			{
				return m_wheelDriveType;
			}
			set
			{
				if (m_wheelDriveType != value)
				{
					m_changedProps.Add("wd");
					m_wheelDriveType = value;
				}
			}
		}

		public int CollisionType
		{
			get
			{
				return m_collisionsType;
			}
			set
			{
				if (m_collisionsType != value)
				{
					m_changedProps.Add("ct");
					m_collisionsType = value;
				}
			}
		}

		public bool ChampModeRoom
		{
			get
			{
				return m_champModeRoom;
			}
			set
			{
				if (m_champModeRoom != value)
				{
					m_changedProps.Add("ch");
					m_champModeRoom = value;
				}
			}
		}

		public SmartfoxRoomOptions()
		{
		}

		public SmartfoxRoomOptions(SmartfoxLobby.RoomSingleInfo room)
		{
			string value = "";
			int result = 0;
			m_changedProps.Clear();
			m_Name = room.name;
			m_PlayerCount = room.usersNumber;
			room.properties.TryGetValue("c", out m_Caption);
			room.properties.TryGetValue("k", out m_Version);
			room.properties.TryGetValue("o", out m_roomPlayersList);
			room.properties.TryGetValue("bu", out m_bannedUsersList);
			room.properties.TryGetValue("g", out m_xboxRoomGuid);
			room.properties.TryGetValue("3", out m_roomCreatorId);
			room.properties.TryGetValue("nm", out var value2);
			bool.TryParse(value2, out m_roomNightMode);
			room.properties.TryGetValue("lm", out m_roomLightPreset);
			room.properties.TryGetValue("al", out var value3);
			int.TryParse(value3, out m_roomAdhLimit);
			room.properties.TryGetValue("pd", out var value4);
			int.TryParse(value4, out m_powerDensityLimit);
			room.properties.TryGetValue("wd", out var value5);
			int.TryParse(value5, out m_wheelDriveType);
			room.properties.TryGetValue("ch", out var value6);
			bool.TryParse(value6, out m_champModeRoom);
			room.properties.TryGetValue("x", out value);
			int.TryParse(value, out m_crossPlayMask);
			if (m_crossPlayMask < 0)
			{
				m_crossPlayMask = 0;
			}
			room.properties.TryGetValue("j", out m_ModName);
			room.properties.TryGetValue("m", out value);
			int.TryParse(value, out m_MaxPlayers);
			room.properties.TryGetValue("tn", out m_TrackName);
			room.properties.TryGetValue("2", out value);
			int.TryParse(value, out m_TrackID);
			room.properties.TryGetValue("et", out value);
			ulong.TryParse(value, out m_externalTrackId);
			room.properties.TryGetValue("usid", out value);
			byte.TryParse(value, out m_ugcServiceId);
			room.properties.TryGetValue("sp", out value);
			int.TryParse(value, out m_spawnPointIndex);
			room.properties.TryGetValue("as", out value);
			int.TryParse(value, out m_assistVersion);
			room.properties.TryGetValue("tc", out value);
			int.TryParse(value, out m_tractionControlMode);
			room.properties.TryGetValue("ct", out value);
			int.TryParse(value, out m_collisionsType);
			room.properties.TryGetValue("s2", out value);
			int.TryParse(value, out result);
			m_newRoomSettings = (RoomData.RoomSettingFlags)result;
			room.properties.TryGetValue("l", out value);
			if (!int.TryParse(value, out result))
			{
				result = 0;
			}
			m_VoteMapLocked = result > 0;
			InitFilterCaption();
		}

		private RoomVariable GetVariable(string varName, HashSet<string> changedProps = null)
		{
			if (changedProps == null || changedProps.Contains(varName))
			{
				return GetVar(varName);
			}
			return null;
		}

		public void GetChangedVars(Room room, HashSet<string> changedProps = null)
		{
			RoomVariable roomVariable = null;
			roomVariable = GetVariable("c", changedProps);
			if (roomVariable != null)
			{
				m_Caption = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("k", changedProps);
			if (roomVariable != null)
			{
				Version = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("x", changedProps);
			if (roomVariable != null)
			{
				CrossPlayMask = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("j", changedProps);
			if (roomVariable != null)
			{
				ModName = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("tn", changedProps);
			if (roomVariable != null)
			{
				TrackName = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("2", changedProps);
			if (roomVariable != null)
			{
				m_TrackID = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("et", changedProps);
			if (roomVariable != null)
			{
				ulong.TryParse(roomVariable.GetStringValue(), out m_externalTrackId);
			}
			roomVariable = GetVariable("usid", changedProps);
			if (roomVariable != null)
			{
				m_ugcServiceId = (byte)roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("sp", changedProps);
			if (roomVariable != null)
			{
				int.TryParse(roomVariable.GetStringValue(), out m_spawnPointIndex);
			}
			roomVariable = GetVariable("as", changedProps);
			if (roomVariable != null)
			{
				m_assistVersion = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("tc", changedProps);
			if (roomVariable != null)
			{
				m_tractionControlMode = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("s2", changedProps);
			if (roomVariable != null)
			{
				m_newRoomSettings = (RoomData.RoomSettingFlags)roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("o", changedProps);
			if (roomVariable != null)
			{
				m_roomPlayersList = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("bu", changedProps);
			if (roomVariable != null)
			{
				ISFSArray sFSArrayValue = roomVariable.GetSFSArrayValue();
				if (sFSArrayValue != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					foreach (object item in sFSArrayValue)
					{
						stringBuilder.Append($"{item},");
					}
					m_bannedUsersList = stringBuilder.ToString();
				}
			}
			roomVariable = GetVariable("g", changedProps);
			if (roomVariable != null)
			{
				m_xboxRoomGuid = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("3", changedProps);
			if (roomVariable != null)
			{
				m_roomCreatorId = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("nm", changedProps);
			if (roomVariable != null)
			{
				m_roomNightMode = roomVariable.GetBoolValue();
			}
			roomVariable = GetVariable("lm", changedProps);
			if (roomVariable != null)
			{
				m_roomLightPreset = roomVariable.GetStringValue();
			}
			roomVariable = GetVariable("al", changedProps);
			if (roomVariable != null)
			{
				m_roomAdhLimit = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("pd", changedProps);
			if (roomVariable != null)
			{
				m_powerDensityLimit = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("wd", changedProps);
			if (roomVariable != null)
			{
				m_wheelDriveType = roomVariable.GetIntValue();
			}
			roomVariable = GetVariable("ct", changedProps);
			if (roomVariable != null)
			{
				m_collisionsType = roomVariable.GetIntValue();
			}
			if (changedProps == null || changedProps.Contains("l"))
			{
				m_VoteMapLocked = GetVoteMapLockedVar();
			}
			roomVariable = GetVariable("ch", changedProps);
			if (roomVariable != null)
			{
				m_champModeRoom = roomVariable.GetBoolValue();
			}
		}

		private bool GetVoteMapLockedVar()
		{
			object value = GetVar("l").Value;
			if (!int.TryParse((value != null) ? value.ToString() : "", out var result))
			{
				result = 0;
			}
			return result > 0;
		}

		public SmartfoxRoomOptions(Room room)
		{
			m_changedProps.Clear();
			Room = room;
			m_MaxPlayers = room.MaxUsers;
			m_Name = room.Name;
			GetChangedVars(room);
			InitFilterCaption();
		}

		private void InitFilterCaption()
		{
			Overlay.filterTextManager.TryGetFilteredCachedText(TextFilterType.Roomname, UserPlatform.Id.Unknown, m_Caption, out m_filteredCaption);
		}

		public string FilterCaption(Action<string> filteredCallback = null)
		{
			m_filteredCaption = Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Roomname, UserPlatform.Id.Unknown, m_Caption, delegate(string filtered)
			{
				if (!m_filteredCaption.Equals(filtered))
				{
					m_filteredCaption = filtered;
					filteredCallback.Call(filtered);
				}
			});
			return m_filteredCaption;
		}

		public SmartfoxRoomOptions Clone()
		{
			SmartfoxRoomOptions obj = MemberwiseClone() as SmartfoxRoomOptions;
			obj.m_changedProps.Clear();
			return obj;
		}

		public RoomVariable GetVar(string name)
		{
			if (Room.ContainsVariable(name))
			{
				return Room.GetVariable(name);
			}
			return m_zeroSettings;
		}

		public ISFSObject ToRoomSettings()
		{
			m_changedProps.Clear();
			m_settingsSFSObject.PutUtfString("n", Name);
			m_settingsSFSObject.PutUtfString("c", m_Caption);
			m_settingsSFSObject.PutUtfString("k", Version);
			m_settingsSFSObject.PutInt("x", CrossPlayMask);
			m_settingsSFSObject.PutUtfString("j", ModName);
			m_settingsSFSObject.PutShort("m", (short)MaxPlayers);
			m_settingsSFSObject.PutUtfString("tn", TrackName);
			m_settingsSFSObject.PutInt("2", TrackID);
			m_settingsSFSObject.PutUtfString("et", ExternalTrackId.ToString());
			m_settingsSFSObject.PutUtfString("usid", ugcServiceId.ToString());
			m_settingsSFSObject.PutUtfString("sp", SpawnPointIndex.ToString());
			m_settingsSFSObject.PutUtfString("as", AssistVersion.ToString());
			m_settingsSFSObject.PutUtfString("tc", RoomTractionControlMode.ToString());
			m_settingsSFSObject.PutByte("s2", (byte)NewRoomSettings);
			m_settingsSFSObject.PutByte("s", (byte)OldRoomSettings);
			m_settingsSFSObject.PutInt("l", VoteMapLocked ? 1 : 0);
			m_settingsSFSObject.PutUtfString("o", RoomPlayersList);
			m_settingsSFSObject.PutUtfString("bu", BannedUsersList);
			m_settingsSFSObject.PutUtfString("g", XboxRoomGuid);
			m_settingsSFSObject.PutUtfString("3", RoomCreatorId);
			m_settingsSFSObject.PutUtfString("lm", RoomLightPreset);
			m_settingsSFSObject.PutBool("nm", RoomNightMode);
			m_settingsSFSObject.PutInt("al", RoomAdhLimit);
			m_settingsSFSObject.PutBool("ch", ChampModeRoom);
			m_settingsSFSObject.PutInt("pd", RoomPowerDensityLimit);
			m_settingsSFSObject.PutInt("wd", RoomWheelDriveType);
			m_settingsSFSObject.PutInt("ct", CollisionType);
			return m_settingsSFSObject;
		}

		public ICollection<RoomVariable> ToChangedSfsVariables()
		{
			List<RoomVariable> list = new List<RoomVariable>();
			if (m_changedProps.Contains("n"))
			{
				list.Add(new SFSRoomVariable("n", Name));
			}
			if (m_changedProps.Contains("x"))
			{
				list.Add(new SFSRoomVariable("x", CrossPlayMask));
			}
			if (m_changedProps.Contains("o"))
			{
				list.Add(new SFSRoomVariable("o", RoomPlayersList));
			}
			if (m_changedProps.Contains("bu"))
			{
				list.Add(new SFSRoomVariable("bu", BannedUsersList));
			}
			if (m_changedProps.Contains("g"))
			{
				list.Add(new SFSRoomVariable("g", XboxRoomGuid));
			}
			if (m_changedProps.Contains("3"))
			{
				list.Add(new SFSRoomVariable("3", RoomCreatorId));
			}
			if (m_changedProps.Contains("nm"))
			{
				list.Add(new SFSRoomVariable("nm", RoomNightMode));
			}
			if (m_changedProps.Contains("lm"))
			{
				list.Add(new SFSRoomVariable("lm", RoomLightPreset));
			}
			if (m_changedProps.Contains("al"))
			{
				list.Add(new SFSRoomVariable("al", RoomAdhLimit));
			}
			if (m_changedProps.Contains("pd"))
			{
				list.Add(new SFSRoomVariable("pd", RoomPowerDensityLimit));
			}
			if (m_changedProps.Contains("wd"))
			{
				list.Add(new SFSRoomVariable("wd", RoomWheelDriveType));
			}
			if (m_changedProps.Contains("ct"))
			{
				list.Add(new SFSRoomVariable("ct", CollisionType));
			}
			if (m_changedProps.Contains("c"))
			{
				list.Add(new SFSRoomVariable("c", m_Caption));
			}
			if (m_changedProps.Contains("m"))
			{
				list.Add(new SFSRoomVariable("m", (short)MaxPlayers));
			}
			if (m_changedProps.Contains("tn"))
			{
				list.Add(new SFSRoomVariable("tn", TrackName));
			}
			if (m_changedProps.Contains("2"))
			{
				list.Add(new SFSRoomVariable("2", TrackID));
			}
			if (m_changedProps.Contains("et"))
			{
				list.Add(new SFSRoomVariable("et", ExternalTrackId.ToString()));
			}
			if (m_changedProps.Contains("usid"))
			{
				list.Add(new SFSRoomVariable("usid", ugcServiceId.ToString()));
			}
			if (m_changedProps.Contains("sp"))
			{
				list.Add(new SFSRoomVariable("sp", SpawnPointIndex.ToString()));
			}
			if (m_changedProps.Contains("as"))
			{
				list.Add(new SFSRoomVariable("as", AssistVersion.ToString()));
			}
			if (m_changedProps.Contains("tc"))
			{
				list.Add(new SFSRoomVariable("tc", RoomTractionControlMode.ToString()));
			}
			if (m_changedProps.Contains("s2"))
			{
				list.Add(new SFSRoomVariable("s2", (byte)NewRoomSettings));
			}
			if (m_changedProps.Contains("l"))
			{
				list.Add(new SFSRoomVariable("l", VoteMapLocked ? 1 : 0));
			}
			if (m_changedProps.Contains("ch"))
			{
				list.Add(new SFSRoomVariable("ch", ChampModeRoom));
			}
			m_changedProps.Clear();
			return list;
		}

		public ICollection<RoomVariable> ToSfsVariables()
		{
			m_changedProps.Clear();
			ISFSObject val = ToRoomSettings();
			SFSRoomVariable sFSRoomVariable = new SFSRoomVariable("", val, 5);
			sFSRoomVariable.IsPrivate = false;
			return new List<RoomVariable> { sFSRoomVariable };
		}

		public Dictionary<string, string> ToMasterServerRoomSettings()
		{
			m_changedProps.Clear();
			return new Dictionary<string, string>
			{
				{ "n", Name },
				{ "c", m_Caption },
				{ "k", Version },
				{
					"x",
					CrossPlayMask.ToString()
				},
				{ "j", ModName },
				{
					"m",
					((short)MaxPlayers).ToString() ?? ""
				},
				{
					"tn",
					TrackName ?? ""
				},
				{
					"2",
					TrackID.ToString() ?? ""
				},
				{
					"et",
					ExternalTrackId.ToString() ?? ""
				},
				{
					"usid",
					ugcServiceId.ToString() ?? ""
				},
				{
					"sp",
					SpawnPointIndex.ToString() ?? ""
				},
				{
					"as",
					AssistVersion.ToString() ?? ""
				},
				{
					"tc",
					RoomTractionControlMode.ToString() ?? ""
				},
				{
					"s2",
					((byte)NewRoomSettings).ToString() ?? ""
				},
				{
					"s",
					((byte)OldRoomSettings).ToString() ?? ""
				},
				{
					"l",
					(VoteMapLocked ? 1 : 0).ToString() ?? ""
				},
				{ "o", RoomPlayersList },
				{ "bu", BannedUsersList },
				{ "g", XboxRoomGuid },
				{ "3", RoomCreatorId },
				{
					"nm",
					RoomNightMode.ToString()
				},
				{ "lm", RoomLightPreset },
				{
					"al",
					RoomAdhLimit.ToString()
				},
				{
					"pd",
					RoomPowerDensityLimit.ToString()
				},
				{
					"wd",
					RoomWheelDriveType.ToString()
				},
				{
					"ch",
					ChampModeRoom.ToString()
				},
				{
					"ct",
					CollisionType.ToString()
				}
			};
		}
	}
}
