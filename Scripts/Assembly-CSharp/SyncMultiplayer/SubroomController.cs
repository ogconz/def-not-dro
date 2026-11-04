using System;
using System.Collections.Generic;
using ArrayExtension;
using Championship;
using GameOverlay;
using JukeBox;
using UnityEngine;

namespace SyncMultiplayer
{
	public abstract class SubroomController
	{
		protected NetworkGame m_game;

		protected NetGameSubroomsSystem m_system;

		public bool iPlayInThisRoom;

		protected float m_waitingStartTimer;

		protected const float WAITING_START_TIMER_CAP = 5f;

		protected float? m_raceDuration;

		private bool m_smokeCleared;

		private SyncNetFreerideRaceModeState m_raceState;

		public Action OnCountdownOver;

		public SubroomInfo room { get; private set; }

		public NetworkPlayer selfPlayer => m_game.LocalPlayer;

		public bool isSelfCreator => selfPlayer.PlayerId == room.creatorId;

		public float raceDuration
		{
			set
			{
				m_raceDuration = value;
			}
		}

		protected SyncNetFreerideRaceModeState raceState
		{
			get
			{
				if (!m_raceState)
				{
					m_raceState = States.mainCurrent as SyncNetFreerideRaceModeState;
				}
				return m_raceState;
			}
		}

		public event Action<SubroomState, SubroomState> OnChangeSubroomState;

		public SubroomController(SubroomInfo roomInfo, NetGameSubroomsSystem system, NetworkGame game)
		{
			room = roomInfo;
			m_game = game;
			m_system = system;
			OnCountdownOver = (Action)Delegate.Combine(OnCountdownOver, new Action(OnCountdownOverHandler));
			OnChangeSubroomState += OnChangeSubroomStateHandler;
		}

		private void OnCountdownOverHandler()
		{
			ResetSplinePositionForCars();
		}

		public virtual void Release(bool releaseCar)
		{
			UnsubscribeChekpoints();
			if (releaseCar)
			{
				ReleaseCar(selfPlayer?.userCar);
			}
			OnCountdownOver = (Action)Delegate.Remove(OnCountdownOver, new Action(OnCountdownOverHandler));
		}

		protected bool ChangeSubroomState(SubroomState state)
		{
			if (room.state == state)
			{
				return false;
			}
			m_smokeCleared = false;
			SubroomState state2 = room.state;
			room.state = state;
			this.OnChangeSubroomState(state, state2);
			foreach (NetworkPlayer runtimePlayer in room.runtimePlayers)
			{
				m_system.CameraUpdateNetworkTrack(room, runtimePlayer);
			}
			return true;
		}

		protected virtual void OnChangeSubroomStateHandler(SubroomState newState, SubroomState prevState)
		{
			if (newState == SubroomState.WaitingStart || newState == SubroomState.WaitingSecondRace)
			{
				m_waitingStartTimer = 5f;
			}
		}

