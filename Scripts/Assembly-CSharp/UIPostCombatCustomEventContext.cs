using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Binding;
using Binding.Base;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class UIPostCombatCustomEventContext : BaseBindingBehaviourTarget
{
	private RaceMode m_raceMode = RaceMode.Drift;

	private GhostPlayer m_ghostPlayer;

	[Dependency]
	private GamePrefs m_prefs;

	public readonly IntProperty pumpkinsCount = new IntProperty();

	public readonly StringProperty caption = new StringProperty();

	public readonly IntProperty driftPoints = new IntProperty();

	public readonly StringProperty raceTime = new StringProperty();

	public readonly IntProperty coinsReward = new IntProperty();

	public readonly IntProperty expByRace = new IntProperty();

	public readonly StringProperty line3Cost = new StringProperty();

	public readonly StringProperty line3Data = new StringProperty();

	public readonly StringProperty expFactor = new StringProperty();

	public readonly StringProperty coinsFactor = new StringProperty();

	public readonly BoolProperty hasReward = new BoolProperty();

	public readonly BoolProperty hasCup = new BoolProperty();

	public readonly BoolProperty isTimeattack = new BoolProperty(false);

	public readonly BoolProperty driftEventsVisible = new BoolProperty();

	public readonly BoolProperty finishedOutOfTime = new BoolProperty(false);

	public readonly IntProperty eventSpeedCounter = new IntProperty();

	public readonly IntProperty eventSideChangeCounter = new IntProperty();

	public readonly IntProperty eventAngleCounter = new IntProperty();

	public readonly IntProperty eventAccuracyCounter = new IntProperty();

	public readonly IntProperty eventQualityCounter = new IntProperty();

	public readonly IntProperty eventJumpCounter = new IntProperty();

	public readonly IntProperty eventBackwardCounter = new IntProperty();

	public readonly ListProperty challengesList = new ListProperty();

	public readonly BoolProperty challengesListVisible = new BoolProperty();

	public readonly BoolProperty isTimeout = new BoolProperty();

	protected override void OnActivate()
	{
		base.OnActivate();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.mp_battle_end_result.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_exit.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GotoGarage
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = SubmitInput
			}
		});
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer = m_ghostPlayer;
			ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
			GhostPlayer ghostPlayer2 = m_ghostPlayer;
			ghostPlayer2.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer2.onPlayEndEvent, new System.Action(OnPlayEndEvent));
			m_ghostPlayer.Play();
		}
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateFollowCamera();
		}
		m_topPanelContext.BringForwardByDepth();
		m_bottomPanelContext.BringForwardByDepth();
		Overlay.NotifyMPFeature(notify: false);
		HalloweenEventState halloweenEventState = States.mainCurrent as HalloweenEventState;
		if ((bool)halloweenEventState)
		{
			pumpkinsCount.value = halloweenEventState.pumpkinCount;
		}
	}

	private IEnumerator UpdateCupStat()
	{
		yield return null;
	}

	protected override void OnDeactivate()
	{
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer = m_ghostPlayer;
			ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
		base.OnDeactivate();
	}

	public void SetResult(RaceMode raceMode, RaceResult raceResult, BaseRaceModeState.DetailedRaceAward raceAward)
	{
		m_raceMode = raceMode;
		isTimeattack.value = m_raceMode == RaceMode.TimeAttack;
		hasReward.value = m_raceMode != RaceMode.Freeride && m_raceMode != RaceMode.Dyno;
		hasCup.value = m_raceMode == RaceMode.Drift || m_raceMode == RaceMode.TimeAttack;
		finishedOutOfTime.value = raceAward.isOutOfTime;
		raceTime.value = UIUtils.TimeToMSMString(raceAward.raceTimeSpent);
		driftPoints.value = raceAward.raceDriftPoints;
		isTimeout.value = raceAward.isOutOfTime;
		SetCaption(m_raceMode, 0, raceAward.isOutOfTime);
		InitLists(m_raceMode);
		if (hasReward.value && m_raceMode != RaceMode.NetworkFreeride)
		{
			SetupTotalAward(raceAward);
			ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.GainRacePoints, raceAward.raceDriftPoints);
		}
	}

	private void InitLists(RaceMode raceMode)
	{
		driftEventsVisible.value = raceMode != RaceMode.TimeAttack;
		if (driftEventsVisible.value)
		{
			eventSpeedCounter.value = UIDriftEventsContext.countersEvents[1][1];
			eventSideChangeCounter.value = UIDriftEventsContext.countersEvents[1][2];
			eventAngleCounter.value = UIDriftEventsContext.countersEvents[1][3];
			eventAccuracyCounter.value = UIDriftEventsContext.countersEvents[1][4];
			eventQualityCounter.value = UIDriftEventsContext.countersEvents[1][5];
			eventJumpCounter.value = UIDriftEventsContext.countersEvents[1][6];
			eventBackwardCounter.value = UIDriftEventsContext.countersEvents[1][7];
			UIDriftEventsContext.ResetStatsEvents();
		}
		if (raceMode == RaceMode.NetworkFreeride)
		{
			ICollection<NetworkGameStats.SubroomStat> subroomStats = NetworkController.InstanceGame.stats.GetSubroomStats();
			if (subroomStats.Count > 0)
			{
				int num = 0;
				int num2 = 0;
				PostcombatListItem.Data data = new PostcombatListItem.Data
				{
					title = LocalizationKeys.challenges.Localized(),
					value = ""
				};
				challengesList.Add(data);
				foreach (NetworkGameStats.SubroomStat item in subroomStats)
				{
					challengesList.Add(new PostcombatListItem.Data
					{
						title = LocalizationKeys.network_room_mode_.AddLocalized(item.mode.ToString().ToLower()),
						value = $"{item.tryCount}/{item.winCount}"
					});
					num += item.winCount;
					num2 += item.tryCount;
				}
				data.value = $"{num2}/{num}";
			}
		}
		challengesListVisible.value = challengesList.count > 0;
	}

	private void SetupExpAward()
	{
		GameExpController gameExpController = DependencyInjector.Resolve<GameExpController>();
		expByRace.value = Mathf.Max(0, gameExpController.raceExp - gameExpController.GetExpBySource(GameExpController.ExpSource.Drift) - gameExpController.GetExpBySource(GameExpController.ExpSource.Cup));
		gameExpController.Apply();
		float num = m_prefs.expFactor;
		expFactor.value = ((num > 1f) ? $"{num}x" : "");
	}

	private void SetupTotalAward(BaseRaceModeState.DetailedRaceAward raceAward)
	{
		line3Cost.value = raceAward.leftTimeAward.ToString(CultureInfo.InvariantCulture);
		line3Data.value = UIUtils.TimeToMSMString(raceAward.bonusTime);
		coinsReward.value = raceAward.totalAward;
		float num = m_prefs.coinsFactor;
		coinsFactor.value = ((num > 1f) ? $"{num}x" : "");
		SetupExpAward();
	}

	private static string MakeXInfo(int value, string valueInfo, int x)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(value);
		if (!string.IsNullOrEmpty(valueInfo))
		{
			stringBuilder.Append(valueInfo);
		}
		if (x > 1)
		{
			stringBuilder.Append(" x ");
			stringBuilder.Append(x);
		}
		return stringBuilder.ToString();
	}

	private void SetCaption(RaceMode raceMode, int cupIndex, bool isOutOfTime)
	{
		caption.value = (isOutOfTime ? LocalizationKeys.race_result_out_of_time.Localized() : LocalizationKeys.window_title_race_finish.Localized());
	}

	public void SetGhostPlayer(GhostPlayer ghostPlayer)
	{
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer2 = m_ghostPlayer;
			ghostPlayer2.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer2.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
		m_ghostPlayer = ghostPlayer;
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer3 = m_ghostPlayer;
			ghostPlayer3.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer3.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
	}

	private void OnPlayEndEvent()
	{
		if (m_ghostPlayer != null)
		{
			m_ghostPlayer.progress = 0f;
		}
	}

	public void SubmitInput()
	{
		NavigationInputModule.instance.Submit();
	}

	public void GotoGarage()
	{
		States.Change<GarageGUIState>();
	}

	protected override bool OnEscape()
	{
		GotoGarage();
		return true;
	}

	public void OnRetryClick()
	{
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		if ((bool)baseRaceModeState)
		{
			baseRaceModeState.RestartRace();
		}
	}

	public void OnReplayClick()
	{
		HideTopPanel();
		HideBottomPanel();
		base.owner.HideStackAndPush<UIViewingReplayContext>().Init(m_ghostPlayer);
	}
}
