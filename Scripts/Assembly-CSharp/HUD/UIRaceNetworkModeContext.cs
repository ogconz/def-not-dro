using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Binding;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	public abstract class UIRaceNetworkModeContext : UIRaceModeContext
	{
		private class PairPlayerInfo
		{
			public StringProperty nick;

			public StringProperty points;

			public IntProperty playerInputType;
		}

		private class InvalidatedRoomResult
		{
			public PlayerId playerId;

			public string name;

			public int points;

			public int raceProgress;

			public float raceTime;

			public bool disqualified;
		}

		[SerializeField]
		protected Color m_winnerColor;

		[SerializeField]
		protected Color m_noWinnerColor;

		protected IntProperty m_subroomModeInt = new IntProperty(-1);

		protected StringProperty m_playersNumber = new StringProperty();

		protected StringProperty m_pairNick1 = new StringProperty();

		protected StringProperty m_pairNick2 = new StringProperty();

		protected StringProperty m_pairPoints1 = new StringProperty();

		protected StringProperty m_pairPoints2 = new StringProperty();

		protected IntProperty m_playerInputType1 = new IntProperty();

		protected IntProperty m_playerInputType2 = new IntProperty();

		protected StringProperty m_time = new StringProperty();

		protected StringProperty m_penalty = new StringProperty();

		protected StringProperty inGameTime = new StringProperty();

		protected StringProperty m_subroomModeName = new StringProperty();

		protected StringProperty m_subroomConfigName = new StringProperty();

		protected int m_oldInGameTimeMinute = -1;

		protected BoolProperty m_isPenaltyGrowing = new BoolProperty();

		public BoolProperty isTandemMode = new BoolProperty();

		public BoolProperty isTandemChaser = new BoolProperty();

		public FloatProperty tandemFactor = new FloatProperty(0f);

		public BoolProperty isDriftMode = new BoolProperty(startValue: true);

		public BoolProperty showDriftHUD = new BoolProperty(startValue: true);

		public BoolProperty showInclinometer = new BoolProperty(startValue: true);

		public BoolProperty isWaitingOpponent = new BoolProperty(false);

		public BoolProperty localBadConnection = new BoolProperty(false);

		public BoolProperty pairBadConnection1 = new BoolProperty(false);

		public BoolProperty pairBadConnection2 = new BoolProperty(false);

		protected bool m_badConnectionBool;

		protected bool m_pairBadConnection1Bool;

		protected bool m_pairBadConnection2Bool;

		protected InputManager m_inputManager;

		protected NetworkPlayer m_networkLocalPlayer;

		private ListProperty m_playersList = new ListProperty();

		private NetGameSubroomsSystem m_roomsController;

		private UIRaceNetworkFreerideModeContext m_raceNetworkFreerideModeContext;

		protected SubroomView m_subroomView;

		private List<PairPlayerInfo> m_helperPairPlayers;

		private List<InvalidatedRoomResult> m_placesHelper = new List<InvalidatedRoomResult>();

		public static Dictionary<SubroomMode, Color> subroomColor => new Dictionary<SubroomMode, Color>
		{
			{
				SubroomMode.XDS,
				UIUtils.HexToColor("cd1a01")
			},
			{
				SubroomMode.TimeAttack,
				UIUtils.HexToColor("a213a7")
			},
			{
				SubroomMode.TandemXDS,
				UIUtils.HexToColor("ff8005")
			},
			{
				SubroomMode.Tandem,
				UIUtils.HexToColor("ffe900")
			},
			{
				SubroomMode.ClippingDuel,
				UIUtils.HexToColor("00beff")
			},
			{
				SubroomMode.Challenge,
				UIUtils.HexToColor("5dd000")
			}
		};

		public SyncNetFreerideRaceModeState raceState { get; protected set; }

		public bool isSubroomUIActive { get; protected set; }

		public NetGameSubroomsSystem roomsController => m_roomsController;

		protected override void Awake()
		{
			base.Awake();
			m_roomsController = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
			m_roomsController.onSubroomOperation += OnRoomOperation;
			if (m_inputManager == null)
			{
				m_inputManager = InputManager.instance;
			}
			isDriftMode.OnValueChanged += OnDriftModeValueChanged;
			showDriftHUD.OnValueChanged += OnShowDriftHUDChanged;
		}

		private void OnDriftModeValueChanged()
		{
			UIDriftEventsContext.Clear();
			raceState.ResetPoints();
			if ((bool)base.raceDpAction)
			{
				base.raceDpAction.ResetAll();
			}
		}

		private void OnShowDriftHUDChanged()
		{
			if (m_raceNetworkFreerideModeContext == null)
			{
				m_raceNetworkFreerideModeContext = base.owner.Get<UIRaceNetworkFreerideModeContext>();
			}
			if (m_raceNetworkFreerideModeContext != null && m_raceNetworkFreerideModeContext.isActiveAndEnabled)
			{
				m_raceNetworkFreerideModeContext.ShowUIHUDMultiplayerButtonsPanel(showDriftHUD.value);
			}
		}

		public void SetShowDriftHUDValue(bool value)
		{
			if (showDriftHUD.value != value)
			{
				showDriftHUD.value = value;
			}
		}

		protected override void Start()
		{
			base.Start();
			m_networkLocalPlayer = NetworkController.InstanceGame.LocalPlayer;
			InvokeRepeating("UpdateOneSecond", 0f, 1f);
		}

		protected override void OnDestroy()
		{
			m_roomsController.onSubroomOperation -= OnRoomOperation;
			isDriftMode.OnValueChanged -= OnDriftModeValueChanged;
			showDriftHUD.OnValueChanged -= OnShowDriftHUDChanged;
			base.OnDestroy();
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			raceState = States.current as SyncNetFreerideRaceModeState;
		}

		protected virtual void UpdateOneSecond()
		{
			RefreshPlayersNumber();
		}

		protected override void Update()
		{
			base.Update();
			if (m_subroomView != null)
			{
				m_subroomView.Update();
			}
			else
			{
				isDriftMode.value = true;
				RefreshDriftPointAction();
			}
			int minute = DateTime.Now.Minute;
			if (m_oldInGameTimeMinute != minute)
			{
				m_oldInGameTimeMinute = minute;
				inGameTime.value = UIUtils.TimeToHH_MMString(DateTime.Now.Hour, minute);
			}
			CheckBadConectionForLocalPlayer();
		}

		public void RefreshDriftPointAction()
		{
			if (isDriftMode.value && raceState != null)
			{
				RefreshDriftPointAction(raceState.currentDriftPoints);
			}
		}

		private void RefreshPlayersNumber()
		{
			m_playersNumber.value = $"{NetworkController.InstanceGame.Players.Count} / {16}";
		}

		protected static bool IsContextNonEmptyAndEnabled(BaseContext context)
		{
			if (context != null)
			{
				return context.isActiveAndEnabled;
			}
			return false;
		}

		protected NetworkPlayer GetNetworkPlayerByUID(NetworkGame networkGame, string uidString)
		{
			return networkGame?.FindPlayerByUID(uidString);
		}

		protected virtual void OnRoomOperation(SubroomInfo info, SubroomOperation operation)
		{
			switch (operation)
			{
			case SubroomOperation.Spawn:
				OnSubroomSpawn(info);
				break;
			case SubroomOperation.LocalFinished:
				OnSubroomLocalFinished(info);
				break;
			case SubroomOperation.Destroy:
				OnSubroomDestroy(info);
				break;
			case SubroomOperation.StartRace:
				OnSubroomStartRace(info);
				break;
			case SubroomOperation.RaceResult:
				OnSubroomRaceResult(info);
				break;
			case SubroomOperation.Join:
				OnSubroomJoined(info);
				break;
			default:
				GameLog.Print(AlertLevel.Error, "Multiplayer", "OnRoomOperation> not supported room operation");
				break;
			case SubroomOperation.OtherFinished:
				break;
			}
		}

		protected virtual void OnSubroomSpawn(SubroomInfo info)
		{
			isSubroomUIActive = true;
		}

		protected virtual void OnSubroomJoined(SubroomInfo info)
		{
		}

		protected virtual void OnSubroomStartRace(SubroomInfo info)
		{
			m_subroomView = SubroomView.Create(this, info);
			m_subroomModeInt.value = (int)info.subroomMode;
			m_subroomModeName.value = LocalizationKeys.network_room_mode_.AddLocalized(info.subroomMode.ToString().ToLower());
			m_subroomConfigName.value = info.pointName;
		}

		protected virtual void OnSubroomLocalFinished(SubroomInfo info)
		{
			m_subroomModeInt.value = int.MaxValue;
			UIDriftEventsContext.Clear();
			isDriftMode.value = false;
			base.owner.Get<UIFinishContext>().onFinishedEvent += delegate
			{
				if (info.state == SubroomState.WaitingResults || info.state == SubroomState.WaitingSecondRace)
				{
					isWaitingOpponent.value = true;
				}
			};
		}

		protected virtual void OnSubroomRaceResult(SubroomInfo info, bool isTop32 = false)
		{
			if (info != null && info.ContainsPlayer(NetworkController.InstanceGame.LocalPlayer))
			{
				if (info.players.Count != 1)
				{
					ConvertToResult(info);
				}
				if (m_subroomView?.subroom == info)
				{
					m_subroomModeInt.value = -1;
					m_subroomView.Destroy();
					m_subroomView = null;
				}
				if (base.isActive && m_roomsController.myRoom == info)
				{
					ShowPostCombat(info);
				}
			}
		}

		protected virtual void OnSubroomDestroy(SubroomInfo info)
		{
			isSubroomUIActive = false;
			if (m_subroomView?.subroom == info)
			{
				m_subroomView.Destroy();
				m_subroomView = null;
				m_subroomModeInt.value = -1;
			}
		}

		protected virtual void ShowPostCombat(SubroomInfo info)
		{
			bool xrActive = false;
			if (XRModeManager.IsXRActive())
			{
				XRModeManager.TryApplyCockpitCamera();
				CameraSwitch.instance.SetActiveCameraSwitch(isActive: false);
			}
			if (info.subroomMode == SubroomMode.TandemXDS)
			{
				if (info.state == SubroomState.WaitingSecondRace)
				{
					SetShowDriftHUDValue(value: false);
				}
				FillPostCombatInfo<UIXDSPostCombatNetworkContext>();
			}
			else
			{
				FillPostCombatInfo<UIPostCombatNetworkContext>();
			}
			void FillPostCombatInfo<T>() where T : BaseContext, IPostCombatNetworkContext
			{
				UIFinishContext uIFinishContext = base.owner.Get<UIFinishContext>();
				if (uIFinishContext.isActive)
				{
					uIFinishContext.onFinishedEvent += delegate
					{
						TryPushPostCombatAndFillInfo<T>();
					};
				}
				else
				{
					TryPushPostCombatAndFillInfo<T>();
				}
			}
			void TryPushPostCombatAndFillInfo<T>() where T : BaseContext, IPostCombatNetworkContext
			{
				if (base.owner.IsActive<UIBaseDialogContext>())
				{
					base.owner.Hide<UIBaseDialogContext>();
				}
				raceState.contextOwner.Push<T>().Fill(info);
				if (xrActive)
				{
					CameraSwitch.instance.SetActiveCameraSwitch(isActive: true);
				}
			}
		}

		private void ConvertToResult(SubroomInfo info)
		{
			if (info.playersCount == 1)
			{
				return;
			}
			int validatedTimedOut = info.validatedTimedOut;
			List<ValidatedRoomResult> validatedResults = info.validatedResults;
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
			NetworkPlayer localPlayer = NetworkController.InstanceGame.LocalPlayer;
			if (!info.ContainsResult(localPlayer))
			{
				return;
			}
			ReadOnlyCollection<int> mp_challengeList = baseModel.GetSingle<GlobalGameSettings>().expSettings.mp_challengeList;
			int num = Mathf.Min(mp_challengeList.Count, validatedResults.Count - 1);
			int num2 = Mathf.Max(0, Mathf.Min(mp_challengeList.Count, info.resultsCount) - (info.resultsCount - validatedTimedOut));
			int num3 = 0;
			int num4 = num2 + num;
			while (num3 <= num)
			{
				ValidatedRoomResult result = validatedResults[num3];
				if (result.playerId == localPlayer.PlayerId)
				{
					bool flag = info.GetAllPlayersValidatedResult().FindIndex((ValidatedRoomResult x) => x.playerId == result.playerId) + 1 == 1;
					string nameById = baseModel.GetNameById<PlayerCar>(gamePrefs.globals.lastCarId);
					AnalyticsService.MultiplayerRaceResult(flag, nameById);
					NetworkController.InstanceGame.stats.IncSubroomStat(info.subroomMode, flag);
					break;
				}
				num3++;
				num4--;
			}
		}

		public void SetBlockPlayerController(bool block)
		{
			if ((bool)PlayerCarControl.instance && (bool)CameraSwitch.instance && CameraSwitch.instance.GetCar() == PlayerCarControl.instance.car)
			{
				PlayerCarControl.instance.SetBlockControl(block);
			}
			base.driftAngles.enabled = !block;
		}

		public void RefreshTimeAttack()
		{
			m_time.value = UIUtils.TimeToMSMString(raceState.currentLapTime);
			m_isPenaltyGrowing.value = raceState.isPenaltyGrowing;
			m_penalty.value = UIUtils.TimeToMSMString(raceState.currentPenaltyTime);
		}

		public static bool VeryBadConnectionCheck(SubroomMode itemMode)
		{
			NetworkPlayer localPlayer = NetworkController.InstanceGame.LocalPlayer;
			if (itemMode == SubroomMode.TandemXDS && localPlayer.VeryBadConnection)
			{
				return true;
			}
			return false;
		}

		public void RefreshPairPlayersPoints(SubroomInfo subroom)
		{
			if (subroom == null || subroom.positions.Count != 2)
			{
				return;
			}
			if (m_helperPairPlayers == null)
			{
				m_helperPairPlayers = new List<PairPlayerInfo>();
				m_helperPairPlayers.Add(new PairPlayerInfo
				{
					nick = m_pairNick2,
					points = m_pairPoints2,
					playerInputType = m_playerInputType2
				});
				m_helperPairPlayers.Add(new PairPlayerInfo
				{
					nick = m_pairNick1,
					points = m_pairPoints1,
					playerInputType = m_playerInputType1
				});
			}
			CheckConnetctionInTandem(subroom);
			for (int i = 0; i < subroom.positions.Count; i++)
			{
				PlayerId playerId = subroom.positions[i];
				NetworkPlayer networkPlayer = null;
				bool flag = false;
				PairPlayerInfo pairPlayerInfo = m_helperPairPlayers[i];
				networkPlayer = subroom.GetRuntimePlayer(playerId);
				flag = ((!subroom.TryGetResult(playerId, out var result)) ? (networkPlayer == null) : (result.disqualifyReason != DisqualifyReason.Unknown));
				pairPlayerInfo.nick.value = subroom.GetFilteredNickName(playerId);
				if (networkPlayer != null && networkPlayer.PlayerProperties != null)
				{
					pairPlayerInfo.playerInputType.value = (int)networkPlayer.PlayerProperties.InputDeviceType;
				}
				if (flag)
				{
					if (!string.IsNullOrEmpty(pairPlayerInfo.points.value))
					{
						pairPlayerInfo.points.value = string.Empty;
					}
				}
				else
				{
					string value = ((result != null) ? result.asString : ((!subroom.subroomMode.IsXDS()) ? UIUtils.DigitsWithSeparate(networkPlayer.points.driftPoints) : DriftControllerXDS.ConvertScoresToResultString(networkPlayer.points.driftPoints)));
					pairPlayerInfo.points.value = value;
				}
			}
		}

		private void CheckConnectionInDrift(SubroomInfo subroom)
		{
			for (int i = 0; i < m_placesHelper.Count; i++)
			{
				InvalidatedRoomResult invalidatedRoomResult = m_placesHelper[i];
				PlayerInRoomItem byIndex = m_playersList.GetByIndex<PlayerInRoomItem>(i);
				NetworkPlayer runtimePlayer = subroom.GetRuntimePlayer(invalidatedRoomResult.playerId);
				if (runtimePlayer != null)
				{
					if (byIndex.m_badConnection.value != runtimePlayer.BadConnection)
					{
						byIndex.SetConnectionStatus(runtimePlayer.BadConnection);
					}
				}
				else
				{
					byIndex.SetConnectionStatus(connection: false);
				}
			}
		}

		private void CheckConnetctionInTandem(SubroomInfo subroom)
		{
			for (int i = 0; i < subroom.positions.Count; i++)
			{
				PlayerId playerId = subroom.positions[i];
				NetworkPlayer networkPlayer = null;
				string filteredNickName = subroom.GetFilteredNickName(playerId);
				if (filteredNickName == m_pairNick1.value)
				{
					networkPlayer = subroom.GetRuntimePlayer(playerId);
					if (networkPlayer != null)
					{
						if (m_pairBadConnection1Bool != networkPlayer.BadConnection)
						{
							m_pairBadConnection1Bool = networkPlayer.BadConnection;
							pairBadConnection1.value = m_pairBadConnection1Bool;
						}
					}
					else
					{
						m_pairBadConnection1Bool = false;
						pairBadConnection1.value = m_pairBadConnection1Bool;
					}
				}
				else
				{
					if (!(filteredNickName == m_pairNick2.value))
					{
						continue;
					}
					networkPlayer = subroom.GetRuntimePlayer(playerId);
					if (networkPlayer != null)
					{
						if (m_pairBadConnection2Bool != networkPlayer.BadConnection)
						{
							m_pairBadConnection2Bool = networkPlayer.BadConnection;
							pairBadConnection2.value = m_pairBadConnection2Bool;
						}
					}
					else
					{
						m_pairBadConnection2Bool = false;
						pairBadConnection2.value = m_pairBadConnection2Bool;
					}
				}
			}
		}

		private void CheckBadConectionForLocalPlayer()
		{
			if (m_badConnectionBool != m_networkLocalPlayer.BadConnection)
			{
				m_badConnectionBool = m_networkLocalPlayer.BadConnection;
				localBadConnection.value = m_badConnectionBool;
			}
		}

		public void RefreshPlayerList(SubroomInfo subroom)
		{
			if (subroom == null)
			{
				return;
			}
			FillCurrentPlaces(subroom, m_placesHelper);
			if (m_playersList.count != m_placesHelper.Count)
			{
				m_playersList.Clear();
				for (int i = 0; i < m_placesHelper.Count; i++)
				{
					m_playersList.Add(new PlayerInRoomItem(i + 1));
				}
			}
			SubroomMode subroomMode = subroom.subroomMode;
			CheckConnectionInDrift(subroom);
			for (int j = 0; j < m_placesHelper.Count; j++)
			{
				InvalidatedRoomResult invalidatedRoomResult = m_placesHelper[j];
				PlayerInRoomItem byIndex = m_playersList.GetByIndex<PlayerInRoomItem>(j);
				bool isLocal = invalidatedRoomResult.playerId == Overlay.instance.player.id;
				NetworkPlayer runtimePlayer = subroom.GetRuntimePlayer(invalidatedRoomResult.playerId);
				if (runtimePlayer != null && runtimePlayer.PlayerProperties != null)
				{
					byIndex.playerInputType.value = (int)runtimePlayer.PlayerProperties.InputDeviceType;
				}
				if (subroomMode.IsTime())
				{
					byIndex.Fill(invalidatedRoomResult.playerId, invalidatedRoomResult.name, UIUtils.TimeToMSMString(invalidatedRoomResult.raceTime), invalidatedRoomResult.raceProgress, isLocal, invalidatedRoomResult.disqualified);
				}
				else if (subroomMode.IsXDS())
				{
					byIndex.Fill(invalidatedRoomResult.playerId, invalidatedRoomResult.name, DriftControllerXDS.ConvertScoresToResultString(invalidatedRoomResult.points), invalidatedRoomResult.raceProgress, isLocal, invalidatedRoomResult.disqualified);
				}
				else
				{
					byIndex.Fill(invalidatedRoomResult.playerId, invalidatedRoomResult.name, UIUtils.DigitsWithSeparate(invalidatedRoomResult.points), invalidatedRoomResult.raceProgress, isLocal, invalidatedRoomResult.disqualified);
				}
			}
		}

		private void FillCurrentPlaces(SubroomInfo info, List<InvalidatedRoomResult> places)
		{
			if (places.Count != info.playersCount)
			{
				places.Clear();
				for (int i = 0; i < info.playersCount; i++)
				{
					places.Add(new InvalidatedRoomResult());
				}
			}
			int ptr = 0;
			info.PlayersForEach(delegate(PlayerId playerId, string nickName)
			{
				InvalidatedRoomResult invalidatedRoomResult = places[ptr++];
				invalidatedRoomResult.playerId = playerId;
				invalidatedRoomResult.name = nickName;
				invalidatedRoomResult.disqualified = false;
				if (info.TryGetResult(playerId, out var result))
				{
					invalidatedRoomResult.disqualified = result.disqualifyReason != DisqualifyReason.Unknown;
					if (!invalidatedRoomResult.disqualified)
					{
						invalidatedRoomResult.raceProgress = 100;
						invalidatedRoomResult.points = (int)result.points;
						invalidatedRoomResult.raceTime = result.time;
					}
				}
				else
				{
					NetworkPlayer runtimePlayer = info.GetRuntimePlayer(playerId);
					if (runtimePlayer != null)
					{
						invalidatedRoomResult.raceProgress = runtimePlayer.points.raceProgress;
						invalidatedRoomResult.points = runtimePlayer.points.driftPoints;
						invalidatedRoomResult.raceTime = runtimePlayer.points.raceTime;
					}
					else
					{
						invalidatedRoomResult.disqualified = true;
					}
				}
			});
			if (info.subroomMode.IsTime())
			{
				places.Sort(delegate(InvalidatedRoomResult lhs, InvalidatedRoomResult rhs)
				{
					if (CompareByDisqualifed(lhs, rhs, out var result))
					{
						return result;
					}
					if (lhs.raceProgress < rhs.raceProgress)
					{
						return 1;
					}
					if (lhs.raceProgress > rhs.raceProgress)
					{
						return -1;
					}
					float raceTime = lhs.raceTime;
					float raceTime2 = rhs.raceTime;
					if (raceTime < raceTime2)
					{
						return -1;
					}
					return (raceTime > raceTime2) ? 1 : string.Compare(lhs.name, rhs.name);
				});
				return;
			}
			places.Sort(delegate(InvalidatedRoomResult lhs, InvalidatedRoomResult rhs)
			{
				if (CompareByDisqualifed(lhs, rhs, out var result))
				{
					return result;
				}
				if (lhs.points < rhs.points)
				{
					return 1;
				}
				return (lhs.points > rhs.points) ? (-1) : string.Compare(lhs.name, rhs.name);
			});
		}

		private bool CompareByDisqualifed(InvalidatedRoomResult lhs, InvalidatedRoomResult rhs, out int result)
		{
			if (lhs.disqualified && !rhs.disqualified)
			{
				result = 1;
				return true;
			}
			if (!lhs.disqualified && rhs.disqualified)
			{
				result = -1;
				return true;
			}
			if (lhs.disqualified && rhs.disqualified)
			{
				result = string.Compare(lhs.name, rhs.name);
				return true;
			}
			result = 0;
			return false;
		}
	}
}