		public virtual void Update()
		{
			RaceCar userCar = selfPlayer.userCar;
			if (room.state != SubroomState.Race && userCar != null)
			{
				userCar.engineController.PrecombatLeft();
			}
			switch (room.state)
			{
			case SubroomState.WaitingPlayers:
				if (isSelfCreator)
				{
					m_system.UpdateNotInformedPlayers_RoomSpawn();
				}
				if (room.IsFullRoom())
				{
					if (iPlayInThisRoom && (bool)selfPlayer.userCar)
					{
						selfPlayer.userCar.lockState = true;
					}
					ChangeSubroomState(SubroomState.WaitingStart);
				}
				else if (room.GetLiveTime() == 0f)
				{
					if (room.playersCount < 1 || (room.playersCount == 1 && !room.subroomMode.AllowedSelfChallenge()))
					{
						if (isSelfCreator)
						{
							m_system.DestroyRoom(room);
							AnalyticsService.MultiplayerRoomCanceled();
							GameLog.Print(AlertLevel.Notify, "Multiplayer", "Room destroyed by no players");
						}
						else
						{
							m_system.InternalDestroyRoom(room);
						}
					}
					else
					{
						if (iPlayInThisRoom && (bool)selfPlayer.userCar)
						{
							selfPlayer.userCar.lockState = true;
						}
						ChangeSubroomState(SubroomState.WaitingStart);
					}
				}
				if (iPlayInThisRoom)
				{
					ClearSmokeOnAllCars();
				}
				break;
			case SubroomState.WaitingStart:
				if (room.playersCount > 1 || room.subroomMode.AllowedSelfChallenge())
				{
					if (isSelfCreator && room.IsAllPlayersReady())
					{
						SelfStartRaceInvoke();
						break;
					}
					if (m_waitingStartTimer > 0f)
					{
						m_waitingStartTimer -= Time.unscaledDeltaTime;
						break;
					}
				}
				m_system.InternalDestroyRoom(room);
				break;
			case SubroomState.Race:
				if (iPlayInThisRoom)
				{
					ClearSmokeOnAllCars();
					if (userCar != null && userCar.lockState)
					{
						userCar.engineController.StartEnginePrecombat();
						int num = 0;
						if (Time.unscaledTime > room.launchTime)
						{
							userCar.lockState = false;
							OnCountdownOver.SafeCall();
						}
						else
						{
							num = (int)Mathf.Max(0f, room.launchTime - Time.unscaledTime) + 1;
						}
						bool flag = true;
						if (userCar != null)
						{
							flag = userCar.lockedByCamera;
						}
						raceState.ShowCountDown(((bool)raceState && !flag) ? num : (-1));
					}
					if (Time.unscaledTime > room.launchTime + room.raceTime)
					{
						DisqualifyYourself(DisqualifyReason.TimedOut);
					}
					break;
				}
				if (m_system.isChampionshipMode)
				{
					int num2 = 0;
					if (Time.unscaledTime < room.launchTime)
					{
						num2 = (int)Mathf.Max(0f, room.launchTime - Time.unscaledTime) + 1;
					}
					raceState.ShowCountDown(raceState ? num2 : (-1));
					if (num2 > 0)
					{
						break;
					}
				}
				ChangeSubroomState(SubroomState.WaitingResults);
				break;
			case SubroomState.WaitingResults:
				if (room.HasAllPlayersResult())
				{
					GameLog.Print(AlertLevel.Notify, "Multiplayer", "Race results: ");
					room.ResultsForEach(delegate(PlayerId playerId, SubroomInfoRaceResult result)
					{
						GameLog.Print(AlertLevel.Notify, "Multiplayer", "---- " + room.GetFilteredNickName(playerId) + " => " + result.asString);
					});
					FinalizeRace();
				}
				else if (Time.unscaledTime > room.launchTime + room.raceTime + 5f)
				{
					GameLog.Print(AlertLevel.Notify, "Multiplayer", "Race> ignore results, timed out");
					FinalizeRace();
				}
				break;
			case SubroomState.Finished:
				if (iPlayInThisRoom)
				{
					SendStats();
				}
				GameLog.Print(AlertLevel.Notify, "Multiplayer", "Room destroyed by finish");
				m_system.InternalDestroyRoom(room);
				break;
			}
		}

		private void ClearSmokeOnAllCars()
		{
			if (room?.runtimePlayers == null || m_smokeCleared)
			{
				return;
			}
			foreach (NetworkPlayer runtimePlayer in room.runtimePlayers)
			{
				if (runtimePlayer != null && !(runtimePlayer.userCar == null))
				{
					runtimePlayer.userCar.ClearSmoke();
				}
			}
			m_smokeCleared = true;
		}

		protected void SelfStartRaceInvoke()
		{
			ChangeSubroomState(SubroomState.Race);
			room.launchTime = Time.unscaledTime + 5f;
			InitStartPositions();
			m_system.SEND_RoomStartRace(room);
			GameLog.Print(AlertLevel.Notify, "Multiplayer", "RaceStart> ignition!");
			AnalyticsService.MultiplayerRoomStarted();
		}

		private void SendStats()
		{
			if (room.validatedResults.Count == 0 || selfPlayer == null)
			{
				return;
			}
			bool num = room.validatedResults[0].playerId == selfPlayer.PlayerId;
			bool flag = room.players.Count > 1;
			if (!(num && flag))
			{
				return;
			}
			StatsManager.AddToStat(StatsManager.EStats.MpWins, 1);
			bool flag2 = StatsManager.IsWinterfallTrackActive();
			if (room.subroomMode == SubroomMode.ClippingDuel && flag2)
			{
				StatsManager.IncStat(StatsManager.EStats.WeatherHindrance);
			}
			if (flag2)
			{
				StatsManager.IncStat(StatsManager.EStats.QuantityQuality);
			}
			room.ResultsForEach(delegate(PlayerId playerId, SubroomInfoRaceResult result)
			{
				if (Overlay.instance.IsFriend(playerId))
				{
					StatsManager.IncStat(StatsManager.EStats.MpFriendWins);
					ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.MPWonRaceVsFriend);
				}
			});
		}

		private void OnCheckPoint(RaceCar car, int lap, int checkpoint)
		{
			if (iPlayInThisRoom)
			{
				RaceCar userCar = selfPlayer.userCar;
				if (!(userCar != car) && checkpoint == 0)
				{
					userCar.useTC = false;
				}
			}
		}

