using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Binding;
using Binding.Base;
using DI;
using GameInput;
using HUD;
using PrefsModels;
using Rewired;
using UnityEngine;

public class UIFreeDroneCameraContext : BaseBindingBehaviourTarget
{
	private const string DPAD_BIND_NAME = "DPad";

	[Dependency]
	private GamePrefs m_prefs;

	[SerializeField]
	private FreeDroneCamSettingsList m_cameraSettingsList;

	[SerializeField]
	private SpriteProperty m_movementSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_viewSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_navigationSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_turboSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_changeHeightUpSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_changeHeightDownSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_changeFOVSprite = new SpriteProperty();

	[SerializeField]
	private SpriteProperty m_playerListSprite = new SpriteProperty();

	[SerializeField]
	private FreeDroneCamera m_cameraPrefab;

	private FreeDroneCameraSettings m_previewSettings;

	private FreeDroneCameraSettings m_currentSettings;

	private FreeDroneCameraSettings m_initialSettings;

	private FreeDroneCameraSettings m_cachedSettings;

	private bool m_needKeepCachedSettings;

	private bool m_oldHoldRotationButton;

	private FreeDroneCamera m_freeDroneCameraInstance;

	private GameInput.InputManager m_inputManager;

	private UIReportContext m_report;

	private IntProperty m_controllerTypeId = new IntProperty(0);

	private BoolProperty m_following = new BoolProperty(false);

	private BoolProperty m_tracking = new BoolProperty(false);

	private BoolProperty m_showMenu = new BoolProperty(startValue: true);

	private BoolProperty m_showChangePlayerDesc = new BoolProperty(startValue: true);

	private TrackListSettingsItem m_speedMovement;

	private TrackListSettingsItem m_movementDamping;

	private TrackListSettingsItem m_speedMultiplier;

	private TrackListSettingsItem m_maxSpeed;

	private TrackListSettingsItem m_dampingSpeed;

	private TrackListSettingsItem m_speedRotation;

	private TrackListSettingsItem m_targetDamping;

	private TrackListSettingsItem m_dampRotation;

	private TrackListSettingsItem m_lockheadTime;

	private TrackListSettingsItem m_fov;

	private TrackListSettingsItem m_amplitudeGain;

	private TrackListSettingsItem m_frequencyGain;

	private static RaceCar m_targetCar;

	private static List<GhostPlayer.Player> m_ghostPlayers;

	public bool changedIndex { get; private set; }

	public static void SetGhostPlayers(List<GhostPlayer.Player> ghostPlayers)
	{
		if (ghostPlayers == null || ghostPlayers.Count == 0)
		{
			Debug.LogError("Unable to set follow target for camera");
			return;
		}
		m_ghostPlayers = ghostPlayers;
		foreach (GhostPlayer.Player ghostPlayer in m_ghostPlayers)
		{
			if (ghostPlayer.visible)
			{
				m_targetCar = ghostPlayer.ghostCar;
				break;
			}
		}
	}

	private void SetNextXDSGhostCar()
	{
		if (m_ghostPlayers != null && m_ghostPlayers.Count > 1)
		{
			int num = m_ghostPlayers.FindIndex((GhostPlayer.Player player) => player.ghostCar == m_targetCar);
			num = ++num % m_ghostPlayers.Count;
			m_targetCar = m_ghostPlayers[num].ghostCar;
			changedIndex = !changedIndex;
		}
	}

