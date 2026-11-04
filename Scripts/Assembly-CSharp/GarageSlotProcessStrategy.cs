using System.Collections.Generic;
using CarX;
using Cysharp.Threading.Tasks;
using DB.Meta;
using LocalCaching.Manager;
using UnityEngine;

public class GarageSlotProcessStrategy : BaseSlotProcessStrategy
{
	private UIGarageContext m_garageContext;

	private GarageCar3dViewContext m_car3dView;

	private UICarSlotsContext m_carSlotsContext;

	private int m_slotIdOnEnter;

	private Queue<int> m_snapshotSlotQueue = new Queue<int>();

	private bool m_snapshotProcessing;

	public override RaceCar GetRaceCar()
	{
		return m_car3dView.GetRaceCar();
	}

	public override bool IsCarLoaded()
	{
		RaceCar raceCar = GetRaceCar();
		if (raceCar != null)
		{
			return raceCar.IsVisualLoaded;
		}
		return false;
	}

	protected override void OnActivate()
	{
		GarageGUIState garageGUIState = States.mainCurrent as GarageGUIState;
		m_garageContext = garageGUIState.contextOwner.Get<UIGarageContext>();
		m_car3dView = garageGUIState.contextOwner.Get<GarageCar3dViewContext>();
		m_carSlotsContext = garageGUIState.contextOwner.Get<UICarSlotsContext>();
		m_slotIdOnEnter = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		GenerateSnapshotsIfNeed().Forget();
	}

	public override async UniTask ConfirmChangesAsync()
	{
		if (IsCarLoaded())
		{
			int carId = GetCarId();
			int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(carId);
			if (m_slotIdOnEnter != activeSlotId)
			{
				PlayerCar byId = base.m_baseModel.GetById<PlayerCar>(carId);
				IconGeneratorContext.AddToCarHashQueue(new IconCarData
				{
					carId = byId.id,
					iconName = byId.icon
				});
			}
			CloseContext();
		}
	}

	public override async UniTask DiscardChangesAsync()
	{
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		if (m_slotIdOnEnter != activeSlotId)
		{
			await PreviewSlotForCarAsync(m_slotIdOnEnter);
		}
		CloseContext();
	}

	private void ApplySlotForCar(int slotId)
	{
		RaceCar raceCar = GetRaceCar();
		int carId = GetCarId();
		base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
		base.m_visualManager.ClearVisualIfNeed(GetCarId());
		int profileIdForCar = base.m_prefs.carSettings.GetProfileIdForCar(carId);
		base.m_carFactory.GarageApplyProfile(raceCar, profileIdForCar);
		int engineIdForCar = base.m_prefs.carSettings.GetEngineIdForCar(carId);
		TimeController.StartNow(raceCar.swap.carAttached ? raceCar.swap.Refresh(engineIdForCar, force: true) : raceCar.swap.Initialize(raceCar, base.m_prefs, base.m_baseModel, engineIdForCar));
		CarVisualManager.VisualSettings visualForCar = base.m_prefs.carSettings.GetVisualForCar(carId);
		base.m_visualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, raceCar, visualForCar);
		m_garageContext.UpdateInfo();
	}

	private void UpdateCarPhysics()
	{
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
		RaceCar car = GetRaceCar();
		car.ResetVelocity();
		Singletone<Simulator>.instance.OnPostSimulationEvent += UpdateWheelAfterSimulation;
		Singletone<Simulator>.instance.OnUpdateWheelsEvent += UpdateCarModelAfterInterpolation;
		CarProfileCacheManager.RecalculateAll(car, allowApplyProfileByLazy: false);
		void UpdateCarModelAfterInterpolation()
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent -= UpdateCarModelAfterInterpolation;
			Transform parent = car.getTransform.parent;
			car.getTransform.SetPositionAndRotation(parent.position, parent.rotation);
			car.ResetVelocity();
			car.AlignCarPositionAtSpawn();
		}
		void UpdateWheelAfterSimulation()
		{
			Singletone<Simulator>.instance.OnPostSimulationEvent -= UpdateWheelAfterSimulation;
			car.ResetVelocity();
		}
	}

	public override async UniTask PreviewSlotForCarAsync(int slotId)
	{
		if (!m_snapshotProcessing)
		{
			await InternalApplySlotAsync(slotId, forceApplySlot: true);
		}
	}

	private async UniTask InternalApplySlotAsync(int slotId, bool forceApplySlot)
	{
		if (base.m_prefs.globals.lastCarId != GetCarId() || !m_car3dView.IsCarViewCreated())
		{
			return;
		}
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		if (forceApplySlot || activeSlotId != slotId)
		{
			ApplySlotForCar(slotId);
			UpdateCarPhysics();
		}
		if (!m_snapshotProcessing)
		{
			await UISpinner.WaitUntil(IsVisualLoaded, 10000);
		}
		else
		{
			while (!IsVisualLoaded())
			{
				await UniTask.Yield();
			}
		}
		if (IsNeedGenerateSnapshot(slotId))
		{
			await UniTask.DelayFrame(30);
			m_carSlotsContext.GenerateSnapshot();
		}
	}

	private bool IsVisualLoaded()
	{
		RaceCar raceCar = GetRaceCar();
		if (raceCar != null)
		{
			return raceCar.IsVisualLoaded;
		}
		return false;
	}

	private bool IsNeedGenerateSnapshot(int slotId)
	{
		return IconStorageSystem.ReadLiveryPreview(GetCarId(), slotId) == null;
	}

	private bool IsNeedProcessSnapshotQueue()
	{
		return m_snapshotSlotQueue.Count > 0;
	}

	private void CloseContext()
	{
		m_carSlotsContext.Hide();
		m_carSlotsContext.owner.Push<UIGarageContext>();
	}

	private async UniTask GenerateSnapshotsIfNeed()
	{
		UpdateSnapshotQueue();
		if (IsNeedProcessSnapshotQueue() && !m_snapshotProcessing)
		{
			UILockInputContext.SetLock(m_carSlotsContext.owner);
			UILockInputContext.SetStatusText(LocalizationKeys.loading_car.Localized());
			m_snapshotProcessing = true;
			while (!m_car3dView.allCarLoaded)
			{
				await UniTask.Yield();
			}
			while (IsNeedProcessSnapshotQueue())
			{
				await InternalApplySlotAsync(m_snapshotSlotQueue.Dequeue(), forceApplySlot: false);
			}
			m_carSlotsContext.UpdateSelection();
			while (!IsVisualLoaded())
			{
				await UniTask.Yield();
			}
			m_snapshotProcessing = false;
			UILockInputContext.DisableLock();
		}
	}

	private void UpdateSnapshotQueue()
	{
		m_snapshotSlotQueue.Clear();
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		foreach (int usedSlot in base.m_prefs.carSettings.GetUsedSlots(GetCarId()))
		{
			if (usedSlot != activeSlotId && IsNeedGenerateSnapshot(usedSlot))
			{
				m_snapshotSlotQueue.Enqueue(usedSlot);
			}
		}
		if (IsNeedProcessSnapshotQueue() || IsNeedGenerateSnapshot(activeSlotId))
		{
			m_snapshotSlotQueue.Enqueue(activeSlotId);
		}
	}
}
