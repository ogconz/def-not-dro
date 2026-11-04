using System;
using System.Collections;
using System.Collections.Generic;
using Binding.Base;
using Customization;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using UnityEngine;

public class GarageCar3dViewContext : BaseBindingBehaviourTarget
{
	public struct CarInfo
	{
		public string name;

		public float coinsFactor;

		public bool allowCustomization;
	}

	public struct CarLockedInfo
	{
		public DLCCode dlcCode;

		public bool locked;

		public int cost;

		public ECurrencyType currencyType;
	}

	private class PodiumCarData
	{
		public int carId;

		public CarPart dlcBody;

		public PodiumCarData(int id, CarPart body)
		{
			carId = id;
			dlcBody = body;
		}
	}

	private const int NullSelectionIndex = -1;

	private const string DLC_CAR_ROOT_TAG = "DlcCarRoot";

	[Dependency]
	private GraphicsQuality m_graphicsQuality;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private PlayerCarFactory m_playerCarFactory;

	private Coroutine m_createCarCoroutine;

	private Coroutine m_createPodiumCarsCoroutine;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	private List<PlayerCar> m_carsData = new List<PlayerCar>();

	private int m_selectedCarIndex;

	private PlayerCar m_selectedCarData;

	private GameObject m_selectedCarView;

	private RaceCar m_selectedRaceCar;

	private GameObject m_carViewToDestroy;

	private bool m_carViewVisible = true;

	[SerializeField]
	private DLCCode m_podiumCarsDlc;

	[SerializeField]
	private List<int> m_podiumCarIds;

	private readonly List<RaceCar> m_podiumCars = new List<RaceCar>();

	private readonly List<Transform> m_dlcCarRoots = new List<Transform>();

	private List<PodiumCarData> m_dlcCars = new List<PodiumCarData>();

	private Coroutine m_lockCarCoroutine;

	public PlayerCarFactory playerCarFactory => m_playerCarFactory;

	public bool allCarLoaded
	{
		get
		{
			if (m_createCarCoroutine == null)
			{
				return m_createPodiumCarsCoroutine == null;
			}
			return false;
		}
	}

	public PlayerCar selectedCarData => m_selectedCarData;

	public RaceCar currentCar => m_selectedRaceCar;

	public event Action SelectedCarChangedEvent;

	public event Action CarViewLoadedEvent;

	private int? GetLastSelectedCarId()
	{
		if (m_selectedCarData != null)
		{
			return m_selectedCarData.id;
		}
		return null;
	}

	public GameGlobalFeature AsGameFeature()
	{
		if (m_selectedCarData != null)
		{
			return m_selectedCarData.AsGameFeature();
		}
		return GameGlobalFeature.Unknown;
	}

	public void RebuildCarList(int currentVendorId = 0, bool selectedFromList = false)
	{
		List<PlayerCar> carsData = m_model.QueryAll<PlayerCar>();
		UIGarageContext uIGarageContext = base.owner.Get<UIGarageContext>();
		switch ((uIGarageContext != null) ? uIGarageContext.GetCurrentFilter() : UIGarageContext.CarFilterType.All)
		{
		case UIGarageContext.CarFilterType.All:
			FillCarList();
			CarDataSort(CompareByAllSort);
			break;
		case UIGarageContext.CarFilterType.Favourite:
			carsData.RemoveAll((PlayerCar carData) => !m_prefs.favoriteCars.IsFavorite(carData.id));
			FillCarList();
			CarDataSort(CompareByAllSort);
			TrySelectFirstFavCar();
			break;
		case UIGarageContext.CarFilterType.Vendor:
			carsData = carsData.FindAll((PlayerCar car) => car.vendorId == currentVendorId);
			FillCarList();
			CarDataSort(CompareByAllSort);
			break;
		}
		void CarDataSort(Func<PlayerCar, PlayerCar, int> compareRule)
		{
			m_carsData.Sort((PlayerCar left, PlayerCar right) => compareRule(left, right));
			m_selectedCarIndex = FindCarIndexByUserSettingsCarId(GetCarId());
		}
		static int CompareByAllSort(PlayerCar left, PlayerCar right)
		{
			if (left.sort == right.sort)
			{
				return left.name.CompareTo(right.name);
			}
			return left.sort.CompareTo(right.sort);
		}
		void FillCarList()
		{
			m_carsData.Clear();
			foreach (PlayerCar item in carsData)
			{
				if (UIUtils.IsCarVisible(m_prefs, item))
				{
					m_carsData.Add(item);
				}
			}
		}
		void TrySelectFirstFavCar()
		{
			if (m_carsData.Count > 0 && !selectedFromList && !m_prefs.favoriteCars.IsFavorite(m_selectedCarData.id))
			{
				SelectCarById(m_carsData[0].id);
			}
		}
	}

