using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Championship;
using Cinemachine;
using DI;
using FMODUnity;
using GameInput;
using GameOverlay;
using HUD;
using Microsoft.Win32;
using PrefsModels;
using Rewired;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class XRModeManager : MonoBehaviour
{
	public enum CameraMode
	{
		XR = 0,
		NonXR = 1
	}

	private enum ShowXRRoomRule
	{
		NeedShowRoom = 0,
		NeedHideRoom = 1
	}

	private enum InvokeActionMoment
	{
		OnActivate = 0,
		OnDeactivate = 1
	}

	private struct XRRaceRoomActivityRule
	{
		public Action<BaseContext> onChangedActiveContext;

		public InvokeActionMoment invokeMoment;

		public ShowXRRoomRule showRule;
	}

	private const string OPEN_XR_SUBKEY_DIR = "SOFTWARE\\Khronos\\OpenXR\\1";

	private const string STEAM_VR_RUNTIME = "steamxr";

	private const string OCULUS_RUNTIME = "oculus";

	private const string AVAILABLE_RUNTIMES = "AvailableRuntimes";

	private const string XR_RUNTIME_JSON_VAR = "XR_RUNTIME_JSON";

	private const string XR_CANVAS_EVENT_CAMERA_NAME = "XRCanvasEventCamera";

	private const string CAM_MODE_HOLDER_NAME = "CamerasHolder";

	public static XRModeManager instance;

	private XRCanvasChanger m_XRCanvasChanger;

	private GamePrefs m_prefs;

	private GraphicsQuality m_graphicsQuality;

	[SerializeField]
	private GameObject m_XRRoomPrefab;

	[SerializeField]
	private GameObject m_XRCameraModePrefab;

	[SerializeField]
	private GameObject m_XRCursor;

	private GameObject m_camHolder;

	private GameObject m_XRModeContainer;

	private GameObject m_baseCameraContainer;

	private GameObject m_XRRoom;

	private Camera m_baseCamera;

	private Camera m_XRCamera;

	private Camera m_eventCamera;

	private CinemachineBrain m_XRCinemachine;

	private StudioListener m_XRFMODListener;

	private XROrigin m_XROrigin;

	private XRLoader m_XRLoader;

	private Vector3? m_cachedXROffsetRotation;

	private Vector3? m_cachedXROffsetPosition;

	private bool m_XRSubsystemActive;

	private bool m_initXRManagerPassed;

	public static Action<XRModeManager> onXRInstanceCreated;

	public static Action<XRModeManager> onXRInstanceDestroyed;

	public System.Action OnXRSubsystemActivated;

	public System.Action OnXRSubsystemDeactivated;

	public System.Action OnHeadDeviceMounted;

	public System.Action OnHeadDeviceUnmounted;

	public Action<Camera> OnCameraModeChanged;

	public Action<Camera> OnXRRoomActivated;

	public Action<Camera> OnXRRoomDeactivated;

	private bool m_isWaitingMountHeadDevice;

	private bool m_isHeadDeviceInited;

	private BaseState m_lastState;

	private BaseRaceModeState m_lastBaseRaceState;

	private bool m_isNowValidState = true;

	private float m_timeOnAwake;

	private bool m_needHandleTimeToAwake;

	private bool m_isXRInitedOnLaunch;

	private bool m_needForceInit;

	private bool m_XRRoomInRaceActivated;

	private bool m_onAboutExitState;

	private Dictionary<BaseRaceModeState, System.Action> m_subscribedOnShutdownStates = new Dictionary<BaseRaceModeState, System.Action>();

	private Dictionary<BaseContext, XRRaceRoomActivityRule> m_subscribedContexts = new Dictionary<BaseContext, XRRaceRoomActivityRule>();

	private string m_cachedEnvironmentXRVariableValue;

	private Coroutine m_XRCameraResetCoroutine;

	public CameraMode curActiveCameraMode { get; private set; } = CameraMode.NonXR;

	public bool XRSubsystemActive
	{
		get
		{
			return m_XRSubsystemActive;
		}
		private set
		{
			m_XRSubsystemActive = value;
			if (m_XRSubsystemActive)
			{
				OnXRSubsystemActivated.SafeCall();
			}
			else
			{
				OnXRSubsystemDeactivated.SafeCall();
			}
		}
	}

	public bool isHMDMounted
	{
		get
		{
			if (m_isWaitingMountHeadDevice)
			{
				return XRSettings.isDeviceActive;
			}
			return false;
		}
	}

	public bool isHMDWearing
	{
		get
		{
			if (m_isHeadDeviceInited)
			{
				return XRSettings.enabled;
			}
			return false;
		}
	}

	public bool isXRRoomInited => m_XRRoom != null;

	public bool isManagerSleeps => Time.realtimeSinceStartup <= m_timeOnAwake;

	private void Awake()
	{
		instance = this;
		onXRInstanceCreated.SafeCall(instance);
		m_XRCanvasChanger = GetComponent<XRCanvasChanger>();
		States.onStateActivate += HandleStateSwitch;
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		TimeController.StartNow(TryActivateXROnLaunch());
	}

	private void OnDestroy()
	{
		States.onStateActivate -= HandleStateSwitch;
		if (instance == this)
		{
			onXRInstanceDestroyed.SafeCall(instance);
			instance = null;
		}
	}

	private IEnumerator TryActivateXROnLaunch()
	{
		while (!Overlay.IsReady())
		{
			yield return null;
		}
		bool isSteamVRTarget = false;
		bool isOculusTarget = false;
		if (!Array.Exists(Environment.GetCommandLineArgs(), delegate(string arg)
		{
			isSteamVRTarget = arg.Contains("steamxr");
			isOculusTarget = !isSteamVRTarget && arg.Contains("oculus");
			return isSteamVRTarget || isOculusTarget;
		}) && !m_prefs.globals.XRActivated)
		{
			yield break;
		}
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Khronos\\OpenXR\\1");
		if (registryKey == null)
		{
			yield break;
		}
		RegistryKey registryKey2 = registryKey.OpenSubKey("AvailableRuntimes");
		if (registryKey2 == null)
		{
			yield break;
		}
		string[] valueNames = registryKey2.GetValueNames();
		if (valueNames != null && valueNames.Length != 0)
		{
			string targetKey = (isSteamVRTarget ? "steamxr" : "oculus");
			string text = Array.Find(valueNames, (string key) => key.Contains(targetKey));
			if (text != null)
			{
				m_cachedEnvironmentXRVariableValue = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
				Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", text);
			}
			yield return new WaitUntil(() => States.isInited() && GameManager.isInGarage);
			SetXRModeActive(needActivate: true);
			yield return OnPassedXRManagerInit();
		}
	}

	private IEnumerator OnPassedXRManagerInit()
	{
		yield return new WaitUntil(() => m_initXRManagerPassed);
		curActiveCameraMode = ((!XRSettings.enabled) ? CameraMode.NonXR : CameraMode.XR);
		m_isXRInitedOnLaunch = curActiveCameraMode == CameraMode.XR;
	}

	public static bool IsXRActive()
	{
		if (instance != null)
		{
			return instance.isHMDWearing;
		}
		return false;
	}

	private void HandleStateSwitch(BaseState currState)
	{
		if (IsLoadingState(currState))
		{
			if (m_lastState.parent is BaseRaceState || m_lastState is BaseRaceState)
			{
				OnExitRaceState();
			}
			m_isNowValidState = false;
		}
		else
		{
			if (m_lastState == currState && !m_needForceInit)
			{
				return;
			}
			m_needForceInit = false;
			m_isNowValidState = true;
			m_XRRoomInRaceActivated = false;
			m_lastState = currState;
			if (!(currState is GarageGUIState))
			{
				if (currState is BaseRaceState)
				{
					TryInitRaceSwitchParams();
				}
			}
			else
			{
				TryInitGarageXRRoom();
			}
		}
	}

	private bool IsLoadingState(BaseState currState)
	{
		if (!(States.mainCurrent is LoadRaceState) && !(currState is LoadRaceState))
		{
			return currState is LoadSceneState;
		}
		return true;
	}

	private void Update()
	{
		if (m_needHandleTimeToAwake)
		{
			if (isManagerSleeps)
			{
				return;
			}
			m_needHandleTimeToAwake = false;
		}
		if (m_isNowValidState)
		{
			HandleHelmetBehaviour();
			if (isHMDWearing)
			{
				HandleXRCursorPosition();
			}
		}
	}

	private void FixedUpdate()
	{
		ManageInput();
	}

	private void ManageInput()
	{
		GameInput.InputManager inputManager = GameInput.InputManager.instance;
		if (!(inputManager == null) && inputManager.IsActionPressed(ActionType.VRCamReset))
		{
			TryResetXRCamPosAndRotation();
		}
	}

	public void SleepForSecondsRealtime(float time)
	{
		m_timeOnAwake = Time.realtimeSinceStartup + time;
		m_needHandleTimeToAwake = true;
	}

	private void HandleHelmetBehaviour()
	{
		if (isHMDMounted)
		{
			HandleMount();
		}
		if (isHMDWearing)
		{
			HandleWearingHeadDevice();
		}
	}

	private void HandleMount()
	{
		UIMessageBox.HideBaseDialog();
		m_isWaitingMountHeadDevice = false;
		m_isHeadDeviceInited = true;
		CheckIfNeedCloseSettingsContext();
		ChangeActiveCameraModeBy(CameraMode.XR);
		OnHeadDeviceMounted.SafeCall();
	}

	private void HandleWearingHeadDevice()
	{
		if (!XRSettings.isDeviceActive && m_XRCamera != null && m_XRCamera.fieldOfView != 60f)
		{
			m_XRCamera.fieldOfView = 60f;
		}
	}

	private void HandleUnmount()
	{
		m_isHeadDeviceInited = false;
		TryShowPauseMenuIfNeed();
		TryHideMPContexts();
		TimeController.StartNow(ShowDialogNeedMountDevice());
		ChangeActiveCameraModeBy(CameraMode.NonXR);
		OnHeadDeviceUnmounted.SafeCall();
	}

	private void TrySetDisplayResolution()
	{
		if (m_graphicsQuality == null)
		{
			m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		}
		ReadOnlyCollection<GraphicSettings.ResolutionInfo> supportedResolutionList = m_graphicsQuality.supportedResolutionList;
		if (supportedResolutionList == null || supportedResolutionList.Count < 1)
		{
			return;
		}
		GraphicSettings.ResolutionInfo resolutionInfo = supportedResolutionList[supportedResolutionList.Count - 1];
		if (!resolutionInfo.Equals(m_graphicsQuality.resolution))
		{
			int num = 1920;
			int num2 = 1080;
			if (resolutionInfo.width < num || resolutionInfo.height < num2)
			{
				resolutionInfo = new GraphicSettings.ResolutionInfo(num, num2);
			}
			m_graphicsQuality.resolution = resolutionInfo;
		}
	}

	public void HandleXRCursorPosition()
	{
		if (!Cursor.visible && m_XRCursor.activeInHierarchy)
		{
			m_XRCursor.SetActive(value: false);
			return;
		}
		if (Cursor.visible && !m_XRCursor.activeInHierarchy)
		{
			m_XRCursor.SetActive(value: true);
		}
		if (m_XRCanvasChanger == null || m_XRCamera == null)
		{
			return;
		}
		Vector2 screenPosition = ReInput.controllers.Mouse.screenPosition;
		RectTransform rootRectTransform = m_XRCanvasChanger.GetRootRectTransform();
		if (!(rootRectTransform == null))
		{
			m_XRCanvasChanger.GetRenderTextureResolution(out var width, out var height);
			if (width != Screen.width || height != Screen.height)
			{
				m_XRCanvasChanger.SetRenderTextureResolution(Screen.width, Screen.height);
			}
			Camera cam = ((m_XRCanvasChanger.currentRenderMode == RenderMode.WorldSpace) ? m_eventCamera : m_baseCamera);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRectTransform, screenPosition, cam, out var localPoint);
			m_XRCursor.transform.localPosition = localPoint;
		}
	}

	private void TryInitGarageXRRoom()
	{
		TrySetActionOnShutdownRaceState(needUnsubscribe: true);
		if (isHMDWearing)
		{
			Camera.main.fieldOfView = 50f;
			TryInstantiateAndPlaceXRRoom();
			ApplyCachedXRTransformOffset();
			ChangeActiveCameraModeBy(CameraMode.XR, onInitializationState: true);
		}
		else
		{
			OnXRSubsystemActivated = (System.Action)Delegate.Remove(OnXRSubsystemActivated, new System.Action(TryInstantiateAndPlaceXRRoom));
			OnXRSubsystemActivated = (System.Action)Delegate.Combine(OnXRSubsystemActivated, new System.Action(TryInstantiateAndPlaceXRRoom));
		}
	}

	private void TryInitRaceSwitchParams()
	{
		if (!IsCameraContainerPlacedOnScene())
		{
			TrySetActionOnShutdownRaceState(needUnsubscribe: true);
			m_onAboutExitState = false;
			TryInstantiateAndPlaceXRRoom();
			m_lastBaseRaceState = m_lastState as BaseRaceModeState;
			TrySetActionOnShutdownRaceState(needUnsubscribe: false, ActivateXRRoomRace);
			if (isHMDWearing)
			{
				ChangeActiveCameraModeBy(CameraMode.XR, onInitializationState: true);
			}
		}
	}

	private void DeactivateXRRoomRace()
	{
		if (States.mainCurrent is BaseRaceState && isHMDWearing)
		{
			SetActiveXRRoom(needActivate: false);
			SetActiveXRRaceMode(needActivate: true);
			TrySetXRCinemachineActive(needActivate: true);
			TrySetFMODListenerActive(needActivate: true);
			m_XRRoomInRaceActivated = false;
			OnCameraModeChanged.SafeCall(m_XRCamera);
		}
	}

	private void ActivateXRRoomRace()
	{
		if (!(States.mainCurrent is BaseRaceState))
		{
			return;
		}
		m_needForceInit = true;
		if (!isHMDWearing)
		{
			return;
		}
		SetActiveXRRaceMode(needActivate: false);
		SetActiveXRRoom(needActivate: true);
		if (!(m_XROrigin == null))
		{
			TrySetXRCinemachineActive(needActivate: false);
			TrySetFMODListenerActive(needActivate: false);
			m_XROrigin.transform.localPosition = Vector3.zero;
			Transform transform = m_XRRoom.transform.FindChildDeph("Display");
			if (transform != null)
			{
				m_XROrigin.transform.rotation = Quaternion.LookRotation(transform.transform.position - m_XROrigin.transform.position);
			}
			m_XRRoomInRaceActivated = true;
			OnCameraModeChanged.SafeCall(m_baseCamera);
		}
	}

	private void TrySetXRCinemachineActive(bool needActivate)
	{
		if (!(m_XRCinemachine == null))
		{
			m_XRCinemachine.enabled = needActivate;
		}
	}

	private void TrySetFMODListenerActive(bool needActivate)
	{
		if (!(m_XRFMODListener == null))
		{
			m_XRFMODListener.enabled = needActivate;
		}
	}

	private void ChangeActiveCameraModeBy(CameraMode selectedCamera, bool onInitializationState = false)
	{
		curActiveCameraMode = selectedCamera;
		if (!IsCameraContainerPlacedOnScene())
		{
			return;
		}
		if (selectedCamera == CameraMode.XR)
		{
			LaunchXRModeByState(m_lastState, onInitializationState);
			if (m_XRCameraResetCoroutine != null)
			{
				TimeController.StopNow(m_XRCameraResetCoroutine);
				m_XRCameraResetCoroutine = null;
			}
			m_XRCameraResetCoroutine = TimeController.StartNow(ApplyCachedXRTransformOffsetAsync());
		}
		if (selectedCamera == CameraMode.NonXR)
		{
			LaunchNonXRModeByState(m_lastState, onInitializationState);
		}
	}

	public void TryResetXRCamPosAndRotation()
	{
		if (isHMDWearing && !(m_XRCamera == null) && !(m_XROrigin == null) && !(m_XROrigin.CameraFloorOffsetObject == null))
		{
			Transform transform = m_XRCamera.transform;
			Vector3 localEulerAngles = transform.localEulerAngles;
			Vector3 position = m_XROrigin.transform.position;
			Transform transform2 = m_XROrigin.CameraFloorOffsetObject.transform;
			if (!(Vector3.Distance(position, transform.position) < 1E-05f))
			{
				transform2.localEulerAngles = new Vector3(0f, 0f - localEulerAngles.y, 0f);
				transform2.position += position - transform.position;
				m_cachedXROffsetPosition = transform2.localPosition;
				m_cachedXROffsetRotation = transform2.localEulerAngles;
			}
		}
	}

	private IEnumerator ApplyCachedXRTransformOffsetAsync()
	{
		if (!isHMDWearing || m_XROrigin == null || !m_cachedXROffsetPosition.HasValue || !m_cachedXROffsetRotation.HasValue)
		{
			yield break;
		}
		GameObject curXRCamOffsetContainer = m_XROrigin.CameraFloorOffsetObject;
		if (curXRCamOffsetContainer == null || m_lastBaseRaceState == null)
		{
			yield break;
		}
		UIRaceModeContext raceContext = m_lastBaseRaceState.raceContext;
		if (!(raceContext == null))
		{
			while (!raceContext.isActive || !raceContext.uiBuilded)
			{
				yield return null;
			}
			curXRCamOffsetContainer.transform.localPosition = m_cachedXROffsetPosition.GetValueOrDefault();
			curXRCamOffsetContainer.transform.localEulerAngles = m_cachedXROffsetRotation.GetValueOrDefault();
			m_XRCameraResetCoroutine = null;
		}
	}

	private void ApplyCachedXRTransformOffset()
	{
		if (isHMDWearing && !(m_XROrigin == null) && m_cachedXROffsetPosition.HasValue && m_cachedXROffsetRotation.HasValue)
		{
			GameObject cameraFloorOffsetObject = m_XROrigin.CameraFloorOffsetObject;
			if (!(cameraFloorOffsetObject == null))
			{
				cameraFloorOffsetObject.transform.localPosition = m_cachedXROffsetPosition.GetValueOrDefault();
				cameraFloorOffsetObject.transform.localEulerAngles = m_cachedXROffsetRotation.GetValueOrDefault();
			}
		}
	}

	private void LaunchXRModeByState(BaseState state, bool onInitializationState)
	{
		m_XROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
		if (state is GarageGUIState || state is MultiplayerPCState || state is ChampionshipGameState)
		{
			SetActiveXRRoom(needActivate: true);
			OnCameraModeChanged.SafeCall(m_XRCamera);
		}
		else if (state is XDSRaceModeState || state.parent is XDSRaceModeState)
		{
			SubscribeOnInvokableXDSContexts();
			if (!onInitializationState)
			{
				SetActiveXRRaceMode(needActivate: true);
			}
			OnCameraModeChanged.SafeCall(m_XRCamera);
		}
		else if (state is BaseRaceState || state.parent is BaseRaceState)
		{
			if (state is Top32ViewReplayState)
			{
				ActivateXRRoomRace();
			}
			else if (IsPostCombatShownInXRRoom() || isRaceEnded())
			{
				SetActiveXRRoom(needActivate: true);
				OnCameraModeChanged.SafeCall(m_baseCamera);
			}
			else
			{
				SetActiveXRRaceMode(needActivate: true, onInitializationState);
				OnCameraModeChanged.SafeCall(m_XRCamera);
			}
		}
	}

	private void LaunchNonXRModeByState(BaseState state, bool onInitializationState)
	{
		if (state is GarageGUIState || state is MultiplayerPCState || state is ChampionshipGameState)
		{
			SetActiveXRRoom(needActivate: false);
		}
		if (state is BaseRaceState || state.parent is BaseRaceState)
		{
			if (IsPostCombatShownInXRRoom() || isRaceEnded())
			{
				SetActiveXRRoom(needActivate: false);
			}
			SetActiveXRRaceMode(needActivate: false);
		}
		OnCameraModeChanged.SafeCall(m_baseCamera);
	}

	private void SetActiveXRRaceMode(bool needActivate, bool prevXRStarted = false)
	{
		if (m_baseCameraContainer == null || m_XRModeContainer == null)
		{
			return;
		}
		m_baseCameraContainer.SetActive(!needActivate);
		m_XRModeContainer.SetActive(needActivate);
		if (needActivate)
		{
			UILoadingScreenContext loadingScreen = GetLoadingScreen();
			if (loadingScreen != null && loadingScreen.isActive)
			{
				prevXRStarted = true;
			}
			m_XRCanvasChanger.LaunchWorldSpaceCanvas(m_XRCamera, m_eventCamera, m_XROrigin.transform, prevXRStarted);
			if (CameraSwitch.instance != null)
			{
				CameraSwitch.instance.ForceApplyCockpitCamera();
			}
		}
		else
		{
			m_XRCanvasChanger.LaunchScreenSpaceOverlayCanvas(m_baseCamera);
		}
	}

	private void TryInstantiateAndPlaceXRRoom()
	{
		if (!isXRRoomInited)
		{
			InitXRCameraAndContrainer();
			m_XRRoom = UnityEngine.Object.Instantiate(m_XRRoomPrefab);
			m_XRRoom.transform.SetY((m_lastState is GarageGUIState) ? (-30f) : (-300f));
			m_XRModeContainer.transform.position = m_XRRoom.transform.position;
		}
	}

	private void InitXRCameraAndContrainer()
	{
		m_XRModeContainer = UnityEngine.Object.Instantiate(m_XRCameraModePrefab);
		m_XRCamera = m_XRModeContainer.GetComponentInChildren<Camera>();
		m_baseCameraContainer = GetRootMainCameraContainer();
		m_camHolder = new GameObject("CamerasHolder");
		UniteCamerasIntoContainer(m_camHolder.transform);
		m_XROrigin = m_XRModeContainer.GetComponentInChildren<XROrigin>();
		if (m_lastState is BaseRaceModeState || m_lastState.parent is BaseRaceModeState)
		{
			m_XRCamera.gameObject.AddComponent<UICameraCustomUpdateCaller>();
			m_XRCinemachine = m_XROrigin.gameObject.AddComponent<CinemachineBrain>();
			m_XRFMODListener = m_XROrigin.gameObject.AddComponent<StudioListener>();
			if (m_eventCamera == null)
			{
				CreateXREventCamera();
			}
		}
	}

	private void CreateXREventCamera()
	{
		GameObject gameObject = new GameObject("XRCanvasEventCamera", typeof(Camera));
		m_eventCamera = gameObject.GetComponent<Camera>();
		m_eventCamera.enabled = false;
		gameObject.transform.SetParent(m_XRCanvasChanger.canvasRootTransform);
		gameObject.transform.localPosition = new Vector3(0f, 0f, -1200f);
	}

	private void UniteCamerasIntoContainer(Transform parentTransform)
	{
		if (!(m_baseCameraContainer == null) && !(m_XRModeContainer == null))
		{
			m_baseCameraContainer.transform.SetParent(m_camHolder.transform);
			m_XRModeContainer.transform.SetParent(m_camHolder.transform);
		}
	}

	private void SetActiveXRRoom(bool needActivate)
	{
		if (!(m_XRRoom == null) && !(m_baseCameraContainer == null) && !(m_XRModeContainer == null))
		{
			m_XRRoom.SetActive(needActivate);
			m_baseCameraContainer.SetActive(value: true);
			m_XRModeContainer.SetActive(needActivate);
			if (needActivate)
			{
				m_XRCanvasChanger.ConfigureXRRoomCanvas(m_XRCamera, m_baseCamera);
				OnXRRoomActivated.SafeCall(m_baseCamera);
			}
			else
			{
				m_XRCanvasChanger.ConfigureNonXRRoomCanvas(m_XRCamera, m_baseCamera);
				OnXRRoomDeactivated.SafeCall(m_baseCamera);
			}
		}
	}

	public void SetXRModeActive(bool needActivate)
	{
		if (needActivate && !XRSettings.enabled)
		{
			TimeController.StartNow(ActivateXR());
		}
		else if (!needActivate && XRSettings.enabled)
		{
			DeactivateXR();
		}
	}

	private IEnumerator ActivateXR()
	{
		yield return StartXRSubsystem();
		if (!XRSubsystemActive)
		{
			ShowDialogCheckDeviceLink();
			GameLog.Print(AlertLevel.Warning, "VirtualReality", "Failed to start XR subsystem");
		}
		else
		{
			TimeController.StartNow(ShowDialogNeedMountDevice());
		}
	}

	private void DeactivateXR()
	{
		StopXRSubsystem();
		if (XRSubsystemActive)
		{
			GameLog.Print(AlertLevel.Warning, "VirtualReality", "Failed to deactivate XR");
			return;
		}
		m_isWaitingMountHeadDevice = false;
		m_isHeadDeviceInited = false;
		m_initXRManagerPassed = false;
		if (curActiveCameraMode == CameraMode.XR)
		{
			ChangeActiveCameraModeBy(CameraMode.NonXR);
		}
	}

	private IEnumerator StartXRSubsystem()
	{
		if (!XRSubsystemActive)
		{
			GameLog.Print(AlertLevel.Notify, "VirtualReality", "Initializing XR...");
			XRManagerSettings XRManager = XRGeneralSettings.Instance.Manager;
			if (XRManager.activeLoader == null)
			{
				yield return XRManager.InitializeLoader();
				m_XRLoader = XRManager.activeLoader;
			}
			if (m_XRLoader == null)
			{
				XRSubsystemActive = false;
				GameLog.Print(AlertLevel.Warning, "VirtualReality", "Initializing XR Failed.");
			}
			else
			{
				GameLog.Print(AlertLevel.Notify, "VirtualReality", "Starting XR...");
				ShowGodvoiceCamResetBinding();
				TryActivateXRCursor();
				TrySetDisplayResolution();
				XRManager.StartSubsystems();
				XRSubsystemActive = true;
			}
			m_initXRManagerPassed = true;
		}
	}

	private void StopXRSubsystem()
	{
		if (XRSubsystemActive)
		{
			GameLog.Print(AlertLevel.Notify, "VirtualReality", "Stopping XR...");
			DeactivateXRCursor();
			XRManagerSettings manager = XRGeneralSettings.Instance.Manager;
			manager.StopSubsystems();
			manager.DeinitializeLoader();
			XRSubsystemActive = false;
			GameLog.Print(AlertLevel.Notify, "VirtualReality", "XR stopped completely.");
		}
	}

	private IEnumerator ShowDialogNeedMountDevice()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState != null)
		{
			ContextOwner contextOwner = baseGameState.contextOwner;
			if (contextOwner != null)
			{
				UIBaseDialogContext baseDialog = contextOwner.Get<UIBaseDialogContext>();
				if (baseDialog != null && baseDialog.isActive)
				{
					DeactivateXR();
					yield return new WaitUntil(() => !baseDialog.isActive);
				}
			}
		}
		UIMessageBox.ShowDialog(LocalizationKeys.vr_put_on_head_device.Localized(), delegate
		{
			m_prefs.globals.XRActivated = false;
			DeactivateXR();
		}, new DialogButtonData[1]
		{
			new DialogButtonData
			{
				text = LocalizationKeys.Cancel.Localized(),
				action = delegate
				{
					m_prefs.globals.XRActivated = false;
					DeactivateXR();
				}
			}
		});
		if (!XRSubsystemActive)
		{
			yield return StartXRSubsystem();
		}
		m_isWaitingMountHeadDevice = true;
	}

	public static void ShowDialogNotAvailable()
	{
		UIMessageBox.ShowDialog(LocalizationKeys.vr_section_not_available.Localized(), XRCanvasChanger.GetActiveBaseDialogAndActivateOverride, new DialogButtonData[1]
		{
			new DialogButtonData
			{
				text = LocalizationKeys.Ok.Localized(),
				action = delegate
				{
					XRCanvasChanger.GetActiveBaseDialogAndActivateOverride();
				}
			}
		});
		XRCanvasChanger.GetActiveBaseDialogAndDisableOverride();
	}

	private void ShowDialogCheckDeviceLink()
	{
		UIMessageBox.ShowDialog(LocalizationKeys.vr_check_connect.Localized(), delegate
		{
			m_prefs.globals.XRActivated = false;
		}, new DialogButtonData[1]
		{
			new DialogButtonData
			{
				text = LocalizationKeys.Ok.Localized(),
				action = delegate
				{
					m_prefs.globals.XRActivated = false;
				}
			}
		});
	}

	private void ShowGodvoiceCamResetBinding()
	{
		BindElement vRCamResetKeyboardBind = GetVRCamResetKeyboardBind();
		if (vRCamResetKeyboardBind != null)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vr_reset_bind_godvoice.LocalizedFormat(vRCamResetKeyboardBind.elementName));
		}
	}

	public BindElement GetVRCamResetKeyboardBind()
	{
		return GameInput.InputManager.instance.GetActionInfo(ActionType.VRCamReset)?.activeBinds.Find((BindElement bind) => bind.deviceInfo.rewiredType == ControllerType.Keyboard);
	}

	private void CheckIfNeedCloseSettingsContext()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (!(baseGameState == null) && baseGameState.contextOwner.topContext is UIGeneralSettingsContext uIGeneralSettingsContext)
		{
			uIGeneralSettingsContext.NavCancel();
		}
	}

	private ContextOwner GetContextOwnerByState(BaseState baseState)
	{
		if (baseState == null)
		{
			return null;
		}
		BaseGameState baseGameState = baseState as BaseGameState;
		if (baseGameState == null)
		{
			return null;
		}
		return baseGameState.contextOwner;
	}

	public static UILoadingScreenContext GetLoadingScreen()
	{
		LoadRaceState loadRaceState = States.State<LoadRaceState>();
		if (loadRaceState == null)
		{
			return null;
		}
		return loadRaceState.contextOwner?.Get<UILoadingScreenContext>();
	}

	private void TryShowPauseMenuIfNeed()
	{
		BaseRaceModeState baseRaceModeState = m_lastState as BaseRaceModeState;
		if (!(baseRaceModeState == null) && !(m_lastState is InGameMenuState) && IsSinglePlayerRaceMode(baseRaceModeState.raceMode))
		{
			bool flag = IsLoadingContext() || IsPostCombatShownInXRRoom() || isRaceEnded();
			if (m_lastState is XDSRaceModeState xDSRaceModeState)
			{
				flag |= isXDSPlayerProfileContext(xDSRaceModeState);
			}
			if (!flag)
			{
				InGameMenuState.Show();
			}
		}
	}

	private void TryHideMPContexts()
	{
		ContextOwner contextOwnerByState = GetContextOwnerByState(States.current);
		if (contextOwnerByState == null)
		{
			return;
		}
		BaseContext topContext = contextOwnerByState.topContext;
		if (topContext == null)
		{
			return;
		}
		if (!(topContext is UIRaceStatsContext) && !(topContext is UICreateCompetitionContext) && !(topContext is UIJoinCompetitionContext))
		{
			if (topContext is UIReportContext)
			{
				topContext.Hide();
				UIRaceStatsContext uIRaceStatsContext = contextOwnerByState.Get<UIRaceStatsContext>();
				if (uIRaceStatsContext != null)
				{
					uIRaceStatsContext.Hide();
				}
			}
		}
		else
		{
			topContext.Hide();
		}
	}

	private bool isXDSPlayerProfileContext(XDSRaceModeState XDSRaceModeState)
	{
		ContextOwner contextOwner = XDSRaceModeState.contextOwner;
		if (contextOwner == null)
		{
			return false;
		}
		UIPlayerProfileContext uIPlayerProfileContext = contextOwner.Get<UIPlayerProfileContext>();
		if (uIPlayerProfileContext == null)
		{
			return false;
		}
		return uIPlayerProfileContext.isActive;
	}

	private bool isRaceEnded()
	{
		BaseRaceState baseRaceState = m_lastState as BaseRaceState;
		if (baseRaceState == null)
		{
			return false;
		}
		if (!baseRaceState.isRaceCompleted)
		{
			return baseRaceState.isRaceEnded;
		}
		return true;
	}

	private bool IsLoadingContext()
	{
		LoadRaceState loadRaceState = States.State<LoadRaceState>();
		if (loadRaceState == null)
		{
			return false;
		}
		ContextOwner contextOwner = loadRaceState.contextOwner;
		if (contextOwner == null)
		{
			return false;
		}
		UILoadingScreenContext uILoadingScreenContext = contextOwner.Get<UILoadingScreenContext>();
		if (uILoadingScreenContext == null || !uILoadingScreenContext.isActive)
		{
			return false;
		}
		return true;
	}

	private bool IsPostCombatShownInXRRoom()
	{
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		if (baseRaceModeState == null)
		{
			return false;
		}
		if (baseRaceModeState.IsPostcombatNowActive)
		{
			return isXRRoomInited;
		}
		return false;
	}

	private bool IsSinglePlayerRaceMode(RaceMode raceMode)
	{
		if (raceMode != RaceMode.Drift && raceMode != RaceMode.Dyno && raceMode != RaceMode.Freeride && raceMode != RaceMode.TimeAttack)
		{
			return raceMode == RaceMode.XDS;
		}
		return true;
	}

	private bool IsCameraContainerPlacedOnScene()
	{
		if (m_camHolder != null)
		{
			return m_camHolder.activeInHierarchy;
		}
		return false;
	}

	private GameObject GetRootMainCameraContainer()
	{
		m_baseCamera = Camera.main;
		if (m_baseCamera == null)
		{
			return null;
		}
		return m_baseCamera.transform.root.gameObject;
	}

	private void SubscribeOnInvokableXDSContexts()
	{
		XDSRaceModeState xDSRaceModeState = m_lastState as XDSRaceModeState;
		if (xDSRaceModeState == null)
		{
			xDSRaceModeState = m_lastState.parent as XDSRaceModeState;
			if (xDSRaceModeState == null)
			{
				return;
			}
		}
		XDSRaceModeState xDSRaceModeState2 = xDSRaceModeState;
		xDSRaceModeState2.onPreRestart = (System.Action)Delegate.Remove(xDSRaceModeState2.onPreRestart, new System.Action(ActivateXRRoomRace));
		XDSRaceModeState xDSRaceModeState3 = xDSRaceModeState;
		xDSRaceModeState3.onPreRestart = (System.Action)Delegate.Combine(xDSRaceModeState3.onPreRestart, new System.Action(ActivateXRRoomRace));
		XDSRaceModeState xDSRaceModeState4 = xDSRaceModeState;
		xDSRaceModeState4.isAboutToDeactivateUserCarAction = (System.Action)Delegate.Remove(xDSRaceModeState4.isAboutToDeactivateUserCarAction, new System.Action(ActivateXRRoomRace));
		XDSRaceModeState xDSRaceModeState5 = xDSRaceModeState;
		xDSRaceModeState5.isAboutToDeactivateUserCarAction = (System.Action)Delegate.Combine(xDSRaceModeState5.isAboutToDeactivateUserCarAction, new System.Action(ActivateXRRoomRace));
		ContextOwner contextOwnerByState = GetContextOwnerByState(xDSRaceModeState);
		if (contextOwnerByState != null)
		{
			SubscribeOnChangeActiveStateContext(contextOwnerByState.Get<UICarSelectorContext>(), ShowXRRoomRule.NeedShowRoom, HandleLaunchXRRaceRoom, InvokeActionMoment.OnActivate);
			SubscribeOnChangeActiveStateContext(contextOwnerByState.Get<UIXDSPreCombatWheelsContext>(), ShowXRRoomRule.NeedShowRoom, HandleLaunchXRRaceRoom, InvokeActionMoment.OnActivate);
			SubscribeOnChangeActiveStateContext(contextOwnerByState.Get<UIXDSPostCombatContext>(), ShowXRRoomRule.NeedShowRoom, HandleLaunchXRRaceRoom, InvokeActionMoment.OnActivate);
			SubscribeOnChangeActiveStateContext(contextOwnerByState.Get<UIRaceXDSModeContext>(), ShowXRRoomRule.NeedHideRoom, HandleLaunchXRRaceRoom, InvokeActionMoment.OnActivate);
		}
	}

	private void SubscribeOnChangeActiveStateContext(BaseContext context, ShowXRRoomRule invokeRule, Action<BaseContext> OnActiveChanged, InvokeActionMoment invokeMoment)
	{
		if (!(context == null))
		{
			context.onActiveChanged = (Action<BaseContext>)Delegate.Remove(context.onActiveChanged, OnActiveChanged);
			context.onActiveChanged = (Action<BaseContext>)Delegate.Combine(context.onActiveChanged, OnActiveChanged);
			if (!m_subscribedContexts.ContainsKey(context))
			{
				m_subscribedContexts.Add(context, new XRRaceRoomActivityRule
				{
					onChangedActiveContext = OnActiveChanged,
					showRule = invokeRule,
					invokeMoment = invokeMoment
				});
			}
		}
	}

	private void OnExitRaceState()
	{
		m_onAboutExitState = true;
		ActivateXRRoomRace();
		UnsubscribeOnActiveChangeContexts();
	}

	private void UnsubscribeOnActiveChangeContexts()
	{
		if (m_subscribedContexts == null || m_subscribedContexts.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<BaseContext, XRRaceRoomActivityRule> subscribedContext in m_subscribedContexts)
		{
			BaseContext key = subscribedContext.Key;
			key.onActiveChanged = (Action<BaseContext>)Delegate.Remove(key.onActiveChanged, subscribedContext.Value.onChangedActiveContext);
		}
		m_subscribedContexts.Clear();
	}

	private void HandleLaunchXRRaceRoom(BaseContext baseContext)
	{
		XRRaceRoomActivityRule xRRaceRoomActivityRule = m_subscribedContexts[baseContext];
		if ((baseContext.isActive && xRRaceRoomActivityRule.invokeMoment == InvokeActionMoment.OnActivate) || (!baseContext.isActive && xRRaceRoomActivityRule.invokeMoment == InvokeActionMoment.OnDeactivate) || m_onAboutExitState)
		{
			TimeController.StartNow(TryToggleXRRaceRoomActivityOnBuildedContext(baseContext, xRRaceRoomActivityRule.showRule));
		}
	}

	private IEnumerator TryToggleXRRaceRoomActivityOnBuildedContext(BaseContext baseContext, ShowXRRoomRule showRule)
	{
		if (!(baseContext == null))
		{
			while (!baseContext.uiBuilded)
			{
				yield return null;
			}
			if (showRule == ShowXRRoomRule.NeedShowRoom && !m_XRRoomInRaceActivated)
			{
				ActivateXRRoomRace();
			}
			else if (showRule == ShowXRRoomRule.NeedHideRoom && m_XRRoomInRaceActivated)
			{
				DeactivateXRRoomRace();
			}
		}
	}

	private void TrySetActionOnShutdownRaceState(bool needUnsubscribe = false, System.Action onShutdownAction = null)
	{
		if (!(m_lastBaseRaceState == null))
		{
			bool flag = m_subscribedOnShutdownStates.ContainsKey(m_lastBaseRaceState);
			if (!needUnsubscribe && onShutdownAction != null && !flag)
			{
				BaseRaceModeState lastBaseRaceState = m_lastBaseRaceState;
				lastBaseRaceState.OnShutdown = (System.Action)Delegate.Combine(lastBaseRaceState.OnShutdown, onShutdownAction);
				m_subscribedOnShutdownStates[m_lastBaseRaceState] = onShutdownAction;
			}
			else if (flag)
			{
				BaseRaceModeState lastBaseRaceState2 = m_lastBaseRaceState;
				lastBaseRaceState2.OnShutdown = (System.Action)Delegate.Remove(lastBaseRaceState2.OnShutdown, m_subscribedOnShutdownStates[m_lastBaseRaceState]);
				m_subscribedOnShutdownStates.Remove(m_lastBaseRaceState);
			}
		}
	}

	private void TryActivateXRCursor()
	{
		if (!(m_XRCanvasChanger == null))
		{
			m_XRCursor.SetActive(value: true);
			m_XRCursor.transform.SetParent(m_XRCanvasChanger.canvasRootTransform);
			Canvas component = m_XRCursor.GetComponent<Canvas>();
			if (component != null)
			{
				component.overrideSorting = true;
			}
			m_XRCursor.transform.localScale = Vector3.one;
			m_XRCursor.transform.localEulerAngles = Vector3.zero;
		}
	}

	private void DeactivateXRCursor()
	{
		m_XRCursor.SetActive(value: false);
	}

	public static void TryApplyCockpitCamera()
	{
		CameraSwitch cameraSwitch = CameraSwitch.instance;
		if (cameraSwitch != null && IsXRActive())
		{
			cameraSwitch.ForceApplyCockpitCamera();
		}
	}

	private void OnApplicationQuit()
	{
		StopXRSubsystem();
		Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", m_cachedEnvironmentXRVariableValue);
	}
}
