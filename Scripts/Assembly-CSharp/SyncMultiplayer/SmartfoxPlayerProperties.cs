using System;
using System.Collections.Generic;
using GameOverlay;
using MessagePack;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Util;
using VinylSystem;

namespace SyncMultiplayer
{
	public class SmartfoxPlayerProperties
	{
		public const string VISUAL_DATA_KEY = "vs";

		public const string PROFILE_DATA_KEY = "pr";

		public const string PARAM_PLAYER_ID = "1";

		public const string PARAM_CARD_ID = "2";

		public const string PARAM_PROFILE_ID = "3";

		public const string PARAM_VISUAL_SETTINGS = "4";

		public const string PARAM_PROFILE_SETTINGS = "5";

		public const string PARAM_PLAYER_NAME = "6";

		public const string PARAM_SWAP_ID = "7";

		public const string PARAM_VOTE_VALUE = "v";

		public const string PARAM_VINYL_LAYERS = "vnl";

		public const string PARAM_CAR_PAINT_HOLDER = "cph";

		public const string PARAM_PLAYER_STATE = "pst";

		public const string CROSS_PLAY_MASK = "x";

		public const string BAN_REASON_TYPE = "rt";

		public const string BAN_REASON_TYPE_EXPIRATION = "rte";

		public const string PARAM_INPUT_DEVICE_TYPE = "idt";

		public const string PARAM_MDB = "mdb";

		public const string PARAM_HAS_LIVERY = "hlr";

		private PlayerId m_playerID;

		private int m_carId;

		private int m_profileId;

		private int m_swapId;

		private string m_name;

		private CarVisualManager.VisualSettings m_visual;

		private ProfilesInfo.Profile m_profile;

		private NetPlayerInputSystem.PlayerInputDevice m_inputDeviceType;

		private PlayerCarState m_playerState;

		private bool? m_vote;

		private int m_crossPlayMask;

		private string m_banReasonsList = string.Empty;

		private bool m_hasLivery;

		private HashSet<string> m_changes = new HashSet<string>();

		public PlayerId PlayerID
		{
			get
			{
				return m_playerID;
			}
			set
			{
				if (!object.Equals(m_playerID, value))
				{
					m_changes.Add("1");
					m_playerID = value;
				}
			}
		}

		public int CarId
		{
			get
			{
				return m_carId;
			}
			set
			{
				if (!object.Equals(m_carId, value))
				{
					m_changes.Add("2");
					m_carId = value;
				}
			}
		}

		public int ProfileId
		{
			get
			{
				return m_profileId;
			}
			set
			{
				if (!object.Equals(m_profileId, value))
				{
					m_changes.Add("3");
					m_profileId = value;
				}
			}
		}