	public bool IsCarViewCreated()
	{
		return m_selectedCarView != null;
	}

	public void SetGarageCarStatePhysAndHoldPosition(bool disablePhysics, bool holdPosition)
	{
		if ((bool)m_selectedRaceCar)
		{
			m_selectedRaceCar.HoldGaragePosition(holdPosition);
		}
		if (m_lockCarCoroutine != null)
		{
			StopCoroutine(m_lockCarCoroutine);
		}
		if (base.gameObject.activeSelf)
		{
			m_lockCarCoroutine = StartCoroutine(LockGaragePositionCoroutine(disablePhysics, disablePhysics ? 3f : 0f));
		}
	}

	public void SetCarsSound(bool enable)
	{
		if (!(currentCar == null) && currentCar.enableCollisionHitSound != enable)
		{
			currentCar.enableCollisionHitSound = enable;
		}
	}

	private IEnumerator LockGaragePositionCoroutine(bool value, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if ((bool)m_selectedRaceCar)
		{
			m_selectedRaceCar.carX.SetCarActive(!value);
			m_selectedRaceCar.carX.getRigidbody.isKinematic = value;
		}
		m_lockCarCoroutine = null;
	}

	private void DestroyCurrentCarView(bool instant)
	{
		SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
		DestroyCurrentCarViewImmediate();
		m_carViewToDestroy = m_selectedCarView;
		if (instant)
		{
			DestroyCurrentCarViewImmediate();
		}
	}

	private void DestroyCurrentCarViewImmediate()
	{
		if (m_carViewToDestroy != null)
		{
			UnityEngine.Object.Destroy(m_carViewToDestroy);
			m_carViewToDestroy = null;
		}
	}

	private void UpdateViewVisibility()
	{
		if (m_selectedCarView != null && m_selectedCarView.activeSelf != m_carViewVisible)
		{
			m_selectedCarView.SetActive(m_carViewVisible);
		}
	}

	private void CreateCarViewByData()
	{
		UISpinner.Show();
		GameLog.Print(AlertLevel.Notify, "Debug", "CreateCarViewByData begin");
		DestroyCurrentCarView(instant: true);
		if (m_selectedCarData != null)
		{
			StopAllCoroutines();
			GameLog.Check(AlertLevel.Notify, "Debug", "Check PlayerCarFactory", m_playerCarFactory != null);
			if (m_createCarCoroutine == null)
			{
				m_createCarCoroutine = TimeController.StartNow(m_playerCarFactory.GarageCreateCarAsync(m_selectedCarData.id, ActionOnCarCreated, inGarage: true, Vector3.zero, Quaternion.identity));
			}
		}
		GameLog.Print(AlertLevel.Notify, "Debug", "CreateCarViewByData end");
		void ActionOnCarCreated(RaceCar car)
		{
			m_selectedRaceCar = car;
			SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
			m_selectedCarView = ((car == null) ? null : car.gameObject);
			UpdateViewVisibility();
			DestroyCurrentCarViewImmediate();
			this.CarViewLoadedEvent.SafeCall();
			UISpinner.HideOnCarLoaded(car);
			m_createCarCoroutine = null;
			SetCarsSound(enable: false);
		}
	}

