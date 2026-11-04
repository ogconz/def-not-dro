using System;
using System.Collections;
using System.Collections.Generic;
using CarProfile;
using CarSuspensionSystem;
using Customization;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using LocalCaching.Manager;
using PoolingSystem.Application;
using PoolingSystem.Controller;
using SimpleJSON;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utils.VinylLayerExtensions;

public class PlayerCarFactory
{
	public class EnginesPoolManager
	{
		public int count => poolApplication.GetPoolCount();

		public EnginePoolApplication poolApplication { get; private set; }

		public EnginesPoolManager()
		{
			InitializePool();
		}

		private void InitializePool()
		{
			GameObject gameObject = new GameObject("EnginesPoolParent");
			gameObject.ResetPRS();
			PoolController poolController = gameObject.AddComponent<PoolController>();
			poolApplication = new EnginePoolApplication(poolController);
			poolApplication.PreparePool();
		}

		public GameObject GivePrefabPool(int swapId)
		{
			return poolApplication.GivePrefabFromPool(swapId);
		}

		public void ReturnToPool(GameObject gameObject)
		{
			poolApplication.ReturnToPool(gameObject);
		}
	}

	public const string GarageCarPositionMarkerName = "CarPositionMarker";

	public const string CarPrefabsFolder = "prefabs/cars/";

	private const float ADDITIONAL_CAR_UPSHIFT = 0.2f;

	private const float CAR_SPAWN_HEIGHT_COEFF = 0.44f;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	private readonly DLCCode[] CONSOLE_ALLOWED_DLC_PARTS = new DLCCode[12]
	{
		DLCCode.Police,
		DLCCode.Unknown,
		DLCCode.SouthAmericaStyle,
		DLCCode.StreetTuners,
		DLCCode.HitTheWall,
		DLCCode.SayakaS15,
		DLCCode.YoungTimers,
		DLCCode.Sayaka180SX,
		DLCCode.SayakaRX7,
		DLCCode.WildUnion,
		DLCCode.MeowtorMadness,
		DLCCode.FreshX
	};

	private Dictionary<int, (string partType, int partId)> m_specialLiveryApplyCars = new Dictionary<int, (string, int)>
	{
		{
			108,
			("chassis", 108999)
		},
		{
			125,
			("chassis", 125999)
		},
		{
			132,
			("chassis", 132999)
		},
		{
			147,
			("chassis", 147999)
		},
		{
			148,
			("chassis", 148999)
		}
	};

	private int m_lastInstantiateFrame;

	public CarVisualManager visualManager => m_carVisualManager;

	public EnginesPoolManager enginesPool { get; private set; }

	public CarItemsPool carItemsPool { get; } = new CarItemsPool();

	public PlayerCarFactory()
	{
		DependencyInjector.InjectObjectsInto(this);
		InitEnginesPool();
		carItemsPool.Initialize(m_model);
	}

	private void InitEnginesPool()
	{
		enginesPool = new EnginesPoolManager();
	}