		protected void OnRaceEnd(RaceCar car, int lap, float lapTime, bool isPersonalBestLap, bool isAbsoluteBestLap)
		{
			if (!iPlayInThisRoom)
			{
				return;
			}
			if (States.current is BaseRaceModeState baseRaceModeState)
			{
				baseRaceModeState.UpdateRichPresence();
			}
			RaceCar userCar = selfPlayer.userCar;
			if (selfPlayer.userCar != car)
			{
				return;
			}
			ReleaseCar(userCar);
			if (room.state == SubroomState.Race)
			{
				ChangeSubroomState(SubroomState.WaitingResults);
				SubroomInfoRaceResult result = CalcSelfRaceResult();
				ReceiveResult(selfPlayer, result);
				OnSelfFinished(result);
				m_system.LocalFinished(room, result);
				ReleaseCar(userCar);
				if ((bool)raceState)
				{
					raceState.SetupTimeLimit(0);
					raceState.driftController.ResetClippingQueues();
					raceState.driftController.allowRepeatDrift = true;
					UnsubscribeChekpoints();
				}
			}
		}

		protected void ShowRaceFinish(DisqualifyReason roomRsn)
		{
			if (!roomRsn.IsEmpty())
			{
				if (roomRsn == DisqualifyReason.TimedOut)
				{
					raceState.ShowRaceFinish(string.Empty, timeIsOut: true);
					return;
				}
				string customMsg = LocalizationKeys.xds_rsn_.AddLocalized(room.disqualifyReason.ToString().ToLower());
				raceState.ShowRaceFinish(customMsg, timeIsOut: true);
			}
			else
			{
				raceState.ShowRaceFinish();
			}
		}

		private void UnsubscribeChekpoints()
		{
			if (!(raceState == null))
			{
				CheckpointManager checkpointManager = raceState.checkpointManager;
				checkpointManager.OnLapCompleted -= OnRaceEnd;
				checkpointManager.OnCheckpointReached -= OnCheckPoint;
			}
		}

		private void SubscribeChekpoints()
		{
			if (!(raceState == null))
			{
				UnsubscribeChekpoints();
				CheckpointManager checkpointManager = raceState.checkpointManager;
				checkpointManager.OnLapCompleted += OnRaceEnd;
				checkpointManager.OnCheckpointReached += OnCheckPoint;
			}
		}

		protected virtual void OnSelfFinished(SubroomInfoRaceResult result)
		{
			ShowRaceFinish(room.disqualifyReason);
		}

		protected virtual bool TryFinalizeRace()
		{
			return true;
		}

		protected virtual void OnFinalizeRace()
		{
			ChangeSubroomState(SubroomState.Finished);
		}

		protected void FinalizeRace()
		{
			if (TryFinalizeRace())
			{
				UpdateValidatedResults();
				OnFinalizeRace();
				m_system.InvokeSubroomOperation(room, SubroomOperation.RaceResult);
			}
			RaceCar userCar = selfPlayer.userCar;
			if (iPlayInThisRoom)
			{
				if ((bool)raceState)
				{
					raceState.StopDrift();
				}
				if ((bool)userCar)
				{
					userCar.useTC = false;
					userCar.lockState = false;
				}
			}
		}

		protected virtual void OnValidateResult()
		{
			room.ResultsForEach(delegate(PlayerId playerId, SubroomInfoRaceResult result)
			{
				if (!result.isFinished)
				{
					room.validatedTimedOut++;
				}
				else
				{
					room.validatedResults.Add(new ValidatedRoomResult(playerId, result));
				}
			});
			room.validatedResults.Sort((ValidatedRoomResult lhs, ValidatedRoomResult rhs) => -lhs.result.sortOrder.CompareTo(rhs.result.sortOrder));
		}

		protected void UpdateValidatedResults()
		{
			room.validatedTimedOut = 0;
			room.validatedResults.Clear();
			OnValidateResult();
		}

		protected virtual void InitStartPositions()
		{
			room.positions.Clear();
			room.positions.AddRange(room.playersIds);
			room.positions.Shuffle();
		}

		protected virtual void PrepareCarBeforeStarting(RaceCar userCar)
		{
			if ((bool)userCar)
			{
				userCar.useTC = false;
				userCar.lockState = true;
				userCar.disableRGearTime = room.launchTime + 3f;
				userCar.positionOnSpline.SetDirty();
			}
		}

		protected virtual void PrepareRoomBeforeStarting()
		{
			if (iPlayInThisRoom)
			{
				raceState.SetupTandemLead(null);
			}
			room.tandemLeadId = PlayerId.Invalid;
		}

