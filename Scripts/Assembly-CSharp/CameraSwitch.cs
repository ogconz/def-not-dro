using System;
using System.Collections.Generic;
using CarX;
using DI;
using GameInput;
using HUD;
using PrefsModels;
using Rewired;
using SyncMultiplayer;
using TrackLayout.Behaviours;
using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Scripts/Camera/CameraSwitch")]
public class CameraSwitch : MonoBehaviour
{
	public enum ECameraType
	{
		Race = 0,
		Replay = 1,
		PhotoSession = 2,
		Follow = 3
	}

	[Serializable]
	public class RearCameraRefInfo
	{
		public int[] mainIndexes;
	}

	private const float DEG_360 = 360f;

	private Camera m_mainCamera;

	[SerializeField]
	private BaseCamera[] m_raceCameras;

	[SerializeField]
	private BaseCamera[] m_rearCameras;

	[SerializeField]
	private RearCameraRefInfo[] m_mainToRearCameraRefs;

	[SerializeField]
	private BaseCamera[] m_replayCameras;

	[SerializeField]
	private BaseCamera[] m_photoSessionCameras;

	[SerializeField]
	private FollowCamera m_followCamera;

	[SerializeField]
	private VolumeProfile m_cockpitVolumeProfile;

	private int m_raceCameraIndex;

	private int m_replayCameraIndex;

	private int m_photoSessionCameraIndex;

	private ECameraType m_lastCameraType;

	private int m_lastCameraIndex;

	private RaceCar m_raceCar;

	private PlayerCarControl m_playerCarControl;

	private Car m_car;

	private bool m_outsideView;

	private bool m_frontStaticView;

	private GlobalModel m_globals;

	private bool m_isLastRearCameraActive;

	private float m_timerOffsetRevert;

	public float pauseRevert = 3f;

	private bool m_viewModeEnabled;

	private float m_revertOffsetCamera = 2f;

	protected GamePrefs m_prefs;

	private CARXRearCamera m_freemodeCam;

	private float m_rotX;

	private float m_rotY;

	private bool m_isCameraSwitchActive = true;

	private UIFreeCamContext m_freeCameraUI;

	private Volume m_cockpitVolume;

	private bool isAnotherPlayer;

	public RaceCar targetRaceCar => m_raceCar;

	public bool rotateMode { get; set; } = true;

	public bool isThirdPersonCamera
	{
		get
		{
			BaseCamera baseCamera = FindActiveCamera();
			if (!(baseCamera is CARXRearCamera))
			{
				return baseCamera is CameraPhotoSession;
			}
			return true;
		}
	}

	public static CameraSwitch instance { get; private set; }

	public ECameraType cameraType { get; private set; }

	public int cameraIndex => cameraType switch
	{
		ECameraType.Race => m_raceCameraIndex, 
		ECameraType.Replay => m_replayCameraIndex, 
		ECameraType.PhotoSession => m_photoSessionCameraIndex, 
		_ => 0, 
	};

	public event Action<ECameraType> onCameraChanged;

	public event Action<RaceCar> onSwitchCar;

	public string GetCameraLabel()
	{
		return cameraType switch
		{
			ECameraType.Follow => m_followCamera.label, 
			ECameraType.Race => m_raceCameras[cameraIndex].label, 
			_ => string.Empty, 
		};
	}