	protected override void OnActivate()
	{
		if (!InitCameraSettingsIfNeed())
		{
			return;
		}
		InitDroneCameraInstance();
		base.OnActivate();
		RebuildBottomPanel();
		if (m_report == null)
		{
			m_report = base.owner.Get<UIReportContext>();
		}
		if (m_report != null)
		{
			UIReportContext report = m_report;
			report.showUIReportPanelAction = (Action<bool>)Delegate.Combine(report.showUIReportPanelAction, new Action<bool>(OnShowUIReportPanelHandler));
		}
		if (m_freeDroneCameraInstance != null)
		{
			FreeDroneCamera freeDroneCameraInstance = m_freeDroneCameraInstance;
			freeDroneCameraInstance.onChangedAutofocusMode = (System.Action)Delegate.Combine(freeDroneCameraInstance.onChangedAutofocusMode, new System.Action(OnChangedFollowingMode));
			FreeDroneCamera freeDroneCameraInstance2 = m_freeDroneCameraInstance;
			freeDroneCameraInstance2.onChangedTrackingMode = (System.Action)Delegate.Combine(freeDroneCameraInstance2.onChangedTrackingMode, new System.Action(OnChangedTrackingMode));
			FreeDroneCameraMovement droneCameraMovement = m_freeDroneCameraInstance.droneCameraMovement;
			droneCameraMovement.onChangedFOV = (Action<float>)Delegate.Combine(droneCameraMovement.onChangedFOV, new Action<float>(OnChangedFOV));
			if (!m_needKeepCachedSettings)
			{
				if ((bool)m_targetCar)
				{
					SetCameraToCar(m_targetCar, forceEnableOptions: false);
				}
				RepositionCamera();
				SetHeadVisibility();
			}
		}
		RebuildList();
		TimeController.StartNow(SelectFirstCameraListElemAsync());
		InitInputDescriptionPanel();
		changedIndex = false;
		bool flag = States.mainCurrent is XDSRaceModeState;
		m_showChangePlayerDesc.value = States.mainCurrent is SyncNetFreerideRaceModeState || (flag && m_ghostPlayers != null && m_ghostPlayers.Count > 1);
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.onSwitchCar -= OnSwitchCarHandler;
			CameraSwitch.instance.onSwitchCar += OnSwitchCarHandler;
		}
	}

	private IEnumerator SelectFirstCameraListElemAsync()
	{
		yield return null;
		m_cameraSettingsList.selectedIndex = 0;
	}

	private void InitInputDescriptionPanel()
	{
		m_inputManager = GameInput.InputManager.instance;
		if (!(m_inputManager == null))
		{
			GameInput.InputManager inputManager = m_inputManager;
			inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(inputManager.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceType));
			OnChangeActiveDeviceType(m_inputManager.lastController);
		}
	}

	private void LoadInputDescriptionConsoleIcons()
	{
		ActionElementResources.PlatformTypeElementAction value = (ActionElementResources.PlatformTypeElementAction)m_controllerTypeId.value;
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if (instance == null)
		{
			return;
		}
		Controller lastController = instance.lastController;
		ActionElementResources instance2 = ScriptableSingleton<ActionElementResources>.instance;
		if (lastController != null && !(instance2 == null))
		{
			BindElement actionInfoByDevice = instance.GetActionInfoByDevice(ActionType.FlyMovement, lastController);
			BindElement actionInfoByDevice2 = instance.GetActionInfoByDevice(ActionType.FlyHeightUp, lastController);
			BindElement actionInfoByDevice3 = instance.GetActionInfoByDevice(ActionType.FlyHeightDown, lastController);
			BindElement actionInfoByDevice4 = instance.GetActionInfoByDevice(ActionType.FlyRotationHorizontal, lastController);
			BindElement actionInfoByDevice5 = instance.GetActionInfoByDevice(ActionType.FlyTurboMode, lastController);
			BindElement actionInfoByDevice6 = instance.GetActionInfoByDevice(ActionType.FlyChangeFOV, lastController);
			BindElement actionInfoByDevice7 = instance.GetActionInfoByDevice(ActionType.FlyStatistics, lastController);
			m_movementSprite.value = null;
			m_changeHeightUpSprite.value = null;
			m_changeHeightDownSprite.value = null;
			m_viewSprite.value = null;
			m_turboSprite.value = null;
			m_changeFOVSprite.value = null;
			m_playerListSprite.value = null;
			if (actionInfoByDevice != null && actionInfoByDevice.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_movementSprite.value = instance2.GetBindActionSprite(actionInfoByDevice, value);
			}
			if (actionInfoByDevice2 != null && actionInfoByDevice2.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_changeHeightUpSprite.value = instance2.GetBindActionSprite(actionInfoByDevice2, value);
			}
			if (actionInfoByDevice3 != null && actionInfoByDevice3.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_changeHeightDownSprite.value = instance2.GetBindActionSprite(actionInfoByDevice3, value);
			}
			if (actionInfoByDevice4 != null && actionInfoByDevice4.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_viewSprite.value = instance2.GetBindActionSprite(actionInfoByDevice4, value);
			}
			if (actionInfoByDevice5 != null && actionInfoByDevice5.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_turboSprite.value = instance2.GetBindActionSprite(actionInfoByDevice5, value);
			}
			if (actionInfoByDevice6 != null && actionInfoByDevice6.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_changeFOVSprite.value = instance2.GetBindActionSprite(actionInfoByDevice6, value);
			}
			if (actionInfoByDevice7 != null && actionInfoByDevice7.deviceInfo.hardwareIdentifier.Equals(lastController.hardwareIdentifier))
			{
				m_playerListSprite.value = instance2.GetBindActionSprite(actionInfoByDevice7, value);
			}
			m_navigationSprite.value = instance2.GetActionSpriteByBindName("DPad", value);
		}
	}

	protected override void OnDeactivate()
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.onSwitchCar -= OnSwitchCarHandler;
		}
		if (m_needKeepCachedSettings)
		{
			HideBottomPanel();
			base.OnDeactivate();
			return;
		}
		if (m_freeDroneCameraInstance != null)
		{
			FreeDroneCamera freeDroneCameraInstance = m_freeDroneCameraInstance;
			freeDroneCameraInstance.onChangedAutofocusMode = (System.Action)Delegate.Remove(freeDroneCameraInstance.onChangedAutofocusMode, new System.Action(OnChangedFollowingMode));
			FreeDroneCamera freeDroneCameraInstance2 = m_freeDroneCameraInstance;
			freeDroneCameraInstance2.onChangedTrackingMode = (System.Action)Delegate.Remove(freeDroneCameraInstance2.onChangedTrackingMode, new System.Action(OnChangedTrackingMode));
			FreeDroneCameraMovement droneCameraMovement = m_freeDroneCameraInstance.droneCameraMovement;
			droneCameraMovement.onChangedFOV = (Action<float>)Delegate.Remove(droneCameraMovement.onChangedFOV, new Action<float>(OnChangedFOV));
			m_freeDroneCameraInstance.gameObject.SetActive(value: false);
		}
		CameraSwitch instance = CameraSwitch.instance;
		if (instance != null)
		{
			instance.ActivateLastSavedCam();
			instance.ForceResetCameraYRotation();
		}
		if (m_inputManager != null)
		{
			GameInput.InputManager inputManager = m_inputManager;
			inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(inputManager.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceType));
		}
		if (m_report != null)
		{
			UIReportContext report = m_report;
			report.showUIReportPanelAction = (Action<bool>)Delegate.Remove(report.showUIReportPanelAction, new Action<bool>(OnShowUIReportPanelHandler));
		}
		if (States.current is FreeDroneState)
		{
			UnityEngine.Object.Destroy(m_freeDroneCameraInstance.gameObject);
			m_freeDroneCameraInstance = null;
		}
		if (!m_needKeepCachedSettings)
		{
			m_cachedSettings = null;
		}
		HideBottomPanel();
		base.OnDeactivate();
	}

	private void InitDroneCameraInstance()
	{
		if (m_freeDroneCameraInstance == null)
		{
			m_freeDroneCameraInstance = UnityEngine.Object.Instantiate(m_cameraPrefab);
		}
		if (!m_freeDroneCameraInstance.gameObject.activeInHierarchy)
		{
			m_freeDroneCameraInstance.gameObject.SetActive(value: true);
		}
	}

	private bool InitCameraSettingsIfNeed()
	{
		if (m_cameraSettingsList == null)
		{
			m_cameraSettingsList = GetComponentInChildren<FreeDroneCamSettingsList>();
			if (m_cameraSettingsList == null)
			{
				GameLog.Print(AlertLevel.Error, "UI", "Not found settings list for FreeDroneCamera");
				return false;
			}
		}
		return true;
	}

	private void SetHeadVisibility()
	{
		RaceCar raceCar = ((m_targetCar != null) ? m_targetCar : PlayerCarControl.instance.car);
		if (raceCar != null)
		{
			CameraSwitch.instance.InformChangeCameraView(raceCar.carX, outside: true, frontCam: false);
			CarCameraSwitchHandler driverCamSwitchHandler = raceCar.driverCamSwitchHandler;
			if (driverCamSwitchHandler != null)
			{
				driverCamSwitchHandler.OnChangeCameraView(outside: true);
			}
		}
	}

	private void Update()
	{
		if (m_freeDroneCameraInstance.inputData.holdRotateButton)
		{
			if (GetCursorVisible() && !m_oldHoldRotationButton)
			{
				m_oldHoldRotationButton = true;
				SetCursor(newVisibleStatus: false);
			}
		}
		else if (!GetCursorVisible() && m_oldHoldRotationButton)
		{
			m_oldHoldRotationButton = false;
			SetCursor(newVisibleStatus: true);
		}
	}

	public override bool IsAllowNavigation()
	{
		return false;
	}

	private void RebuildBottomPanel()
	{
		ShowBottomPanel(new ActionHintData[3]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			},
			new ActionHintData
			{
				hint = LocalizationKeys.reset.Localized(),
				type = ActionType.FlyReset,
				action = NavReset
			},
			new ActionHintData
			{
				hint = LocalizationKeys.input_action_.AddLocalized(ActionType.HideUI.ToString().ToLower()),
				type = ActionType.FlyHideUI,
				action = NavFlyHideUI
			}
		}, null, ignoreRepeat: true);
	}

	private void OnChangedFOV(float fov)
	{
		if (!(m_cameraSettingsList == null) && m_fov != null)
		{
			if (Mathf.Abs(m_fov.value - fov) > Mathf.Epsilon)
			{
				m_fov.value = fov;
			}
			m_freeDroneCameraInstance.inputData.UpdateFovIndex(m_fov.value);
			m_cameraSettingsList.UpdateList();
		}
	}

	private void OnChangedFollowingMode()
	{
		m_following.value = m_freeDroneCameraInstance.autofocusMode;
	}

	private void OnChangedTrackingMode()
	{
		m_tracking.value = m_freeDroneCameraInstance.trackingMode;
	}

	private void OnChangeActiveDeviceType(Controller controller)
	{
		if (controller != null)
		{
			m_controllerTypeId.value = (int)TryGetCompatiblePlatformTypeByMeta(controller.GetMetaInfo());
			if (m_controllerTypeId.value != 0)
			{
				LoadInputDescriptionConsoleIcons();
			}
		}
	}

	private ActionElementResources.PlatformTypeElementAction TryGetCompatiblePlatformTypeByMeta(ControllerMetaInfo controllerMetaInfo)
	{
		if (controllerMetaInfo.rewiredType == ControllerType.Keyboard)
		{
			return ActionElementResources.PlatformTypeElementAction.Default;
		}
		if (controllerMetaInfo.isPS4PadCompatible)
		{
			return ActionElementResources.PlatformTypeElementAction.DualShock;
		}
		if (controllerMetaInfo.isXboxCompatible)
		{
			return ActionElementResources.PlatformTypeElementAction.Xbox;
		}
		return ActionElementResources.PlatformTypeElementAction.Default;
	}

	private void OnShowUIRaceStatsHandler(bool showStats)
	{
		m_freeDroneCameraInstance.BlockUserControl(showStats);
	}

	private void OnShowUIReportPanelHandler(bool showReport)
	{
		m_freeDroneCameraInstance.BlockUserControl(showReport);
	}

	public Transform GetTargetTransform()
	{
		return m_freeDroneCameraInstance.targetTransform;
	}

	private void OnSwitchCarHandler(RaceCar car)
	{
		SetCameraToCar(car, forceEnableOptions: false);
	}

	public void SetCameraToCar(RaceCar userCar, bool forceEnableOptions)
	{
		if (!(userCar == null) && !(m_freeDroneCameraInstance == null))
		{
			m_targetCar = userCar;
			m_freeDroneCameraInstance.targetTransform = m_targetCar.transform;
			m_freeDroneCameraInstance.SwitchTrackingMode(forceEnableOptions || m_tracking.value);
			m_freeDroneCameraInstance.SwitchAutofocusMode(forceEnableOptions || m_following.value);
			if (m_freeDroneCameraInstance.trackingMode)
			{
				RepositionCamera();
			}
		}
	}

	private void RepositionCamera()
	{
		if (m_freeDroneCameraInstance.targetTransform != null)
		{
			Transform targetTransform = m_freeDroneCameraInstance.targetTransform;
			m_freeDroneCameraInstance.SetPosition(targetTransform.transform.position - targetTransform.forward * 4f + Vector3.up * 2f);
			m_freeDroneCameraInstance.LookAt(targetTransform.position);
			m_freeDroneCameraInstance.IgnoreCollision();
		}
	}

	private void RebuildList()
	{
		if (!(m_cameraSettingsList == null))
		{
			InitIfNeedCameraListElements();
			ApplyCameraSettingsParams();
			m_cameraSettingsList.Clear();
			m_cameraSettingsList.AddItem(m_speedMovement);
			m_cameraSettingsList.AddItem(m_speedMultiplier);
			m_cameraSettingsList.AddItem(m_movementDamping);
			m_cameraSettingsList.AddItem(m_dampingSpeed);
			m_cameraSettingsList.AddItem(m_speedRotation);
			m_cameraSettingsList.AddItem(m_dampRotation);
			m_cameraSettingsList.AddItem(m_targetDamping);
			m_cameraSettingsList.AddItem(m_lockheadTime);
			m_cameraSettingsList.AddItem(m_fov);
			m_cameraSettingsList.AddItem(m_frequencyGain);
			m_cameraSettingsList.AddItem(m_amplitudeGain);
			if (!m_needKeepCachedSettings)
			{
				m_currentSettings = m_previewSettings.Copy();
			}
			m_initialSettings = m_currentSettings.Copy();
			m_cameraSettingsList.RecalcList();
			m_freeDroneCameraInstance.inputData.UpdateFovIndex(m_fov.value);
		}
	}

	private void InitIfNeedCameraListElements()
	{
		if (m_speedMovement == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMovement");
			m_speedMovement = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_speed_movement.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.speedMovement = m_speedMovement.value;
					m_previewSettings.speedMovement = m_speedMovement.value;
				}
			};
		}
		if (m_movementDamping == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("movementDamping");
			m_movementDamping = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_movement_damping.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.movementDamping = m_movementDamping.value;
					m_previewSettings.movementDamping = m_movementDamping.value;
				}
			};
		}
		if (m_speedMultiplier == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMultiplier");
			m_speedMultiplier = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_speed_multiplier.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.speedMultiplier = m_speedMultiplier.value;
					m_previewSettings.speedMultiplier = m_speedMultiplier.value;
				}
			};
		}
		if (m_dampingSpeed == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampingSpeed");
			m_dampingSpeed = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_speed_damping.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.dampingSpeed = m_dampingSpeed.value;
					m_previewSettings.dampingSpeed = m_dampingSpeed.value;
				}
			};
		}
		if (m_speedRotation == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedRotation");
			m_speedRotation = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_speed_rotation.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.speedRotation = m_speedRotation.value;
					m_previewSettings.speedRotation = m_speedRotation.value;
				}
			};
		}
		if (m_targetDamping == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("targetDamping");
			m_targetDamping = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_target_damping.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.targetDamping = m_targetDamping.value;
					m_previewSettings.targetDamping = m_targetDamping.value;
				}
			};
		}
		if (m_dampRotation == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampRotation");
			m_dampRotation = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_rotation_damping.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.dampRotation = m_dampRotation.value;
					m_previewSettings.dampRotation = m_dampRotation.value;
				}
			};
		}
		if (m_lockheadTime == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("lockHeadTime");
			m_lockheadTime = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_lockhead_time.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.lockHeadTime = m_lockheadTime.value;
					m_previewSettings.lockHeadTime = m_lockheadTime.value;
				}
			};
		}
		if (m_fov == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("fov");
			m_fov = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step)
			{
				title = LocalizationKeys.freedrone_params_fov.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.fov = m_fov.value;
					m_previewSettings.fov = m_fov.value;
				}
			};
		}
		if (m_frequencyGain == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("frequencyGain");
			m_frequencyGain = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_frequency_gain.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.frequencyGain = m_frequencyGain.value;
					m_previewSettings.frequencyGain = m_frequencyGain.value;
				}
			};
		}
		if (m_amplitudeGain == null)
		{
			FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("amplitudeGain");
			m_amplitudeGain = new TrackListSettingsItem(dataParam.min, dataParam.max, dataParam.step, "{0:F1}")
			{
				title = LocalizationKeys.freedrone_params_amplitude_gain.Localized(),
				onChangedValue = delegate
				{
					m_freeDroneCameraInstance.amplitudeGain = m_amplitudeGain.value;
					m_previewSettings.amplitudeGain = m_amplitudeGain.value;
				}
			};
		}
	}

	private void ApplyCameraSettingsParams()
	{
		if (m_cachedSettings != null)
		{
			m_previewSettings = m_cachedSettings.Copy();
		}
		else
		{
			m_previewSettings = GetDefaultCameraSettings();
		}
		if (m_freeDroneCameraInstance != null)
		{
			ApplyToCameraInstanceParams(m_previewSettings);
		}
		m_speedMovement.value = m_previewSettings.speedMovement;
		m_movementDamping.value = m_previewSettings.movementDamping;
		m_speedMultiplier.value = m_previewSettings.speedMultiplier;
		m_dampingSpeed.value = m_previewSettings.dampingSpeed;
		m_speedRotation.value = m_previewSettings.speedRotation;
		m_targetDamping.value = m_previewSettings.targetDamping;
		m_dampRotation.value = m_previewSettings.dampRotation;
		m_lockheadTime.value = m_previewSettings.lockHeadTime;
		m_fov.value = m_previewSettings.fov;
		m_frequencyGain.value = m_previewSettings.frequencyGain;
		m_amplitudeGain.value = m_previewSettings.amplitudeGain;
	}

	private void ApplyToCameraInstanceParams(FreeDroneCameraSettings cameraParams)
	{
		if (cameraParams != null)
		{
			m_freeDroneCameraInstance.speedMovement = cameraParams.speedMovement;
			m_freeDroneCameraInstance.movementDamping = cameraParams.movementDamping;
			m_freeDroneCameraInstance.speedMultiplier = cameraParams.speedMultiplier;
			m_freeDroneCameraInstance.dampingSpeed = cameraParams.dampingSpeed;
			m_freeDroneCameraInstance.speedRotation = cameraParams.speedRotation;
			m_freeDroneCameraInstance.targetDamping = cameraParams.targetDamping;
			m_freeDroneCameraInstance.dampRotation = cameraParams.dampRotation;
			m_freeDroneCameraInstance.lockHeadTime = cameraParams.lockHeadTime;
			m_freeDroneCameraInstance.fov = cameraParams.fov;
			m_freeDroneCameraInstance.frequencyGain = cameraParams.frequencyGain;
			m_freeDroneCameraInstance.amplitudeGain = cameraParams.amplitudeGain;
		}
	}

	private void CloseContext()
	{
		m_needKeepCachedSettings = false;
		SetCursor(newVisibleStatus: true);
		if (States.current is FreeDroneState)
		{
			States.Pop();
		}
		else
		{
			Hide();
		}
	}

	public void NavReset()
	{
		FreeDroneCameraLimits.DataParams dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMovement");
		m_speedMovement.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("movementDamping");
		m_movementDamping.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMultiplier");
		m_speedMultiplier.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampingSpeed");
		m_dampingSpeed.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedRotation");
		m_speedRotation.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("targetDamping");
		m_targetDamping.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampRotation");
		m_dampRotation.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("lockHeadTime");
		m_lockheadTime.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("fov");
		m_fov.value = dataParam.defaultValue;
		m_freeDroneCameraInstance.droneCameraMovement.SetDefaultFovIndex();
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("frequencyGain");
		m_frequencyGain.value = dataParam.defaultValue;
		dataParam = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("amplitudeGain");
		m_amplitudeGain.value = dataParam.defaultValue;
		m_cameraSettingsList.RecalcList();
		Save();
	}

	private bool GetCursorVisible()
	{
		return Cursor.visible;
	}

	private void SetCursor(bool newVisibleStatus)
	{
		m_inputManager.SetCursorVisibility(newVisibleStatus);
	}

	[GameHotkey(ActionType.FlyStatistics, false, false)]
	public void ShowStatsPanel()
	{
		if (!m_showMenu.value)
		{
			return;
		}
		bool flag = true;
		BaseState mainCurrent = States.mainCurrent;
		if (mainCurrent is SyncNetFreerideRaceModeState)
		{
			UIRaceStatsContext uIRaceStatsContext = base.owner.Get<UIRaceStatsContext>();
			if (uIRaceStatsContext != null)
			{
				m_cachedSettings = m_previewSettings.Copy();
				m_needKeepCachedSettings = true;
				uIRaceStatsContext.visibleInviteHint = false;
				base.owner.HideStackAndPush<UIRaceStatsContext>();
				m_showMenu.value = true;
			}
		}
		else if (mainCurrent is XDSRaceModeState || flag)
		{
			SetNextXDSGhostCar();
			SetCameraToCar(m_targetCar, forceEnableOptions: true);
		}
	}

	[GameHotkey(ActionType.NavNextPage, false, false)]
	public void SwitchAutofocus()
	{
		m_freeDroneCameraInstance.SwitchAutofocusMode(!m_freeDroneCameraInstance.autofocusMode);
	}

	[GameHotkey(ActionType.NavPrevPage, false, false)]
	public void SwitchTracking()
	{
		if (!m_freeDroneCameraInstance.trackingMode)
		{
			m_freeDroneCameraInstance.ResetRelative();
		}
		m_freeDroneCameraInstance.SwitchTrackingMode(!m_freeDroneCameraInstance.trackingMode);
	}

	[GameHotkey(ActionType.NavDPadUp, false, false)]
	public void NavUp()
	{
		if (m_cameraSettingsList.GetCount() > 0 && m_cameraSettingsList.selectedIndex > 0)
		{
			m_cameraSettingsList.selectedIndex--;
		}
		m_cameraSettingsList?.UpdateList();
	}

	[GameHotkey(ActionType.NavDPadDown, false, false)]
	public void NavDown()
	{
		if (m_cameraSettingsList.GetCount() > 0 && m_cameraSettingsList.selectedIndex + 1 < m_cameraSettingsList.GetCount())
		{
			m_cameraSettingsList.selectedIndex++;
		}
		m_cameraSettingsList?.UpdateList();
	}

	public void NavCancel()
	{
		if (!PublicInstancePropertiesEqual(m_previewSettings, m_currentSettings))
		{
			UIMessageBox.ShowComplexDialog(LocalizationKeys.customization_cameras_save_changes_text.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), delegate
			{
				Save(onExit: true);
				SetCursor(newVisibleStatus: true);
				CloseContext();
			}, LocalizationKeys.Yes.Localized(), delegate
			{
				if (m_initialSettings != null)
				{
					m_previewSettings = m_initialSettings.Copy();
				}
				Save(onExit: true);
				CloseContext();
			}, LocalizationKeys.No.Localized());
		}
		else
		{
			CloseContext();
		}
	}

	public void NavFlyHideUI()
	{
		m_showMenu.value = false;
		GameInput.InputManager.instance.SetCursorVisibility(showCursor: false, lockState: true);
		HideBottomPanel();
	}

	[GameHotkey(ActionType.NavBack, false, false)]
	public void NavFlyShowUI()
	{
		m_showMenu.value = true;
		GameInput.InputManager.instance.SetCursorVisibility(showCursor: true);
		RebuildBottomPanel();
	}

	[GameHotkey(ActionType.FlyHideUI, false, false)]
	public void NavFlyShowUIHideUI()
	{
		TryFlyShowUI();
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public void NavFlyShowNavCancel()
	{
		TryFlyShowUI();
	}

	private bool TryFlyShowUI()
	{
		if (!m_showMenu.value && (bool)m_inputManager)
		{
			NavFlyShowUI();
			return true;
		}
		return false;
	}

	private void Save(bool onExit = false)
	{
		m_prefs.globals.freeDroneSettings = m_previewSettings;
		m_prefs.globals.Save();
		if (onExit)
		{
			BasePrefsModel.SaveAllQueries(inOneFrame: true);
		}
	}

	public static bool PublicInstancePropertiesEqual<T>(T self, T to, params string[] ignore) where T : class
	{
		if (self != null && to != null)
		{
			Type typeFromHandle = typeof(T);
			PropertyInfo[] cachedProperties = typeFromHandle.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public);
			for (int i = 0; i < cachedProperties.Length; i++)
			{
				if (Array.IndexOf(ignore, cachedProperties[i].Name) == -1)
				{
					PropertyInfo property = typeFromHandle.GetProperty(cachedProperties[i].Name);
					object value = property.GetValue(self, null);
					object value2 = property.GetValue(to, null);
					if (value != value2 && (value == null || !value.Equals(value2)))
					{
						return false;
					}
				}
			}
			return true;
		}
		return self == to;
	}

	private FreeDroneCameraSettings GetDefaultCameraSettings()
	{
		FreeDroneCameraSettings freeDroneCameraSettings = new FreeDroneCameraSettings();
		FreeDroneCameraSettings freeDroneSettings = m_prefs.globals.freeDroneSettings;
		if (freeDroneSettings.speedMovement != 0f)
		{
			freeDroneCameraSettings.speedMovement = freeDroneSettings.speedMovement;
			freeDroneCameraSettings.movementDamping = freeDroneSettings.movementDamping;
			freeDroneCameraSettings.speedMultiplier = freeDroneSettings.speedMultiplier;
			freeDroneCameraSettings.dampingSpeed = freeDroneSettings.dampingSpeed;
			freeDroneCameraSettings.speedRotation = freeDroneSettings.speedRotation;
			freeDroneCameraSettings.targetDamping = freeDroneSettings.targetDamping;
			freeDroneCameraSettings.dampRotation = freeDroneSettings.dampRotation;
			freeDroneCameraSettings.lockHeadTime = freeDroneSettings.lockHeadTime;
			freeDroneCameraSettings.fov = freeDroneSettings.fov;
			freeDroneCameraSettings.frequencyGain = freeDroneSettings.frequencyGain;
			freeDroneCameraSettings.amplitudeGain = freeDroneSettings.amplitudeGain;
		}
		else
		{
			freeDroneCameraSettings.speedMovement = m_freeDroneCameraInstance.speedMovement;
			freeDroneCameraSettings.movementDamping = m_freeDroneCameraInstance.movementDamping;
			freeDroneCameraSettings.speedMultiplier = m_freeDroneCameraInstance.speedMultiplier;
			freeDroneCameraSettings.dampingSpeed = m_freeDroneCameraInstance.dampingSpeed;
			freeDroneCameraSettings.speedRotation = m_freeDroneCameraInstance.speedRotation;
			freeDroneCameraSettings.targetDamping = m_freeDroneCameraInstance.targetDamping;
			freeDroneCameraSettings.dampRotation = m_freeDroneCameraInstance.dampRotation;
			freeDroneCameraSettings.lockHeadTime = m_freeDroneCameraInstance.lockHeadTime;
			freeDroneCameraSettings.fov = m_freeDroneCameraInstance.fov;
			freeDroneCameraSettings.frequencyGain = m_freeDroneCameraInstance.frequencyGain;
			freeDroneCameraSettings.amplitudeGain = m_freeDroneCameraInstance.amplitudeGain;
		}
		return freeDroneCameraSettings;
	}
}
