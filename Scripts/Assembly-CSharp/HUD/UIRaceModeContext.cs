using System.Collections;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using Minimaps;
using Rewired;
using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	public class UIRaceModeContext : BaseBindingBehaviourTarget
	{
		private StringProperty m_timer = new StringProperty();

		private StringProperty m_driftPoints = new StringProperty("0");

		private StringProperty m_cameraLabel = new StringProperty();

		private BoolProperty m_wrongWay = new BoolProperty(false);

		private BoolProperty m_enableHUDCameraLabel = new BoolProperty(false);

		private FloatProperty m_cameraLabelAlpha = new FloatProperty();

		private ActionElementBindingProperty m_showRadialMenuAction = new ActionElementBindingProperty();

		protected BoolProperty m_showDriftHUD = new BoolProperty(startValue: true);

		protected BoolProperty m_showDriftCounter = new BoolProperty(startValue: true);

		protected BoolProperty m_enableRadialMenuButton = new BoolProperty();

		private const float SLIDER_LIFETIME = 4f;

		private float m_lastTimeSliderActivated;

		private readonly BoolProperty m_showAngleSlider = new BoolProperty();

		private readonly FloatProperty m_angle = new FloatProperty();

		private readonly FloatProperty m_deadZone = new FloatProperty();

		private bool m_skipLifeTimeAngleSliderCheck;

		[Dependency]
		protected GamePrefs m_gamePrefs;

		[Dependency]
		protected BaseModel m_model;

		protected RaceCar m_userCar;

		protected DriftController m_driftController;

		private int m_oldDriftPoints = -1;

		private CameraSwitch m_cameraSwitch;

		private Coroutine m_fadeCamLabelCoroutine;

		private BaseRaceState m_raceState;

		private float m_cursorMoveTime;

		private Mouse m_mouse;

		private InputSettings.Parameters m_inputParams;

		private const float m_cursorHideTime = 3f;

		private readonly BoolProperty mouseLocked = new BoolProperty();

		public bool wrongWay
		{
			get
			{
				return m_wrongWay.value;
			}
			set
			{
				m_wrongWay.value = value;
			}
		}

		public RaceCar userCar => m_userCar;

		public UIRaceSpeedometerContext speedometr { get; private set; }

		public UIBoostGaugeContext boostGauge { get; private set; }

		public UIRacePointsComboContext raceDpAction { get; private set; }

		public UIHUDDriftAnglesContext driftAngles { get; private set; }

		public UIRearviewMirrorContext rearviewMirror { get; private set; }

		public DriftController GetDriftController()
		{
			return m_driftController;
		}

		public void SetTimer(float? timer)
		{
			m_timer.value = UIUtils.TimeToHMSMString(timer);
		}

		protected void CreateWaypoint()
		{
			PlayerCarControl.instance.car.CreateWaypoint();
			GUICommonGodVoice.ShowText(LocalizationKeys.waypoint_is_created.Localized(), 3f);
		}

		protected void MoveToWaypoint()
		{
			if (PlayerCarControl.instance.car.TryResetToWaypoint())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.you_are_teleported.Localized(), 3f);
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.waypoint_is_not_created.Localized(), 3f);
			}
		}

		protected void GotoDynostand()
		{
			m_raceState = States.current as BaseRaceState;
			if (m_raceState == null)
			{
				return;
			}
			if (XRModeManager.IsXRActive())
			{
				XRModeManager.ShowDialogNotAvailable();
				return;
			}
			PlayerCarProfile profileMetaInfo = m_raceState.userCar.profileMetaInfo;
			if (UIDynostandContext.IsCanDynoAdjust(profileMetaInfo.name, profileMetaInfo.dynoInfo))
			{
				States.Push<DynoMenuState>();
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.garage_hint_profile_dyno_stock.Localized(), 5f);
			}
		}

		protected bool CanProcessRadialMenu()
		{
			if (!GameInput.InputManager.IsInputLocked() && HotkeyManager.CanClick(m_showRadialMenuAction.value))
			{
				return !KeepAliveManager.isUIHidden;
			}
			return false;
		}

		protected void HandleRadialMenuVisibility()
		{
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (!CanProcessRadialMenu())
			{
				if (base.owner.IsActive<UIRadialMenuContext>())
				{
					base.owner.Hide<UIRadialMenuContext>();
				}
				return;
			}
			if (instance.IsActionPressed(ActionType.ShowRadialMenu))
			{
				base.owner.HideAllAndPush<UIRadialMenuContext>();
			}
			if (instance.IsActionReleased(ActionType.ShowRadialMenu))
			{
				base.owner.Hide<UIRadialMenuContext>();
			}
		}

		public void SetDriftPoints(int points)
		{
			if (points != m_oldDriftPoints)
			{
				m_oldDriftPoints = points;
				m_driftPoints.value = UIUtils.DigitsWithSeparate(points);
			}
		}

		public void SetAngleSlider(bool needShow, bool useToggleAction = false)
		{
			if (needShow)
			{
				m_lastTimeSliderActivated = Time.unscaledTime;
			}
			if (useToggleAction)
			{
				m_skipLifeTimeAngleSliderCheck = !m_skipLifeTimeAngleSliderCheck;
			}
			m_showAngleSlider.value = needShow;
		}

		private void SliderAngleUpdate()
		{
			if (!m_showAngleSlider.value)
			{
				return;
			}
			if ((!m_skipLifeTimeAngleSliderCheck && Time.unscaledTime - m_lastTimeSliderActivated > 4f) || (bool)UIRaceHintsContext.activeInstance)
			{
				SetAngleSlider(needShow: false);
				return;
			}
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (!(instance == null))
			{
				m_angle.value = instance.GetActionValue(ActionType.Turn);
				m_deadZone.value = m_inputParams.steeringDeadZone;
			}
		}

		public void UpdateWrongWay()
		{
			if (m_userCar != null)
			{
				wrongWay = m_userCar.wrongWay;
			}
		}

		public void RefreshDriftPointAction(int dp)
		{
			if ((bool)raceDpAction)
			{
				raceDpAction.Refresh(dp);
			}
		}

		protected virtual void Awake()
		{
			speedometr = GetComponentInChildren<UIRaceSpeedometerContext>();
			boostGauge = GetComponentInChildren<UIBoostGaugeContext>();
			raceDpAction = GetComponentInChildren<UIRacePointsComboContext>();
			driftAngles = GetComponentInChildren<UIHUDDriftAnglesContext>();
			rearviewMirror = GetComponentInChildren<UIRearviewMirrorContext>();
		}

		protected virtual void Start()
		{
			m_cameraSwitch = CameraSwitch.instance;
			if ((bool)m_cameraSwitch)
			{
				m_cameraSwitch.onSwitchCar += OnSwitchCar;
				m_cameraSwitch.onCameraChanged += OnCameraSwitchHandler;
			}
			m_enableRadialMenuButton.value = true;
		}

		protected override void OnDestroy()
		{
			if ((bool)m_cameraSwitch)
			{
				m_cameraSwitch.onSwitchCar -= OnSwitchCar;
				m_cameraSwitch.onCameraChanged -= OnCameraSwitchHandler;
				m_cameraSwitch = null;
			}
			SetCursor(newVisibleStatus: true);
			base.OnDestroy();
		}

		protected virtual void OnCameraSwitchHandler(CameraSwitch.ECameraType cameraType)
		{
			if (m_cameraSwitch != null)
			{
				m_cameraLabel.value = m_cameraSwitch?.GetCameraLabel() ?? "";
			}
			StopAndReleaseFadeCamCoroutine();
			if (base.gameObject.activeInHierarchy)
			{
				m_fadeCamLabelCoroutine = StartCoroutine(FadeCamLabelCoroutine());
			}
		}

		private IEnumerator FadeCamLabelCoroutine()
		{
			m_enableHUDCameraLabel.value = true;
			m_cameraLabelAlpha.value = 1f;
			while (m_cameraLabelAlpha.value > 0f)
			{
				m_cameraLabelAlpha.value -= 0.02f;
				yield return null;
			}
			m_enableHUDCameraLabel.value = false;
		}

		private void StopAndReleaseFadeCamCoroutine()
		{
			if (m_fadeCamLabelCoroutine != null)
			{
				StopCoroutine(m_fadeCamLabelCoroutine);
				m_fadeCamLabelCoroutine = null;
			}
		}

		protected virtual void OnSwitchCar(RaceCar raceCar)
		{
			if ((bool)speedometr)
			{
				speedometr.SetCar(raceCar);
			}
			if ((bool)boostGauge)
			{
				boostGauge.SetCar(raceCar);
			}
			MinimapProvider.instance.SetCar(raceCar);
			MinimapProvider.instance.OnSwitchCarHandlerInvoke();
			if ((bool)rearviewMirror)
			{
				rearviewMirror.SetCar(raceCar);
			}
			if ((bool)driftAngles)
			{
				driftAngles.SetCar(raceCar);
			}
		}

		protected virtual void Update()
		{
			MouseUpdate();
			SliderAngleUpdate();
		}

		protected virtual void FixedUpdate()
		{
			MouseFixedUpdate();
			HandleSliderAngleActivation();
		}

		private void HandleSliderAngleActivation()
		{
			PlayerCarControl instance = PlayerCarControl.instance;
			if (instance == null || instance.blockControl)
			{
				return;
			}
			BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
			if (baseRaceState == null)
			{
				return;
			}
			bool flag = NetworkController.InstanceGame?.LocalPlayer?.attachedRoom != null;
			if (!baseRaceState.allowCarRespawnInRace || (flag && !instance.canSpawnCarInMPRace))
			{
				return;
			}
			GameInput.InputManager instance2 = GameInput.InputManager.instance;
			if (!(instance2 == null))
			{
				if (instance2.IsActionPressed(ActionType.ShowInputBar))
				{
					SetAngleSlider(!m_showAngleSlider.value, useToggleAction: true);
				}
				if (instance2.IsActionPressed(ActionType.RespawnCar) && GameInput.InputManager.IsWheelConnected())
				{
					SetAngleSlider(needShow: true);
				}
			}
		}

		public void ReInit()
		{
			Init(States.mainCurrent as BaseRaceState);
		}

		private void Init(BaseRaceState state)
		{
			m_userCar = state.userCar;
			if ((bool)m_userCar)
			{
				m_driftController = m_userCar.GetComponent<DriftController>();
				if ((bool)raceDpAction)
				{
					raceDpAction.Init(m_driftController);
				}
				if ((bool)driftAngles)
				{
					driftAngles.Init(m_gamePrefs.globals, m_driftController);
				}
				OnSwitchCar(m_userCar);
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			Init(States.mainCurrent as BaseRaceState);
			m_mouse = ReInput.controllers.Mouse;
			m_showDriftHUD.value = !m_gamePrefs.globals.hideDriftHud;
			m_showDriftCounter.value = !m_gamePrefs.globals.hideDriftCounter;
			m_enableHUDCameraLabel.value = false;
			if (GameInput.InputManager.IsWheelConnected())
			{
				SetAngleSlider(needShow: true);
			}
		}

		protected override void OnDeactivate()
		{
			SetCursor(newVisibleStatus: true);
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			SetCursor(newVisibleStatus: true);
			if (mouseLocked.value)
			{
				mouseLocked.value = false;
				ResetCursorTime();
				return true;
			}
			return false;
		}

		[GameHotkey(ActionType.NavBack, false, false)]
		public void ShowPauseMenu()
		{
			if (!OnEscape() && States.current is BaseRaceState baseRaceState)
			{
				baseRaceState.LoseFocus();
			}
		}

		public static UIRaceModeContext GetContext(ContextOwner contextOwner, BaseRaceModeState state)
		{
			switch (state.raceMode)
			{
			case RaceMode.Drift:
				return contextOwner.Preload<UIRaceDriftModeContext>();
			case RaceMode.TimeAttack:
				return contextOwner.Preload<UIRaceTimeModeContext>();
			case RaceMode.XDS:
				return contextOwner.Preload<UIRaceXDSModeContext>();
			case RaceMode.NetworkFreeride:
				return contextOwner.Preload<UIRaceNetworkFreerideModeContext>();
			case RaceMode.Freeride:
			case RaceMode.Dyno:
				return contextOwner.Preload<UIRaceFreerideModeContext>();
			case RaceMode.HalloweenEvent:
				return contextOwner.Preload<UIRaceCustomEventModeContext>();
			default:
				return contextOwner.Preload<UIRaceModeContext>();
			}
		}

		private void MouseFixedUpdate()
		{
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (!(instance == null) && !instance.pollingMode && instance.IsActionPressed(ActionType.HideMouse))
			{
				ToggleHideMouseMode();
			}
		}

		private void ToggleHideMouseMode()
		{
			mouseLocked.value = !mouseLocked.value;
		}

		private void MouseUpdate()
		{
			if (m_inputParams == null)
			{
				m_inputParams = GameInput.InputManager.settings.Params;
			}
			if (IsMouseActive())
			{
				ResetCursorTime();
			}
			if (!CheckIfTurnActionBindedOnMouseAndHide())
			{
				SetCursor(!mouseLocked.value && Time.realtimeSinceStartup - m_cursorMoveTime < 3f, mouseLocked.value);
			}
		}

		private bool CheckIfTurnActionBindedOnMouseAndHide()
		{
			if (!mouseLocked.value)
			{
				return false;
			}
			Action actionInfo = GameInput.InputManager.instance.GetActionInfo(ActionType.Turn);
			if (actionInfo != null && actionInfo.lastActiveBind?.deviceInfo?.rewiredType == ControllerType.Mouse)
			{
				Cursor.visible = false;
				return true;
			}
			return false;
		}

		private void ResetCursorTime()
		{
			m_cursorMoveTime = Time.realtimeSinceStartup;
		}

		private void SetCursor(bool newVisibleStatus, bool lockState = false)
		{
			GameInput.InputManager.instance.SetCursorVisibility(newVisibleStatus, lockState);
		}

		private bool IsMouseActive()
		{
			if (mouseLocked.value)
			{
				return false;
			}
			if (m_mouse.GetAnyButton())
			{
				return true;
			}
			float sqrMagnitude = m_mouse.screenPositionDelta.sqrMagnitude;
			float num = 8.99f;
			if (sqrMagnitude > num)
			{
				return true;
			}
			return false;
		}
	}
}