	public void SetCar(RaceCar car, bool resetInterpolation = false)
	{
		if (m_raceCar != null)
		{
			m_raceCar.EnableCarXMultiRaycast(enable: false);
			m_raceCar.ResetHandBrakeShiftAnimation();
		}
		m_raceCar = car;
		if (m_raceCar != null)
		{
			m_raceCar.EnableCarXMultiRaycast(enable: true);
		}
		Car car2 = (car ? car.carX : null);
		m_playerCarControl = PlayerCarControl.instance;
		if (car != null)
		{
			if ((bool)m_followCamera)
			{
				m_followCamera.SetTarget(car);
			}
			BaseCamera[] raceCameras = m_raceCameras;
			for (int i = 0; i < raceCameras.Length; i++)
			{
				raceCameras[i].SetTarget(car);
			}
			raceCameras = m_rearCameras;
			for (int i = 0; i < raceCameras.Length; i++)
			{
				raceCameras[i].SetTarget(car);
			}
			raceCameras = m_replayCameras;
			for (int i = 0; i < raceCameras.Length; i++)
			{
				raceCameras[i].SetTarget(car);
			}
			raceCameras = m_photoSessionCameras;
			for (int i = 0; i < raceCameras.Length; i++)
			{
				raceCameras[i].SetTarget(car);
			}
			SetVisibleNickName(m_raceCar);
		}
		if (car != null && m_car != car2)
		{
			BaseCamera baseCamera = FindActiveCamera();
			if (m_playerCarControl.car != car && !car.isGhost)
			{
				isAnotherPlayer = true;
				SetDefaulsCockpitSettings(m_raceCameras[m_raceCameraIndex]);
			}
			else
			{
				isAnotherPlayer = false;
				ApplyCustomSettings(baseCamera, car.carId);
			}
			m_car = car2;
			InformChangeCameraView(car.carX, !(baseCamera is CockpitCamera), baseCamera is CARXStaticCamera);
		}
		else
		{
			m_car = car2;
		}
		UpdateNetworkTrack();
		ResetActiveRaceCamera(resetInterpolation);
		this.onSwitchCar.SafeCall(m_raceCar);
	}

	private void ResetActiveRaceCamera(bool resetInterpolation)
	{
		if (!resetInterpolation || (cameraType != ECameraType.Race && cameraType != ECameraType.Replay))
		{
			return;
		}
		BaseCamera[] raceCameras = m_raceCameras;
		foreach (BaseCamera baseCamera in raceCameras)
		{
			if (baseCamera.enabled)
			{
				baseCamera.ResetInterpolation();
			}
		}
	}

	public BaseCamera FindActiveCamera()
	{
		BaseCamera[] array = null;
		switch (cameraType)
		{
		case ECameraType.Follow:
			if (m_followCamera.enabled)
			{
				return m_followCamera;
			}
			return null;
		case ECameraType.PhotoSession:
			array = m_photoSessionCameras;
			break;
		case ECameraType.Race:
			array = m_raceCameras;
			break;
		case ECameraType.Replay:
			array = m_replayCameras;
			break;
		}
		return Array.Find(array, (BaseCamera cam) => cam.enabled);
	}

	public void UpdateNetworkTrack()
	{
		if (!(States.mainCurrent as SyncNetFreerideRaceModeState))
		{
			TrackLayoutController trackLayoutController = Singleton<TrackLayoutController>.instance;
			if (trackLayoutController != null)
			{
				trackLayoutController.ResetPlacementOnAdditionalScene(forceDelayToReset: true);
			}
		}
		else
		{
			NetGameSubroomsSystem netGameSubroomsSystem = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
			if ((bool)m_raceCar && m_raceCar.networkPlayer != null)
			{
				netGameSubroomsSystem.ShowMap(m_raceCar.networkPlayer);
			}
			else
			{
				netGameSubroomsSystem.ShowMap(NetworkController.InstanceGame.LocalPlayer);
			}
		}
	}

	public RaceCar GetCar()
	{
		return m_raceCar;
	}

	public void SetActiveCameraSwitch(bool isActive)
	{
		m_isCameraSwitchActive = isActive;
	}

	public void NextRaceCamera(bool activate)
	{
		TryShiftRaceCameraIndex(isNext: true, activate);
	}

	public void PrevRaceCamera(bool activate)
	{
		TryShiftRaceCameraIndex(isNext: false, activate);
	}

	public void ForceApplyCockpitCamera()
	{
		for (int i = 0; i < m_raceCameras.Length; i++)
		{
			if (m_raceCameras[i] is CockpitCamera)
			{
				m_raceCameraIndex = i;
				m_globals.lastCameraId = m_raceCameraIndex;
				ActivateRaceCamera();
				if (isAnotherPlayer)
				{
					SetDefaulsCockpitSettings(m_raceCameras[i]);
				}
				SetVisibleNickName(m_raceCar);
				break;
			}
		}
	}