	public RaceCar CreateRaceCar(int carId, int profileId, CarVisualManager.VisualSettings overrideVisual = null)
	{
		RaceCar raceCar = InstantiateCarPrefab("prefabs/cars/", carId, isLocalCar: true, inGarage: false, 29);
		CarVisualManager.VisualSettings visualSettings;
		if ((bool)raceCar)
		{
			NetworkController.InstanceGame.LocalPlayer.userCar = raceCar;
			SetProfile(raceCar, profileId);
			visualSettings = overrideVisual;
			if (visualSettings == null)
			{
				visualSettings = m_prefs.carSettings.GetVisualForCar(carId);
			}
			if (!raceCar.swap.carAttached)
			{
				TimeController.StartNow(raceCar.swap.Initialize(raceCar, m_prefs, m_model));
			}
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, raceCar, visualSettings);
			raceCar.OnCarVisualLoadFinishEvent += ApplyTireStickersOnCarVisualLoaded;
			PrepareCar(raceCar, GameManager.isInGarage);
		}
		return raceCar;
		void ApplyTireStickersOnCarVisualLoaded(RaceCar car)
		{
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.AllTireStickers, car, visualSettings);
			car.OnCarVisualLoadFinishEvent -= ApplyTireStickersOnCarVisualLoaded;
		}
	}

	public async UniTask<RaceCar> CreateRaceCarAsync(int carId, int profileId, CarVisualManager.VisualSettings overrideVisual = null)
	{
		RaceCar raceCar = await InstantiateCarPrefabAsync("prefabs/cars/", carId, Vector3.zero, Quaternion.identity, isLocalCar: true, inGarage: false, 29);
		CarVisualManager.VisualSettings visualSettings;
		if ((bool)raceCar)
		{
			NetworkController.InstanceGame.LocalPlayer.userCar = raceCar;
			SetProfile(raceCar, profileId);
			visualSettings = overrideVisual;
			if (visualSettings == null)
			{
				visualSettings = m_prefs.carSettings.GetVisualForCar(carId);
			}
			if (!raceCar.swap.carAttached)
			{
				TimeController.StartNow(raceCar.swap.Initialize(raceCar, m_prefs, m_model));
			}
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, raceCar, visualSettings);
			raceCar.OnCarVisualLoadFinishEvent += ApplyTireStickersOnCarVisualLoaded;
			PrepareCar(raceCar, GameManager.isInGarage);
		}
		return raceCar;
		void ApplyTireStickersOnCarVisualLoaded(RaceCar car)
		{
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.AllTireStickers, car, visualSettings);
			car.OnCarVisualLoadFinishEvent -= ApplyTireStickersOnCarVisualLoaded;
		}
	}

	public RaceCar CreatePreviewProfiledRaceCar(int carId, int profileId)
	{
		RaceCar raceCar = InstantiateCarPrefab("prefabs/cars/", carId, isLocalCar: true, inGarage: true, 29);
		if ((bool)raceCar)
		{
			SetProfile(raceCar, profileId);
		}
		return raceCar;
	}

	public async UniTask<RaceCar> CreateNetworkRaceCar(NetworkPlayer host, int carId, ProfilesInfo.Profile profile, int swapId, CarVisualManager.VisualSettings overrideVisual)
	{
		TryApplySpecialLivery(carId);
		if (profile != null && profile.id != 0)
		{
			GameLog.Print(AlertLevel.Notify, "System", $"CreateRaceCar {carId}; {profile.id}");
		}
		int profileid = profile?.id ?? 0;
		CarVisualManager.VisualSettings visualSettings = ((overrideVisual != null) ? overrideVisual : m_prefs.carSettings.GetVisualForCar(carId));
		int skipFrames = UnityEngine.Random.Range(1, 10);
		while (skipFrames-- > 0)
		{
			await UniTask.Yield();
		}
		ResetBodykitIfNeed(carId, visualSettings);
		await UniTask.Yield();
		if (host == null)
		{
			return null;
		}
		RaceCar car = await InstantiateCarPrefabAsync("prefabs/cars/", carId, host.lastRecivedNetworkPosition, host.lastRecivedNetworkRotation, isLocalCar: false, inGarage: false);
		if ((bool)car)
		{
			car.networkPlayer = host;
			car.getRigidbody.detectCollisions = false;
			await UniTask.Yield();
			SetProfile(car, profileid);
			CarVisualManager.CustomizationFlags visualFlags = CarVisualManager.CustomizationFlags.All;
			string value = "SyncNetFreerideRaceModeState";
			if (States.mainCurrent.name.Contains(value))
			{
				visualFlags &= ~CarVisualManager.CustomizationFlags.Vinyls;
			}
			await car.swap.Initialize(car, m_prefs, m_model, swapId).ToUniTask();
			await UniTask.Yield();
			await m_carVisualManager.ApplyVisualAsync(visualFlags, car, visualSettings, profile, isLocalPlayerCar: false);
			await UniTask.Yield();
			car.OnCarVisualLoadFinishEvent += ApplyTireStickersOnCarVisualLoaded;
			PrepareCar(car, inGarage: false);
			if (!car)
			{
				return null;
			}
			if (profile != null)
			{
				car.ApplyAdvancedProfile(NetworkController.InstanceGame.baseModel, profile, m_prefs.carSettings, reset: false, skipSwapControllers: true);
			}
			car.getRigidbody.detectCollisions = true;
			return car;
		}
		return null;
		void ApplyTireStickersOnCarVisualLoaded(RaceCar raceCar)
		{
			raceCar.OnCarVisualLoadFinishEvent -= ApplyTireStickersOnCarVisualLoaded;
			m_carVisualManager.ApplyVisualAsync(CarVisualManager.CustomizationFlags.AllTireStickers, raceCar, visualSettings).Forget();
		}
	}

	public RaceCar CreateGhostCarRealistic(int carId, int profileId, int swapId, CarVisualManager.VisualSettings overrideVisual = null, ProfilesInfo.Profile profile = null, bool needColliders = false, string platformUserId = null)
	{
		TryApplySpecialLivery(carId);
		RaceCar raceCar = InstantiateCarPrefab("prefabs/cars/", carId, isLocalCar: true, inGarage: false, needColliders ? 8 : 17);
		CarVisualManager.VisualSettings visualSettings;
		if (raceCar != null)
		{
			raceCar.isGhost = true;
			raceCar.networkPlayer = TryCreateNetworkPlayer();
			SetProfile(raceCar, profileId);
			visualSettings = overrideVisual ?? m_prefs.carSettings.GetVisualForCar(carId);
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, raceCar, visualSettings, profile);
			raceCar.OnCarVisualLoadFinishEvent += ApplyTireStickersOnCarVisualLoaded;
			PrepareDriver(raceCar);
			TimeController.StartNow(raceCar.swap.Initialize(raceCar, m_prefs, m_model, swapId));
		}
		return raceCar;
		void ApplyTireStickersOnCarVisualLoaded(RaceCar car)
		{
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.AllTireStickers, car, visualSettings);
			car.OnCarVisualLoadFinishEvent -= ApplyTireStickersOnCarVisualLoaded;
		}
		NetworkPlayer TryCreateNetworkPlayer()
		{
			if (string.IsNullOrEmpty(platformUserId))
			{
				return null;
			}
			PlayerId playerId = PlayerId.Parse(platformUserId);
			bool isLocal = Overlay.instance.player.id.Equals(playerId);
			SmartfoxPlayerProperties playerProperties = new SmartfoxPlayerProperties
			{
				PlayerID = playerId
			};
			return new NetworkPlayer(NetworkController.InstanceGame, isLocal, playerProperties);
		}
	}

	public bool SpecialCarCanApplyPart(int carId, CarPart carPart)
	{
		if (carPart == null)
		{
			return false;
		}
		if (!m_specialLiveryApplyCars.TryGetValue(carId, out (string, int) value))
		{
			return true;
		}
		if (!value.Item1.Equals(carPart.type, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return value.Item2 == carPart.id;
	}

	private void TryApplySpecialLivery(int carId)
	{
		if (!m_specialLiveryApplyCars.TryGetValue(carId, out (string, int) value))
		{
			return;
		}
		CarVisualManager.VisualSettings visualSettings = m_prefs.carSettings.GetVisualForCar(carId).Copy();
		CarPartDatabase.SlotScheme slotScheme = new CarPartDatabase.SlotScheme(carId, visualSettings.parts);
		List<CarPart> parts = CarPartDatabase.GetParts(slotScheme, value.Item1, string.Empty);
		if (parts == null)
		{
			return;
		}
		CarPart carPart = null;
		foreach (CarPart item in parts)
		{
			if (item.id == value.Item2)
			{
				carPart = item;
				break;
			}
		}
		if (carPart != null && !visualSettings.parts.Contains(carPart.id))
		{
			visualSettings.parts.Add(carPart.id);
			slotScheme.Equip(carPart.id);
			slotScheme.ToPartList(visualSettings.parts);
			m_carVisualManager.SaveVisualSettingsForCar(carId, visualSettings);
		}
	}

	private void ApplyPredefinedVinylIfNeed(int carId, CarVisualManager.VisualSettings vs, out CarVilylsInfo.VinylPreset vinylPreset)
	{
		vinylPreset = null;
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(carId);
		if (vinylsInfoForCar.GetPresetsList().Count == 0)
		{
			PredefinedVinylData predefinedVinylData = SingletonScriptableObject<PredefinedCarVinylsConfig>.instance.carPredefinedVinylsList.Find((PredefinedVinylData item) => item.carId == carId);
			if (predefinedVinylData != null && JSON.TryParse<CarVilylsInfo.VinylPreset>(predefinedVinylData.vinylsJson.text, out var result))
			{
				vinylPreset = result;
				vinylPreset.SetLocked();
				vinylsInfoForCar.AddPreset(vinylPreset);
				vs.vinylPresetLiveryId = result.liveryId;
				vs.vinylPresetId = result.id;
				vs.vinylLayers.Clear();
				vs.vinylLayers.AddRange(result.layers.Crop());
				m_prefs.carSettings.SetVisualForCar(carId, vs);
			}
		}
	}

	private void ResetBodykitIfNeed(int carId, CarVisualManager.VisualSettings visual)
	{
	}

	private void SetProfile(RaceCar car, int profileId)
	{
		PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(profileId);
		if (byId != null)
		{
			car.profileMetaInfo = byId;
		}
	}

	public void GarageApplyProfile(RaceCar car, int profileId)
	{
		SetProfile(car, profileId);
		PrepareCar(car, inGarage: true);
	}

	public void ResetGaragePositionAndProfile(RaceCar car)
	{
		if (car != null)
		{
			GameObject gameObject = GameObject.Find("CarPositionMarker");
			if (gameObject != null)
			{
				car.transform.SetParent(gameObject.transform, worldPositionStays: false);
				car.transform.SetLocalPositionAndRotation(GetAdjustementPosition(car), Quaternion.identity);
			}
		}
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(car.metaInfo.id);
		GarageApplyProfile(car, profileIdForCar);
	}

	public Vector3 GetAdjustementPosition(RaceCar raceCar)
	{
		float num = 0f;
		CarSuspensionTarget componentInChildren = raceCar.transform.GetComponentInChildren<CarSuspensionTarget>();
		if ((bool)componentInChildren)
		{
			Vector3 position = componentInChildren.frontLeftDamperTarget.position;
			if (Physics.Raycast(new Ray(position + Vector3.up, Vector3.down), out var hitInfo, 10f, 2048))
			{
				Collider collider = hitInfo.collider;
				if ((bool)collider)
				{
					Vector3 vector = collider.bounds.ClosestPoint(position);
					num = (position - vector).sqrMagnitude;
				}
			}
		}
		if (!(num > 0.44f))
		{
			return Vector3.up * 0.2f;
		}
		return Vector3.zero;
	}

	public IEnumerator CarSetVisualAsync(RaceCar car, Action<RaceCar> onCarApplyVisual, CarVisualManager.VisualSettings overrideVisual = null, bool skipProfileRefresh = false)
	{
		if (BaseCar.IsNull(car))
		{
			onCarApplyVisual.Call(car);
			yield break;
		}
		TryApplySpecialLivery(car.carId);
		if (skipProfileRefresh)
		{
			PlayerCarProfile profileMetaInfo = m_model.QueryAll<PlayerCarProfile>().Find((PlayerCarProfile profile) => profile.carId == car.carId && profile.name.Equals("Stock", StringComparison.OrdinalIgnoreCase));
			car.carX.ShutdownEngine();
			car.profileMetaInfo = profileMetaInfo;
		}
		else
		{
			GarageApplyProfile(car, m_prefs.carSettings.GetProfileIdForCar(car.carId));
			int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(car.carId);
			yield return car.swap.Initialize(car, m_prefs, m_model, engineIdForCar);
		}
		CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(car.carId);
		m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, car, overrideVisual ?? visualForCar);
		car.OnCarVisualLoadFinishEvent += VisualLoaded;
		onCarApplyVisual.SafeCall(car);
		void VisualLoaded(RaceCar raceCar)
		{
			CarVisualManager.VisualSettings visualForCar2 = m_prefs.carSettings.GetVisualForCar(car.carId);
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.AllTireStickers, raceCar, visualForCar2);
			car.OnCarVisualLoadFinishEvent -= VisualLoaded;
		}
	}

	public IEnumerator GarageCreateCarAsync(int carId, Action<RaceCar> onCarCreated, bool inGarage, Vector3 position, Quaternion rotation, CarVisualManager.VisualSettings overrideVisual = null, bool skipProfileRefresh = false)
	{
		TryApplySpecialLivery(carId);
		RaceCar car = null;
		PlayerCar playerCarMeta = m_model.GetById<PlayerCar>(carId);
		string prefabPath = GetCarPrefabPath("prefabs/cars/", playerCarMeta);
		GameObject carPrefab;
		if (!string.IsNullOrEmpty(prefabPath))
		{
			carPrefab = null;
			yield return ResourcesWrapper.LoadAsync(prefabPath, delegate(GameObject result)
			{
				carPrefab = result;
			});
			while (!IsCanInstantiateInThisFrame())
			{
				yield return null;
			}
			CheckPrefabLoadResult(prefabPath, carPrefab);
			car = CreateCarInstance(carPrefab, playerCarMeta, isLocalCar: true, inGarage, -1, position, rotation);
			car.carModel.onDispose += ReleaseResources;
		}
		if (car != null)
		{
			if (skipProfileRefresh)
			{
				PlayerCarProfile profileMetaInfo = m_model.QueryAll<PlayerCarProfile>().Find((PlayerCarProfile profile) => profile.carId == car.carId && profile.name.Equals("Stock", StringComparison.OrdinalIgnoreCase));
				car.carX.ShutdownEngine();
				car.profileMetaInfo = profileMetaInfo;
			}
			else
			{
				if (position == Vector3.zero || rotation == Quaternion.identity)
				{
					ResetGaragePositionAndProfile(car);
				}
				int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId);
				GarageApplyProfile(car, profileIdForCar);
			}
			CarVisualManager.VisualSettings visualSettings = m_prefs.carSettings.GetVisualForCar(carId);
			ApplyPredefinedVinylIfNeed(carId, visualSettings, out var _);
			if (!skipProfileRefresh)
			{
				int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(car.carId);
				yield return car.swap.Initialize(car, m_prefs, m_model, engineIdForCar);
			}
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.All, car, overrideVisual ?? visualSettings);
			if (!skipProfileRefresh)
			{
				CarProfileCacheManager.RecalculateAll(car, allowApplyProfileByLazy: false);
			}
			car.OnCarVisualLoadFinishEvent += ApplyTireStickersOnCarVisualLoaded;
		}
		onCarCreated.Call(car);
		void ApplyTireStickersOnCarVisualLoaded(RaceCar raceCar)
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(carId);
			m_carVisualManager.ApplyVisual(CarVisualManager.CustomizationFlags.AllTireStickers, raceCar, visualForCar);
			raceCar.OnCarVisualLoadFinishEvent -= ApplyTireStickersOnCarVisualLoaded;
		}
		void ReleaseResources()
		{
			car.carModel.onDispose -= ReleaseResources;
			ResourcesWrapper.UnloadAsset(carPrefab);
		}
	}

	public static string GetCarPrefabPath(string folder, PlayerCar playerCarMeta)
	{
		if (playerCarMeta == null)
		{
			return null;
		}
		return folder + playerCarMeta.prefab;
	}

	private async UniTask<RaceCar> InstantiateCarPrefabAsync(string folder, int carId, Vector3 position, Quaternion rotation, bool isLocalCar, bool inGarage, int layer = -1)
	{
		PlayerCar playerCarMeta = m_model.GetById<PlayerCar>(carId);
		string prefabPath = GetCarPrefabPath(folder, playerCarMeta);
		if (string.IsNullOrEmpty(prefabPath))
		{
			return null;
		}
		GameObject prefab = null;
		AsyncOperationHandle<GameObject> aoh = ResourcesWrapper.LoadAsync(prefabPath, delegate(GameObject result)
		{
			prefab = result;
		});
		await aoh;
		if (aoh.IsValid())
		{
			await UniTask.WaitUntil(() => prefab);
		}
		CheckPrefabLoadResult(prefabPath, prefab);
		RaceCar carInstance = CreateCarInstance(prefab, playerCarMeta, isLocalCar, inGarage, layer, position, rotation);
		if ((bool)carInstance && carInstance.carModel != null)
		{
			carInstance.carModel.onDispose += ReleaseResources;
		}
		else
		{
			ResourcesWrapper.UnloadAsset(aoh);
		}
		return carInstance;
		void ReleaseResources()
		{
			carInstance.carModel.onDispose -= ReleaseResources;
			ResourcesWrapper.UnloadAsset(aoh);
		}
	}

	private bool IsCanInstantiateInThisFrame()
	{
		if (m_lastInstantiateFrame == Time.frameCount)
		{
			return false;
		}
		m_lastInstantiateFrame = Time.frameCount;
		return true;
	}

	protected RaceCar CreateCarInstance(GameObject prefab, PlayerCar meta, bool isLocalCar, bool inGarage, int layer, Vector3 initPos, Quaternion rotation)
	{
		if (!prefab)
		{
			return null;
		}
		prefab.gameObject.SetActive(value: false);
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, initPos, rotation);
		prefab.gameObject.SetActive(value: true);
		gameObject.RemoveCloneFromName();
		RaceCar component = gameObject.GetComponent<RaceCar>();
		component.carId = meta.id;
		component.Init(meta, layer, isLocalCar, inGarage);
		if (layer != -1)
		{
			gameObject.ReplaceLayerRecursively(8, layer);
		}
		return component;
	}

	private RaceCar InstantiateCarPrefab(string folder, int carId, bool isLocalCar, bool inGarage, int layer = -1)
	{
		RaceCar raceCar = null;
		PlayerCar byId = m_model.GetById<PlayerCar>(carId);
		string carPrefabPath = GetCarPrefabPath(folder, byId);
		if (!string.IsNullOrEmpty(carPrefabPath))
		{
			GameObject go = ResourcesWrapper.Load<GameObject>(carPrefabPath);
			if (go != null)
			{
				CheckPrefabLoadResult(carPrefabPath, go);
				raceCar = CreateCarInstance(go, byId, isLocalCar, inGarage, layer, Vector3.zero, Quaternion.identity);
				raceCar.carModel.onDispose += delegate
				{
					ResourcesWrapper.UnloadAsset(go);
				};
			}
		}
		if (raceCar != null)
		{
			raceCar.carX.interpolation = GameConsole.Bool["SetInterpolate"];
		}
		return raceCar;
	}

	private void CheckPrefabLoadResult(string prefabPath, GameObject prefab)
	{
		if (prefab == null)
		{
			Debug.LogError("Cannot load Car prefab from: " + prefabPath);
		}
	}

	private void PrepareCar(RaceCar car, bool inGarage)
	{
		PrepareDriver(car);
		PreparePhysics(car, inGarage);
	}

	private void PrepareDriver(RaceCar car)
	{
		TimeController.StartNow(car.InstantiateAndInitDriverCoroutine());
	}

	private void ApplyControlsSetupParams()
	{
		ControlsSetup instance = ScriptableSingleton<ControlsSetup>.instance;
		InputManager instance2 = InputManager.instance;
		if (!(instance == null) && !(instance2 == null))
		{
			instance.ApplyParams(instance2.GetLastSteeringMode());
		}
	}

	private void PreparePhysics(RaceCar car, bool inGarage)
	{
		if (inGarage)
		{
			car.carX.ShutdownEngine();
			car.EnableCarXMultiRaycast(enable: true);
			ApplyControlsSetupParams();
		}
		else
		{
			car.EnableCarXMultiRaycast(car.isLocalPlayer);
		}
		car.getRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
		car.getRigidbody.maxAngularVelocity = 60f;
	}
}
