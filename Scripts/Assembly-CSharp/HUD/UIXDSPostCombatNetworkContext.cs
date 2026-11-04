using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Binding;
using Binding.Base;
using DB;
using DI;
using GameInput;
using GameOverlay;
using Quests;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
	[GameHotkeyBlocker(true)]
	public class UIXDSPostCombatNetworkContext : BaseBindingBehaviourTarget, IPostCombatNetworkContext
	{
		protected static readonly string ZeroScoreStr = "0.00";

		public TextureProperty leftAvatar = new TextureProperty();

		public TextureProperty rightAvatar = new TextureProperty();

		public StringProperty leftName = new StringProperty();

		public StringProperty rightName = new StringProperty();

		public StringProperty leftTotal = new StringProperty();

		public StringProperty leftTotalLeader = new StringProperty();

		public StringProperty leftTotalSecond = new StringProperty();

		public StringProperty rightTotal = new StringProperty();

		public StringProperty rightTotalLeader = new StringProperty();

		public StringProperty rightTotalSecond = new StringProperty();

		public StringProperty omtTimer = new StringProperty();

		public IntProperty winner = new IntProperty();

		public StringProperty leftDRL = new StringProperty();

		public StringProperty leftDRC = new StringProperty();

		public StringProperty rightDRL = new StringProperty();

		public StringProperty rightDRC = new StringProperty();

		public BoolProperty showRaceHints = new BoolProperty(startValue: true);

		[SerializeField]
		private UIRaceHintsContext m_hints;

		[SerializeField]
		private UIPostCombatRewardContext m_rewardsContext;

		[SerializeField]
		private ContentSizeFitter m_contentSizeFitterFix;

		[Dependency]
		private GamePrefs m_prefs;

		[Dependency]
		private BaseModel m_model;

		private NetGameSubroomsSystem m_subroomsSystem;

		private UIRaceNetworkModeContext m_raceNetworkModeContext;

		private SubroomInfo m_subroom;

		private float m_timerOMT;

		private void Awake()
		{
			if (m_hints == null)
			{
				m_hints = GetComponentInChildren<UIRaceHintsContext>();
			}
			m_subroomsSystem = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			BringForwardByDepth();
			if (m_raceNetworkModeContext == null)
			{
				m_raceNetworkModeContext = base.owner.Get<UIRaceNetworkFreerideModeContext>();
			}
			if ((bool)PlayerCarControl.instance)
			{
				PlayerCarControl.instance.SetBlockControl(value: true);
			}
		}

		protected override void OnDeactivate()
		{
			if (m_raceNetworkModeContext != null)
			{
				m_raceNetworkModeContext.SetShowDriftHUDValue(value: true);
			}
			if ((bool)PlayerCarControl.instance)
			{
				PlayerCarControl.instance.SetBlockControl(value: false);
			}
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		public void Close()
		{
			Hide();
		}

		public void Cancel()
		{
			if (m_raceNetworkModeContext != null)
			{
				m_raceNetworkModeContext.SetShowDriftHUDValue(value: true);
			}
			if (m_subroom != null)
			{
				m_subroomsSystem.SelfPlayerLeaveRoom(m_subroom);
			}
			ResetOMT(string.Empty);
		}

		private void Update()
		{
			if (m_subroom == null)
			{
				return;
			}
			if (!m_subroomsSystem.ContainsRoom(m_subroom))
			{
				ResetOMT(LocalizationKeys.xds_rsn_.AddLocalized(DisqualifyReason.PlayerOut.ToString().ToLower()));
			}
			else if (m_subroom.state == SubroomState.WaitingSecondRace)
			{
				if (m_timerOMT > 0f)
				{
					m_timerOMT -= Time.unscaledDeltaTime;
					omtTimer.value = UIUtils.DigitsWithSeparate(Mathf.CeilToInt(Mathf.Max(m_timerOMT, 0.1f)));
				}
				else if (m_timerOMT < 0f)
				{
					m_timerOMT = 0f;
					omtTimer.value = LocalizationKeys.network_room_state_waitingstart.Localized();
				}
			}
			else
			{
				Close();
			}
		}

		private void ResetOMT(string omtTimerStr)
		{
			m_subroom = null;
			omtTimer.value = omtTimerStr;
			RefreshHints(clearOnly: false);
		}

		public void Fill(SubroomInfo info, bool showRaceHints = true, bool top32 = false)
		{
			this.showRaceHints.value = showRaceHints;
			m_subroom = null;
			bool wonByQual;
			(ValidatedRoomResult, ValidatedRoomResult)? tuple = InternalFillData(info, out wonByQual);
			if (!tuple.HasValue)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "UIXDSPostCombat error while retriving validated results");
				Hide();
				return;
			}
			RefreshHints(!showRaceHints);
			List<ValidatedRoomResult> allPlayersValidatedResult = info.GetAllPlayersValidatedResult();
			ValidatedRoomResult validatedRoomResult = allPlayersValidatedResult.Find((ValidatedRoomResult r) => r.playerId.Equals(Overlay.playerId));
			int num = allPlayersValidatedResult.FindIndex((ValidatedRoomResult r) => r.playerId.Equals(Overlay.playerId));
			if (validatedRoomResult == null)
			{
				return;
			}
			if (wonByQual && top32)
			{
				num = ((!(tuple.Value.Item1.playerId == Overlay.playerId)) ? 1 : 0);
			}
			int num2 = CalculateExperience(info.subroomMode, num);
			NetworkController.InstanceGame.stats.xp += num2;
			m_prefs.playerProfile.exp += num2;
			GameLog.Print(AlertLevel.Notify, "PlayerProfile", $"(exp) net race finished, exp for position ({num + 1}) is {num2}");
			if (m_rewardsContext == null)
			{
				return;
			}
			m_rewardsContext.Init();
			List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list = CompleteQuests(validatedRoomResult, info, num + 1);
			if (list != null)
			{
				foreach (var item in list)
				{
					foreach (QuestsAsset.QuestReward item2 in item.Item2)
					{
						m_rewardsContext.AddReward(item2.icon, item2.id, item2.count);
					}
				}
			}
			RefreshLayout();
		}

		private int CalculateExperience(SubroomMode mode, int position)
		{
			ReadOnlyCollection<int> mp_challengeList = m_prefs.gameSettings.expSettings.mp_challengeList;
			int result = 0;
			switch (mode)
			{
			case SubroomMode.Challenge:
			case SubroomMode.TimeAttack:
			case SubroomMode.XDS:
				if (position + 1 < mp_challengeList.Count)
				{
					result = mp_challengeList[position];
				}
				break;
			case SubroomMode.ClippingDuel:
			case SubroomMode.Tandem:
			case SubroomMode.TandemXDS:
				if (position == 0)
				{
					result = mp_challengeList[0];
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case SubroomMode.ShowTrack:
				break;
			}
			return result;
		}

		private List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> CompleteQuests(ValidatedRoomResult result, SubroomInfo info, int finishPosition)
		{
			if (result == null || info.subroomMode != SubroomMode.TandemXDS || info.state == SubroomState.WaitingSecondRace)
			{
				return null;
			}
			SubroomInfoRaceResult.XDSResultDetails xDSResultDetails = result.result.GetXDSResultDetails();
			float.TryParse(xDSResultDetails.tl, out var result2);
			float.TryParse(xDSResultDetails.tc, out var result3);
			float num = ((result2 > result3) ? result2 : result3);
			float num2 = result2 + result3;
			List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list = QuestController.EventCompleted("mp_any_race_finish", result.result.isFinished ? 1 : 0, null, getRewards: false);
			list.AddRange(QuestController.EventCompleted("mp_tandem_xds_win", finishPosition));
			list.AddRange(QuestController.EventCompleted("mp_tandem_xds_points_single_run", (int)num));
			list.AddRange(QuestController.EventCompleted("mp_tandem_xds_points_total", (int)num2));
			return list;
		}

		protected virtual (ValidatedRoomResult left, ValidatedRoomResult right)? InternalFillData(SubroomInfo info, out bool wonByQual)
		{
			wonByQual = false;
			List<ValidatedRoomResult> validatedResults = new List<ValidatedRoomResult>();
			info.ResultsForEach(delegate(PlayerId playerId, SubroomInfoRaceResult result)
			{
				validatedResults.Add(new ValidatedRoomResult(playerId, result));
			});
			if (validatedResults.Count != 2)
			{
				return null;
			}
			if (validatedResults[0].playerId == info.tandemLeadId)
			{
				validatedResults.Reverse();
			}
			if (info.validatedResults.Count == 0)
			{
				winner.value = 0;
				m_subroom = info;
				if (m_subroomsSystem.TryGetRoomCtrl(info.id, out var ctrl))
				{
					if (ctrl is TandemSubroomController tandemSubroomController)
					{
						m_timerOMT = tandemSubroomController.startDelay;
					}
				}
				else
				{
					m_timerOMT = 5f;
				}
			}
			else if (validatedResults[0].playerId == info.validatedResults[0].playerId)
			{
				winner.value = -1;
			}
			else
			{
				winner.value = 1;
			}
			ValidatedRoomResult validatedRoomResult = validatedResults[0];
			ValidatedRoomResult validatedRoomResult2 = validatedResults[1];
			SubroomInfoRaceResult.XDSResultDetails xDSResultDetails = validatedRoomResult.result.GetXDSResultDetails();
			SubroomInfoRaceResult.XDSResultDetails xDSResultDetails2 = validatedRoomResult2.result.GetXDSResultDetails();
			if (xDSResultDetails.s.Count != xDSResultDetails2.s.Count)
			{
				xDSResultDetails.s.Clear();
				xDSResultDetails2.s.Clear();
			}
			FilterResultNickname(leftName, validatedRoomResult, info);
			ResolveAvatar(leftAvatar, validatedRoomResult.playerId);
			ValidReasonOrScore(leftTotalLeader, leftDRL, xDSResultDetails.drl, xDSResultDetails.tl);
			ValidReasonOrScore(leftTotalSecond, leftDRC, xDSResultDetails.drc, xDSResultDetails.tc);
			leftTotal.value = GetTotalScoreStr(xDSResultDetails, validatedRoomResult.result);
			FilterResultNickname(rightName, validatedRoomResult2, info);
			ResolveAvatar(rightAvatar, validatedRoomResult2.playerId);
			ValidReasonOrScore(rightTotalLeader, rightDRL, xDSResultDetails2.drl, xDSResultDetails2.tl);
			ValidReasonOrScore(rightTotalSecond, rightDRC, xDSResultDetails2.drc, xDSResultDetails2.tc);
			rightTotal.value = GetTotalScoreStr(xDSResultDetails2, validatedRoomResult2.result);
			return (validatedRoomResult, validatedRoomResult2);
		}

		private void RefreshHints(bool clearOnly)
		{
			if (!m_hints)
			{
				return;
			}
			m_hints.Clear();
			if (!clearOnly)
			{
				if (m_subroom != null)
				{
					m_hints.AddElement(LocalizationKeys.Cancel.Localized(), ActionType.NavCancelBtnB, Cancel);
				}
				else
				{
					m_hints.AddElement(LocalizationKeys.nav_hint_exit.Localized(), ActionType.NavCancelBtnB, Close);
				}
			}
		}

		private void RefreshLayout()
		{
			if (m_contentSizeFitterFix != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_contentSizeFitterFix.transform);
			}
		}

		protected virtual bool IsValidResult(SubroomInfoRaceResult.XDSResultDetails datails)
		{
			if (datails.drl.IsEmpty() && datails.drc.IsEmpty() && !OpponentHadDQ(datails.tl))
			{
				return !OpponentHadDQ(datails.tc);
			}
			return false;
		}

		protected virtual void ValidReasonOrScore(StringProperty scoreSp, StringProperty drSp, DisqualifyReason reason, string score)
		{
			if (reason != DisqualifyReason.Unknown)
			{
				drSp.value = LocalizationKeys.xds_rsn_.AddLocalized(reason.ToString().ToLower());
				scoreSp.value = string.Empty;
			}
			else if (OpponentHadDQ(score))
			{
				string value = (scoreSp.value = string.Empty);
				drSp.value = value;
			}
			else
			{
				drSp.value = string.Empty;
				scoreSp.value = ValidResultStr(score);
			}
		}

		protected virtual string GetTotalScoreStr(SubroomInfoRaceResult.XDSResultDetails rd, SubroomInfoRaceResult result)
		{
			if (!IsValidResult(rd))
			{
				return string.Empty;
			}
			return result.asString;
		}

		protected bool OpponentHadDQ(string scoreStr)
		{
			if (!string.IsNullOrEmpty(scoreStr))
			{
				return scoreStr.Contains(SubroomInfoRaceResult.XDSResultDetails.OpponentHasDQStr);
			}
			return true;
		}

		private void FilterResultNickname(StringProperty stringProperty, ValidatedRoomResult roomResult, SubroomInfo info)
		{
			if (info != null)
			{
				stringProperty.value = info.GetFilteredNickName(roomResult.playerId);
			}
		}

		private void ResolveAvatar(TextureProperty prop, PlayerId playerId)
		{
			if (playerId == NetworkController.InstanceGame.LocalPlayer.PlayerId)
			{
				prop.value = GameManager.GetMainPlayerAvatarNotNull();
				return;
			}
			Overlay.instance.RequestUserAvatarAndName(playerId, delegate(PlayerId id, string name, Texture2D texture)
			{
				prop.value = texture;
			}, AvatarSize.Normal);
		}

		protected static string ValidResultStr(string str)
		{
			if (!string.IsNullOrEmpty(str))
			{
				return str;
			}
			return ZeroScoreStr;
		}
	}
}