	private void TryShiftRaceCameraIndex(bool isNext, bool activate)
	{
		if (!m_isCameraSwitchActive)
		{
			return;
		}
		if (isNext)
		{
			m_raceCameraIndex++;
		}
		else
		{
			m_raceCameraIndex = m_raceCameraIndex + m_raceCameras.Length - 1;
		}
		m_raceCameraIndex %= m_raceCameras.Length;
		m_globals.lastCameraId = m_raceCameraIndex;
		if (activate)
		{
			ActivateRaceCamera();
			if (isAnotherPlayer && m_raceCameras[m_raceCameraIndex] is CockpitCamera)
			{
				SetDefaulsCockpitSettings(m_raceCameras[m_raceCameraIndex]);
			}
			if (m_raceCameras[m_raceCameraIndex] is RearCamera { focusTime: not 0f } rearCamera)
			{
				rearCamera.ResetFocus();
			}
		}
		SetVisibleNickName(m_raceCar);
	}

	public void NextReplayCamera()
	{
		m_replayCameraIndex = (m_replayCameraIndex + 1) % m_replayCameras.Length;
		ActivateReplayCamera();
	}

	public bool CheckForOutsideView(Car checkedCar)
	{
		if (m_car != null && checkedCar != null && checkedCar == m_car)
		{
			return m_outsideView;
		}
		return true;
	}

	public bool CheckForFrontStaticView(Car checkedCar)
	{
		if (m_car != null && checkedCar != null && checkedCar == m_car)
		{
			return m_frontStaticView;
		}
		return false;
	}

	private static void CamerasDeactivate(IList<BaseCamera> baseCameras)
	{
		for (int i = 0; i < baseCameras.Count; i++)
		{
			baseCameras[i].enabled = false;
		}
	}

	private static void CameraSetActivate(BaseCamera cam, bool value)
	{
		if ((bool)cam)
		{
			cam.enabled = value;
		}
	}

	public void ActivateRaceCamera()
	{
		CameraSetActivate(m_followCamera, value: false);
		CamerasDeactivate(m_replayCameras);
		CamerasDeactivate(m_photoSessionCameras);
		CamerasDeactivate(m_rearCameras);
		if (m_raceCameraIndex >= 0 && m_raceCameraIndex < m_raceCameras.Length)
		{
			for (int i = 0; i < m_raceCameraIndex; i++)
			{
				m_raceCameras[i].enabled = false;
			}
			for (int j = m_raceCameraIndex + 1; j < m_raceCameras.Length; j++)
			{
				m_raceCameras[j].enabled = false;
			}
			m_raceCameras[m_raceCameraIndex].enabled = true;
			ApplyCustomSettings(m_raceCameras[m_raceCameraIndex], m_prefs.globals.lastCarId);
			BaseCamera baseCamera = m_raceCameras[m_raceCameraIndex];
			InformChangeCameraView(m_car, !(baseCamera is CockpitCamera), baseCamera is CARXStaticCamera);
			ChangeCameraType(ECameraType.Race);
		}
		else
		{
			for (int k = 0; k < m_raceCameras.Length; k++)
			{
				m_raceCameras[k].enabled = false;
			}
		}
	}

	public void SaveLastActiveCam()
	{
		m_lastCameraType = cameraType;
		m_lastCameraIndex = cameraIndex;
	}

	public void ActivateLastSavedCam()
	{
		ActivateCamera(m_lastCameraType, m_lastCameraIndex);
	}

	private void SetDefaulsCockpitSettings(BaseCamera camera)
	{
		CARXCockpitCamera cARXCockpitCamera = camera as CARXCockpitCamera;
		if (!(cARXCockpitCamera == null))
		{
			cARXCockpitCamera.offset = cARXCockpitCamera.defaultOffset;
			cARXCockpitCamera.eulerAngles = cARXCockpitCamera.defaultEulerAngles;
			cARXCockpitCamera.dynamicCameraMode = m_globals.dynamicCamParams.cockpitCamMode;
		}
	}

	private void ApplyCustomSettings(BaseCamera camera, int carId)
	{
		if (!(camera == null) && !string.IsNullOrEmpty(camera.identifier))
		{
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			string id = carId + camera.identifier;
			CarCameraSettings cameraSettings = carSettings.GetCameraSettings(id);
			if (cameraSettings == null)
			{
				SetDefaulsCockpitSettings(camera);
			}
			else if (!(camera is CARXRearCamera) && camera is CARXCockpitCamera cARXCockpitCamera)
			{
				cARXCockpitCamera.offset = cameraSettings.offset;
				cARXCockpitCamera.eulerAngles = new Vector3(cameraSettings.pitch, cARXCockpitCamera.eulerAngles.y, cARXCockpitCamera.eulerAngles.z);
				cARXCockpitCamera.dynamicCameraMode = m_globals.dynamicCamParams.cockpitCamMode;
				cARXCockpitCamera.velocityScale = ((m_globals.dynamicCamParams.cockpitCamMode > GlobalModel.DynamicCockpitCamMode.OFF) ? m_globals.dynamicCamParams.velocityScale : 0f);
				cARXCockpitCamera.SetFOV(m_globals.cockpitFov);
			}
		}
	}

