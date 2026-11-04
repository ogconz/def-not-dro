using GameInput;
using Rewired;
using SyncMultiplayer;
using UnityEngine;

public class DynoMenuState : BaseMenuState
{
	private const float DistanceMax = 7f;

	private const float YMinLimit = 0f;

	private const float YMaxLimit = 60f;

	private const float CamXRotation = 7f;

	private const float CamYHeight = 0f;

	private CameraPhotoSession m_cameraPhotoSession;

	private Camera m_uiCamera;

	private DynoData m_dynoData;

	private CameraInputController m_inputController;

	private BaseRaceModeState m_race;

	private UIDynostandContext m_context;

	private NetGameDynoSystem m_netDynoSystem;

	private bool m_isNetworkFreeride;

	private Vector3 m_cachedCarPosition;

	private Quaternion m_cachedCarRotation;

	public override void OnEnter()
	{
		BaseMenuState.m_pauseOnShow = false;
		base.OnEnter();
		m_race = (BaseRaceModeState)States.mainCurrent;
		m_isNetworkFreeride = m_race.raceMode == RaceMode.NetworkFreeride;
		TryInitNetDynoSystem();
		TryNotifyPlayersOfEnterDyno(enter: true);
		m_context = base.contextOwner.Push<UIDynostandContext>();
		if (m_race is DynoRaceModeState dynoRaceModeState)
		{
			m_context.Import(dynoRaceModeState.GetDynoData());
		}
		m_race.SuspendGhostRecorder(isSuspend: true);
		m_context.Init(m_race.userCar);
		m_dynoData = Object.FindObjectOfType<DynoData>();
		SetupCar();
		SetupCamera();
		FetchCamera();
		m_inputController = new CameraInputController();
		TryNotifyPlayersOfEnableSync(enable: false);
	}

	public override void OnExit()
	{
		base.OnExit();
		if (m_cameraPhotoSession != null)
		{
			m_cameraPhotoSession.SetHeight(0f);
			m_cameraPhotoSession.SetVerticalTilt(0f);
		}
		if (!(m_context == null) && !BaseCar.IsNull(m_context.userCar))
		{
			ResetCar();
			m_race.UpdateCurrentGhost();
			m_race.SuspendGhostRecorder(isSuspend: false);
			TryNotifyPlayersOfEnableSync(enable: true);
			TryNotifyPlayersOfEnterDyno(enter: false);
		}
	}

	private void CalcStandalone(ref float deltaXRotation, ref float deltaYRotation, ref float deltaDistance)
	{
		if (Input.GetMouseButton(0))
		{
			if (m_uiCamera == null && !FetchCamera())
			{
				return;
			}
			if (Physics.Raycast(m_uiCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, float.PositiveInfinity) && hitInfo.collider.gameObject.layer == 13)
			{
				m_cameraPhotoSession.UpdateCameraPosition(deltaXRotation, deltaYRotation, deltaDistance);
				return;
			}
		}
		m_inputController?.CalcStandalone(Time.unscaledDeltaTime, ref deltaXRotation, ref deltaYRotation, ref deltaDistance);
	}

	private void LateUpdate()
	{
		if (!(m_cameraPhotoSession == null) && !GameInput.InputManager.IsInputLocked())
		{
			ControllerType type = GameInput.InputManager.instance.lastController.type;
			if ((type != ControllerType.Keyboard && type != ControllerType.Mouse) || !UIUtils.IsPointerOverGameObject())
			{
				float deltaXRotation = 0f;
				float deltaYRotation = 0f;
				float deltaDistance = 0f;
				CalcStandalone(ref deltaXRotation, ref deltaYRotation, ref deltaDistance);
				m_cameraPhotoSession.UpdateCameraPosition(deltaXRotation, deltaYRotation, deltaDistance);
			}
		}
	}

