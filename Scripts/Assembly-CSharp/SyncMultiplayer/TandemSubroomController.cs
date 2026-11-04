using System;
using System.Collections.Generic;
using ArrayExtension;
using GameOverlay;
using UnityEngine;

namespace SyncMultiplayer
{
	public class TandemSubroomController : SubroomController
	{
		protected float m_startDelay;

		protected Dictionary<PlayerId, SubroomInfoRaceResult> m_firstResults = new Dictionary<PlayerId, SubroomInfoRaceResult>();

		public float startDelay => Mathf.Max(m_startDelay, 0f);

		public TandemSubroomController(SubroomInfo roomInfo, NetGameSubroomsSystem system, NetworkGame game)
			: base(roomInfo, system, game)
		{
			if (roomInfo.tandemState == TandemState.None)
			{
				roomInfo.tandemState = TandemState.FirstRace;
			}
		}

		protected override void OnReceiveResult(PlayerId senderPlayerId, SubroomInfoRaceResult result)
		{
			m_firstResults[senderPlayerId] = result;
			NetworkPlayer networkPlayer = base.selfPlayer;
			if ((networkPlayer == null || !(networkPlayer.PlayerId == senderPlayerId)) && base.room.tandemLeadId == senderPlayerId && result.disqualifyReason != DisqualifyReason.Unknown)
			{
				ForceRaceEnd();
			}
		}

		protected override SubroomInfoRaceResult CalcSelfRaceResult()
		{
			if (base.room.tandemState == TandemState.SecondRace)
			{
				if (m_firstResults.TryGetValue(base.selfPlayer.PlayerId, out var value))
				{
					return new SubroomInfoRaceResult((int)value.points, base.selfPlayer.points.driftPoints, base.room.disqualifyReason);
				}
				return new SubroomInfoRaceResult(0, base.selfPlayer.points.driftPoints, base.room.disqualifyReason);
			}
			return base.CalcSelfRaceResult();
		}

		public override void Update()
		{
			if (base.room.state == SubroomState.WaitingSecondRace)
			{
				if (base.room.runtimePlayers.Count != 2)
				{
					if (base.room.tandemState == TandemState.FirstRace && iPlayInThisRoom)
					{
						m_system.SelfPlayerLeaveRoom(base.room);
					}
					else
					{
						FinalizeRace();
					}
				}
				else
				{
					if (m_startDelay > 0f)
					{
						m_startDelay -= Time.unscaledDeltaTime;
						return;
					}
					if (base.isSelfCreator)
					{
						SelfStartRaceInvoke();
					}
					else
					{
						if (m_waitingStartTimer > 0f)
						{
							m_waitingStartTimer -= Time.unscaledDeltaTime;
							return;
						}
						FinalizeRace();
					}
				}
			}
			base.Update();
		}

		protected override void PrepareRoomBeforeStarting()
		{
			int count = base.room.positions.Count;
			if (count >= 1)
			{
				PlayerId playerId = base.room.positions[count - 1];
				if (iPlayInThisRoom)
				{
					NetworkPlayer runtimePlayer = base.room.GetRuntimePlayer(playerId);
					RaceCar raceCar = runtimePlayer?.userCar;
					if (raceCar == null && runtimePlayer != null)
					{
						runtimePlayer.OnCarAttached = (Action<NetworkPlayer>)Delegate.Combine(runtimePlayer.OnCarAttached, new Action<NetworkPlayer>(SetupTandemLead));
					}
					else
					{
						base.raceState.SetupTandemLead(raceCar);
					}
				}
				base.room.tandemLeadId = playerId;
			}
			else
			{
				base.PrepareRoomBeforeStarting();
			}
		}

		protected override bool TryFinalizeRace()
		{
			if (base.room.HasAllPlayersResult() && base.room.tandemState == TandemState.FirstRace && base.room.runtimePlayers.Count == base.room.playersCount)
			{
				m_raceDuration = null;
				base.room.tandemLeadId = base.room.positions[0];
				base.room.tandemState = TandemState.SecondRace;
				m_startDelay = 5f;
				if (iPlayInThisRoom)
				{
					ChangeSubroomState(SubroomState.WaitingSecondRace);
				}
				else
				{
					base.room.ClearResults();
					ChangeSubroomState(SubroomState.WaitingSecondRace);
				}
				return false;
			}
			return true;
		}

		protected override void InitStartPositions()
		{
			if (base.room.tandemState == TandemState.SecondRace)
			{
				base.room.positions.Reverse();
				return;
			}
			base.room.positions.Clear();
			if (base.room.subroomFlags.HasFlag(SubroomFlags.LeaderRandom))
			{
				base.room.positions.AddRange(base.room.playersIds);
				base.room.positions.Shuffle();
				return;
			}
			base.room.positions.Add(base.room.creatorId);
			foreach (PlayerId playersId in base.room.playersIds)
			{
				if (playersId != base.room.creatorId)
				{
					base.room.positions.Add(playersId);
				}
			}
			if (base.room.subroomFlags.HasFlag(SubroomFlags.LeaderCreator))
			{
				base.room.positions.Reverse();
			}
		}

		private void SetupTandemLead(NetworkPlayer player)
		{
			if (player != null)
			{
				base.raceState.SetupTandemLead(player.userCar);
				player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(player.OnCarAttached, new Action<NetworkPlayer>(SetupTandemLead));
			}
		}

		public override void Release(bool releaseCar)
		{
			NetworkPlayer networkPlayer = null;
			if (base.room != null)
			{
				_ = base.room.tandemLeadId;
				if (!base.room.tandemLeadId.IsInvalid)
				{
					networkPlayer = base.room.GetRuntimePlayer(base.room.tandemLeadId);
				}
			}
			if (networkPlayer != null)
			{
				NetworkPlayer networkPlayer2 = networkPlayer;
				networkPlayer2.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(networkPlayer2.OnCarAttached, new Action<NetworkPlayer>(SetupTandemLead));
			}
			base.Release(releaseCar);
		}
	}
}