		public virtual void StartRace(float startTime, List<PlayerId> positions)
		{
			ChangeSubroomState(SubroomState.Race);
			room.launchTime = startTime;
			room.disqualifyReason = DisqualifyReason.Unknown;
			room.positions = positions;
			room.ClearResults();
			if (iPlayInThisRoom)
			{
				if (!(States.current is Top32NetRaceModeState))
				{
					UpdateRichPresenceInRace();
				}
				CameraSwitch.instance.SetCar(PlayerCarControl.instance.car);
				foreach (NetworkPlayer runtimePlayer in room.runtimePlayers)
				{
					if (runtimePlayer.userCar != null)
					{
						runtimePlayer.userCar.positionOnSpline.SetDirty();
						runtimePlayer.userCar.carX.ResetTransmission();
						runtimePlayer.userCar.carX.gear = 1;
					}
				}
				SoundController.Play2DUISoundIfFits(SoundSample.MPSubraceReady, room.launchTime + 2f);
				SoundController.PlayMusic(MusicSignal.Subrace);
				if (!m_raceDuration.HasValue)
				{
					PlaceLocalPlayerAtStart(out var value);
					m_raceDuration = value;
				}
				room.raceTime = m_raceDuration.Value;
				m_system.InvokeSubroomOperation(room, SubroomOperation.StartRace);
				if ((bool)raceState)
				{
					PrepareCarBeforeStarting(selfPlayer.userCar);
					raceState.SetupTimeLimit(int.MaxValue);
					SubscribeChekpoints();
				}
			}
			else
			{
				m_system.InvokeSubroomOperation(room, SubroomOperation.Destroy);
			}
			PrepareRoomBeforeStarting();
		}

		private void UpdateRichPresenceInRace()
		{
			IRichPresence richPresence = Overlay.instance.richPresence;
			richPresence.SetVariable(RichPresenceVariables.SubroomMode, room.subroomMode);
			richPresence.Show(RichPresenceStatus.MultiplayerSubroom);
		}

		protected virtual SubroomInfoRaceResult CalcSelfRaceResult()
		{
			return new SubroomInfoRaceResult(selfPlayer.points.driftPoints, room.disqualifyReason);
		}

		private void ResetSplinePositionForCars()
		{
			foreach (NetworkPlayer runtimePlayer in room.runtimePlayers)
			{
				if (runtimePlayer == null || runtimePlayer.userCar == null || runtimePlayer.userCar.positionOnSpline == null)
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", $"Unable to set player '{runtimePlayer}' on spline!");
				}
				else
				{
					runtimePlayer.userCar.positionOnSpline.SetDirty();
				}
			}
		}

		public virtual void DisqualifyYourself(DisqualifyReason reason)
		{
			if (room.state == SubroomState.Race)
			{
				room.disqualifyReason = reason;
				GameLog.Print(AlertLevel.Notify, "Multiplayer", $"Race> stop by disqualify - reason: {reason}");
				ForceRaceEnd();
			}
		}

		public void ForceRaceEnd()
		{
			if (selfPlayer != null)
			{
				OnRaceEnd(selfPlayer.userCar, 0, 0f, isPersonalBestLap: false, isAbsoluteBestLap: false);
			}
		}

		public void ReceiveResult(NetworkPlayer sender, SubroomInfoRaceResult result)
		{
			room.AddResult(sender, result);
			if (iPlayInThisRoom)
			{
				OnReceiveResult(sender.PlayerId, result);
			}
		}

		protected virtual void OnReceiveResult(PlayerId senderPlayerId, SubroomInfoRaceResult result)
		{
		}

		protected virtual void ReleaseCar(RaceCar car)
		{
			if ((bool)car)
			{
				car.useTC = false;
				car.lockState = false;
				car.SetCurrentTrack(Track.instance);
			}
		}

		protected void PlaceLocalPlayerAtStart(out float raceDuration)
		{
			raceDuration = NetworkSubraceDescriptor.DefaultRaceDuration;
			List<PlayerId> positions = room.positions;
			int num = Mathf.Max(0, positions.IndexOf(selfPlayer.PlayerId));
			SubroomMode subroomMode = room.subroomMode;
			bool isLeader = !subroomMode.IsTandem() || num == 1;
			NetworkSubraceDescriptor.Descriptor descriptor = selfPlayer.Restart(room.pointName, num, num % 2 == 0 && num >= positions.Count - 1, subroomMode.IsXDS(), isLeader, resetStatsEvents: false);
			if (descriptor != null)
			{
				raceDuration = descriptor.raceTime;
			}
			m_game.OnLocalPlayerPlacedOnTrack();
		}
	}
}