	private void SetSelectedCarData(PlayerCar carData)
	{
		if (m_selectedCarData != carData)
		{
			bool flag = m_selectedCarData != null && carData != null && m_selectedCarData.id != carData.id;
			m_selectedCarData = carData;
			if (!IsCarViewCreated() || flag)
			{
				CreateCarViewByData();
			}
			else
			{
				DestroyCurrentCarView(instant: true);
			}
		}
	}

	private int GetSelectedCarIndex()
	{
		return m_selectedCarIndex;
	}

	private PlayerCar GetCarsDataByIndex(int index)
	{
		if (index == -1)
		{
			return null;
		}
		return m_carsData[index];
	}

	private void SelectCarByIndex(int carIndex)
	{
		if (m_selectedCarIndex != carIndex)
		{
			m_selectedCarIndex = carIndex;
			SetSelectedCarData(GetCarsDataByIndex(carIndex));
			SaveUnlockedSelectedCarId();
			this.SelectedCarChangedEvent.SafeCall();
		}
	}

	private int FindCarIndexByUserSettingsCarId(int? carId)
	{
		if (carId.HasValue)
		{
			return m_carsData.FindIndex((PlayerCar carData) => carData.id == carId.Value);
		}
		return -1;
	}

	public List<PlayerCar> GetAllCar()
	{
		return m_carsData;
	}

	public void GetAllCarChangesVisual(HashSet<IconCarData> outPlayerCars, bool all = false)
	{
		if (outPlayerCars == null)
		{
			return;
		}
		outPlayerCars.Clear();
		if (all)
		{
			m_prefs.globals.carsVisualUpdate.Clear();
			{
				foreach (PlayerCar carsDatum in m_carsData)
				{
					outPlayerCars.Add(new IconCarData
					{
						carId = carsDatum.id,
						iconName = carsDatum.icon
					});
					m_prefs.globals.carsVisualUpdate.Add(carsDatum.id);
				}
				return;
			}
		}
		foreach (int playerCar in m_prefs.globals.carsVisualUpdate)
		{
			PlayerCar playerCar2 = m_carsData.Find((PlayerCar carData) => carData.id == playerCar);
			if (playerCar2 != null)
			{
				outPlayerCars.Add(new IconCarData
				{
					carId = playerCar2.id,
					iconName = playerCar2.icon
				});
			}
		}
	}

	public void SelectCarById(int? carId)
	{
		int carIndex = FindCarIndexByUserSettingsCarId(carId);
		SelectCarByIndex(carIndex);
	}

	public void CreateDlcCarsIfNeeded()
	{
		if (m_createPodiumCarsCoroutine == null)
		{
			m_createPodiumCarsCoroutine = StartCoroutine(CreatePodiumCarsCoroutine(delegate
			{
				m_createPodiumCarsCoroutine = null;
			}));
		}
	}

