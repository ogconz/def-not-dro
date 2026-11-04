using System.Collections.Generic;
using System.Threading;
using CarX;
using Cysharp.Threading.Tasks;
using DB.Meta;
using LocalCaching.Manager;
using PrefsModels;
using TrackLayout.Data;
using UnityEngine;

public class FreerideSlotProcessStrategy : BaseSlotProcessStrategy
{
	protected BaseRaceState m_raceState;

	protected GhostRecorder m_ghostRecorder;

	protected UICarSlotsContext m_carSlotsContext;

	protected int m_carIdOnEnter;

	private Dictionary<int, int> m_changedCarsOrSlots = new Dictionary<int, int>();

	public override RaceCar GetRaceCar()
	{
		if (!(PlayerCarControl.instance != null))
		{
			return null;
		}
		return PlayerCarControl.instance.car;
	}

	public override bool IsCarLoaded()
	{
		RaceCar raceCar = GetRaceCar();
		if (raceCar != null && raceCar.IsVisualLoaded)
		{
			return !m_raceState.carSwapRunning;
		}
		return false;
	}

	protected override void OnActivate()
	{
		m_raceState = States.mainCurrent as BaseRaceState;
		m_ghostRecorder = m_raceState.ghostRecorder;
		InGameMenuState inGameMenuState = States.current as InGameMenuState;
		m_carSlotsContext = inGameMenuState.contextOwner.Get<UICarSlotsContext>();
		SaveOriginalCar();
		SetupCamera();
		GameManager.instance.SetGamePause(pause: false);
	}

	protected override void OnDeactivate()
	{
		ReleaseCamera();
		if (IsAnyCarOrSlotChanged())
		{
			RemoveCarFromChangedList(GetCarId());
			RestoreSlotForCars();
			m_ghostRecorder.RiseSuspended(isSuspend: true);
			m_ghostRecorder.AddRecordSource(GetRaceCar());
		}
		m_ghostRecorder.SetPauseRecord(paused: false);
		BasePrefsModel.SaveAllQueries(inOneFrame: false);
		GameManager.instance.SetGamePause(pause: true);
	}

	public override async UniTask ConfirmChangesAsync()
	{
		if (IsCarLoaded() && States.current is InGameMenuState)
		{
			States.Pop();
		}
	}

	public override async UniTask DiscardChangesAsync()
	{
		await RevertChangesAndRestoreCarAsync();
		m_carSlotsContext.Hide();
	}