	private void ChangeCameraType(ECameraType type)
	{
		cameraType = type;
		this.onCameraChanged.SafeCall(cameraType);
	}

	private int FindRearByRaceCameraIndex(int raceCameraIndex)
	{
		for (int i = 0; i < m_mainToRearCameraRefs.Length; i++)
		{
			int[] mainIndexes = m_mainToRearCameraRefs[i].mainIndexes;
			for (int j = 0; j < mainIndexes.Length; j++)
			{
				if (mainIndexes[j] == raceCameraIndex)
				{
					return i;
				}
			}
		}
		return 0;
	}

	private void SetVisibleNickName(RaceCar car)
	{
		if (car != null && car.prefs.globals.showNickNames)
		{
			car.SetVisibleUIName(CheckForOutsideView(car.carX));
		}
	}

	public void ActivateRearRaceCamera()
	{
		CameraSetActivate(m_followCamera, value: false);
		CamerasDeactivate(m_replayCameras);
		CamerasDeactivate(m_photoSessionCameras);
		CamerasDeactivate(m_raceCameras);
		int num = FindRearByRaceCameraIndex(m_raceCameraIndex);
		for (int i = 0; i < m_rearCameras.Length; i++)
		{
			m_rearCameras[i].enabled = i == num;
		}
		if (num >= 0 && num < m_rearCameras.Length)
		{
			BaseCamera baseCamera = m_rearCameras[num];
			InformChangeCameraView(m_car, !(baseCamera is CockpitCamera), baseCamera is CARXStaticCamera);
			ChangeCameraType(ECameraType.Race);
		}
	}

	public void ActivateReplayCamera()
	{
		m_globals.lastReplayCameraIndex = m_replayCameraIndex;
		CameraSetActivate(m_followCamera, value: false);
		CamerasDeactivate(m_raceCameras);
		CamerasDeactivate(m_rearCameras);
		CamerasDeactivate(m_photoSessionCameras);
		for (int i = 0; i < m_replayCameras.Length; i++)
		{
			BaseCamera baseCamera = m_replayCameras[i];
			baseCamera.enabled = i == m_replayCameraIndex;
			if (baseCamera.enabled && baseCamera is RearCamera rearCamera)
			{
				rearCamera.rotationY = 0f;
				rearCamera.ResetInterpolation();
			}
		}
		if (m_replayCameraIndex >= 0 && m_replayCameraIndex < m_replayCameras.Length)
		{
			BaseCamera baseCamera2 = m_replayCameras[m_replayCameraIndex];
			ChangeCameraType(ECameraType.Replay);
			InformChangeCameraView(m_car, !(baseCamera2 is CockpitCamera), baseCamera2 is CARXStaticCamera);
		}
	}

	public void ActivatePhotoSessionCamera()
	{
		CameraSetActivate(m_followCamera, value: false);
		CamerasDeactivate(m_raceCameras);
		CamerasDeactivate(m_rearCameras);
		CamerasDeactivate(m_replayCameras);
		for (int i = 0; i < m_photoSessionCameras.Length; i++)
		{
			m_photoSessionCameras[i].enabled = i == m_photoSessionCameraIndex;
		}
		if ((bool)m_car && m_photoSessionCameraIndex >= 0 && m_photoSessionCameraIndex < m_photoSessionCameras.Length)
		{
			InformChangeCameraView(m_car, outside: true, m_photoSessionCameras[m_photoSessionCameraIndex] is CARXStaticCamera);
			ChangeCameraType(ECameraType.PhotoSession);
		}
	}

	public void ActivateFollowCamera()
	{
		CamerasDeactivate(m_raceCameras);
		CamerasDeactivate(m_rearCameras);
		CamerasDeactivate(m_replayCameras);
		CamerasDeactivate(m_photoSessionCameras);
		CameraSetActivate(m_followCamera, value: true);
		InformChangeCameraView(m_car, outside: true, frontCam: false);
		ChangeCameraType(ECameraType.Follow);
	}