	private IEnumerator CreatePodiumCarsCoroutine(Action callback)
	{
		EnsureDlcCarRoots();
		if (m_dlcCarRoots.Count == 0 || m_podiumCars.Count > 0)
		{
			callback.SafeCall();
			yield break;
		}
		if (m_dlcCars.Count == 0)
		{
			foreach (PlayerCar carsDatum in m_carsData)
			{
				foreach (CarPart carBody in CarPartDatabase.GetCarBodies(carsDatum.id))
				{
					if (carBody.DLCCode == m_podiumCarsDlc)
					{
						int carId = carBody.carId;
						if (m_podiumCarIds.Contains(carId))
						{
							m_dlcCars.Add(new PodiumCarData(carId, carBody));
						}
					}
				}
			}
		}
		int idx = 0;
		int i = 0;
		while (i < m_dlcCars.Count)
		{
			PodiumCarData podiumCarData = m_dlcCars[i];
			CarVisualManager.VisualSettings visualSettings = new CarVisualManager.VisualSettings();
			visualSettings.parts.Add(podiumCarData.dlcBody.id);
			Vector3 position = Vector3.zero;
			Quaternion rotation = Quaternion.identity;
			if (i < m_dlcCarRoots.Count)
			{
				position = m_dlcCarRoots[i].position;
				rotation = m_dlcCarRoots[i].rotation;
			}
			yield return SpawnPodiumCar(podiumCarData.carId, visualSettings, delegate(RaceCar podiumCar)
			{
				if (podiumCar != null)
				{
					podiumCar.isPodiumCar = true;
					m_podiumCars.Add(podiumCar);
					idx++;
				}
			}, position, rotation);
			int num = i + 1;
			i = num;
		}
		float time = Time.unscaledTime;
		yield return new WaitUntil(() => idx >= m_dlcCars.Count || Time.unscaledTime - time > 10f);
		yield return new WaitUntil(delegate
		{
			foreach (RaceCar podiumCar in m_podiumCars)
			{
				if (!podiumCar.IsVisualLoaded || !podiumCar.onGround)
				{
					return false;
				}
			}
			return true;
		});
		yield return null;
		foreach (RaceCar podiumCar2 in m_podiumCars)
		{
			Rigidbody getRigidbody = podiumCar2.getRigidbody;
			if ((bool)getRigidbody)
			{
				getRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
				getRigidbody.isKinematic = true;
			}
		}
		callback.SafeCall();
	}

	public void ApplyVisualPodiumCar(RaceCar car, Action<RaceCar> callback = null)
	{
		CarVisualManager.VisualSettings visualSettings = m_prefs.carSettings.GetVisualForCar(car.carId).Copy();
		visualSettings.RemoveExternalParts(car);
		TimeController.StartNow(m_playerCarFactory.CarSetVisualAsync(car, ActionOnCarCreated, visualSettings));
		void ActionOnCarCreated(RaceCar raceCar)
		{
			if (!BaseCar.IsNull(raceCar))
			{
				this.CarViewLoadedEvent.SafeCall();
				callback.SafeCall(raceCar);
			}
		}
	}

	public void CreateCustomPodiumCar(int raceCarId, Transform root, bool lastCar = false, Action<RaceCar> callback = null, bool holdPosition = true)
	{
		if (lastCar)
		{
			raceCarId = m_prefs.globals.lastCarId;
		}
		TimeController.StartNow(m_playerCarFactory.GarageCreateCarAsync(raceCarId, ActionOnCarCreated, inGarage: true, root.position, root.rotation));
		async void ActionOnCarCreated(RaceCar car)
		{
			if (!BaseCar.IsNull(car) && !(root == null))
			{
				await UniTask.DelayFrame(10);
				ApplyVisualPodiumCar(car, delegate
				{
					car.isPodiumCar = true;
					car.gameObject.name = "CarId" + raceCarId;
					car.getTransform.position = root.position;
					car.getTransform.rotation = root.rotation;
					if (holdPosition)
					{
						car.HoldGaragePosition(hold: true);
					}
					car.lockState = true;
					CarLightController component = car.gameObject.GetComponent<CarLightController>();
					if (component != null)
					{
						component.SetForceDisabled(disabled: true);
						component.enabled = false;
					}
					this.CarViewLoadedEvent.SafeCall();
					callback.SafeCall(car);
				});
			}
		}
	}

	public bool IsPodiumCar(RaceCar car)
	{
		return m_podiumCars.Contains(car);
	}

	private IEnumerator SpawnPodiumCar(int carId, CarVisualManager.VisualSettings visuals, Action<RaceCar> onCarCreated, Vector3 position, Quaternion rotation)
	{
		if (m_carsData.Find((PlayerCar el) => el.id == carId) == null)
		{
			onCarCreated.SafeCall(null);
			yield break;
		}
		RaceCar podiumCar = null;
		bool carCreated = false;
		yield return m_playerCarFactory.GarageCreateCarAsync(carId, delegate(RaceCar car)
		{
			podiumCar = car;
			carCreated = true;
		}, inGarage: true, position, rotation, visuals, skipProfileRefresh: true);
		yield return new WaitUntil(() => carCreated);
		if (podiumCar == null)
		{
			onCarCreated.SafeCall(null);
			yield break;
		}
		if (podiumCar.carX != null)
		{
			podiumCar.lockState = true;
		}
		CarLightController component = podiumCar.gameObject.GetComponent<CarLightController>();
		if (component != null)
		{
			component.SetForceDisabled(disabled: true);
			component.enabled = false;
		}
		onCarCreated.SafeCall(podiumCar);
	}

