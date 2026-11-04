using System;
using System.Collections;
using System.Collections.Generic;
using Binding.Base;
using CarX;
using CarX.Internal;
using Customization;
using DB.Meta;
using DI;
using TriLib;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class IconGeneratorContext : BaseBindingBehaviourTarget
{
	private const float WaitPhysicsTime = 0.025f;

	private const float TimeToNew = 0.2f;

	private const int Version = 1;

	private Camera m_camera;

	private CameraRotationCustom m_cameraRotation;

	private bool m_isCameraFind;

	[SerializeField]
	private float m_sizeBounds = 1f;

	[SerializeField]
	private Vector2Int m_sizeRenderTexture;

	[SerializeField]
	private float m_scaleDestTexture = 0.25f;

	[SerializeField]
	private RenderTexture m_renderTexture;

	private bool m_isRunningSnapshot;

	private bool m_isCarViewLoaded;

	public Action snapshotCallbackStart;

	public Action<RenderTexture> snapshotCallback;

	private GarageCar3dViewContext m_garage3dView;

	private CarDesc m_carDesc;

	private static RaceCar m_currentCar;

	private static int m_currentCarId;

	private static RaceCar m_lastCar;

	private static List<RaceCar> m_loadedCars = new List<RaceCar>();

	private List<PlayerCar> m_allCar;

	private Transform m_cameraTarget;

	public int m_loadedCarCameraRotUpdate;

	private Dictionary<int, IconSceneGroup> m_sceneCategory = new Dictionary<int, IconSceneGroup>();

	[SerializeField]
	private IconSceneGroup m_defaultSceneIconGroup;

	[SerializeField]
	private IconSceneGroup m_selectSceneIconGroup;

	[SerializeField]
	private RenderTexture m_icon;

	private static List<IconCarRenderTexture> m_temporaryCarIcons = new List<IconCarRenderTexture>();

	private static Dictionary<int, IconCarData> m_carListSnapshot = new Dictionary<int, IconCarData>();

	private static GamePrefs m_prefs;

	private HashSet<IconCarData> m_carsVisualChanged = new HashSet<IconCarData>();

	private Dictionary<int, string> m_carPaths = new Dictionary<int, string>();

	private GraphicsQuality m_graphicsQuality;

	private WaitForSeconds m_waitPhysics = new WaitForSeconds(0.025f);

	private WaitForSeconds m_waitFocus = new WaitForSeconds(0.0025f);

	private Vector3 m_lastCarPosition;

	private Quaternion m_lastCarRotation;

	private static float m_timeToGetNew;

	private void Start()
	{
		m_allCar = m_garage3dView.GetAllCar();
		foreach (PlayerCar item in m_allCar)
		{
			m_carPaths[item.id] = item.icon;
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		SceneManager.LoadScene("IconGenerator", LoadSceneMode.Additive);
		GizmoNonEditor.Init();
		m_isCarViewLoaded = true;
		m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		CarsIconVisualValid();
		GameConsole.SetCmd("allCarIcon", delegate
		{
			CarsIconVisualValid(allCar: true);
		});
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		SceneManager.UnloadSceneAsync("IconGenerator");
		m_carListSnapshot.Clear();
		DeleteCars(m_loadedCars);
	}

	private void CarsIconVisualValid(bool allCar = false)
	{
		if (GarageContextValid())
		{
			if (!CheckVersionCompatibility())
			{
				ClearStorage();
			}
			if (allCar)
			{
				ClearStorage();
			}
			m_garage3dView.GetAllCarChangesVisual(m_carsVisualChanged, allCar);
			if (m_carsVisualChanged.Count > 0)
			{
				UpdateCarsIconFrom(m_carsVisualChanged.GetEnumerator());
			}
		}
	}

	private bool CheckVersionCompatibility()
	{
		return m_prefs.globals.iconGeneratorVersion >= 1;
	}

	public static void AddToCarHashQueue(IconCarData carData, bool onSave = false)
	{
		if (!IsContainQueue(carData))
		{
			m_timeToGetNew = 0.2f;
			m_carListSnapshot.Add(carData.carId, carData);
		}
	}

	public static bool IsContainQueue(IconCarData car)
	{
		if (!m_carListSnapshot.ContainsKey(car.carId))
		{
			return m_currentCarId == car.carId;
		}
		return true;
	}

	private void UpdateCarsIconFrom(IEnumerator<IconCarData> cars)
	{
		while (true)
		{
			IconCarData current = cars.Current;
			if (GetIconFromStorage(current) != null)
			{
				if (cars.MoveNext())
				{
					continue;
				}
				break;
			}
			bool flag = cars.MoveNext();
			AddToCarHashQueue(current, !flag);
			if (flag)
			{
				continue;
			}
			break;
		}
	}

	private bool GarageContextValid()
	{
		if (m_garage3dView == null)
		{
			m_garage3dView = States.State<GarageGUIState>().contextOwner.Get<GarageCar3dViewContext>();
			if (m_garage3dView == null)
			{
				return false;
			}
		}
		return true;
	}

	public IEnumerator SelectAndSnapshotCar(IconCarData carData, bool saveAsset = false, Action onDone = null)
	{
		m_currentCarId = carData.carId;
		SelectPodiumCarFromGarage(carData.carId);
		while (!m_isCarViewLoaded)
		{
			yield return null;
		}
		if (!BaseCar.IsNull(m_currentCar))
		{
			TimeController.StartNow(MakeSnapshotAsync(saveAsset));
			while (m_isRunningSnapshot)
			{
				yield return null;
			}
			onDone.SafeCall();
		}
	}

	public void SelectPodiumCarFromGarage(int carId, bool lastCar = false)
	{
		SelectCategory(carId);
		if (GarageContextValid() && CameraValid())
		{
			m_isCarViewLoaded = false;
			if (!BaseCar.IsNull(m_lastCar) && carId == m_lastCar.carId)
			{
				m_garage3dView.ApplyVisualPodiumCar(m_lastCar, OnLoadedCar);
				return;
			}
			DeleteCars(m_loadedCars);
			m_garage3dView.CreateCustomPodiumCar(carId, m_cameraTarget, lastCar, OnLoadedCar);
		}
	}

	public void AddCategory(IconSceneGroup category, int[] carIds)
	{
		if (carIds != null && carIds.Length != 0)
		{
			foreach (int key in carIds)
			{
				m_sceneCategory[key] = category;
			}
		}
		else
		{
			m_defaultSceneIconGroup = category;
		}
		category.gameObject.SetActive(value: false);
	}

	private void SelectCategory(int carId)
	{
		if (m_selectSceneIconGroup != null && m_selectSceneIconGroup.gameObject.activeSelf)
		{
			m_selectSceneIconGroup.gameObject.SetActive(value: false);
		}
		m_selectSceneIconGroup = (m_sceneCategory.TryGetValue(carId, out var value) ? value : m_defaultSceneIconGroup);
		if (!(m_selectSceneIconGroup == null) && !m_selectSceneIconGroup.gameObject.activeSelf)
		{
			try
			{
				m_selectSceneIconGroup.gameObject.SetActive(value: true);
			}
			catch (Exception)
			{
			}
			m_sizeBounds = m_selectSceneIconGroup.sizeBound;
			m_cameraTarget = m_selectSceneIconGroup.cameraPoint;
		}
	}

	private static void DeleteCars(List<RaceCar> cars)
	{
		if (cars == null || cars.Count <= 0)
		{
			return;
		}
		foreach (RaceCar car in cars)
		{
			UnityEngine.Object.Destroy(car.gameObject);
		}
		cars.Clear();
	}

	private void OnLoadedCar(RaceCar car)
	{
		if (!BaseCar.IsNull(m_lastCar) && m_lastCar.carId == car.carId)
		{
			m_currentCar = m_lastCar;
			m_isCarViewLoaded = true;
			m_currentCar.OnCarVisualLoadFinishEvent += delegate
			{
				car.getTransform.SetLightLayer(RenderingLayerMask.RenderingLayer3);
			};
			return;
		}
		m_currentCar = car;
		m_loadedCars.Add(car);
		m_currentCar.OnCarVisualLoadFinishEvent += delegate
		{
			car.getTransform.SetLightLayer(RenderingLayerMask.RenderingLayer3);
		};
		m_loadedCarCameraRotUpdate = 1;
		m_currentCar.carX.GetCarDesc(ref m_carDesc);
		if (Physics.Raycast(new Ray(m_currentCar.getTransform.position + Vector3.up, Vector3.down), out var hitInfo, 10f, 2048))
		{
			m_currentCar.GetWallCheckPointPosition(out var rearLeft, out var rearRight);
			Vector3 position = m_currentCar.carX.transform.position;
			position = new Vector3(position.x, hitInfo.transform.position.y + Mathf.Max(rearLeft.y, rearRight.y), position.z);
			m_currentCar.carX.transform.position = position;
		}
		m_cameraRotation.distanceFromCenter = 64f;
		m_cameraRotation.IdleRotation();
		m_cameraRotation.IdleTransitions();
		car.getTransform.SetLightLayer(RenderingLayerMask.RenderingLayer3);
		m_isCarViewLoaded = true;
	}

	private void StartFromHashQueue()
	{
		if (m_carListSnapshot.Count <= 0 || !m_isCarViewLoaded || m_isRunningSnapshot)
		{
			return;
		}
		if (m_timeToGetNew <= 0f)
		{
			m_timeToGetNew = 0.2f;
			KeyValuePair<int, IconCarData> keyValuePair = m_carListSnapshot.First();
			int key = keyValuePair.Key;
			if (key != 0 && !CarPartDatabase.IsCarBodypartsListContainsUGC(key))
			{
				TimeController.StartNow(SelectAndSnapshotCar(keyValuePair.Value));
			}
			m_carListSnapshot.Remove(keyValuePair.Key);
		}
		else
		{
			m_timeToGetNew -= Time.deltaTime;
		}
	}

	private void Update()
	{
		if (base.owner.IsActive<UIDynostandContext>() || base.owner.IsActive<UISetupsContext>() || base.owner.IsActive<UIBodyCustomizationContext>() || base.owner.IsActive<UIPlayerProfileContext>())
		{
			return;
		}
		bool num = BaseCar.IsNull(m_currentCar);
		bool flag = CameraValid();
		if (num && flag)
		{
			StartFromHashQueue();
		}
		if (!num && m_loadedCarCameraRotUpdate > 0 && flag)
		{
			m_currentCar.SetHeadlightsEnabled(lightsEnabled: false);
			Vector3 position = m_camera.transform.position;
			if (!BaseCar.IsNull(m_currentCar))
			{
				m_currentCar.carX.GetCarDesc(ref m_carDesc);
				m_camera.aspect = (float)m_sizeRenderTexture.x / (float)m_sizeRenderTexture.y;
				Bounds bounds = m_carDesc.weight.bounds;
				float num2 = math.max(bounds.extents.x, math.max(bounds.extents.y, bounds.extents.z));
				Vector3[] boxCorner = GizmoNonEditor.GetBoxCorner(bounds.center, Vector3.one * num2 * m_sizeBounds, m_currentCar.carX.getTransform.worldToLocalMatrix);
				Vector3 position2 = m_currentCar.carX.getTransform.position;
				MathExt.BoundRect maxMinCameraRectBound = MathExt.GetMaxMinCameraRectBound(m_camera, position2, position, m_cameraRotation.distanceFromCenter, boxCorner);
				m_cameraRotation.distanceFromCenter = maxMinCameraRectBound.GetAspect();
				m_cameraRotation.distanceFromCenter = math.clamp(m_cameraRotation.distanceFromCenter, 2f, 64f);
				m_cameraRotation.IdleRotation();
				m_cameraRotation.IdleTransitions();
				m_cameraRotation.Stop();
				m_loadedCarCameraRotUpdate--;
			}
		}
	}

	private bool CameraValid()
	{
		m_isCameraFind = true;
		if (m_camera == null)
		{
			GameObject gameObject = GameObject.FindWithTag("IconGeneratorCamera");
			if (gameObject != null)
			{
				m_camera = gameObject.GetComponent<Camera>();
				if (m_camera != null)
				{
					m_camera.gameObject.SetActive(value: false);
					Transform parent = m_camera.transform.parent;
					m_camera.forceIntoRenderTexture = true;
					if (m_cameraTarget == null)
					{
						m_cameraTarget = parent.GetComponent<CameraTarget>().cameraTarget;
					}
					if (m_cameraRotation == null)
					{
						m_cameraRotation = parent.GetComponent<CameraRotationCustom>();
					}
					m_isCameraFind = false;
				}
			}
		}
		return m_isCameraFind;
	}

	private IEnumerator MakeSnapshotAsync(bool createAsset)
	{
		if (Valid())
		{
			Debug.LogError("Camera or Car is null");
			yield break;
		}
		m_isRunningSnapshot = true;
		int count = 4;
		int countMax = 1000;
		float time = 4f;
		snapshotCallbackStart.SafeCall();
		snapshotCallbackStart = null;
		if (m_currentCar != m_lastCar)
		{
			m_lastCarPosition = m_currentCar.carX.getTransform.position;
			m_lastCarRotation = m_currentCar.carX.getTransform.rotation;
		}
		else
		{
			m_currentCar.carX.getTransform.SetPositionAndRotation(new Vector3(m_lastCarPosition.x, m_currentCar.carX.getTransform.position.y, m_lastCarPosition.z), m_lastCarRotation);
		}
		while (count > 0 && time > 0f)
		{
			if (TryAddCarToQueue())
			{
				yield break;
			}
			if (m_currentCar.carX.velocity.magnitude <= 0.1f || countMax <= 0)
			{
				count--;
				countMax--;
			}
			else
			{
				count = 4;
			}
			time -= 0.025f;
			yield return m_waitPhysics;
		}
		if (TryAddCarToQueue())
		{
			yield break;
		}
		m_renderTexture = RenderTexture.GetTemporary(m_sizeRenderTexture.x, m_sizeRenderTexture.y, 0, RenderTextureFormat.ARGB32);
		m_camera.targetTexture = m_renderTexture;
		yield return m_waitFocus;
		if (!TryAddCarToQueue())
		{
			m_graphicsQuality.SetSnapShotQualityLevel(1f);
			m_camera.gameObject.SetActive(value: true);
			m_camera.Render();
			m_camera.Render();
			m_graphicsQuality.SetLastSnapShotQualityLevel();
			m_camera.gameObject.SetActive(value: false);
			m_icon = Texture2DUtils.FilteredDownscale(m_renderTexture, (int)((float)m_sizeRenderTexture.x * m_scaleDestTexture), (int)((float)m_sizeRenderTexture.y * m_scaleDestTexture));
			RenderTexture.active = null;
			IconCarData iconCarData = new IconCarData
			{
				carId = m_currentCar.carId,
				iconName = m_currentCar.metaInfo.icon
			};
			int activeSlotId = m_prefs.carSettings.GetActiveSlotId(m_currentCar.carId);
			RenderTexture renderTexture = GetOrAddRenderTexture(size: new Vector2Int((int)((float)m_sizeRenderTexture.x * m_scaleDestTexture), (int)((float)m_sizeRenderTexture.y * m_scaleDestTexture)), carId: m_currentCar.carId, slotId: activeSlotId);
			Graphics.Blit(m_icon, renderTexture);
			if (!createAsset)
			{
				IconStorageSystem.SaveGaragePreview(iconCarData.carId, activeSlotId, RenderTextureToTexture(m_icon, TextureFormat.RGB24));
			}
			RenderTexture.ReleaseTemporary(m_icon);
			RenderTexture.ReleaseTemporary(m_renderTexture);
			m_icon = null;
			m_isRunningSnapshot = false;
			m_lastCar = m_currentCar;
			m_currentCar = null;
			m_currentCarId = -1;
			m_camera.targetTexture = null;
			snapshotCallback.SafeCall(renderTexture);
			snapshotCallback = null;
		}
		bool TryAddCarToQueue()
		{
			if (Valid())
			{
				snapshotCallback = null;
				m_isRunningSnapshot = false;
				m_currentCarId = -1;
				AddToCarHashQueue(new IconCarData(m_currentCar.carId, m_currentCar.metaInfo.icon));
				return true;
			}
			return false;
		}
		bool Valid()
		{
			if (CameraValid() && !BaseCar.IsNull(m_currentCar))
			{
				return m_graphicsQuality == null;
			}
			return true;
		}
	}

	private Texture2D RenderTextureToTexture(RenderTexture target, TextureFormat format)
	{
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = target;
		Texture2D texture2D = new Texture2D(target.width, target.height, format, mipChain: false, linear: false);
		texture2D.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		return texture2D;
	}

	private static RenderTexture GetIconCache(int carId, int slotId)
	{
		foreach (IconCarRenderTexture temporaryCarIcon in m_temporaryCarIcons)
		{
			if (temporaryCarIcon.carId == carId && temporaryCarIcon.slotId == slotId)
			{
				return temporaryCarIcon.texture;
			}
		}
		return null;
	}

	private static RenderTexture GetOrAddRenderTexture(int carId, int slotId, Vector2Int size)
	{
		RenderTexture iconCache = GetIconCache(carId, slotId);
		if (iconCache != null)
		{
			return iconCache;
		}
		IconCarRenderTexture item = new IconCarRenderTexture
		{
			carId = carId,
			slotId = slotId,
			texture = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32)
		};
		m_temporaryCarIcons.Add(item);
		return item.texture;
	}

	public static RenderTexture GetIconFromStorage(IconCarData car)
	{
		int carId = car.carId;
		int slotId = ((m_prefs != null) ? m_prefs.carSettings.GetActiveSlotId(carId) : 0);
		RenderTexture iconCache = GetIconCache(carId, slotId);
		if (iconCache != null)
		{
			return iconCache;
		}
		Texture2D texture2D = IconStorageSystem.ReadGaragePreview(carId, slotId);
		if (texture2D != null)
		{
			Graphics.Blit(texture2D, GetOrAddRenderTexture(carId, slotId, new Vector2Int(texture2D.width, texture2D.height)));
		}
		return GetIconCache(carId, slotId);
	}

	public static void ClearStorage()
	{
		m_carListSnapshot.Clear();
		m_temporaryCarIcons.Clear();
		m_currentCar = null;
		DeleteCars(m_loadedCars);
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.globals.carsVisualUpdate.Clear();
		gamePrefs.globals.iconGeneratorVersion = 1;
	}
}
