using System.Collections;
using System.Collections.Generic;
using GameOverlay;
using UnityEngine;

namespace SyncMultiplayer
{
	public class TandemXDSSubroomController : TandemSubroomController
	{
		private Coroutine m_badConnectionDQCoroutine;

		public bool isSafeMode => !base.room.subroomFlags.HasFlag(SubroomFlags.XDSCollide);

		public TandemXDSSubroomController(SubroomInfo roomInfo, NetGameSubroomsSystem system, NetworkGame game)
			: base(roomInfo, system, game)
		{
		}

		protected override SubroomInfoRaceResult CalcSelfRaceResult()
		{
			bool isLeader = base.selfPlayer.PlayerId == base.room.tandemLeadId;
			if (base.room.tandemState == TandemState.SecondRace && m_firstResults.TryGetValue(base.selfPlayer.PlayerId, out var value))
			{
				return value.CombineWithSecondTandemRace(UIXDSInfoContext.instance.controller, base.room.disqualifyReason, isLeader);
			}
			return new SubroomInfoRaceResult(UIXDSInfoContext.instance.controller, base.room.disqualifyReason, isLeader);
		}

		public override void StartRace(float startTime, List<PlayerId> positions)
		{
			if (iPlayInThisRoom)
			{
				UIXDSInfoContext.instance.PrepareForStart(startTime - Time.unscaledTime);
			}
			base.StartRace(startTime, positions);
		}

		protected override void OnReceiveResult(PlayerId senderPlayerId, SubroomInfoRaceResult result)
		{
			m_firstResults[senderPlayerId] = result;
			NetworkPlayer networkPlayer = base.selfPlayer;
			if (networkPlayer != null && networkPlayer.PlayerId == senderPlayerId)
			{
				return;
			}
			if (base.room.tandemLeadId == senderPlayerId)
			{
				if (result.disqualifyReason == DisqualifyReason.Unknown)
				{
					return;
				}
				if (result.disqualifyReason == DisqualifyReason.ChaserCarCollide)
				{
					if (DriftControllerXDS.instance.isLeaderCritBrakingDetected)
					{
						UIXDSInfoContext.instance.DiscardRace();
						DisqualifyYourself(result.disqualifyReason);
					}
					else
					{
						UIXDSInfoContext.instance.DiscardRace();
						DisqualifyYourself(DisqualifyReason.LeaderCarCollide);
					}
				}
				else
				{
					DiscardRace(discardScore: false);
				}
			}
			else if (!result.disqualifyReason.IsEmpty() && !isSafeMode)
			{
				if (result.disqualifyReason.IsChaserGuilty())
				{
					DiscardRace();
				}
				else if (result.disqualifyReason.IsLeaderGuilty() && DriftControllerXDS.instance.isLeaderCritBrakingDetected)
				{
					UIXDSInfoContext.instance.DiscardRace();
					DisqualifyYourself(DisqualifyReason.LeaderCarCollide);
				}
			}
		}

		protected override void OnSelfFinished(SubroomInfoRaceResult result)
		{
			UIXDSInfoContext.instance.HoldResult();
			SubroomInfoRaceResult value2;
			if (base.room.tandemLeadId == base.selfPlayer.PlayerId)
			{
				if (!isSafeMode)
				{
					bool flag = result.disqualifyReason.IsChaserGuilty();
					if (!flag)
					{
						PlayerId key = base.room.positions.Find((PlayerId x) => x != base.room.tandemLeadId);
						if (m_firstResults.TryGetValue(key, out var value) && value.disqualifyReason.IsChaserGuilty() && value.disqualifyReason != DisqualifyReason.RaceStopped)
						{
							flag = true;
						}
					}
					if (flag)
					{
						result.disqualifyReason = DisqualifyReason.RaceStopped;
						result.isFinished = true;
						base.raceState.ShowRaceFinish(LocalizationKeys.tandem_opponent_disqualified.Localized());
						return;
					}
				}
			}
			else if ((base.room.disqualifyReason.IsEmpty() || base.room.disqualifyReason.IsLeaderGuilty()) && m_firstResults.TryGetValue(base.room.tandemLeadId, out value2) && !value2.disqualifyReason.IsEmpty())
			{
				base.raceState.ShowRaceFinish(LocalizationKeys.tandem_opponent_disqualified.Localized());
				return;
			}
			base.OnSelfFinished(result);
		}

		private void DiscardRace(bool discardScore = true)
		{
			if ((bool)UIXDSInfoContext.instance)
			{
				UIXDSInfoContext.instance.DiscardRace(discardScore);
			}
			ForceRaceEnd();
		}