	private void DestroyPodiumCars()
	{
		foreach (RaceCar podiumCar in m_podiumCars)
		{
			if (podiumCar != null)
			{
				UnityEngine.Object.Destroy(podiumCar.gameObject);
			}
		}
		m_podiumCars.Clear();
	}

	private void EnsureDlcCarRoots()
	{
		if (m_dlcCarRoots.Count != 0)
		{
			return;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("DlcCarRoot");
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				gameObject.transform.DestroyChildren();
				m_dlcCarRoots.Add(gameObject.transform);
			}
		}
	}

	public void SelectNextCar()
	{
		if (m_carsData.Count <= 0)
		{
			return;
		}
		int selectedCarIndex = GetSelectedCarIndex();
		selectedCarIndex++;
		if (selectedCarIndex >= m_carsData.Count)
		{
			selectedCarIndex = 0;
		}
		PlayerCar carsDataByIndex = GetCarsDataByIndex(selectedCarIndex);
		while (carsDataByIndex != null && m_prefs.locks.GetCarLockState(carsDataByIndex.id))
		{
			selectedCarIndex++;
			if (selectedCarIndex >= m_carsData.Count)
			{
				selectedCarIndex = 0;
			}
			carsDataByIndex = GetCarsDataByIndex(selectedCarIndex);
		}
		SelectCarByIndex(selectedCarIndex);
	}

	public void SelectPrevCar()
	{
		if (m_carsData.Count <= 0)
		{
			return;
		}
		int selectedCarIndex = GetSelectedCarIndex();
		selectedCarIndex--;
		if (selectedCarIndex < 0)
		{
			selectedCarIndex = m_carsData.Count - 1;
		}
		PlayerCar carsDataByIndex = GetCarsDataByIndex(selectedCarIndex);
		while (carsDataByIndex != null && m_prefs.locks.GetCarLockState(carsDataByIndex.id))
		{
			selectedCarIndex--;
			if (selectedCarIndex < 0)
			{
				selectedCarIndex = m_carsData.Count - 1;
			}
			carsDataByIndex = GetCarsDataByIndex(selectedCarIndex);
		}
		SelectCarByIndex(selectedCarIndex);
	}

	public bool IsSelected()
	{
		return m_selectedCarIndex != -1;
	}

	public CarInfo GetCarInfo()
	{
		CarInfo result = default(CarInfo);
		if (m_selectedCarData != null)
		{
			result.name = m_selectedCarData.name;
			result.coinsFactor = m_prefs.locks.GetCarCurrentCoinsFactor();
			result.allowCustomization = m_selectedCarData.customizationEnabled;
		}
		return result;
	}

	public CarLockedInfo GetCarLockedInfo()
	{
		CarLockedInfo result = default(CarLockedInfo);
		if (m_selectedCarData != null)
		{
			DLCCode dlcCode = m_selectedCarData.dlcCode;
			if (dlcCode != DLCCode.Unknown)
			{
				result.locked = !Overlay.instance.IsDLCInstalled(dlcCode);
			}
			else
			{
				result.locked = m_prefs.locks.GetCarLockState(m_selectedCarData.id);
			}
			result.dlcCode = dlcCode;
			result.cost = m_selectedCarData.cost;
			result.currencyType = TransactionManager.AsCurrencyType(m_selectedCarData);
		}
		return result;
	}

	public RaceCar GetRaceCar()
	{
		return m_selectedRaceCar;
	}

	public int? GetCarId()
	{
		if (m_selectedCarData != null)
		{
			return m_selectedCarData.id;
		}
		return null;
	}

	public string GetDLCString()
	{
		if (m_selectedCarData != null)
		{
			return m_selectedCarData.dlc;
		}
		return null;
	}

	public DLCCode GetDLCCode()
	{
		if (m_selectedCarData != null)
		{
			return m_selectedCarData.dlcCode;
		}
		return DLCCode.Unknown;
	}

	public bool IsContainCarId(int id)
	{
		return m_carsData.Exists((PlayerCar car) => car.id == id);
	}

	public void ApplyWheelSizeUpdate(ProfilesInfo.Profile profile)
	{
		if (m_selectedCarView != null)
		{
			m_carVisualManager.ApplyWheelSizeUpdate(m_selectedRaceCar, profile);
		}
	}

	public void IconGenerate()
	{
		int id = m_selectedRaceCar.metaInfo.id;
		int activeSlotId = m_prefs.carSettings.GetActiveSlotId(id);
		IconStorageSystem.DeleteLiveryPreview(id, activeSlotId);
		IconGeneratorContext.AddToCarHashQueue(new IconCarData
		{
			carId = id,
			iconName = m_selectedRaceCar.metaInfo.icon
		});
	}

	public void ApplyCustomization(CarVisualManager.CustomizationFlags applyFlags, CarVisualManager.VisualSettings vs, bool forceProfileApply = false)
	{
		if (m_selectedCarView != null)
		{
			m_carVisualManager.ApplyVisual(applyFlags, m_selectedRaceCar, vs, null, isLocalPlayerCar: true, forceProfileApply);
		}
	}

	public void ApplyDefaultCustomization()
	{
		if (m_selectedCarData != null)
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(m_selectedCarData.id);
			ApplyCustomization(CarVisualManager.CustomizationFlags.All, visualForCar);
		}
	}

	public void ApplyProfile(int profileId)
	{
		if (m_selectedCarView != null)
		{
			m_playerCarFactory.GarageApplyProfile(m_selectedRaceCar, profileId);
		}
	}

	public void ApplyDefaultProfile()
	{
		if (m_selectedCarView != null)
		{
			int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_selectedCarData.id);
			m_playerCarFactory.GarageApplyProfile(m_selectedRaceCar, profileIdForCar);
		}
	}

	public void Set3DModelVisible(bool value)
	{
		m_carViewVisible = value;
		UpdateViewVisibility();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		int? lastSelectedCarId = GetLastSelectedCarId();
		m_selectedCarIndex = FindCarIndexByUserSettingsCarId(lastSelectedCarId);
		SetSelectedCarData(GetCarsDataByIndex(m_selectedCarIndex));
		RebuildCarList();
		if (IsSelected())
		{
			return;
		}
		if (m_prefs.globals.lastCarId != -1)
		{
			PlayerCar carsDataByIndex = GetCarsDataByIndex(FindCarIndexByUserSettingsCarId(m_prefs.globals.lastCarId));
			if (carsDataByIndex != null)
			{
				if (!UIUtils.IsCarVisible(m_prefs, carsDataByIndex))
				{
					m_prefs.globals.lastCarId = 3;
				}
			}
			else
			{
				m_prefs.globals.lastCarId = 3;
			}
		}
		SelectCarById(m_prefs.globals.lastCarId);
	}

	protected override void OnDeactivate()
	{
		DestroyPodiumCars();
		DestroyCurrentCarView(instant: true);
		base.OnDeactivate();
	}

	protected override bool OnEscape()
	{
		base.owner.HideStackAndPush<UIMainMenuContext>();
		return false;
	}

	public void SaveUnlockedSelectedCarId()
	{
		int? carId = GetCarId();
		if (carId.HasValue && !m_prefs.locks.GetCarLockState(carId.Value) && m_prefs.globals.lastCarId != carId.Value)
		{
			m_prefs.globals.lastCarId = carId.Value;
			m_prefs.globals.Save();
		}
	}
}