	public void ActivateCamera(ECameraType type, int index)
	{
		switch (type)
		{
		case ECameraType.Race:
			m_raceCameraIndex = index;
			ActivateRaceCamera();
			break;
		case ECameraType.Replay:
			m_replayCameraIndex = index;
			ActivateReplayCamera();
			break;
		case ECameraType.PhotoSession:
			m_photoSessionCameraIndex = index;
			ActivatePhotoSessionCamera();
			break;
		case ECameraType.Follow:
			ActivateFollowCamera();
			break;
		}
	}

	public void InformChangeCameraView(Car car, bool outside, bool frontCam)
	{
		m_outsideView = outside;
		m_frontStaticView = frontCam;
		if (m_cockpitVolume != null)
		{
			m_cockpitVolume.enabled = !outside;
		}
		if (!(car != null))
		{
			return;
		}
		RaceCar component = car.GetComponent<RaceCar>();
		if (!(component != null))
		{
			return;
		}
		if (component.IsVisualLoaded)
		{
			UpdateCarCamera(component, outside);
			return;
		}
		component.OnCarVisualLoadFinishEvent += delegate(RaceCar loadedCar)
		{
			UpdateCarCamera(loadedCar, outside);
		};
	}

	private void UpdateCarCamera(RaceCar car, bool outside)
	{
		CarCameraSwitchHandler driverCamSwitchHandler = car.driverCamSwitchHandler;
		if (driverCamSwitchHandler != null)
		{
			driverCamSwitchHandler.OnChangeCameraView(outside);
		}
	}

	public void ResetCamera()
	{
		m_outsideView = true;
		m_raceCameraIndex = m_globals.lastCameraId;
		if (m_raceCameraIndex >= m_raceCameras.Length)
		{
			m_raceCameraIndex = 0;
		}
		m_replayCameraIndex = m_globals.lastReplayCameraIndex;
		m_photoSessionCameraIndex = 0;
		ActivateRaceCamera();
	}

	public void DeattachCam()
	{
		if (cameraType != ECameraType.Race)
		{
			return;
		}
		if (m_freeCameraUI != null)
		{
			m_freeCameraUI.CacheRaceCameraSelection(m_lastCameraType, m_raceCameraIndex);
		}
		int raceCameraIndex = m_raceCameraIndex;
		CARXRearCamera cARXRearCamera = m_raceCameras[m_raceCameraIndex] as CARXRearCamera;
		while (cARXRearCamera == null)
		{
			PrevRaceCamera(activate: true);
			cARXRearCamera = m_raceCameras[m_raceCameraIndex] as CARXRearCamera;
			if (raceCameraIndex == m_raceCameraIndex)
			{
				break;
			}
		}
		if (cARXRearCamera != null)
		{
			m_freemodeCam = cARXRearCamera;
			m_freemodeCam.rotationY = 0f;
			m_freemodeCam.enabled = false;
			Vector3 eulerAngles = m_freemodeCam.transform.rotation.eulerAngles;
			m_rotX = eulerAngles.x;
			m_rotY = eulerAngles.y;
			if (m_raceCar != null)
			{
				m_raceCar.lockedByCamera = true;
			}
			if (m_playerCarControl == null)
			{
				m_playerCarControl = PlayerCarControl.instance;
			}
			if (m_playerCarControl != null)
			{
				m_playerCarControl.enabled = false;
			}
			if (m_freeCameraUI != null)
			{
				m_freeCameraUI.ShowUI();
			}
		}
	}

	public void AttachCam()
	{
		if (cameraType != ECameraType.Race)
		{
			return;
		}
		if (m_freemodeCam != null)
		{
			if (m_raceCar != null)
			{
				m_raceCar.lockedByCamera = false;
			}
			if (m_playerCarControl == null)
			{
				m_playerCarControl = PlayerCarControl.instance;
			}
			if (m_playerCarControl != null)
			{
				m_playerCarControl.enabled = true;
			}
			m_freemodeCam.enabled = true;
			m_freemodeCam = null;
		}
		if (m_freeCameraUI != null)
		{
			m_freeCameraUI.HideUI();
		}
	}