	private void ApplySlotForCar(int slotId)
	{
		RaceCar raceCar = GetRaceCar();
		int carId = GetCarId();
		base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
		int profileIdForCar = base.m_prefs.carSettings.GetProfileIdForCar(carId);
		raceCar.profileMetaInfo = base.m_baseModel.GetById<PlayerCarProfile>(profileIdForCar);
		int engineIdForCar = base.m_prefs.carSettings.GetEngineIdForCar(carId);
		TimeController.StartNow(raceCar.swap.carAttached ? raceCar.swap.Refresh(engineIdForCar, force: true) : raceCar.swap.Initialize(raceCar, base.m_prefs, base.m_baseModel, engineIdForCar));
		int lastTrackId = base.m_prefs.globals.lastTrackId;
		base.m_prefs.carSettings.SetWinterTiresForcar(carId, TrackMetaDB.IsWinterTrackWithIce(lastTrackId));
		CarVisualManager.VisualSettings visualForCar = base.m_prefs.carSettings.GetVisualForCar(carId);
		base.m_visualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, raceCar, visualForCar);
		PlayerCarControl.instance.car = raceCar;
	}

	protected async UniTask RevertChangesAndRestoreCarAsync()
	{
		if (IsAnyCarOrSlotChanged())
		{
			RestoreSlotForCars();
			SetCarId(m_carIdOnEnter);
			int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(m_carIdOnEnter);
			await PreviewSlotForCarAsync(activeSlotId);
		}
	}

	protected bool IsAnyCarOrSlotChanged()
	{
		return m_changedCarsOrSlots.Count > 0;
	}

	protected void RemoveCarFromChangedList(int carId)
	{
		m_changedCarsOrSlots.Remove(carId);
	}

	protected void RecordSlotForCar(int carId, int slotId)
	{
		if (!m_changedCarsOrSlots.ContainsKey(carId))
		{
			m_changedCarsOrSlots[carId] = base.m_prefs.carSettings.GetActiveSlotId(carId);
		}
		if (carId == m_carIdOnEnter && slotId == m_changedCarsOrSlots[carId])
		{
			m_changedCarsOrSlots.Remove(carId);
		}
	}

	protected void RestoreSlotForCars()
	{
		foreach (var (carId, slotId) in m_changedCarsOrSlots)
		{
			base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
		}
		m_changedCarsOrSlots.Clear();
	}

	private void UpdateCarPhysics()
	{
		RaceCar car = GetRaceCar();
		GetCarId();
		car.ResetVelocity();
		Singletone<Simulator>.instance.OnPostSimulationEvent += UpdateWheelAfterSimulation;
		Singletone<Simulator>.instance.OnUpdateWheelsEvent += UpdateCarModelAfterInterpolation;
		CarProfileCacheManager.RecalculateAll(car, allowApplyProfileByLazy: false);
		void UpdateCarModelAfterInterpolation()
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent -= UpdateCarModelAfterInterpolation;
			car.ResetVelocity();
			car.AlignCarPositionAtSpawn();
		}
		void UpdateWheelAfterSimulation()
		{
			Singletone<Simulator>.instance.OnPostSimulationEvent -= UpdateWheelAfterSimulation;
			car.ResetVelocity();
		}
	}

	protected virtual async UniTask PreviewSwapCarAsync(int carId)
	{
		m_ghostRecorder.SetPauseRecord(paused: true);
		m_ghostRecorder.SetLockRecordSource(isLocked: true);
		CancellationToken cancelToken = new CancellationTokenSource(20000).Token;
		await m_raceState.SwapCar(carId);
		await UISpinner.WaitUntil(IsCarLoaded, cancelToken);
		m_ghostRecorder.SetLockRecordSource(isLocked: false);
		RaceCar raceCar = GetRaceCar();
		raceCar.ResetVelocity();
		raceCar.AlignCarPositionAtSpawn();
	}

	public override async UniTask PreviewSlotForCarAsync(int slotId)
	{
		int carId = GetCarId();
		RecordSlotForCar(carId, slotId);
		if (base.m_prefs.globals.lastCarId != carId)
		{
			await PreviewSwapCarAsync(carId);
		}
		else
		{
			ApplySlotForCar(slotId);
		}
		UpdateCarPhysics();
		await UISpinner.WaitUntil(IsCarLoaded, 10000);
	}

	protected void SaveOriginalCar()
	{
		m_carIdOnEnter = base.m_prefs.globals.lastCarId;
	}

	protected void SetupCamera()
	{
		RaceCar raceCar = GetRaceCar();
		Transform getTransform = raceCar.getTransform;
		Vector3 vector = getTransform.up * 1f;
		Vector3 vector2 = getTransform.forward * 5.275406f;
		Vector3 vector3 = -getTransform.right * 3f;
		Vector3 vector4 = vector + vector2 + vector3;
		m_carSlotsContext.InitCamera();
		m_carSlotsContext.ConfigureCamera(raceCar, getTransform.position + vector4, 0.63f);
		SubscribeCameraSwitchEvent();
	}

	protected void ReleaseCamera()
	{
		UnsubscribeCameraSwitchEvent();
		m_carSlotsContext.ResetCamera();
		CameraSwitch.instance.ActivateRaceCamera();
	}

	private void OverrideCameraType(CameraSwitch.ECameraType eCameraType)
	{
		if ((bool)CameraSwitch.instance && eCameraType != CameraSwitch.ECameraType.PhotoSession)
		{
			CameraSwitch.instance.ActivatePhotoSessionCamera();
		}
	}

	private void SubscribeCameraSwitchEvent()
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.onCameraChanged -= OverrideCameraType;
			CameraSwitch.instance.onCameraChanged += OverrideCameraType;
		}
	}

	private void UnsubscribeCameraSwitchEvent()
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.onCameraChanged -= OverrideCameraType;
		}
	}
}
