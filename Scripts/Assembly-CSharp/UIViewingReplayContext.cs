using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using PrefsModels;
using UnityEngine;

public class UIViewingReplayContext : BaseBindingBehaviourTarget
{
	private enum RewindDirectionType
	{
		None = 0,
		Right = 1,
		Left = 2
	}

	public System.Action onEnableTelemetry;

	public const float REWIND_TIME_SCALE = 4f;

	private const float HIDDEN_REPLAY_PRESS_PERIOD = 0.1f;

	private const float INTERVAL_TIME_GAP = 0.05f;

	private BoolProperty m_replayPlaying = new BoolProperty();

	private FloatProperty m_progress = new FloatProperty();

	private bool m_forceContinuePlaying;

	private RewindDirectionType m_currentRewindStatus;

	private GhostPlayer m_ghostPlayer;

	private InputManager m_inputManager;

	private bool m_droneModeActivated;

	private bool m_photoModeActivated;

	private bool m_isGhostCalculating;

	private float m_dtSliderProgress;

	private float m_holdedProgress;

	private float m_hiddenReplayPressedTime;

	private float m_timeButtonPressed;

	private bool m_pointerPressed;

	private bool m_pointerReleased;

	private bool m_pointerDownHolded;

	private bool m_contextChanged;

	private List<ActionHintData> m_actionsHint = new List<ActionHintData>();

	private bool isSliderPressed
	{
		get
		{
			if (!m_pointerPressed)
			{
				return m_pointerDownHolded;
			}
			return true;
		}
	}

	public void Init(GhostPlayer ghostPlayer, bool customEndHandler = false)
	{
		m_ghostPlayer = ghostPlayer;
		if (m_ghostPlayer == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "GhostPlayer is null");
			return;
		}
		m_replayPlaying.value = m_ghostPlayer.isPlaying;
		if (!customEndHandler)
		{
			GhostPlayer ghostPlayer2 = m_ghostPlayer;
			ghostPlayer2.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer2.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
		m_ghostPlayer.progress = 0f;
		m_ghostPlayer.ClearEffects();
		m_ghostPlayer.Play();
	}