	public void AttachUI(UIFreeCamContext ui)
	{
		m_freeCameraUI = ui;
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			FMODCarX.Initialize();
			m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
		}
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
		if (m_cockpitVolume != null)
		{
			UnityEngine.Object.Destroy(m_cockpitVolume.gameObject);
		}
		States.onStateActivate -= EnableOcclusionCulling;
	}

	private void Start()
	{
		GameObject gameObject = new GameObject("cockpit_volume");
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		m_cockpitVolume = gameObject.AddComponent<Volume>();
		m_cockpitVolume.isGlobal = true;
		m_cockpitVolume.sharedProfile = m_cockpitVolumeProfile;
		m_cockpitVolume.enabled = false;
		m_mainCamera = Camera.main;
		States.onStateActivate += EnableOcclusionCulling;
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		ResetCamera();
	}

	private void EnableOcclusionCulling(BaseState state)
	{
		if (state is BaseRaceState)
		{
			m_mainCamera.useOcclusionCulling = true;
		}
	}

	private void OnDisable()
	{
		if (m_freemodeCam != null)
		{
			AttachCam();
		}
	}

	private void Update()
	{
		if (m_freemodeCam != null)
		{
			HandleFreeCamMode();
		}
	}

	private void HandleFreeCamMode()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			m_freeCameraUI.AddCameraPoint();
		}
		CalcCameraRotation();
		CalcCameraPosition();
	}

	private void CalcCameraRotation()
	{
		bool isMouseInputDevice = GameInput.InputManager.instance.GetActionValue(ActionType.FreeCamView) > 0f;
		Vector3 axisValuesBy = GetAxisValuesBy(isMouseInputDevice);
		SetFreeCamModeRotation(axisValuesBy.x, axisValuesBy.y, axisValuesBy.z);
	}

	private void CalcCameraPosition()
	{
		GameInput.InputManager inputManager = GameInput.InputManager.instance;
		float actionValue = inputManager.GetActionValue(ActionType.FlyMovement);
		float actionValue2 = inputManager.GetActionValue(ActionType.Brake);
		float actionValue3 = inputManager.GetActionValue(ActionType.FlyStrafe);
		float num = (actionValue - actionValue2) * 0.25f;
		float num2 = actionValue3 * 0.25f;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			num *= 4f;
		}
		base.transform.position += base.transform.forward * num + base.transform.right * num2;
	}

	private Vector3 GetAxisValuesBy(bool isMouseInputDevice)
	{
		GameInput.InputManager inputManager = GameInput.InputManager.instance;
		inputManager.SetCursorVisibility(!isMouseInputDevice);
		if (isMouseInputDevice)
		{
			return new Vector3(ReInput.controllers.Mouse.GetAxis(1), ReInput.controllers.Mouse.GetAxis(0), 0.01f);
		}
		return new Vector3(inputManager.GetActionValue(ActionType.FlyRotationVertical), inputManager.GetActionValue(ActionType.FlyRotationHorizontal), 0.1f);
	}

	private void SetFreeCamModeRotation(float verticalAxis, float horizontalAxis, float sensitivity)
	{
		m_rotX -= verticalAxis * m_freemodeCam.speedRotationOrbit * sensitivity;
		m_rotY += horizontalAxis * m_freemodeCam.speedRotationOrbit * sensitivity;
		base.transform.rotation = Quaternion.Euler(m_rotX, m_rotY, 0f);
	}

	private void FixedUpdate()
	{
		if (cameraType != ECameraType.Race || States.current is InGameMenuState)
		{
			return;
		}
		UpdateCameraSwitch();
		UpdateFreeCamSwitching(States.current);
		CARXRearCamera cARXRearCamera = m_raceCameras[m_raceCameraIndex] as CARXRearCamera;
		if (cARXRearCamera == null)
		{
			return;
		}
		if (!rotateMode)
		{
			if (cARXRearCamera.rotationY != 0f)
			{
				cARXRearCamera.rotationY = 0f;
			}
			return;
		}
		if (!RotateCameraWithGamePad(cARXRearCamera))
		{
			RotateCameraWithMouse(cARXRearCamera);
		}
		if (!m_viewModeEnabled && Time.time >= m_timerOffsetRevert + pauseRevert)
		{
			cARXRearCamera.rotationY = Mathf.LerpAngle(cARXRearCamera.rotationY, 0f, Time.deltaTime * m_revertOffsetCamera);
		}
	}

	private void UpdateCameraSwitch()
	{
		if (m_isCameraSwitchActive && !GameInput.InputManager.IsInputLocked())
		{
			GameInput.InputManager inputManager = GameInput.InputManager.instance;
			if (inputManager.IsActionPressed(ActionType.NextCamera))
			{
				NextRaceCamera(activate: true);
			}
			if (inputManager.IsActionPressed(ActionType.PrevCamera))
			{
				PrevRaceCamera(activate: true);
			}
			if (inputManager.IsActionPressed(ActionType.RearViewCamera))
			{
				m_isLastRearCameraActive = true;
				ActivateRearRaceCamera();
			}
			if (inputManager.IsActionReleased(ActionType.RearViewCamera) || (m_isLastRearCameraActive && !inputManager.IsActionHold(ActionType.RearViewCamera)))
			{
				m_isLastRearCameraActive = false;
				ActivateRaceCamera();
			}
		}
	}

	private bool RotateCameraWithGamePad(CARXRearCamera cam)
	{
		if (GameInput.InputManager.instance.IsLastActiveJoystickAxis2D(m_prefs.globals.cameraOrbitStick, out var axis))
		{
			m_viewModeEnabled = axis.sqrMagnitude > 0f;
			if (m_viewModeEnabled)
			{
				m_timerOffsetRevert = Time.time;
			}
			if (axis == Vector2.zero)
			{
				cam.rotationY = Mathf.LerpAngle(cam.rotationY, 0f, Time.deltaTime * 10f);
				return true;
			}
			float num = Mathf.Atan2(axis.x, axis.y) * 57.29578f;
			if (cam.rotationY * num < 0f)
			{
				num -= Mathf.Sign(num) * 360f;
			}
			if (Mathf.Abs(cam.rotationY - num) > 1f)
			{
				cam.rotationY = Mathf.LerpAngle(cam.rotationY, num, Time.deltaTime * 10f);
			}
			float rotationY = cam.rotationY;
			if (Mathf.Abs(rotationY) > 360f)
			{
				cam.rotationY = rotationY - Mathf.Sign(rotationY) * 360f;
			}
			return true;
		}
		return false;
	}

	private bool RotateCameraWithMouse(CARXRearCamera cam)
	{
		m_viewModeEnabled = !GameInput.InputManager.IsInputLocked() && GameInput.InputManager.instance.GetActionValue(ActionType.FreeCamView) > 0f;
		if (m_viewModeEnabled)
		{
			m_timerOffsetRevert = Time.time;
			cam.rotationY += ReInput.controllers.Mouse.GetAxis(0) * cam.speedRotationOrbit * cam.distance * 0.01f;
			return true;
		}
		return false;
	}

	public void ForceResetCameraYRotation()
	{
		if (m_raceCameras[m_raceCameraIndex] is CARXRearCamera cARXRearCamera)
		{
			cARXRearCamera.rotationY = 0f;
		}
	}

	private void UpdateFreeCamSwitching(BaseState state)
	{
		if (!(state is SyncNetFreerideRaceModeState) && !(state is FreerideRaceModeState))
		{
			return;
		}
		UIRaceNetworkFreerideModeContext uIRaceNetworkFreerideModeContext = (state as BaseGameState).contextOwner.Get<UIRaceNetworkFreerideModeContext>();
		GameInput.InputManager inputManager = GameInput.InputManager.instance;
		if (!GameInput.InputManager.IsInputLocked() && inputManager.IsActionPressed(ActionType.FreeCamMode) && !PlayerCarControl.instance.blockControl && !XRModeManager.IsXRActive() && (!(uIRaceNetworkFreerideModeContext != null) || (!uIRaceNetworkFreerideModeContext.IsRaceContextActive() && !uIRaceNetworkFreerideModeContext.roomsController.iPlayedInRoom && !uIRaceNetworkFreerideModeContext.isSubroomUIActive)))
		{
			if (KeepAliveManager.isUIHidden)
			{
				KeepAliveManager.SetUIVisible(isVisible: true);
			}
			if (m_freemodeCam == null)
			{
				DeattachCam();
			}
			else
			{
				AttachCam();
			}
		}
	}
}
