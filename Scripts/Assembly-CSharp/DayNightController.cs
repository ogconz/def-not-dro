using System;
using System.Collections.Generic;
using DI;
using UnityEngine;
using UnityEngine.Events;
using Utils.SceneManagement;

public class DayNightController : MonoBehaviour
{
	private const string FRONTLIGHT_PREFABPATH = "prefabs/cars/_shared/headlight";

	private const string BACKLIGHT_PREFABPATH = "prefabs/cars/_shared/rearlight";

	private const float DELAY_CHANGE_MODE_TIME = 0.2f;

	private static List<IDayNightBehaviour> dayNightBehaviours = new List<IDayNightBehaviour>();

	private GamePrefs m_prefs;

	public bool autoStart;

	public string dayScenario;

	public string nightScenario;

	public UnityEvent OnChangeDayNightMode;

	private string m_lastSceneLightingName;

	private LightControlSystem m_lightControlSystem;

	private Transform m_mainCameraTransform;

	private AdditiveSceneController m_additiveSceneLoadController = new AdditiveSceneController();

	private float m_lastTimeModeChanged;

	public static DayNightController instance { get; private set; }

	public Light FrontLightPrefab { get; private set; }

	public GameObject BackLightProjector { get; private set; }

	public Light TailLight { get; private set; }

	public bool IsDayLight
	{
		get
		{
			if (IsAppliedMode)
			{
				return m_lastSceneLightingName.Equals(dayScenario);
			}
			return false;
		}
	}

	public bool IsAppliedMode => !string.IsNullOrEmpty(m_lastSceneLightingName);

	public bool isReady => m_additiveSceneLoadController.IsReady();

	public bool isTimeoutToChangeModeOver => Time.unscaledTime - m_lastTimeModeChanged > 0.2f;

	private void Awake()
	{
		instance = this;
		m_lightControlSystem = new LightControlSystem();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		base.gameObject.AddComponent<LightVariantController>();
		XRModeManager xRModeManager = XRModeManager.instance;
		if (xRModeManager != null)
		{
			xRModeManager.OnCameraModeChanged = (Action<Camera>)Delegate.Combine(xRModeManager.OnCameraModeChanged, new Action<Camera>(ChangeCameraTo));
		}
	}

	private void ChangeCameraTo(Camera camera)
	{
		if (!(camera == null))
		{
			m_mainCameraTransform = camera.transform;
		}
	}

	private void Start()
	{
		InitResources();
	}

	private void InitResources()
	{
		if (FrontLightPrefab == null)
		{
			GameObject gameObject = ResourcesWrapper.Load<GameObject>("prefabs/cars/_shared/headlight");
			if (gameObject != null)
			{
				FrontLightPrefab = gameObject.GetComponent<Light>();
			}
		}
		if (FrontLightPrefab == null)
		{
			Debug.LogError("Cannot load light prefab from: prefabs/cars/_shared/headlight");
		}
		if (BackLightProjector == null)
		{
			GameObject gameObject2 = ResourcesWrapper.Load<GameObject>("prefabs/cars/_shared/rearlight");
			if (gameObject2 != null)
			{
				BackLightProjector = gameObject2;
				TailLight = BackLightProjector.GetComponent<Light>();
			}
		}
		if (BackLightProjector == null)
		{
			Debug.LogError("Cannot load back light prefab from: prefabs/cars/_shared/rearlight");
		}
	}

	private void OnDestroy()
	{
		dayNightBehaviours.Clear();
		instance = null;
		OnChangeDayNightMode.RemoveAllListeners();
		m_lightControlSystem?.Dispose();
		m_lightControlSystem = null;
		m_additiveSceneLoadController.Unload();
		XRModeManager xRModeManager = XRModeManager.instance;
		if (xRModeManager != null)
		{
			xRModeManager.OnCameraModeChanged = (Action<Camera>)Delegate.Remove(xRModeManager.OnCameraModeChanged, new Action<Camera>(ChangeCameraTo));
		}
	}

	private void OnSceneReady(GameEvents.EventInfo e)
	{
		bool flag = true;
		if (autoStart)
		{
			if (m_prefs != null)
			{
				flag = !m_prefs.globals.nightModeActive;
			}
		}
		else
		{
			flag = !BaseRaceState.NightModeActive;
		}
		TryApplyMode(flag ? dayScenario : nightScenario, flag);
	}

	public void SetDayNightMode(bool isDay)
	{
		TryApplyMode(isDay ? dayScenario : nightScenario, isDay);
	}

	private void TryApplyMode(string name, bool isDay)
	{
		if (!(m_lastSceneLightingName == name))
		{
			m_lastSceneLightingName = name;
			m_lastTimeModeChanged = Time.unscaledTime;
			OnChangeDayNightMode?.Invoke();
			LoadLightingScene(name, isDay);
		}
	}

	private void LoadLightingScene(string name, bool isDay)
	{
		if (string.IsNullOrEmpty(name))
		{
			Debug.LogError("Cannot load LightingScenario: " + name);
			return;
		}
		m_additiveSceneLoadController.LoadAdditionalScene(name);
		m_lastSceneLightingName = name;
		if (isDay)
		{
			for (int i = 0; i < dayNightBehaviours.Count; i++)
			{
				dayNightBehaviours[i].ChangeToDay();
			}
		}
		else
		{
			for (int j = 0; j < dayNightBehaviours.Count; j++)
			{
				dayNightBehaviours[j].ChangeToNight();
			}
		}
	}

	public static void Register(IDayNightBehaviour beh)
	{
		dayNightBehaviours.Add(beh);
	}

	public static void UnRegister(IDayNightBehaviour beh)
	{
		dayNightBehaviours.Remove(beh);
	}

	public void RegisterLight(LightControlAccessor light)
	{
		m_lightControlSystem?.TryAdd(light);
	}

	public void UnRegisterLight(LightControlAccessor light)
	{
		m_lightControlSystem?.Remove(light);
	}

	private Transform GetMainCameraTransform()
	{
		if (m_mainCameraTransform == null)
		{
			Camera main = Camera.main;
			if (!main)
			{
				return null;
			}
			GameObject gameObject = main.gameObject;
			if (gameObject != null)
			{
				m_mainCameraTransform = gameObject.GetComponent<Transform>();
			}
		}
		return m_mainCameraTransform;
	}

	private void Update()
	{
		m_additiveSceneLoadController.Update();
		Transform mainCameraTransform = GetMainCameraTransform();
		if (!IsDayLight && mainCameraTransform != null)
		{
			m_lightControlSystem?.BeginUpdate(mainCameraTransform.position, GraphicsQuality.nightSettings.fadeDistance);
		}
	}

	private void LateUpdate()
	{
		m_lightControlSystem?.EndUpdate();
	}
}