		public int SwapId
		{
			get
			{
				return m_swapId;
			}
			set
			{
				if (!object.Equals(m_swapId, value))
				{
					m_changes.Add("7");
					m_swapId = value;
				}
			}
		}

		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				if (!object.Equals(m_name, value))
				{
					m_changes.Add("6");
					m_name = value;
				}
			}
		}

		public CarVisualManager.VisualSettings VisualSettings
		{
			get
			{
				return m_visual;
			}
			set
			{
				if (!object.Equals(m_visual, value))
				{
					m_changes.Add("4");
					m_visual = value;
				}
			}
		}

		public ProfilesInfo.Profile Profile
		{
			get
			{
				return m_profile;
			}
			set
			{
				if (!object.Equals(m_profile, value))
				{
					m_changes.Add("5");
					m_profile = value;
				}
			}
		}

		public NetPlayerInputSystem.PlayerInputDevice InputDeviceType
		{
			get
			{
				return m_inputDeviceType;
			}
			set
			{
				if (!object.Equals(m_inputDeviceType, value))
				{
					m_changes.Add("idt");
					m_inputDeviceType = value;
				}
			}
		}

		public PlayerCarState PlayerState
		{
			get
			{
				return m_playerState;
			}
			set
			{
				if (!object.Equals(m_playerState, value))
				{
					m_changes.Add("pst");
					m_playerState = value;
				}
			}
		}

		public bool? Vote
		{
			get
			{
				return m_vote;
			}
			set
			{
				if (!object.Equals(m_vote, value))
				{
					m_changes.Add("v");
					m_vote = value;
				}
			}
		}

		public int CrossPlayMask
		{
			get
			{
				return m_crossPlayMask;
			}
			set
			{
				if (!object.Equals(m_crossPlayMask, value))
				{
					m_changes.Add("x");
					m_crossPlayMask = value;
				}
			}
		}

		public HashSet<PlayerId> IgnoredPlayers { get; private set; } = new HashSet<PlayerId>();

		public bool IsShowVinylsDisabled => m_banReasonsList.Contains(AdminBanReason.ACCESS_LIVERY.ToString());

		public bool HasLivery
		{
			get
			{
				return m_hasLivery;
			}
			set
			{
				if (!object.Equals(m_hasLivery, value))
				{
					m_changes.Add("hlr");
					m_hasLivery = value;
				}
			}
		}

		public double BanExpirationTime { get; private set; }

		public bool isDefaultBranchUser { get; private set; }

		public void SetFromUser(Sfs2X.Entities.User user, HashSet<string> changed = null, Action callback = null)
		{
			if (user == null)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "SmartfoxPlayerProperties - user == null");
				return;
			}
			UserVariable variable = user.GetVariable("rt");
			if (variable != null)
			{
				m_banReasonsList = variable.Value.ToString();
			}
			UserVariable variable2 = user.GetVariable("rte");
			if (variable2 != null)
			{
				BanExpirationTime = Convert.ToInt64(variable2.Value);
			}
			isDefaultBranchUser = user.GetVariable("mdb") != null;
			if (PlayerId.TryParse(user.Name, out var playerId))
			{
				PlayerID = playerId;
			}
			if (GetVariableIfNeedChange(user, changed, "2", out var variable3))
			{
				CarId = variable3.GetIntValue();
			}
			if (GetVariableIfNeedChange(user, changed, "3", out variable3))
			{
				ProfileId = variable3.GetIntValue();
			}
			if (GetVariableIfNeedChange(user, changed, "7", out variable3))
			{
				SwapId = variable3.GetIntValue();
			}
			if (GetVariableIfNeedChange(user, changed, "6", out variable3))
			{
				Name = UIUtils.FilterInvalidTags(variable3.GetStringValue());
			}
			if (GetVariableIfNeedChange(user, null, "v", out variable3))
			{
				Vote = variable3.GetBoolValue();
			}
			else
			{
				Vote = null;
			}
			if (GetVariableIfNeedChange(user, changed, "idt", out variable3))
			{
				InputDeviceType = (NetPlayerInputSystem.PlayerInputDevice)variable3.GetIntValue();
			}
			if (GetVariableIfNeedChange(user, changed, "pst", out variable3))
			{
				PlayerState = (PlayerCarState)variable3.Value;
			}
			if (GetVariableIfNeedChange(user, changed, "x", out variable3))
			{
				CrossPlayMask = variable3.GetIntValue();
				if (CrossPlayMask <= 0)
				{
					CrossPlayMask = PlayerID.platformMask;
				}
			}
			if (GetVariableIfNeedChange(user, changed, "hlr", out variable3))
			{
				HasLivery = variable3.GetBoolValue();
			}
			if (GetVariableIfNeedChange(user, changed, "vs", out variable3))
			{
				SFSObject sFSObject = (SFSObject)variable3.Value;
				VisualSettings = CustomTypes.DeserializeCarVS(sFSObject.GetByteArray("4").Bytes).MutateToLocalParts();
				sFSObject.GetByteArray("vnl");
				VisualSettings.vinylLayers = new List<VinylLayer>();
				VisualSettings.colors = CarVisualManager.DeserializeCarPaint(sFSObject.GetByteArray("cph").Bytes);
			}
			if (GetVariableIfNeedChange(user, changed, "pr", out variable3))
			{
				SFSObject sFSObject = (SFSObject)variable3.Value;
				ByteArray byteArray = sFSObject.GetByteArray("5");
				if (byteArray != null)
				{
					Profile = MessagePackSerializer.Deserialize<ProfilesInfo.Profile>(byteArray.Bytes);
				}
			}
			callback.SafeCall();
		}

		public void SetFromProps(SmartfoxPlayerProperties props)
		{
			PlayerID = props.PlayerID;
			CarId = props.CarId;
			ProfileId = props.ProfileId;
			SwapId = props.SwapId;
			Name = props.Name;
			VisualSettings = props.VisualSettings;
			Profile = props.Profile;
			InputDeviceType = props.InputDeviceType;
			PlayerState = props.PlayerState;
			Vote = props.Vote;
			CrossPlayMask = props.CrossPlayMask;
			IgnoredPlayers = props.IgnoredPlayers;
			m_banReasonsList = props.m_banReasonsList;
			BanExpirationTime = props.BanExpirationTime;
		}

		private bool GetVariableIfNeedChange(Sfs2X.Entities.User user, HashSet<string> changed, string param, out UserVariable variable)
		{
			variable = null;
			if (user == null)
			{
				return false;
			}
			variable = user.GetVariable(param);
			if (changed == null)
			{
				if (variable != null)
				{
					return variable.Value != null;
				}
				return false;
			}
			if (changed.Contains(param))
			{
				return variable.Value != null;
			}
			return false;
		}

		public ICollection<UserVariable> ToSfsVariables()
		{
			ResetChanges();
			return new List<UserVariable>
			{
				new SFSUserVariable("6", Name),
				new SFSUserVariable("2", CarId),
				new SFSUserVariable("3", ProfileId),
				new SFSUserVariable("7", SwapId),
				new SFSUserVariable("x", CrossPlayMask),
				new SFSUserVariable("idt", (int)InputDeviceType),
				new SFSUserVariable("pst", (int)PlayerState),
				new SFSUserVariable("vs", GenerateVisualData(), 5),
				new SFSUserVariable("pr", GenerateProfileData(), 5),
				new SFSUserVariable("hlr", HasLivery)
			};
		}

		public ICollection<UserVariable> ToChangedSfsVariables()
		{
			List<UserVariable> list = new List<UserVariable>();
			if (m_changes.Contains("6"))
			{
				list.Add(new SFSUserVariable("6", Name));
			}
			if (m_changes.Contains("2"))
			{
				list.Add(new SFSUserVariable("2", CarId));
			}
			if (m_changes.Contains("3"))
			{
				list.Add(new SFSUserVariable("3", ProfileId));
			}
			if (m_changes.Contains("7"))
			{
				list.Add(new SFSUserVariable("7", SwapId));
			}
			if (m_changes.Contains("x"))
			{
				list.Add(new SFSUserVariable("x", CrossPlayMask));
			}
			if (m_changes.Contains("idt"))
			{
				list.Add(new SFSUserVariable("idt", (int)InputDeviceType));
			}
			if (m_changes.Contains("pst"))
			{
				list.Add(new SFSUserVariable("pst", (int)PlayerState));
			}
			if (m_changes.Contains("hlr"))
			{
				list.Add(new SFSUserVariable("hlr", HasLivery));
			}
			if (m_changes.Contains("4") || m_changes.Contains("vnl") || m_changes.Contains("cph"))
			{
				list.Add(new SFSUserVariable("vs", GenerateVisualData(), 5));
			}
			if (m_changes.Contains("5"))
			{
				list.Add(new SFSUserVariable("pr", GenerateProfileData(), 5));
			}
			ResetChanges();
			return list;
		}

		private SFSObject GenerateVisualData()
		{
			SFSObject sFSObject = new SFSObject();
			byte[] buf = CarVisualManager.SerializeCarPaint(VisualSettings);
			sFSObject.PutByteArray("cph", new ByteArray(buf));
			buf = CustomTypes.SerializeCarVS(ClearLiveriesAndColor());
			sFSObject.PutByteArray("4", new ByteArray(buf));
			return sFSObject;
		}

		private SFSObject GenerateProfileData()
		{
			SFSObject sFSObject = new SFSObject();
			byte[] buf = MessagePackSerializer.Serialize(Profile);
			sFSObject.PutByteArray("5", new ByteArray(buf));
			return sFSObject;
		}

		private CarVisualManager.VisualSettings ClearLiveriesAndColor()
		{
			CarVisualManager.VisualSettings visualSettings = VisualSettings.Copy();
			visualSettings.vinylLayers = null;
			visualSettings.colors = null;
			return visualSettings;
		}

		private void ResetChanges()
		{
			m_changes.Clear();
		}
	}
}