	private void TryInitNetDynoSystem()
	{
		if (m_isNetworkFreeride && m_netDynoSystem == null)
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame.CurrentRoom != null)
			{
				m_netDynoSystem = instanceGame.systems.Get<NetGameDynoSystem>();
			}
		}
	}

	private void TryNotifyPlayersOfEnterDyno(bool enter)
	{
		if (m_isNetworkFreeride)
		{
			m_netDynoSystem?.UpdatePlayerDynoState(enter);
		}
	}

	private void TryNotifyPlayersOfEnableSync(bool enable)
	{
		if (m_isNetworkFreeride)
		{
			m_netDynoSystem?.UpdateCarSyncState(enable);
		}
	}

	private void SetupCar()
	{
		RaceCar userCar = m_context.userCar;
		if (!(userCar == null))
		{
			userCar.carX.ShutdownEngine();
			userCar.carX.handbrake = 1f;
			userCar.carX.gear = 0;
			userCar.carX.ResetTransmission();
			userCar.lockState = true;
			CameraSwitch.instance.SetCar(userCar);
			userCar.getRigidbody.velocity = Vector3.zero;
			userCar.getRigidbody.angularVelocity = Vector3.zero;
			userCar.AlignCarPositionAtSpawn();
			if (!m_isNetworkFreeride)
			{
				TeleportCarToDynoPoint(userCar);
			}
			userCar.ClearSmoke();
		}
	}

	private void ResetCar()
	{
		RaceCar userCar = m_context.userCar;
		if (!m_isNetworkFreeride)
		{
			TeleportCarToCachedPoint(userCar);
		}
		userCar.FlipCar();
		userCar.lockState = false;
		userCar.carX.clutch = 1f;
		userCar.engineController.SetIgnition(enabled: true);
		userCar.engineController.ForceStartEngine(bumpRpm: true);
	}

	private void TeleportCarToDynoPoint(RaceCar car)
	{
		Transform getTransform = car.getTransform;
		m_cachedCarPosition = getTransform.position;
		m_cachedCarRotation = getTransform.rotation;
		if (!(m_dynoData == null))
		{
			Vector3 position = m_dynoData.CarInitialTransform.position - getTransform.forward * car.lengthFront;
			Quaternion rotation = m_dynoData.CarInitialTransform.rotation;
			getTransform.SetPositionAndRotation(position, rotation);
		}
	}

	private void TeleportCarToCachedPoint(RaceCar car)
	{
		car.getTransform.SetPositionAndRotation(m_cachedCarPosition, m_cachedCarRotation);
	}

	private void SetupCamera()
	{
		CameraSwitch.instance.SetCar(m_context.userCar);
		CameraSwitch.instance.ActivateCamera(CameraSwitch.ECameraType.PhotoSession, 0);
		m_cameraPhotoSession = Object.FindObjectOfType<CameraPhotoSession>();
		if (!(m_cameraPhotoSession == null))
		{
			m_cameraPhotoSession.ChangeTarget(CameraPhotoSession.PhotoSessionCameraTarget.Center);
			m_cameraPhotoSession.SetLimits(7f, 0f, 60f);
			Vector3 vector = ((m_dynoData != null) ? m_dynoData.CameraInitialTransform.position : Vector3.zero);
			Vector3 vector2 = m_context.userCar.getTransform.position - vector;
			float num = Mathf.Sqrt(vector2.x * vector2.x + vector2.z * vector2.z);
			if (num > 0f)
			{
				m_cameraPhotoSession.SetCameraPosition(Mathf.Atan2(vector2.x, vector2.z) * 57.29578f, Mathf.Atan(vector2.y / num) * 57.29578f, vector2.magnitude);
			}
			m_cameraPhotoSession.SetHeight(0f);
			m_cameraPhotoSession.SetVerticalTilt(7f);
		}
	}

	private bool FetchCamera()
	{
		m_uiCamera = Camera.main;
		if (m_uiCamera == null)
		{
			m_uiCamera = Object.FindObjectOfType<Camera>();
		}
		return m_uiCamera != null;
	}
}