		protected override void OnValidateResult()
		{
			if (!iPlayInThisRoom)
			{
				GameLog.Print(AlertLevel.Notify, "Multiplayer", "Exiting OnValidateResult - we are not a participant of this subroom!");
				return;
			}
			DriftControllerXDS.instance.DiscardRace();
			foreach (PlayerId playersId in base.room.playersIds)
			{
				if (!base.room.TryGetResult(playersId, out var result) || result.disqualifyReason == DisqualifyReason.PlayerOut)
				{
					if (m_firstResults.TryGetValue(playersId, out result))
					{
						result = result.CombineWithSecondTandemRace(DriftControllerXDS.instance, DisqualifyReason.PlayerOut, playersId == base.room.tandemLeadId);
						base.room.AddResult(playersId, result);
					}
					else
					{
						result = new SubroomInfoRaceResult(DriftControllerXDS.instance, DisqualifyReason.PlayerOut, playersId == base.room.tandemLeadId);
						base.room.AddResult(playersId, result);
					}
				}
			}
			if (base.room.tandemState == TandemState.FirstRace)
			{
				base.room.tandemState = TandemState.SecondRace;
				base.room.tandemLeadId = base.room.positions[0];
			}
			ValidatedRoomResult left = null;
			ValidatedRoomResult right = null;
			base.room.ResultsForEach(delegate(PlayerId playerId, SubroomInfoRaceResult result2)
			{
				if (playerId == base.room.tandemLeadId)
				{
					right = new ValidatedRoomResult(playerId, result2);
				}
				else
				{
					left = new ValidatedRoomResult(playerId, result2);
				}
			});
			if (left == null || right == null)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "TandemXDS - not validate result");
				return;
			}
			left.result.isFinished = false;
			right.result.isFinished = false;
			SubroomInfoRaceResult.XDSResultDetails xDSResultDetails = left.result.GetXDSResultDetails();
			SubroomInfoRaceResult.XDSResultDetails xDSResultDetails2 = right.result.GetXDSResultDetails();
			int num = 0;
			int num2 = 0;
			if (!isSafeMode)
			{
				if (xDSResultDetails.drl.IsChaserGuilty())
				{
					if (xDSResultDetails2.drc.IsLeaderGuilty())
					{
						xDSResultDetails.drl = xDSResultDetails2.drc;
						xDSResultDetails2.drc = DisqualifyReason.Unknown;
					}
					else if (xDSResultDetails2.drc.IsChaserGuilty() || xDSResultDetails2.drc.IsEmpty())
					{
						xDSResultDetails2.drc = xDSResultDetails.drl;
						xDSResultDetails.drl = DisqualifyReason.Unknown;
					}
				}
				else if (xDSResultDetails.drl.IsLeaderGuilty())
				{
					if (xDSResultDetails2.drc.IsChaserGuilty())
					{
						xDSResultDetails.drl = DisqualifyReason.Unknown;
					}
					else if (xDSResultDetails2.drc.IsLeaderGuilty())
					{
						xDSResultDetails2.drc = DisqualifyReason.Unknown;
					}
				}
				else if (xDSResultDetails2.drc.IsLeaderGuilty())
				{
					xDSResultDetails.drl = xDSResultDetails2.drc;
					xDSResultDetails2.drc = DisqualifyReason.Unknown;
				}
				if (xDSResultDetails2.drl.IsChaserGuilty())
				{
					if (xDSResultDetails.drc.IsLeaderGuilty())
					{
						xDSResultDetails2.drl = xDSResultDetails.drc;
						xDSResultDetails.drc = DisqualifyReason.Unknown;
					}
					else if (xDSResultDetails.drc.IsChaserGuilty() || xDSResultDetails.drc.IsEmpty())
					{
						xDSResultDetails.drc = xDSResultDetails2.drl;
						xDSResultDetails2.drl = DisqualifyReason.Unknown;
					}
				}
				else if (xDSResultDetails2.drl.IsLeaderGuilty())
				{
					if (xDSResultDetails.drc.IsChaserGuilty())
					{
						xDSResultDetails2.drl = DisqualifyReason.Unknown;
					}
					else if (xDSResultDetails.drc.IsLeaderGuilty())
					{
						xDSResultDetails.drc = DisqualifyReason.Unknown;
					}
				}
				else if (xDSResultDetails.drc.IsLeaderGuilty())
				{
					xDSResultDetails2.drl = xDSResultDetails.drc;
					xDSResultDetails.drc = DisqualifyReason.Unknown;
				}
			}
			if (xDSResultDetails.drl == DisqualifyReason.Unknown)
			{
				num++;
				if (!isSafeMode && xDSResultDetails2.drc.IsChaserGuilty())
				{
					SetOpponentHasDQ(ref xDSResultDetails.tl);
				}
			}
			if (xDSResultDetails.drc == DisqualifyReason.Unknown)
			{
				num++;
				if (xDSResultDetails2.drl != DisqualifyReason.Unknown)
				{
					SetOpponentHasDQ(ref xDSResultDetails.tc);
				}
			}
			if (xDSResultDetails2.drl == DisqualifyReason.Unknown)
			{
				num2++;
				if (!isSafeMode && xDSResultDetails.drc.IsChaserGuilty())
				{
					SetOpponentHasDQ(ref xDSResultDetails2.tl);
				}
			}
			if (xDSResultDetails2.drc == DisqualifyReason.Unknown)
			{
				num2++;
				if (xDSResultDetails.drl != DisqualifyReason.Unknown)
				{
					SetOpponentHasDQ(ref xDSResultDetails2.tc);
				}
			}
			left.result.SetXDSResultDatails(xDSResultDetails);
			right.result.SetXDSResultDatails(xDSResultDetails2);
			if (num == num2)
			{
				bool flag = xDSResultDetails.drl.IsEmpty() && xDSResultDetails2.drl.IsEmpty() && TotalScoreIsValid(xDSResultDetails.tl) && TotalScoreIsValid(xDSResultDetails2.tl);
				if (States.mainCurrent is SyncNetFreerideRaceModeState { doNotAllowOMT: not false } || (flag && left.result.points != right.result.points))
				{
					left.result.isFinished = true;
					right.result.isFinished = true;
					base.room.validatedResults.Add(left);
					base.room.validatedResults.Add(right);
					base.room.validatedResults.Sort((ValidatedRoomResult lhs, ValidatedRoomResult rhs) => -lhs.result.sortOrder.CompareTo(rhs.result.sortOrder));
				}
			}
			else if (num > num2)
			{
				left.result.isFinished = true;
				base.room.validatedResults.Add(left);
				base.room.validatedResults.Add(right);
			}
			else
			{
				right.result.isFinished = true;
				base.room.validatedResults.Add(right);
				base.room.validatedResults.Add(left);
			}
			static void SetOpponentHasDQ(ref string score)
			{
				score += SubroomInfoRaceResult.XDSResultDetails.OpponentHasDQStr;
			}
			static bool TotalScoreIsValid(string totalScore)
			{
				if (string.IsNullOrEmpty(totalScore))
				{
					return false;
				}
				return !totalScore.Contains(SubroomInfoRaceResult.XDSResultDetails.OpponentHasDQStr);
			}
		}

		protected override void OnFinalizeRace()
		{
			if (base.room.validatedResults.Count != 0 || base.room.runtimePlayers.Count != 2)
			{
				base.OnFinalizeRace();
				return;
			}
			m_raceDuration = null;
			if (base.room.settings.xdsFlags.HasFlag(SubroomFlags.LeaderRandom))
			{
				base.room.settings.xdsFlags &= ~SubroomFlags.LeaderRandom;
				if (base.room.tandemLeadId == base.room.creatorId)
				{
					base.room.settings.xdsFlags |= SubroomFlags.LeaderClient;
				}
				else
				{
					base.room.settings.xdsFlags |= SubroomFlags.LeaderCreator;
				}
			}
			base.room.tandemState = TandemState.FirstRace;
			m_startDelay = 10f;
			if (iPlayInThisRoom)
			{
				ChangeSubroomState(SubroomState.WaitingSecondRace);
				return;
			}
			base.room.ClearResults();
			ChangeSubroomState(SubroomState.WaitingSecondRace);
		}

		public override void Update()
		{
			base.Update();
			if (iPlayInThisRoom && base.room.disqualifyReason.IsEmpty() && base.room.state == SubroomState.Race)
			{
				CheckConnectionState();
			}
		}

		private void CheckConnectionState()
		{
			if (m_badConnectionDQCoroutine == null && base.selfPlayer != null && (base.selfPlayer.BadConnection || base.selfPlayer.VeryBadConnection))
			{
				float delaySec = 0f;
				m_badConnectionDQCoroutine = TimeController.StartNow(DelayedBadConnectionDQ(delaySec, null));
			}
		}

		private IEnumerator DelayedBadConnectionDQ(float delaySec, int? msgHash)
		{
			while (true)
			{
				if (base.selfPlayer == null || (!base.selfPlayer.BadConnection && !base.selfPlayer.VeryBadConnection))
				{
					m_badConnectionDQCoroutine = null;
					yield break;
				}
				if (base.selfPlayer.VeryBadConnection)
				{
					break;
				}
				yield return null;
			}
			if (delaySec > 0f)
			{
				yield return new WaitForSecondsRealtime(delaySec);
			}
			if (msgHash.HasValue)
			{
				GUICommonGodVoice.HideTextWithHash(msgHash.Value);
			}
			if (base.selfPlayer != null && base.selfPlayer.VeryBadConnection && iPlayInThisRoom && base.room != null && base.room.disqualifyReason.IsEmpty() && base.room.state == SubroomState.Race && DriftControllerXDS.instance != null)
			{
				DriftControllerXDS.instance.DisqualifyYourself(DisqualifyReason.BadConnection);
			}
			m_badConnectionDQCoroutine = null;
		}

		public override void Release(bool releaseCar)
		{
			base.Release(releaseCar);
			if (m_badConnectionDQCoroutine != null)
			{
				TimeController.StopNow(m_badConnectionDQCoroutine);
				m_badConnectionDQCoroutine = null;
			}
		}
	}
}
