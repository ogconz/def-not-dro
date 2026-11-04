using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DI;
using GameInput;
using GameOverlay;
using Tayx.Graphy;
using Tayx.Graphy.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class UIPhotoModeContext : BaseBindingBehaviourTarget
{
	private const string BlobshadowCarGOName = "blobshadowCar";

	private const string SparksGOName = "Sparks";

	private const string WheelsParticlesGOName = "WheelsParticles";

	private const string ObstacleGOName = "Obstacle";

	private const string BeamGeometryGOName = "Beam Geometry";

	private const string FlameRootGOName = "Flame_root";

	private const string WheelsGOName = "Wheels";

	private const string CloneGOPrefix = "(Clone";

	private const float AccelerationScaleDefault = 1f;

	private const int ScreenshotQuality = 2;

	private const float FovMin = 20f;

	private const float FovMax = 70f;

	private const float HeightDefault = 0f;

	private const float HeightMin = -0.5f;

	private const float HeightMax = 1f;

	private const float TiltDefault = 0f;

	private const float TiltMin = -30f;

	private const float TiltMax = 30f;

	private const float HeightDelta = 0.01f;

	private const float FovDelta = 0.1f;

	private const float TiltDelta = 0.1f;

	private readonly List<string> m_ignoreCarPartsLayerChanges = new List<string> { "blobshadowCar", "WheelsParticles", "Sparks", "Obstacle" };

	private readonly Dictionary<string, int> m_carPartsPhotoModeRenderLayers = new Dictionary<string, int>
	{
		{ "Wheels", 27 },
		{ "Flame_root", 0 },
		{ "Beam Geometry", 0 },
		{ "Obstacle", 23 }
	};

	[Dependency]
	private GamePrefs m_prefs;

	private FloatProperty m_fov = new FloatProperty();

	private FloatProperty m_height = new FloatProperty();

	private FloatProperty m_tilt = new FloatProperty();

	private StringProperty m_fovUi = new StringProperty();

	private StringProperty m_heightUi = new StringProperty();

	private StringProperty m_tiltUi = new StringProperty();

	private string m_cameraFovTextLocalized;

	private string m_cameraHeightTextLocalized;

	private string m_cameraTiltTextLocalized;

	private BoolProperty m_cameraControl = new BoolProperty();

	private BoolProperty m_cameraControlPanel = new BoolProperty(startValue: true);

	private StringProperty m_targetName = new StringProperty();

	private IntProperty m_logo = new IntProperty();

	private BoolProperty m_isCapture = new BoolProperty();

	private BoolProperty m_isHideUI = new BoolProperty();

	private BoolProperty m_isPS4 = new BoolProperty();

	private BoolProperty m_isXbox = new BoolProperty();

	private StringProperty m_selectedCameraName = new StringProperty();

	private BoolProperty m_canScreenshotUI = new BoolProperty();

	private SwitchListSettingsItem m_cameraMode;

	private TrackListSettingsItem m_speedCameraGate;

	private TrackListSettingsItem m_chromaticAberration;

	private TrackListSettingsItem m_cameraFocus;

	private TrackListSettingsItem m_exposition;

	private TrackListSettingsItem m_aperture;

	private TrackListSettingsItem m_сontrast;

	private TrackListSettingsItem m_saturation;

	private TrackListSettingsItem m_brightness;

	private TrackListSettingsItem m_vignetting;

	private TrackListSettingsItem m_temperature;

	private SwitchListSettingsItem m_pilot;

	private HeaderListSettingsItem m_localSettings;

	[SerializeField]
	private Volume m_volume;

	private CameraPhotoSession m_cameraPhotoSession;

	private CinemachineVirtualCamera m_virtualCamera;

	private Camera m_camera;

	private CameraInputController m_inputController;

	private bool m_isUiActive;

	public Canvas logoOverlayCanvasReference;

	private Canvas m_logoOverlayCanvasInstance;

	private Graphic[] m_logos;

	private Graphic m_logoCurrent;

	[SerializeField]
	private UIPhotoModeSettingsList m_photoModeSettingsList;

	private LightVariantController m_lvc;

	private HDProbe m_reflectionProbe;

	private bool m_updateToRealtimeMode;

	private Coroutine m_reflectionModeChangeCoroutine;

	private Vector3 m_probeAttachLastPos;

	private string[] m_cameraModeValues;

	private string[] m_pilotValues;

	private Dictionary<GameObject, int> m_carPartsDefaultRenderLayers = new Dictionary<GameObject, int>();

	private float m_tiltDefault;

	private float m_heightDefault;

	private float m_fovDefault;

	private float m_lastTimePressedHints;

	private float m_inputAccelerateScale = 1f;

	private List<ActionHintData> m_actionHintList = new List<ActionHintData>();

	private void Awake()
	{
		m_fov.OnValueChanged += FovOnChanged;
		m_height.OnValueChanged += HeightOnChanged;
		m_tilt.OnValueChanged += TiltOnChanged;
		m_isCapture.OnValueChanged += OnCaptureStateChanged;
		m_cameraControl.OnValueChanged += OnChangeHint;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		SetBlurPassActive(value: true);
		m_logo.value = -1;
		CameraSwitch.instance.ActivatePhotoSessionCamera();
		if (m_cameraPhotoSession == null)
		{
			m_cameraPhotoSession = UnityEngine.Object.FindObjectOfType<CameraPhotoSession>();
		}
		if (m_cameraPhotoSession != null)
		{
			m_cameraPhotoSession.ChangeTarget(CameraPhotoSession.PhotoSessionCameraTarget.Center);
			SetTargetName(CameraPhotoSession.PhotoSessionCameraTarget.Center);
			ApplyPhotoModCorrectLayerToCar(m_cameraPhotoSession.targetRaceCar);
			CameraSwitch.instance.onSwitchCar += ChangeCarTarget;
		}
		if (m_camera == null)
		{
			m_camera = Camera.main;
		}
		if (m_virtualCamera == null)
		{
			m_virtualCamera = m_cameraPhotoSession.virtualCamera;
		}
		m_fovDefault = m_virtualCamera.m_Lens.FieldOfView;
		m_logoOverlayCanvasInstance = UnityEngine.Object.Instantiate(logoOverlayCanvasReference);
		m_logos = m_logoOverlayCanvasInstance.GetComponentsInChildren<Graphic>(includeInactive: true);
		m_logo.OnValueChanged += LogoPosOnChanged;
		if (m_inputController == null)
		{
			m_inputController = new CameraInputController();
		}
		TryAttachUpdateReflectionProbe(needAttachUpdate: true);
		ShowTopPanel(ETopPanelState.Title, "");
		HideTopPanel();
		ResetCamera();
		ShowHints();
		Overlay.instance.OnPhotoMode(isEnter: true);
		switch (Overlay.instance.GetPlatform())
		{
		case UserPlatform.Id.Unknown:
		case UserPlatform.Id.Steam:
			m_canScreenshotUI.value = true;
			break;
		case UserPlatform.Id.PS4:
			m_isPS4.value = true;
			break;
		case UserPlatform.Id.Xbox:
			m_isXbox.value = true;
			break;
		}
		m_isCapture.value = false;
		BuildPhotoSettingsList();
		OnChangeControlPanel();
	}

	protected override void OnDeactivate()
	{
		m_logo.OnValueChanged -= LogoPosOnChanged;
		HideBottomPanel();
		HideTopPanel();
		ResetCamera();
		Overlay.instance.OnPhotoMode(isEnter: false);
		TryAttachUpdateReflectionProbe(needAttachUpdate: false);
		UnityEngine.Object.Destroy(m_logoOverlayCanvasInstance.gameObject);
		RaceCar targetRaceCar = m_cameraPhotoSession.targetRaceCar;
		if (!BaseCar.IsNull(targetRaceCar))
		{
			targetRaceCar.UpdateDriverVisibility();
			RestoreRaceCarPartRenderLayers();
		}
		CameraSwitch.instance.onSwitchCar -= ChangeCarTarget;
		SetBlurPassActive(value: false);
		base.OnDeactivate();
	}

	private void LogoPosOnChanged()
	{
		for (int i = 0; i < m_logos.Length; i++)
		{
			if (i == m_logo.value)
			{
				m_logoCurrent = m_logos[i];
				m_logoCurrent.gameObject.SetActive(value: true);
			}
			else
			{
				m_logos[i].gameObject.SetActive(value: false);
			}
		}
	}

	private void SetBlurPassActive(bool value)
	{
		List<CustomPassVolume> list = new List<CustomPassVolume>();
		CustomPassVolume.GetActivePassVolumes(CustomPassInjectionPoint.BeforePostProcess, list);
		foreach (CustomPassVolume item in list)
		{
			CustomPass customPass = item.customPasses.Find((CustomPass customPass2) => customPass2.name == "PhotoModBlurPass");
			if (customPass != null)
			{
				customPass.enabled = value;
				break;
			}
		}
	}

	private void ChangeCarTarget(RaceCar raceCar)
	{
		ApplyPhotoModCorrectLayerToCar(raceCar);
		OnChangePilot();
		OnChangeSpeedGate();
	}

	private void ApplyPhotoModCorrectLayerToCar(RaceCar car)
	{
		ChangeRaceCarLayer(car, 29);
		foreach (KeyValuePair<string, int> carPartsPhotoModeRenderLayer in m_carPartsPhotoModeRenderLayers)
		{
			SetLayerToCarChildrenObjects(car, carPartsPhotoModeRenderLayer.Key, carPartsPhotoModeRenderLayer.Value);
		}
	}

	private void RestoreRaceCarPartRenderLayers()
	{
		foreach (KeyValuePair<GameObject, int> carPartsDefaultRenderLayer in m_carPartsDefaultRenderLayers)
		{
			carPartsDefaultRenderLayer.Key.layer = carPartsDefaultRenderLayer.Value;
		}
		m_carPartsDefaultRenderLayers.Clear();
	}

	private void SetLayerToCarChildrenObjects(RaceCar raceCar, string childRootName, LayerMask layerMask)
	{
		if (BaseCar.IsNull(raceCar))
		{
			return;
		}
		Transform[] array = raceCar.getTransform.FindAllComponent((Transform findTrans) => findTrans.gameObject.name.Contains(childRootName));
		if (array == null)
		{
			return;
		}
		Transform[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			Transform[] componentsInChildren = array2[num].GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				if (!m_carPartsDefaultRenderLayers.ContainsKey(transform.gameObject))
				{
					m_carPartsDefaultRenderLayers[transform.gameObject] = transform.gameObject.layer;
				}
				transform.gameObject.layer = layerMask;
			}
		}
	}

	private void ChangeRaceCarLayer(RaceCar raceCar, LayerMask layerMask)
	{
		if (BaseCar.IsNull(raceCar))
		{
			return;
		}
		Transform[] componentsInChildren = raceCar.gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			string text = gameObject.name;
			int num = text.IndexOf("(Clone", StringComparison.Ordinal);
			if (num != -1)
			{
				text = text.Substring(0, num);
			}
			if (!m_ignoreCarPartsLayerChanges.Contains(text))
			{
				if (!m_carPartsDefaultRenderLayers.ContainsKey(gameObject))
				{
					m_carPartsDefaultRenderLayers[gameObject] = gameObject.layer;
				}
				gameObject.gameObject.layer = layerMask;
			}
		}
	}

	private void OnChangeHint()
	{
		HideBottomPanel();
		ShowHints();
	}

	private void ShowHints()
	{
		m_actionHintList.Clear();
		m_actionHintList.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
		m_actionHintList.Add(new ActionHintData
		{
			hint = LocalizationKeys.reset.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = ResetCameraAndSetting
		});
		if (!m_cameraControl.value)
		{
			m_actionHintList.Add(new ActionHintData
			{
				hint = LocalizationKeys.camera_additional_settings.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = ToggleAdditionalSettingHint
			});
		}
		ShowBottomPanel(m_actionHintList);
	}

	private void HideUI()
	{
		m_isHideUI.value = !m_isHideUI.value;
		if (m_isHideUI.value)
		{
			HideBottomPanel();
		}
		else
		{
			ShowHints();
		}
		SetFpsCounterActive(!m_isHideUI.value);
	}

	private void SetFpsCounterActive(bool active)
	{
		if (G_Singleton<GraphyManager>.Instance == null)
		{
			GameLog.LogWarning("GraphyManager is not exist!");
		}
		else
		{
			G_Singleton<GraphyManager>.Instance.gameObject.SetActive(active);
		}
	}

	private void BuildPhotoSettingsList()
	{
		if (m_pilotValues == null)
		{
			m_pilotValues = new string[2]
			{
				LocalizationKeys.control_off.Localized(),
				LocalizationKeys.control_on.Localized()
			};
		}
		if (m_cameraModeValues == null)
		{
			m_cameraModeValues = new string[3]
			{
				LocalizationKeys.camera_point_unfocus.Localized(),
				LocalizationKeys.camera_point_center.Localized(),
				LocalizationKeys.camera_point_back.Localized()
			};
		}
		m_cameraFovTextLocalized = LocalizationKeys.camera_fov.Localized();
		m_cameraHeightTextLocalized = LocalizationKeys.camera_height.Localized();
		m_cameraTiltTextLocalized = LocalizationKeys.camera_tilt.Localized();
		m_selectedCameraName.value = LocalizationKeys.cockpit_camera.Localized();
		if (m_localSettings == null)
		{
			m_localSettings = new HeaderListSettingsItem();
			m_photoModeSettingsList.AddItem(m_localSettings);
		}
		m_localSettings.title = LocalizationKeys.local_camera_settings.Localized();
		InitListSettingsItem(ref m_cameraMode, LocalizationKeys.camera_mode.Localized(), m_cameraModeValues, OnChangeCameraMod);
		InitListSettingsItem(ref m_speedCameraGate, LocalizationKeys.view_camera_gate.Localized(), 0f, 100f, 1f, OnChangeSpeedGate);
		InitListSettingsItem(ref m_cameraFocus, LocalizationKeys.view_camera_focus.Localized(), 0f, 100f, 1f, OnChangeCameraFocus);
		InitListSettingsItem(ref m_exposition, LocalizationKeys.view_camera_exposition.Localized(), -1f, 1f, 0.01f, OnChangeExposition);
		InitListSettingsItem(ref m_aperture, LocalizationKeys.view_camera_aperture.Localized(), 0f, 25f, 0.25f, OnChangeAperture);
		InitListSettingsItem(ref m_chromaticAberration, LocalizationKeys.view_camera_chromatic_aberration.Localized(), 0f, 1f, 0.01f, OnChangeChromaticAberration);
		InitListSettingsItem(ref m_сontrast, LocalizationKeys.view_camera_сontrast.Localized(), -75f, 75f, 1f, OnChangeContrast);
		InitListSettingsItem(ref m_saturation, LocalizationKeys.view_camera_saturation.Localized(), -75f, 75f, 1f, OnChangeSaturation);
		InitListSettingsItem(ref m_brightness, LocalizationKeys.view_camera_brightness.Localized(), -0.75f, 0.75f, 0.01f, OnChangeBrightness);
		InitListSettingsItem(ref m_vignetting, LocalizationKeys.view_camera_vignetting.Localized(), 0f, 0.5f, 0.005f, OnChangeVignetting);
		InitListSettingsItem(ref m_temperature, LocalizationKeys.view_camera_temperature.Localized(), -75f, 75f, 1f, OnChangeTemperature);
		InitListSettingsItem(ref m_pilot, LocalizationKeys.view_camera_pilot.Localized(), m_pilotValues, OnChangePilot);
		SetByPhotoCamParameters(m_prefs.globals.photoCamParams);
		OnChangePilot();
		OnChangeCameraMod();
		m_photoModeSettingsList.RecalcList();
		m_photoModeSettingsList.Reselect(m_photoModeSettingsList.GetItem(1));
	}

	private void OnChangePilot()
	{
		RaceCar targetRaceCar = m_cameraPhotoSession.targetRaceCar;
		if (!BaseCar.IsNull(targetRaceCar))
		{
			targetRaceCar.SetDriverSystemVisibility(steeringWheelVisible: true, m_pilot.index != 0);
		}
	}

	private void OnChangeTemperature()
	{
		if (m_volume.profile.TryGet<WhiteBalance>(out var component))
		{
			component.temperature.value = m_temperature.value;
		}
	}

	private void OnChangeVignetting()
	{
		if (m_volume.profile.TryGet<Vignette>(out var component))
		{
			component.intensity.value = m_vignetting.value;
		}
	}

	private void OnChangeBrightness()
	{
		if (m_volume.profile.TryGet<LiftGammaGain>(out var component))
		{
			component.gain.value = new Vector4(m_brightness.value, m_brightness.value, m_brightness.value, m_brightness.value);
		}
	}

	private void OnChangeSaturation()
	{
		if (m_volume.profile.TryGet<ColorAdjustments>(out var component))
		{
			component.saturation.value = m_saturation.value;
		}
	}

	private void OnChangeContrast()
	{
		if (m_volume.profile.TryGet<ColorAdjustments>(out var component))
		{
			component.contrast.value = m_сontrast.value;
		}
	}

	private void OnChangeChromaticAberration()
	{
		if (m_volume.profile.TryGet<ChromaticAberration>(out var component))
		{
			component.intensity.value = m_chromaticAberration.value;
		}
	}

	private void OnChangeAperture()
	{
		if (m_volume.profile.TryGet<DepthOfField>(out var component))
		{
			component.nearMaxBlur = m_aperture.value;
			component.farMaxBlur = m_aperture.value;
			component.Override(component, 1f);
		}
	}

	private void OnChangeExposition()
	{
		if (m_volume.profile.TryGet<ColorAdjustments>(out var component))
		{
			component.postExposure.value = m_exposition.value;
		}
	}

	private void OnChangeCameraFocus()
	{
		if (m_volume.profile.TryGet<DepthOfField>(out var component))
		{
			component.farFocusStart.value = m_cameraFocus.value / 5f;
			component.farFocusEnd.value = m_cameraFocus.value;
			component.nearFocusStart.value = 1f;
			component.nearFocusEnd.value = 0f;
		}
	}

	private void OnChangeSpeedGate()
	{
		RaceCar targetRaceCar = m_cameraPhotoSession.targetRaceCar;
		if (!BaseCar.IsNull(targetRaceCar) && m_volume.profile.TryGet<DirectionBlurComponent>(out var component))
		{
			component.carPos.value = targetRaceCar.getTransform.position;
			component.carForward.value = targetRaceCar.getTransform.forward;
			component.direction.value = targetRaceCar.carX.velocity;
			component.radius.value = math.lerp(0f, 1f, m_speedCameraGate.value / m_speedCameraGate.max);
		}
	}

	private void OnChangeCameraMod()
	{
		if (m_volume.profile.TryGet<DirectionBlurComponent>(out var component))
		{
			component.layerMaskFlash.value = 536870912;
			switch (m_cameraMode.index)
			{
			case 0:
				component.layerMask.value = 134217728;
				component.invertMask.value = false;
				break;
			case 1:
				component.layerMask.value = 671088640;
				component.invertMask.value = false;
				break;
			case 2:
				component.layerMask.value = 536870912;
				component.invertMask.value = true;
				break;
			default:
				component.layerMask.value = 134217728;
				component.invertMask.value = false;
				break;
			}
		}
	}

	private void InitListSettingsItem(ref TrackListSettingsItem item, string title, float min = 0f, float max = 100f, float step = 1f, System.Action onChangedHandler = null)
	{
		if (item == null)
		{
			item = new TrackListSettingsItem(min, max, step, "{0:F" + $"{math.ceil(math.log10(1f / step))}" + "}");
			m_photoModeSettingsList.AddItem(item);
		}
		item.title = title;
		item.onChangedValue = onChangedHandler;
	}

	private void InitListSettingsItem(ref SwitchListSettingsItem item, string title, string[] values, System.Action onChangedHandler = null)
	{
		if (item == null)
		{
			item = new SwitchListSettingsItem();
			m_photoModeSettingsList.AddItem(item);
		}
		item.title = title;
		item.list.Clear();
		item.list.AddRange(values);
		item.index = 0;
		item.onChangedValue = onChangedHandler;
	}

	private LightVariantController InitLightVariantControllerIfNeed()
	{
		if (m_lvc == null)
		{
			DayNightController instance = DayNightController.instance;
			if ((bool)instance)
			{
				m_lvc = instance.GetComponent<LightVariantController>();
			}
		}
		return m_lvc;
	}

	private void TryAttachUpdateReflectionProbe(bool needAttachUpdate)
	{
		if (InitLightVariantControllerIfNeed() == null)
		{
			return;
		}
		if (m_reflectionProbe == null)
		{
			m_reflectionProbe = m_lvc.reflectionProbe;
		}
		m_updateToRealtimeMode = needAttachUpdate;
		if (needAttachUpdate)
		{
			if (m_reflectionProbe != null)
			{
				m_probeAttachLastPos = m_reflectionProbe.transform.position;
			}
			LightVariantController lvc = m_lvc;
			lvc.onProbeUpdated = (System.Action)Delegate.Combine(lvc.onProbeUpdated, new System.Action(UpdateReflectionProbeMode));
		}
		else
		{
			if (m_reflectionProbe != null)
			{
				m_reflectionProbe.transform.position = m_probeAttachLastPos;
			}
			LightVariantController lvc2 = m_lvc;
			lvc2.onProbeUpdated = (System.Action)Delegate.Remove(lvc2.onProbeUpdated, new System.Action(UpdateReflectionProbeMode));
		}
		UpdateReflectionProbeMode();
	}

	private void UpdateReflectionProbeMode()
	{
		TryStopReflectionProbeUpdateCoroutine();
		m_reflectionModeChangeCoroutine = TimeController.StartNow(ChangeReflectionProbeMode());
	}

	private void TryStopReflectionProbeUpdateCoroutine()
	{
		if (m_reflectionModeChangeCoroutine != null)
		{
			TimeController.StopNow(m_reflectionModeChangeCoroutine);
			m_reflectionModeChangeCoroutine = null;
		}
	}

	private IEnumerator ChangeReflectionProbeMode()
	{
		if (m_reflectionProbe == null)
		{
			if (InitLightVariantControllerIfNeed() == null)
			{
				TryStopReflectionProbeUpdateCoroutine();
				yield break;
			}
			m_reflectionProbe = m_lvc.reflectionProbe;
		}
		if (m_reflectionProbe == null)
		{
			TryStopReflectionProbeUpdateCoroutine();
			yield break;
		}
		if (m_updateToRealtimeMode)
		{
			if (!CameraSwitch.instance)
			{
				TryStopReflectionProbeUpdateCoroutine();
				yield break;
			}
			RaceCar car = CameraSwitch.instance.GetCar();
			if (!car)
			{
				TryStopReflectionProbeUpdateCoroutine();
				yield break;
			}
			float y = car.GetDesc().carXDesc.weight.bounds.center.y;
			m_reflectionProbe = m_lvc.reflectionProbe;
			m_reflectionProbe.transform.position = car.transform.position + new Vector3(0f, y, 0f);
			if (m_reflectionProbe.mode != ProbeSettings.Mode.Realtime)
			{
				m_reflectionProbe.mode = ProbeSettings.Mode.Realtime;
			}
			if (m_reflectionProbe.realtimeMode != ProbeSettings.RealtimeMode.OnDemand)
			{
				m_reflectionProbe.realtimeMode = ProbeSettings.RealtimeMode.OnDemand;
			}
			m_reflectionProbe.RequestRenderNextUpdate();
		}
		else if (m_reflectionProbe.mode != ProbeSettings.Mode.Custom)
		{
			m_reflectionProbe.mode = ProbeSettings.Mode.Custom;
		}
		TryStopReflectionProbeUpdateCoroutine();
	}

	public void SwitchPositionLogo()
	{
		if (m_cameraControlPanel.value && !m_isCapture.value)
		{
			m_logo.value++;
			if (m_logo.value > 3)
			{
				m_logo.value = 0;
			}
		}
	}

	public void MakeShot()
	{
		if (m_cameraControlPanel.value && !m_isCapture.value)
		{
			ShareCaptureAsync().Forget();
		}
	}

	private void SaveModeParameters()
	{
		m_prefs.globals.photoCamParams = GetByPhotoCamParameters();
		m_prefs.SaveAll();
	}

	private void GoToBack()
	{
		if (!m_isCapture.value)
		{
			if (m_cameraControl.value)
			{
				ToggleAdditionalSettingHint();
				return;
			}
			Hide();
			SaveModeParameters();
			ResetCamera();
		}
	}

	public void NavNextPage()
	{
		if (!m_isCapture.value)
		{
			SetTargetName(m_cameraPhotoSession.NextTarget());
		}
	}

	public void NavPrevPage()
	{
		if (!m_isCapture.value)
		{
			SetTargetName(m_cameraPhotoSession.PrevTarget());
		}
	}

	private void ToggleAdditionalSettingHint()
	{
		m_cameraControl.value = !m_cameraControl.value;
		m_cameraControlPanel.value = !m_cameraControl.value;
	}

	private void ResetCameraAndSetting()
	{
		if (!m_isCapture.value)
		{
			if (m_cameraControlPanel.value)
			{
				ResetCamera();
			}
			else if (m_cameraControl.value)
			{
				SetByPhotoCamParameters(new PhotoCamParameters());
			}
		}
	}

	private void ResetCamera()
	{
		m_cameraPhotoSession.ChangeTarget(CameraPhotoSession.PhotoSessionCameraTarget.Center);
		SetTargetName(CameraPhotoSession.PhotoSessionCameraTarget.Center);
		m_logo.value = 0;
		TweenCameraParam(m_height, m_heightDefault, -0.5f, 1f);
		TweenCameraParam(m_tilt, m_tiltDefault, -30f, 30f);
		TweenCameraParam(m_fov, m_fovDefault, 20f, 70f);
		OnChangeControlPanel();
	}

	private void SetByPhotoCamParameters(PhotoCamParameters photoCamParameters)
	{
		m_cameraMode.index = photoCamParameters.cameraMode;
		m_speedCameraGate.SetTrackValue(photoCamParameters.speedCameraGate);
		m_cameraFocus.SetTrackValue(photoCamParameters.cameraFocus);
		m_exposition.SetTrackValue(photoCamParameters.exposition);
		m_aperture.SetTrackValue(photoCamParameters.aperture);
		m_chromaticAberration.SetTrackValue(photoCamParameters.chromaticAberration);
		m_сontrast.SetTrackValue(photoCamParameters.contrast);
		m_saturation.SetTrackValue(photoCamParameters.saturation);
		m_brightness.SetTrackValue(photoCamParameters.brightness);
		m_vignetting.SetTrackValue(photoCamParameters.vignetting);
		m_temperature.SetTrackValue(photoCamParameters.temperature);
		m_pilot.index = photoCamParameters.pilot;
		m_photoModeSettingsList.RecalcList();
		OnChangePilot();
	}

	private PhotoCamParameters GetByPhotoCamParameters()
	{
		return new PhotoCamParameters
		{
			cameraMode = m_cameraMode.index,
			speedCameraGate = m_speedCameraGate.normalizedValue,
			cameraFocus = m_cameraFocus.normalizedValue,
			exposition = m_exposition.normalizedValue,
			aperture = m_aperture.normalizedValue,
			chromaticAberration = m_chromaticAberration.normalizedValue,
			contrast = m_сontrast.normalizedValue,
			saturation = m_saturation.normalizedValue,
			brightness = m_brightness.normalizedValue,
			vignetting = m_vignetting.normalizedValue,
			temperature = m_temperature.normalizedValue,
			pilot = m_pilot.index
		};
	}

	private void SetTargetName(CameraPhotoSession.PhotoSessionCameraTarget target)
	{
		StringProperty targetName = m_targetName;
		targetName.value = target switch
		{
			CameraPhotoSession.PhotoSessionCameraTarget.Center => LocalizationKeys.photo_session_cam_target_center_caps.Localized(), 
			CameraPhotoSession.PhotoSessionCameraTarget.FrontLeft => LocalizationKeys.photo_session_cam_target_fl_caps.Localized(), 
			CameraPhotoSession.PhotoSessionCameraTarget.FrontRight => LocalizationKeys.photo_session_cam_target_fr_caps.Localized(), 
			CameraPhotoSession.PhotoSessionCameraTarget.RearLeft => LocalizationKeys.photo_session_cam_target_rl_caps.Localized(), 
			CameraPhotoSession.PhotoSessionCameraTarget.RearRight => LocalizationKeys.photo_session_cam_target_rr_caps.Localized(), 
			_ => m_targetName.value, 
		};
	}

	public void CameraTiltDec()
	{
		ProceedCameraInputDeltaByHint(m_tilt, -0.1f, -30f, 30f);
	}

	public void CameraTiltInc()
	{
		ProceedCameraInputDeltaByHint(m_tilt, 0.1f, -30f, 30f);
	}

	public void CameraHeightDec()
	{
		ProceedCameraInputDeltaByHint(m_height, -0.01f, -0.5f, 1f);
	}

	public void CameraHeightInc()
	{
		ProceedCameraInputDeltaByHint(m_height, 0.01f, -0.5f, 1f);
	}

	public void CameraFovInc()
	{
		ProceedCameraInputDeltaByHint(m_fov, 0.1f, 20f, 70f);
	}

	public void CameraFovDec()
	{
		ProceedCameraInputDeltaByHint(m_fov, -0.1f, 20f, 70f);
	}

	private void ProceedCameraInputDeltaByHint(FloatProperty param, float delta, float min, float max)
	{
		AccelerateInputScaleValue();
		TweenCameraParam(param, param.value + delta * m_inputAccelerateScale, min, max);
		m_lastTimePressedHints = Time.realtimeSinceStartup;
	}

	private void AccelerateInputScaleValue()
	{
		if (Time.realtimeSinceStartup - m_lastTimePressedHints < 0.2f)
		{
			m_inputAccelerateScale *= 1.3f;
		}
		else
		{
			m_inputAccelerateScale = 1f;
		}
		m_inputAccelerateScale = math.clamp(m_inputAccelerateScale, 1f, 40f);
	}

	public void TweenCameraParam(FloatProperty currentValue, float newValue, float min, float max)
	{
		DOTween.To(delegate(float value)
		{
			currentValue.value = value;
		}, currentValue.value, math.clamp(newValue, min, max), 0.1f).SetUpdate(UpdateType.Normal, isIndependentUpdate: true).Play()
			.OnComplete(OnChangeControlPanel);
	}

	private void OnChangeControlPanel()
	{
		m_tiltUi.value = $"{m_cameraTiltTextLocalized} {0f - m_tilt.value:0.0}";
		m_heightUi.value = $"{m_cameraHeightTextLocalized} {m_height.value:0.00}";
		m_fovUi.value = $"{m_cameraFovTextLocalized} {m_fov.value:0.0}";
	}

	private void LateUpdate()
	{
		float deltaXRotation = 0f;
		float deltaYRotation = 0f;
		float deltaDistance = 0f;
		if (InputManager.instance.IsActionPressed(ActionType.HideUI))
		{
			HideUI();
		}
		if (UIUtils.IsPointerOverGameObject() && Input.GetMouseButtonDown(0))
		{
			m_isUiActive = true;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			m_isUiActive = false;
		}
		if (!m_isUiActive && m_inputController != null)
		{
			m_inputController.CalcStandalone(Time.unscaledDeltaTime, ref deltaXRotation, ref deltaYRotation, ref deltaDistance);
		}
		if (m_cameraPhotoSession != null)
		{
			m_cameraPhotoSession.UpdateCameraPositionPhotoMode(deltaXRotation, deltaYRotation, deltaDistance);
		}
	}

	private async UniTask ShareCaptureAsync()
	{
		await UniTask.SwitchToMainThread();
		m_isCapture.value = true;
		m_canScreenshotUI.value = false;
		HideUI();
		await UniTask.DelayFrame(1);
		Texture2D screenshot = await CaptureScreen();
		await UniTask.SwitchToMainThread();
		await Overlay.instance.ShareScreenshot(() => screenshot);
		if ((bool)screenshot)
		{
			UnityEngine.Object.Destroy(screenshot);
		}
		HideUI();
		m_canScreenshotUI.value = true;
		m_isCapture.value = false;
	}

	private void OnCaptureStateChanged()
	{
		if (base.owner != null)
		{
			UIBottomPanelContext uIBottomPanelContext = base.owner.Get<UIBottomPanelContext>();
			if ((bool)uIBottomPanelContext)
			{
				uIBottomPanelContext.gameObject.SetActive(!m_isCapture.value);
			}
		}
	}

	private async UniTask<Texture2D> CaptureScreen()
	{
		await UniTask.SwitchToMainThread();
		Texture2D texture2D = ScreenCapture.CaptureScreenshotAsTexture(2);
		bool flag = texture2D.width % 4 == 0;
		bool flag2 = texture2D.height % 4 == 0;
		if (flag && flag2)
		{
			texture2D.Compress(highQuality: true);
		}
		return texture2D;
	}

	private void FovOnChanged()
	{
		m_cameraPhotoSession.SetFOV(m_fov.value);
	}

	private void HeightOnChanged()
	{
		m_cameraPhotoSession.SetHeight(m_height.value);
	}

	private void TiltOnChanged()
	{
		m_cameraPhotoSession.SetTilt(m_tilt.value);
	}

	[GameHotkey(ActionType.NavUp, false, false)]
	public void NavUp()
	{
		if (m_cameraControl.value)
		{
			if (m_photoModeSettingsList.GetCount() > 0 && m_photoModeSettingsList.selectedIndex > 1)
			{
				m_photoModeSettingsList.selectedIndex--;
			}
			m_photoModeSettingsList.UpdateList();
		}
	}

	[GameHotkey(ActionType.NavDown, false, false)]
	public void NavDown()
	{
		if (m_cameraControl.value)
		{
			if (m_photoModeSettingsList.GetCount() > 0 && m_photoModeSettingsList.selectedIndex + 1 < m_photoModeSettingsList.GetCount())
			{
				m_photoModeSettingsList.selectedIndex++;
			}
			m_photoModeSettingsList.UpdateList();
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavLeft()
	{
		if (m_cameraControl.value)
		{
			BaseSettingsItemController selectedController = m_photoModeSettingsList.GetSelectedController();
			if (!(selectedController == null))
			{
				selectedController.item.PrevValue();
				selectedController.UpdateValue();
			}
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavRight()
	{
		if (m_cameraControl.value)
		{
			BaseSettingsItemController selectedController = m_photoModeSettingsList.GetSelectedController();
			if (!(selectedController == null))
			{
				selectedController.item.NextValue();
				selectedController.UpdateValue();
			}
		}
	}
}
