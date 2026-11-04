using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using DI;
using UnityEngine;

public class NetworkRaceState : BaseRaceState
{
	private ObscuredInt TimeLimit = 60;

	private ObscuredInt m_driftCoinsEarned;

	private int m_driftCoinsStartValue;

	protected DriftController m_driftController;

	[Dependency]
	private MultiplayerManager m_multiplayerManager;

	public Action onQualificationEndEvent;

	public int currentDriftPoints => (int)m_driftCoinsEarned + ((!(m_driftController == null)) ? m_driftController.pointsAndExtraPoints : 0) - m_driftCoinsStartValue;

	public int opponentGhostDriftPoints { get; set; }

	public bool isForcedFinish { get; private set; }

	public bool isBattleRace => m_multiplayerManager.currentState == MultiplayerManager.EMultiplayerState.BattleRace;

	public UIMultiplayerData uIData { get; private set; }

	protected override void AddUserPlayer(List<(GhostData, string, string)> overrideGhostDatas = null)
	{
		if (m_recordedGhostData != null)
		{
			m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
			{
				data = m_recordedGhostData,
				setupCamera = true,
				nickName = m_multiplayerManager.currentUser.nickname,
				profileData = m_prefs.carSettings.GetProfileInfo(m_recordedGhostData.profileId, m_recordedGhostData.carId)
			});
		}
	}

	private void SetupRecorderAndPlayer()
	{
		m_recordedGhostData = null;
		m_ghostRecorder.SetRecordSource(base.userCar, this);
		m_ghostRecorder.Record();
		m_ghostPlayer.progress = 0f;
		m_ghostPlayer.Play();
	}

	private void ConfigureDriftController()
	{
		m_driftController = base.userCar.gameObject.GetOrAddComponent<DriftController>();
		m_driftController.Configure(allowDriftOnOutsideTrack: false, allowRepeatDrift: false, considerWrongWay: true, base.checkpointManager);
		m_driftController.earnedPointsEvent -= OnEarnedPointsEventHandle;
		m_driftCoinsStartValue = UnityEngine.Random.Range(0, 10000);
		m_driftCoinsEarned = m_driftCoinsStartValue;
	}

	public override void OnPaused()
	{
		base.OnPaused();
		base.contextOwner.Get<UICountDownContext>().Pause();
		ShowUI(show: false);
		if (base.contextOwner.IsActive<UIEliminationCountdownContext>())
		{
			base.contextOwner.Hide<UIEliminationCountdownContext>();
		}
	}

	public override void OnResume()
	{
		base.OnResume();
		base.contextOwner.Get<UICountDownContext>().Unpause();
		ShowUI(show: true);
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateRaceCamera();
		}
	}

	private void OnRaceStartHandle(RaceCar car)
	{
		if (car == base.userCar)
		{
			SetControlState(isPlayerControl: true);
			base.userCar.useTC = false;
		}
	}

	private void SetControlState(bool isPlayerControl)
	{
		SetAiControlState(!isPlayerControl);
		SetPlayerControlState(isPlayerControl);
	}

	private void OnEarnedPointsEventHandle(int points)
	{
		m_driftCoinsEarned = (int)m_driftCoinsEarned + points;
	}

	public override void ForceFinishRace()
	{
		isForcedFinish = true;
		OnRaceCompletedHandle();
	}

	public override void EliminationWarning()
	{
		base.contextOwner.Add<UIEliminationCountdownContext>().Play();
	}

	protected override void ShowUI(bool show)
	{
		base.ShowUI(show);
		if (show)
		{
			base.contextOwner.Add<UICountDownContext>();
			base.contextOwner.Add<UIHUDMiniMapContext>();
			base.contextOwner.Add<UIInputOverlayContext>();
		}
		else
		{
			base.contextOwner.Hide<UICountDownContext>();
			base.contextOwner.Hide<UIHUDMiniMapContext>();
			base.contextOwner.Hide<UIInputOverlayContext>();
		}
	}

	public void OnOpponentDriftPointsChanged(int dp)
	{
		if (dp < 0)
		{
			Debug.LogError("NetworkRaceState::OnUserDriftPointsChanged(): incorrect dp.");
		}
		else
		{
			opponentGhostDriftPoints = dp;
		}
	}
}