	protected override void OnActivate()
	{
		if (m_contextChanged)
		{
			RefreshContextOnExit();
			m_contextChanged = false;
		}
		m_progress.OnValueChanged += OnProgressChanged;
		base.OnActivate();
		HideTopPanel();
		BasePrefsModel.LockSaves(flag: true);
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateReplayCamera();
		}
		if (m_inputManager == null)
		{
			m_inputManager = InputManager.instance;
		}
		m_replayPlaying.value = m_ghostPlayer?.isPlaying ?? true;
		RefreshHints();
		DisableMouseInputFlags();
	}

	private void RefreshHints()
	{
		m_actionsHint.Clear();
		m_actionsHint.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
		if (!XRModeManager.IsXRActive())
		{
			m_actionsHint.Add(new ActionHintData
			{
				hint = LocalizationKeys.menu_pause_photosession.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = GoToPhotoMode
			});
		}
		m_actionsHint.Add(new ActionHintData
		{
			hint = LocalizationKeys.input_action_nextcamera.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = NextReplayCamera
		});
		m_actionsHint.Add(new ActionHintData
		{
			hint = (m_replayPlaying.value ? LocalizationKeys.menu_pause_title : LocalizationKeys.replay_play).Localized(),
			type = ActionType.NavSelectBtnA,
			action = TogglePause
		});
		m_actionsHint.Add(new ActionHintData
		{
			hint = LocalizationKeys.free_camera_button.Localized(),
			type = ActionType.NavBtnExpand,
			action = GoToDroneMode
		});
		if (0 == 0)
		{
			ActionHintData item = new ActionHintData
			{
				hint = LocalizationKeys.input_action_.AddLocalized(ActionType.HideUI.ToString().ToLower()),
				type = ActionType.HideUI,
				action = HideUI
			};
			if ((1u & ((m_inputManager != null && !m_inputManager.GetActionBinds(ActionType.HideUI).Equals("NONE")) ? 1u : 0u)) != 0)
			{
				m_actionsHint.Add(item);
			}
		}
		ShowBottomPanel(m_actionsHint.ToArray());
	}

	private void ShowTelemetry()
	{
		onEnableTelemetry.SafeCall();
	}

	private void RefreshContextOnExit()
	{
		if (m_ghostPlayer == null)
		{
			GameLog.Print(AlertLevel.Error, "System", $"{base.owner.topContext}.onDeactivateContext: GhostPlayer is null");
			return;
		}
		if (m_ghostPlayer.isPlaying && m_photoModeActivated)
		{
			SetGamePause(pause: false);
		}
		if (m_photoModeActivated)
		{
			CameraSwitch.instance.ActivateLastSavedCam();
		}
		m_photoModeActivated = false;
		m_droneModeActivated = false;
		RefreshHints();
		GhostPlayer ghostPlayer = m_ghostPlayer;
		ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		DisableMouseInputFlags();
	}

	private void HideUI()
	{
		KeepAliveManager.ToggleUIInRaceMode();
		InputManager.instance.SetCursorVisibility(!KeepAliveManager.isUIHidden, KeepAliveManager.isUIHidden);
	}

	protected override void OnDeactivate()
	{
		m_progress.OnValueChanged -= OnProgressChanged;
		HideBottomPanel();
		HideTopPanel();
		base.OnDeactivate();
		if (m_droneModeActivated)
		{
			m_droneModeActivated = false;
			return;
		}
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ResetCamera();
		}
		BasePrefsModel.LockSaves(flag: false);
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer = m_ghostPlayer;
			ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
			SetGamePause(pause: false);
			if (m_ghostPlayer.isRewind)
			{
				m_ghostPlayer.Rewind(0f);
			}
			ResetRewindStatus();
		}
	}

	private void ResetRewindStatus()
	{
		m_currentRewindStatus = RewindDirectionType.None;
	}

	private void GoToBack()
	{
		Hide();
	}

	public void NextReplayCamera()
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.NextReplayCamera();
		}
	}

	public void TogglePause()
	{
		if (m_ghostPlayer != null && !isSliderPressed && !m_isGhostCalculating)
		{
			StopRewindGhost();
			PauseReplay(m_ghostPlayer.isPlaying);
		}
	}

	public void GoToPhotoMode()
	{
		if (!isSliderPressed)
		{
			m_photoModeActivated = true;
			StopRewindGhost();
			CameraSwitch.instance.SaveLastActiveCam();
			base.owner.HideStackAndPush<UIPhotoModeContext>();
			SetGamePause(pause: true);
			m_contextChanged = true;
		}
	}

	private void GoToDroneMode()
	{
		if (!isSliderPressed)
		{
			m_droneModeActivated = true;
			StopRewindGhost();
			UIFreeDroneCameraContext.SetGhostPlayers(m_ghostPlayer?.players);
			base.owner.HideStackAndPush<UIFreeDroneCameraContext>().SetCameraToCar(CameraSwitch.instance.GetCar(), forceEnableOptions: true);
			m_contextChanged = true;
			ResetRewindStatus();
		}
	}

	private void PauseReplay(bool pause)
	{
		if (pause)
		{
			m_ghostPlayer?.Pause();
		}
		else
		{
			m_ghostPlayer?.Play();
		}
		m_replayPlaying.value = m_ghostPlayer != null && m_ghostPlayer.isPlaying;
		SetGamePause(pause);
		RefreshHints();
	}

	private void SetGamePause(bool pause)
	{
		m_gameManager.SetGamePause(pause);
		SoundController.instance.fxAudioPause = pause;
	}

	private void OnPlayEndEvent()
	{
		if (m_ghostPlayer != null)
		{
			m_ghostPlayer.progress = 0f;
		}
	}

	private void StopRewindGhost()
	{
		if (m_ghostPlayer != null && m_ghostPlayer.isRewind)
		{
			m_ghostPlayer.Rewind(0f);
		}
	}

	private void PlayNullClickEffect()
	{
		InputManager.PlayClickSound();
	}

	private void Rewind(bool isRewind, float timeScale = 0f)
	{
		if (m_ghostPlayer != null)
		{
			m_ghostPlayer.Rewind(isRewind ? (timeScale * Time.fixedDeltaTime) : 0f);
		}
	}

	private void DisableMouseInputFlags()
	{
		m_pointerPressed = false;
		m_pointerDownHolded = false;
		m_pointerReleased = true;
	}

	public void OnPausePressEvent()
	{
		PlayNullClickEffect();
		TogglePause();
	}

	private void OnRewindProcess(bool isPressed, RewindDirectionType targetDirection)
	{
		if (m_currentRewindStatus != targetDirection && (m_currentRewindStatus == RewindDirectionType.None || targetDirection == RewindDirectionType.None))
		{
			if (isPressed)
			{
				m_forceContinuePlaying = m_replayPlaying.value;
				PlayNullClickEffect();
				PauseReplay(pause: false);
			}
			else
			{
				PauseReplay(!m_forceContinuePlaying);
			}
			switch (targetDirection)
			{
			case RewindDirectionType.Right:
				Rewind(isPressed, 4f);
				m_currentRewindStatus = RewindDirectionType.Right;
				break;
			case RewindDirectionType.Left:
				Rewind(isPressed, -4f);
				m_currentRewindStatus = RewindDirectionType.Left;
				break;
			case RewindDirectionType.None:
				Rewind(isRewind: false);
				m_currentRewindStatus = RewindDirectionType.None;
				break;
			}
		}
	}

	public void OnProgressSliderPointerDown()
	{
		m_pointerDownHolded = true;
		m_pointerReleased = false;
		PlayNullClickEffect();
	}

	public void OnProgressSliderPointerUp()
	{
		DisableMouseInputFlags();
		if (!m_isGhostCalculating)
		{
			PauseReplay(pause: true);
		}
	}

	private void FixedUpdate()
	{
		if (m_ghostPlayer != null && !m_pointerDownHolded && !m_isGhostCalculating)
		{
			m_progress.value = m_ghostPlayer.progress;
		}
		HandleRewind();
		UsingActionsWithHideUI();
	}

	private void Update()
	{
		HandleSingleClickOnBar();
		HandleSliderDrag();
		if (m_gameManager.gamePaused)
		{
			HandleRewind();
			UsingActionsWithHideUI();
		}
	}

	private void HandleSliderDrag()
	{
		if (m_ghostPlayer == null || !m_pointerPressed || m_isGhostCalculating || !m_pointerDownHolded)
		{
			return;
		}
		if (Mathf.Abs(m_dtSliderProgress) > 0f)
		{
			if (m_gameManager.gamePaused)
			{
				SetGamePause(pause: false);
			}
		}
		else if (!m_gameManager.gamePaused)
		{
			SetGamePause(pause: true);
		}
		m_dtSliderProgress = 0f;
	}

	private void HandleSingleClickOnBar()
	{
		if (m_ghostPlayer == null || m_pointerPressed)
		{
			return;
		}
		if (m_isGhostCalculating)
		{
			m_progress.value = m_ghostPlayer.progress;
		}
		m_holdedProgress = m_progress.value;
		if (m_pointerDownHolded && !m_pointerPressed && !m_isGhostCalculating)
		{
			m_pointerPressed = true;
			m_ghostPlayer.ghostCar.carX.rpm = 0f;
			m_ghostPlayer.ClearSmokes();
			if (!m_ghostPlayer.isPlaying)
			{
				PauseReplay(pause: false);
			}
			m_ghostPlayer.CurTimeOverride(m_ghostPlayer.length * m_holdedProgress - 0.05f);
			TimeController.StartNow(SetGhostPositionByTimeGap());
		}
	}

	private IEnumerator SetGhostPositionByTimeGap()
	{
		if (m_ghostPlayer != null)
		{
			m_isGhostCalculating = true;
			float startGhostPosition = m_ghostPlayer.length * m_holdedProgress - 0.05f;
			float endGhostPosition = m_ghostPlayer.length * m_holdedProgress;
			WaitForFixedUpdate skipWaitFixedUpdate = new WaitForFixedUpdate();
			for (float t = 0.5f; t <= 1f; t += 0.5f)
			{
				yield return skipWaitFixedUpdate;
				m_ghostPlayer?.CurTimeOverride(Mathf.Lerp(startGhostPosition, endGhostPosition, t));
			}
			m_isGhostCalculating = false;
			SetGamePause(pause: true);
		}
	}

	private void UsingActionsWithHideUI()
	{
		if (!(m_inputManager == null) && KeepAliveManager.isUIHidden && !(m_hiddenReplayPressedTime + 0.1f >= Time.unscaledTime))
		{
			bool flag = false;
			if (m_inputManager.IsActionPressed(ActionType.NavCancelBtnB) || m_inputManager.IsActionPressed(ActionType.HideUI))
			{
				HideUI();
				flag = true;
			}
			if (m_inputManager.IsActionPressed(ActionType.NavSelectBtnA))
			{
				TogglePause();
				flag = true;
			}
			if (m_inputManager.IsActionPressed(ActionType.NavAdditional0BtnX))
			{
				NextReplayCamera();
				flag = true;
			}
			if (flag)
			{
				m_hiddenReplayPressedTime = Time.unscaledTime;
			}
		}
	}

	private void HandleRewind()
	{
		if (!isSliderPressed)
		{
			if (InputManager.instance.IsActionPressed(ActionType.NavRight))
			{
				OnRewindProcess(isPressed: true, RewindDirectionType.Right);
			}
			if (InputManager.instance.IsActionReleased(ActionType.NavRight) && m_currentRewindStatus == RewindDirectionType.Right)
			{
				OnRewindProcess(isPressed: false, RewindDirectionType.None);
			}
			if (InputManager.instance.IsActionPressed(ActionType.NavLeft))
			{
				OnRewindProcess(isPressed: true, RewindDirectionType.Left);
			}
			if (InputManager.instance.IsActionReleased(ActionType.NavLeft) && m_currentRewindStatus == RewindDirectionType.Left)
			{
				OnRewindProcess(isPressed: false, RewindDirectionType.None);
			}
		}
	}

	public void OnForwardButtonPressEvent(bool isPressed)
	{
		if (isPressed)
		{
			OnRewindProcess(isPressed: true, RewindDirectionType.Right);
		}
		else if (m_currentRewindStatus == RewindDirectionType.Right)
		{
			OnRewindProcess(isPressed: false, RewindDirectionType.None);
		}
	}

	public void OnBackwardButtonPressEvent(bool isPressed)
	{
		if (isPressed)
		{
			OnRewindProcess(isPressed: true, RewindDirectionType.Left);
		}
		else if (m_currentRewindStatus == RewindDirectionType.Left)
		{
			OnRewindProcess(isPressed: false, RewindDirectionType.None);
		}
	}

	private void OnProgressChanged()
	{
		if ((m_ghostPlayer != null || !m_isGhostCalculating) && m_pointerDownHolded)
		{
			m_dtSliderProgress = m_ghostPlayer.progress - m_progress.value;
			m_ghostPlayer.progress = m_progress.value;
			if (m_dtSliderProgress > 0f)
			{
				m_ghostPlayer.ClearSmokes();
			}
		}
	}
}
