using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Binding.Elements;
using GameInput;
using Sfs2X.Entities;
using SyncMultiplayer;

namespace HUD
{
	[RaycastBlocker(true)]
	public class UICreateCompetitionContext : BaseBindingBehaviourTarget
	{
		private string CLIPPING_CFG = "clipping";

		private ColorProperty m_colorIcon = new ColorProperty();

		private SpriteProperty m_configSprite = new SpriteProperty();

		private StringProperty m_description = new StringProperty();

		private ListProperty m_settings = new ListProperty();

		private SwitcherListElementData<SubroomMode> m_subroomModeSwitcher;

		private SwitcherListElementData<int> m_playersSwitcher;

		private SwitcherListElementData<bool> m_XDSCollideSwitcher;

		private SwitcherListElementData<bool> m_XDSRecordSwitcher;

		private SwitcherListElementData<SubroomFlags> m_tandemSwitcher;

		private SwitcherListElementData<string> m_configSwitcher;

		private UIRaceNetworkFreerideModeContext m_raceContext;

		private string[][] m_trackPoints;

		private readonly int[] m_tracksTandemExceptions = new int[1] { 41 };

		private void Start()
		{
			InitiateTrackPoints();
			m_subroomModeSwitcher = new SwitcherListElementData<SubroomMode>((SubroomMode x) => LocalizationKeys.network_room_mode_.AddLocalized(x.ToString().ToLower()));
			m_playersSwitcher = new SwitcherListElementData<int>((int x) => $"{LocalizationKeys.network_race_player.Localized()} {x}");
			m_XDSCollideSwitcher = new SwitcherListElementData<bool>((bool x) => (x ? LocalizationKeys.xds_room_safemode_on : LocalizationKeys.xds_room_safemode_off).Localized());
			m_XDSRecordSwitcher = new SwitcherListElementData<bool>((bool x) => (x ? LocalizationKeys.xds_room_record_on : LocalizationKeys.xds_room_record_off).Localized());
			m_configSwitcher = new SwitcherListElementData<string>();
			m_tandemSwitcher = new SwitcherListElementData<SubroomFlags>((SubroomFlags x) => LocalizationKeys.xds_room_.AddLocalized(x.ToString().ToLower()));
			m_XDSCollideSwitcher.Init(new bool[2] { false, true });
			m_XDSRecordSwitcher.Init(new bool[2] { true, false });
			m_tandemSwitcher.Init(new SubroomFlags[3]
			{
				SubroomFlags.LeaderRandom,
				SubroomFlags.LeaderCreator,
				SubroomFlags.LeaderClient
			});
			m_settings.Add(m_subroomModeSwitcher);
			m_settings.Add(m_playersSwitcher);
			m_settings.Add(m_configSwitcher);
			m_subroomModeSwitcher.onChanged += OnChangedMode;
			m_playersSwitcher.onChanged += OnChangedPlayersCount;
			FillModeList();
			m_settings.Selection(0, selected: true);
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			if (m_raceContext == null)
			{
				m_raceContext = base.owner.Get<UIRaceNetworkFreerideModeContext>();
			}
			UIRaceHintsContext.ShowHints(base.owner, new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavCancelBtnB
				},
				new ActionHintData
				{
					hint = LocalizationKeys.network_game_btn_create.Localized(),
					type = ActionType.NavSelectBtnA,
					action = NavSelectBtnA
				}
			});
			m_raceContext.SetBlockPlayerController(block: true);
			XRModeManager.TryApplyCockpitCamera();
		}

		protected override void OnDeactivate()
		{
			base.owner.Hide<UIRaceHintsContext>();
			m_raceContext.SetBlockPlayerController(block: false);
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		private void FillModeList()
		{
			if (m_subroomModeSwitcher.isEmpty)
			{
				Room currentRoom = NetworkController.InstanceGame.CurrentRoom;
				m_subroomModeSwitcher.Clear();
				SmartfoxRoomOptions smartfoxRoomOptions = ((currentRoom != null) ? new SmartfoxRoomOptions(currentRoom) : null);
				RoomData.RoomSettingFlags flags = smartfoxRoomOptions?.NewRoomSettings ?? RoomData.RoomSettingFlags.ALL;
				InitializeCompetitionSelection(smartfoxRoomOptions, flags);
			}
		}

		private void InitializeCompetitionSelection(SmartfoxRoomOptions currentSettings, RoomData.RoomSettingFlags flags)
		{
			bool num = IsDriftAllowed(flags);
			bool flag = IsTimeAttackAllowed(flags);
			if (num)
			{
				if (m_trackPoints[6].Length != 0)
				{
					m_subroomModeSwitcher.Add(SubroomMode.TandemXDS);
					m_subroomModeSwitcher.Add(SubroomMode.XDS);
					if (!CheckIsNeedSkipTandem(currentSettings))
					{
						m_subroomModeSwitcher.Add(SubroomMode.Tandem);
					}
				}
				if (IsClippingConfigExists())
				{
					m_subroomModeSwitcher.Add(SubroomMode.ClippingDuel);
				}
				m_subroomModeSwitcher.Add(SubroomMode.Challenge);
			}
			if (flag)
			{
				m_subroomModeSwitcher.Add(SubroomMode.TimeAttack);
			}
			if (GameConsole.Bool["ChampionshipMode"])
			{
				m_subroomModeSwitcher.Add(SubroomMode.ShowTrack);
			}
			m_subroomModeSwitcher.Validate();
		}

		private bool IsDriftAllowed(RoomData.RoomSettingFlags flags)
		{
			return (flags & RoomData.RoomSettingFlags.DRIFT_ALLOWED) != 0;
		}

		private bool IsTimeAttackAllowed(RoomData.RoomSettingFlags flags)
		{
			return (flags & RoomData.RoomSettingFlags.TIME_ALLOWED) != 0;
		}

		private bool CheckIsNeedSkipTandem(SmartfoxRoomOptions currentSettings)
		{
			if (currentSettings == null)
			{
				return false;
			}
			return Array.Exists(m_tracksTandemExceptions, (int trackId) => trackId == currentSettings.TrackID);
		}

		private bool IsClippingConfigExists()
		{
			ConfigurationManager instance = ConfigurationManager.instance;
			if (instance == null)
			{
				return false;
			}
			return instance.GetAllNetworkNodes()?.Exists((string p) => p.ToLower().StartsWith(CLIPPING_CFG)) ?? false;
		}

		private void FillPointsData(SubroomMode mode)
		{
			m_configSwitcher.Init(m_trackPoints[(uint)mode]);
		}

		private void RebuildingPlayersSize(SubroomMode mode)
		{
			List<int> list = new List<int>(4);
			if (mode == SubroomMode.ShowTrack)
			{
				list.Add(1);
			}
			else
			{
				if (mode.AllowedSelfChallenge())
				{
					list.Add(1);
				}
				list.Add(2);
				if (!mode.DisableMoreTwoChallenge())
				{
					list.Add(3);
					list.Add(4);
				}
			}
			m_playersSwitcher.Init(list);
			m_playersSwitcher.index = list.Count - 1;
		}

		private void OnChangedMode()
		{
			FillPointsData(m_subroomModeSwitcher.item);
			RebuildingPlayersSize(m_subroomModeSwitcher.item);
			m_description.value = LocalizationKeys.room_desc_.AddLocalized(m_subroomModeSwitcher.item.ToString().ToLower());
			if ((bool)m_raceContext.subroomModeIcons)
			{
				m_configSprite.value = m_raceContext.subroomModeIcons.GetSprite(m_subroomModeSwitcher.item.ToString());
			}
			m_colorIcon.value = UIRaceNetworkModeContext.subroomColor[m_subroomModeSwitcher.item];
			UpdateXDSOptions(m_subroomModeSwitcher.item);
		}

		private void OnChangedPlayersCount()
		{
			if (m_subroomModeSwitcher.item == SubroomMode.TandemXDS || m_subroomModeSwitcher.item == SubroomMode.Tandem)
			{
				UpdateXDSOptions(SubroomMode.TandemXDS);
			}
		}

		private void UpdateXDSOptions(SubroomMode mode)
		{
			m_settings.Remove(m_XDSCollideSwitcher);
			m_settings.Remove(m_XDSRecordSwitcher);
			m_settings.Remove(m_tandemSwitcher);
			if ((mode == SubroomMode.TandemXDS || mode == SubroomMode.Tandem) && m_playersSwitcher.item > 1)
			{
				m_settings.Add(m_tandemSwitcher);
			}
			if (mode == SubroomMode.TandemXDS)
			{
				if (m_playersSwitcher.item > 1)
				{
					m_settings.Add(m_XDSCollideSwitcher);
				}
				else if (m_playersSwitcher.item == 1)
				{
					m_settings.Add(m_XDSRecordSwitcher);
				}
			}
		}

		public void NavCancelBtnB()
		{
			Hide();
		}

		public void NavSelectBtnA()
		{
			Hide();
			if (UIRaceNetworkModeContext.VeryBadConnectionCheck(m_subroomModeSwitcher.item))
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.xds_cant_create_badconnection.Localized(), 3f);
			}
			else
			{
				SpawnRoom();
			}
		}

		private void SpawnRoom()
		{
			SubroomSettings subroomSettings = new SubroomSettings
			{
				subroomMode = m_subroomModeSwitcher.item,
				pointName = m_configSwitcher.item,
				playersCount = m_playersSwitcher.item,
				xdsFlags = SubroomFlags.None
			};
			bool flag = subroomSettings.subroomMode == SubroomMode.TandemXDS;
			if (m_settings.IndexOf(m_XDSCollideSwitcher) >= 0 && !m_XDSCollideSwitcher.item)
			{
				subroomSettings.xdsFlags |= SubroomFlags.XDSCollide;
				if (flag)
				{
					AnalyticsService.LogTandemXDSinRoom();
				}
			}
			else if (flag)
			{
				AnalyticsService.LogSafeModeXDS();
			}
			if (m_settings.IndexOf(m_XDSRecordSwitcher) >= 0 && m_XDSRecordSwitcher.item)
			{
				subroomSettings.xdsFlags |= SubroomFlags.XDSRecord;
			}
			if (m_settings.IndexOf(m_tandemSwitcher) >= 0)
			{
				subroomSettings.xdsFlags |= m_tandemSwitcher.item;
			}
			m_raceContext.SpawnRoom(subroomSettings);
		}

		[GameHotkey(ActionType.NavDown, false, false)]
		public void NavDown()
		{
			m_settings.Selection(m_settings.GetSelectedIndex() + 1, selected: true);
		}

		[GameHotkey(ActionType.NavUp, false, false)]
		public void NavUp()
		{
			m_settings.Selection(m_settings.GetSelectedIndex() - 1, selected: true);
		}

		private void InitiateTrackPoints()
		{
			SubroomMode[] obj = Enum.GetValues(typeof(SubroomMode)) as SubroomMode[];
			int num = 0;
			SubroomMode[] array = obj;
			foreach (SubroomMode subroomMode in array)
			{
				if ((int)subroomMode > num)
				{
					num = (int)subroomMode;
				}
			}
			num++;
			m_trackPoints = new string[num][];
			List<string> list = new List<string>();
			List<string>[] array2 = new List<string>[num];
			for (int j = 0; j < num; j++)
			{
				array2[j] = new List<string>();
			}
			ConfigurationManager instance = ConfigurationManager.instance;
			if ((bool)instance)
			{
				foreach (string allNetworkNode in instance.GetAllNetworkNodes())
				{
					array2[3].Add(allNetworkNode);
					string text = allNetworkNode.ToLower();
					if (text.StartsWith("drift"))
					{
						array2[0].Add(allNetworkNode);
					}
					else if (text.StartsWith("time"))
					{
						array2[1].Add(allNetworkNode);
					}
					else if (text.StartsWith("xds"))
					{
						array2[6].Add(allNetworkNode);
						array2[5].Add(allNetworkNode);
					}
					else if (text.StartsWith("tandem"))
					{
						array2[4].Add(allNetworkNode);
					}
					else if (text.StartsWith("clipping"))
					{
						array2[2].Add(allNetworkNode);
						array2[4].Add(allNetworkNode);
					}
					else
					{
						list.Add(allNetworkNode);
					}
				}
			}
			if (list.Count == 0)
			{
				list.Add("Error: have no data!");
			}
			else
			{
				for (int k = 0; k < num; k++)
				{
					if (k == 0 || k == 1)
					{
						array2[k].AddRange(list);
					}
				}
			}
			for (int l = 0; l < num; l++)
			{
				m_trackPoints[l] = ((array2[l].Count == 0 && l != 6) ? list.ToArray() : array2[l].ToArray());
			}
		}
	}
}
