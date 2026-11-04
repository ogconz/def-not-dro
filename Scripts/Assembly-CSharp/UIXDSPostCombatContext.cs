using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarX.Gameplay.ClippingZone;
using DB;
using DI;
using GameInput;
using Quests;
using SyncMultiplayer;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIXDSPostCombatContext : BaseBindingBehaviourTarget
{
	public class ResultItemNode : BaseBindingBehaviourTarget
	{
		public StringProperty sectorName = new StringProperty();

		public StringProperty points = new StringProperty();

		public StringProperty discard = new StringProperty();

		public BoolProperty discardVisible = new BoolProperty();

		public BoolProperty disqualifiedLabelVisable = new BoolProperty();

		public BoolProperty totalMode = new BoolProperty();

		public TransformProperty clipZonesNode = new TransformProperty();

		public void FillFrom(string name, DriftControllerXDS.SectorInfo info, Transform clipZonePrefab)
		{
			totalMode.value = false;
			sectorName.value = name;
			points.value = (info.info.isblank ? " - BLANK - " : DriftControllerXDS.ConvertScoresToResultString(info.GetFinalScores()));
			discard.value = ((info.discardReason == DisqualifyReason.Unknown) ? "" : LocalizationKeys.xds_rsn_.AddLocalized(info.discardReason.ToString().ToLower()));
			discardVisible.value = info.discardReason != DisqualifyReason.Unknown;
			disqualifiedLabelVisable.value = info.discardReason != DisqualifyReason.LeaderCarCollide;
			if ((bool)clipZonesNode.value)
			{
				clipZonePrefab.gameObject.SetActive(value: false);
				for (int i = 0; i < info.clippings.Count; i++)
				{
					BaseClipping clipping = info.clippings[i];
					ClipZoneItemNode orAddComponent = UnityEngine.Object.Instantiate(clipZonePrefab, clipZonesNode.value, worldPositionStays: false).gameObject.GetOrAddComponent<ClipZoneItemNode>();
					orAddComponent.gameObject.SetActive(value: true);
					orAddComponent.FillFrom(clipping);
				}
			}
		}

		public void FillFrom(string name, float totalScore, DisqualifyReason disqualifyReason)
		{
			totalMode.value = true;
			sectorName.value = name;
			points.value = DriftControllerXDS.ConvertScoresToResultString(totalScore);
			discard.value = ((disqualifyReason == DisqualifyReason.Unknown) ? "" : LocalizationKeys.xds_rsn_.AddLocalized(disqualifyReason.ToString().ToLower()));
			discardVisible.value = disqualifyReason != DisqualifyReason.Unknown;
			disqualifiedLabelVisable.value = disqualifyReason != DisqualifyReason.LeaderCarCollide;
		}
	}

	public class ClipZoneItemNode : BaseBindingBehaviourTarget
	{
		public BoolProperty failed = new BoolProperty();

		public void FillFrom(BaseClipping clipping)
		{
			failed.value = clipping.state != BaseClipping.State.Activated;
		}
	}

	[Dependency]
	private TransactionManager m_transactionManager;

	[Dependency]
	private BaseModel m_model;

	[SerializeField]
	private ElementNavigate m_firstSelected;

	[SerializeField]
	private UIPostCombatRewardContext m_rewards;

	public GameObject resultListNode;

	public Transform resultItemPrefab;

	public Transform clipZoneItemPrefab;

	public BoolProperty isTotalResult = new BoolProperty();

	public BoolProperty isContinueAllowed = new BoolProperty();

	private GamePrefs m_prefs;

	private GhostPlayer m_ghostPlayer;

	public StringProperty raceType = new StringProperty();

	public StringProperty playerRole = new StringProperty();

	public StringProperty firstTotalResult = new StringProperty();

	public StringProperty secondTotalResult = new StringProperty();

	public StringProperty totalResult = new StringProperty();

	private bool m_replayAllowed;

	private bool m_continueAllowed;

	private int m_firstRaceExp;

	private int m_secondRaceExp;

	private int m_expGained;

	private GameExpController m_gameExpController;

	private bool m_prevStateTotal;

	private IEnumerator m_restartReplay;

	protected override void OnActivate()
	{
		base.OnActivate();
		if ((bool)m_firstSelected)
		{
			m_firstSelected.Select();
		}
		m_gameExpController = DependencyInjector.Resolve<GameExpController>();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.mp_battle_end_result.Localized());
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		XDSRaceModeState xDSRaceModeState = States.mainCurrent as XDSRaceModeState;
		m_replayAllowed = !xDSRaceModeState.firstRace;
		m_continueAllowed = !xDSRaceModeState.firstRace || xDSRaceModeState.disqualifyReason == DisqualifyReason.Unknown;
		isTotalResult.value = m_prevStateTotal;
		RefreshButtons(xDSRaceModeState);
		m_ghostPlayer = xDSRaceModeState.ghostController.player;
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
		FillResultList();
		firstTotalResult.value = DriftControllerXDS.ConvertScoresToResultString(xDSRaceModeState.firstResult);
		secondTotalResult.value = DriftControllerXDS.ConvertScoresToResultString(UIXDSInfoContext.instance.scores);
		totalResult.value = DriftControllerXDS.ConvertScoresToResultString(xDSRaceModeState.firstResult + UIXDSInfoContext.instance.scores);
		m_topPanelContext.BringForwardByDepth();
		m_bottomPanelContext.BringForwardByDepth();
		playerRole.value = (xDSRaceModeState.firstRace ? LocalizationKeys.xds_ui_leader.Localized() : LocalizationKeys.xds_ui_chaser.Localized());
		raceType.value = (xDSRaceModeState.firstRace ? LocalizationKeys.xds_ui_race1.Localized() : LocalizationKeys.xds_ui_race2.Localized());
		PostActivityFeed(xDSRaceModeState);
	}

	private void RefreshButtons(XDSRaceModeState state)
	{
		List<ActionHintData> list = new List<ActionHintData>();
		if (!state.firstRace && !isTotalResult.value && state.disqualifyReason == DisqualifyReason.Unknown)
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.mp_battle_end_result.Localized(),
				type = ActionType.NavCancelBtnB,
				action = delegate
				{
					GetFinishReward(state, state.raceMode, state.raceAward);
					m_prevStateTotal = true;
					isTotalResult.value = true;
					RefreshButtons(state);
				}
			});
		}
		else
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_exit.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GotoGarage
			});
		}
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_restart.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = delegate
			{
				RestartRace(isFirstRace: true);
			}
		});
		if (!isTotalResult.value && m_replayAllowed)
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_replay.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = OnReplayClick
			});
		}
		if (!isTotalResult.value && m_continueAllowed)
		{
			list.Add(state.firstRace ? new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_continue.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnContinueClick
			} : new ActionHintData
			{
				hint = LocalizationKeys.xds_retry_second_race.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnContinueClick
			});
		}
		ShowBottomPanel(list.ToArray());
	}

	public void InitRewards()
	{
		m_rewards.Init(LocalizationKeys.xds_ui_race_reward.Localized());
	}

	public void CountPartialExpByReward(BaseRaceModeState.DetailedRaceAward reward)
	{
		m_gameExpController.HandleXDSFinishRace(reward.expFinishAward);
		CountTotalRaceExpReward();
	}

	private void OnFinishedEarnedExpReward()
	{
		m_gameExpController.HandleXDSFinishRace(m_firstRaceExp);
		m_gameExpController.Apply();
	}

	private void CountTotalRaceExpReward()
	{
		XDSRaceModeState xDSRaceModeState = States.mainCurrent as XDSRaceModeState;
		if (!(xDSRaceModeState == null))
		{
			int num = (int)m_prefs.expFactor;
			if (xDSRaceModeState.firstRace)
			{
				m_firstRaceExp = m_gameExpController.raceExp;
				m_secondRaceExp = 0;
			}
			else
			{
				m_secondRaceExp = m_gameExpController.raceExp;
			}
			m_expGained = (m_firstRaceExp + m_secondRaceExp) * num;
		}
	}

	private void GetFinishReward(XDSRaceModeState xdsState, RaceMode raceMode, BaseRaceModeState.DetailedRaceAward reward)
	{
		if (xdsState == null || reward == null)
		{
			return;
		}
		OnFinishedEarnedExpReward();
		m_rewards.AddXpReward(m_expGained, (int)m_prefs.expFactor);
		int num = (int)reward.finishAward * (int)m_prefs.coinsFactor;
		if ((int)reward.finishAward > 0)
		{
			m_rewards.AddCoinsReward(num, (int)m_prefs.coinsFactor);
			m_transactionManager.OnRaceEndAward(num);
		}
		if (raceMode != RaceMode.XDS)
		{
			return;
		}
		float num2 = xdsState.firstResult + UIXDSInfoContext.instance.scores;
		QuestController.EventCompleted("sp_xds_total_points", (int)num2, null, getRewards: false);
		List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list = QuestController.EventCompleted("sp_xds_total_points", (int)num2);
		if (list == null)
		{
			return;
		}
		foreach (var item in list)
		{
			foreach (QuestsAsset.QuestReward item2 in item.Item2)
			{
				m_rewards.AddReward(item2.icon, item2.id, item2.count);
			}
		}
	}

	private int GetExpForPostCombat(GameExpController expController)
	{
		XDSRaceModeState obj = States.mainCurrent as XDSRaceModeState;
		int num = (int)m_prefs.expFactor;
		if (obj.firstRace)
		{
			m_firstRaceExp = expController.raceExp;
			m_secondRaceExp = 0;
		}
		else
		{
			m_secondRaceExp = expController.raceExp;
		}
		return (m_firstRaceExp + m_secondRaceExp) * num;
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

	private void PostActivityFeed(XDSRaceModeState state)
	{
		if (state.disqualifyReason <= DisqualifyReason.Unknown)
		{
			int storyType = (state.firstRace ? 6 : 7);
			float num = (state.firstRace ? state.firstResult : (state.firstResult + UIXDSInfoContext.instance.scores));
			ActivityFeedService.ProcessStory((ActivityFeedService.StoryType)storyType, num);
		}
	}

	private void OnPlayEndEvent()
	{
		if (m_ghostPlayer != null)
		{
			m_ghostPlayer.holdLastFrame = true;
			if (m_restartReplay != null)
			{
				TimeController.RemoveCoroutine(m_restartReplay);
				m_restartReplay = null;
			}
			m_restartReplay = TimeController.RunCoroutine(RestartReplay());
		}
	}

	private IEnumerator RestartReplay()
	{
		yield return new WaitForSeconds(3f);
		if (m_ghostPlayer != null && m_ghostPlayer.progress == 1f)
		{
			m_ghostPlayer.progress = 0f;
			m_ghostPlayer.Play();
		}
	}

	public void SubmitInput()
	{
		NavigationInputModule.instance.Submit();
	}

	public void GotoGarage()
	{
		m_prevStateTotal = false;
		if (m_restartReplay != null)
		{
			TimeController.RemoveCoroutine(m_restartReplay);
			m_restartReplay = null;
		}
		States.Change<GarageGUIState>();
	}

	protected override bool OnEscape()
	{
		GotoGarage();
		return true;
	}

	private void OnContinueClick()
	{
		if (m_continueAllowed)
		{
			RestartRace(isFirstRace: false);
		}
	}

	private void RestartRace(bool isFirstRace)
	{
		m_prevStateTotal = false;
		if (m_restartReplay != null)
		{
			TimeController.RemoveCoroutine(m_restartReplay);
			m_restartReplay = null;
		}
		m_gameExpController.Reset();
		HideTopPanel();
		HideBottomPanel();
		Hide();
		if (States.mainCurrent is XDSRaceModeState xDSRaceModeState)
		{
			xDSRaceModeState.DoRestart(isFirstRace);
		}
	}

	public void OnReplayClick()
	{
		if (m_replayAllowed)
		{
			m_prevStateTotal = false;
			SoundController.instance.fxAudioPause = false;
			HideTopPanel();
			HideBottomPanel();
			base.owner.HideStackAndPush<UIViewingReplayContext>().Init(m_ghostPlayer, customEndHandler: true);
		}
	}

	public void FillResultList()
	{
		if ((bool)resultListNode)
		{
			XDSRaceModeState xDSRaceModeState = States.mainCurrent as XDSRaceModeState;
			resultListNode.transform.DestroyChildren();
			DriftControllerXDS controller = UIXDSInfoContext.instance.controller;
			resultItemPrefab.gameObject.SetActive(value: false);
			for (int i = 0; i < controller.sectors.Length - 1; i++)
			{
				DriftControllerXDS.SectorInfo info = controller.sectors[i];
				ResultItemNode orAddComponent = UnityEngine.Object.Instantiate(resultItemPrefab, resultListNode.transform, worldPositionStays: false).gameObject.GetOrAddComponent<ResultItemNode>();
				orAddComponent.gameObject.SetActive(value: true);
				orAddComponent.FillFrom(DriftControllerXDS.GetSectorName(i), info, clipZoneItemPrefab);
			}
			ResultItemNode orAddComponent2 = UnityEngine.Object.Instantiate(resultItemPrefab, resultListNode.transform, worldPositionStays: false).gameObject.GetOrAddComponent<ResultItemNode>();
			orAddComponent2.gameObject.SetActive(value: true);
			orAddComponent2.FillFrom(LocalizationKeys.xds_total.Localized(), controller.score, xDSRaceModeState.disqualifyReason);
		}
	}
}
