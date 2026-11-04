using System.Threading;
using Cysharp.Threading.Tasks;
using PrefsModels;
using SyncMultiplayer;
using TrackLayout.Data;
using UnityEngine;

public class NetworkFreerideSlotProcessStrategy : FreerideSlotProcessStrategy
{
	private const float TextUpdateCooldown = 0.5f;

	protected UICarSelectorFreerideContext m_selectorContext;

	private NetworkGame m_networkGame;

	private NetGameSyncPosSystem m_syncPos;

	private NetGameCarSwapSystem m_swapSystem;

	private float m_lastTimeOfUpdateText;

	protected override void OnActivate()
	{
		m_raceState = States.mainCurrent as BaseRaceState;
		InGameMenuState inGameMenuState = States.current as InGameMenuState;
		m_selectorContext = inGameMenuState.contextOwner.Get<UICarSelectorFreerideContext>();
		m_carSlotsContext = inGameMenuState.contextOwner.Get<UICarSlotsContext>();
		m_networkGame = NetworkController.InstanceGame;
		m_syncPos = m_networkGame.systems.Get<NetGameSyncPosSystem>();
		m_swapSystem = m_networkGame.systems.Get<NetGameCarSwapSystem>();
		m_lastTimeOfUpdateText = 0f;
		m_syncPos.SetLocalCarSync(enable: false);
		SaveOriginalCar();
		SetupCamera();
	}

	protected override void OnDeactivate()
	{
		ReleaseCamera();
		m_syncPos.SetLocalCarSync(enable: true);
		BasePrefsModel.SaveAllQueries(inOneFrame: false);
	}

	public override async UniTask ConfirmChangesAsync()
	{
		if (!m_selectorContext.CooldownTimePassed() || !IsCarLoaded())
		{
			return;
		}
		int carId = GetCarId();
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(carId);
		int adhLimit = m_networkGame.lastRoomOptions.RoomAdhLimit;
		int roomPowerDensityLimit = m_networkGame.lastRoomOptions.RoomPowerDensityLimit;
		int roomWheelDriveType = m_networkGame.lastRoomOptions.RoomWheelDriveType;
		int roomTractionControlMode = m_networkGame.lastRoomOptions.RoomTractionControlMode;
		if (!(await NetworkController.ValidateCarAsyncFreeride(adhLimit, roomPowerDensityLimit, roomWheelDriveType, roomTractionControlMode, carId, activeSlotId)))
		{
			return;
		}
		await UpdateTyreVisual(adhLimit);
		m_selectorContext.RecordLastSwapTime();
		PlayerCarControl.instance.car = GetRaceCar();
		if (IsAnyCarOrSlotChanged())
		{
			RemoveCarFromChangedList(carId);
			RestoreSlotForCars();
			if (m_carIdOnEnter == carId)
			{
				m_swapSystem.SEND_CarSlotChange();
			}
			else
			{
				m_swapSystem.SEND_CarSwapStart();
			}
		}
		States.Pop();
		m_networkGame.FillLocalPlayerData();
		await m_swapSystem.CarSwapReady();
	}

	public override async UniTask DiscardChangesAsync()
	{
		await RevertChangesAndRestoreCarAsync();
		m_carSlotsContext.Hide();
	}

	protected override void OnUpdate()
	{
		if (Time.realtimeSinceStartup > m_lastTimeOfUpdateText + 0.5f)
		{
			m_lastTimeOfUpdateText = Time.realtimeSinceStartup;
			if (m_selectorContext.CooldownTimePassed())
			{
				m_carSlotsContext.ResetConfirmHint();
			}
			else
			{
				m_carSlotsContext.ReplaceConfirmHint(m_selectorContext.ConstructWaitText());
			}
		}
	}

	protected virtual async UniTask UpdateTyreVisual(int adhLimit)
	{
		int carId = GetCarId();
		int trackID = m_networkGame.lastRoomOptions.TrackID;
		bool flag = TrackMetaDB.IsWinterTrackWithIce(trackID);
		bool winterTires = base.m_prefs.carSettings.GetVisualForCar(carId).winterTires;
		bool flag2 = m_networkGame.SetTireProtector(trackID, adhLimit);
		if (flag != winterTires || flag2)
		{
			base.m_prefs.carSettings.SetWinterTiresForcar(carId, flag);
			int profileIdForCar = base.m_prefs.carSettings.GetProfileIdForCar(carId);
			base.m_visualManager.ApplyProfile(GetRaceCar(), profileIdForCar, null);
			CarVisualManager.CustomizationFlags applyFlags = CarVisualManager.CustomizationFlags.BodyParts | CarVisualManager.CustomizationFlags.Wheels;
			CarVisualManager.VisualSettings visualForCar = base.m_prefs.carSettings.GetVisualForCar(carId);
			base.m_visualManager.ApplyVisual(applyFlags, GetRaceCar(), visualForCar);
		}
	}

	protected override async UniTask PreviewSwapCarAsync(int carId)
	{
		if (base.m_prefs.globals.lastCarId != GetCarId())
		{
			CancellationToken cancelToken = new CancellationTokenSource(20000).Token;
			await ((SyncNetFreerideRaceModeState)m_raceState).BaseSwapCar(carId);
			await UISpinner.WaitUntil(IsCarLoaded, cancelToken);
			RaceCar raceCar = GetRaceCar();
			raceCar.ResetVelocity();
			raceCar.AlignCarPositionAtSpawn();
		}
	}
}
