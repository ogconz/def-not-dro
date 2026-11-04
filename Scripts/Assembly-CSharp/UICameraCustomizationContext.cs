using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Binding;
using Binding.Base;
using CarX;
using DI;
using GameInput;
using PrefsModels;
using UnityEngine;
using UnityEngine.UI;

[AudioFXBlocker(true)]
public class UICameraCustomizationContext : BaseBindingBehaviourTarget
{
	private enum NavigationMode
	{
		CamGeneralSettingsList = 0,
		DynamCamSettingsList = 1,
		Count = 2
	}

	[Dependency]
	private GamePrefs m_prefs;

	private PrefsCarSlotsModel m_carSettings;

	private NavigationMode m_navMode;

	[Header("Cockpit camera's general settings")]
	[SerializeField]
	private ScrollRect m_camGeneralSettingsScrollRect;

	[SerializeField]
	private BaseSettingsItemController m_prefabItem;

	[Header("Dynamic cockpit camera's settings")]
	[SerializeField]
	private ScrollRect m_dynamicCamSettingsScrollRect;

	[SerializeField]
	private BaseSettingsItemController m_prefabDynamicCamItem;

	private List<BaseSettingsItemController>[] m_listItemUI = new List<BaseSettingsItemController>[2]
	{
		new List<BaseSettingsItemController>(),
		new List<BaseSettingsItemController>()
	};

	private List<BaseListSettingsItem>[] m_listItemData = new List<BaseListSettingsItem>[2]
	{
		new List<BaseListSettingsItem>(),
		new List<BaseListSettingsItem>()
	};

	private BaseSettingsItemController m_currentItemUI = new BaseSettingsItemController();

	private BaseSettingsItemController[] m_cachedSelectedItemUI = new BaseSettingsItemController[2];

	private int m_idCar;

	private BaseCamera m_currentCamera;

	private CarCameraSettings m_previewSettings;

	private CarCameraSettings m_currentSettings;

	private DynamicCamParameters m_previewDynamicCamSettings;

	private DynamicCamParameters m_currentDynamicCamSettings;

	private DynamicCamParameters m_initialDynamicCamSettings;

	private bool m_storedWindowsToggle;

	private bool m_storedDriverToggle;

	private bool m_storedWheelToggle;

	private TrackListSettingsItem m_height;

	private TrackListSettingsItem m_offsetX;

	private TrackListSettingsItem m_offsetZ;

	private TrackListSettingsItem m_pitchAngle;

	private TrackListSettingsItem m_fov;

	private SwitchListSettingsItem m_dynamicCamCockpit;

	private ToggleListSettingsItem m_hideDriver;

	private ToggleListSettingsItem m_hideWheel;

	private ToggleListSettingsItem m_hideWindows;

	private HeaderListSettingsItem m_localSettings;

	private HeaderListSettingsItem m_globalSettings;

	private TrackListSettingsItem m_cockpitCamSmooth;

	private TrackListSettingsItem m_cockpitCamVelScale;

	private TrackListSettingsItem m_cockpitCamMaxRotSpeed;

	private TrackListSettingsItem m_cockpitCamMaxRotAngle;

	private TrackListSettingsItem m_cockpitCamDeadZone;

	private SwitchListSettingsItem m_cockpitCamAimMode;

	private ToggleListSettingsItem m_cockpitCamAccelMovingToggle;

	private TrackListSettingsItem m_cockpitCamAccelMovingSmooth;

	private CameraSwitch m_cameraSwitch;

	private CARXRearCamera m_rearCam;

	private CARXCockpitCamera m_cockpitCam;

	private bool m_setDefault = true;

	private bool m_changed;

	private bool m_valuesStored;

	private bool m_hideWheelPrevValue;

	private bool m_isXRActive;

	public readonly Property<int> camerasCount = new IntProperty(1);

	public readonly Property<string> selectedCameraName = new StringProperty();

	public readonly BoolProperty m_dynamicCamSettingsToggle = new BoolProperty(false);

	private CarCameraSwitchHandler m_pilot;

	private CameraPartMarker[] m_cameraPlaceObjects;

	private GlobalModel m_global => m_prefs.globals;

	protected override void OnActivate()
	{
		m_prefabItem.gameObject.SetActive(value: false);
		m_prefabDynamicCamItem.gameObject.SetActive(value: false);
		m_isXRActive = XRModeManager.IsXRActive();
		base.OnActivate();
		BringForwardByDepth();
		if (m_pilot == null)
		{
			m_pilot = PlayerCarControl.instance.car.driverCamSwitchHandler;
			if (m_pilot != null)
			{
				m_cameraPlaceObjects = m_pilot.cameraPlaceObjects;
			}
		}
		m_carSettings = m_prefs.carSettings;
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.customization_cameras.Localized());
		m_idCar = m_global.lastCarId;
		if (m_cameraSwitch == null)
		{
			m_cameraSwitch = CameraSwitch.instance;
		}
		if (!(m_cameraSwitch.FindActiveCamera() is CARXCockpitCamera))
		{
			m_cameraSwitch.ActivateCamera(CameraSwitch.ECameraType.Race, 4);
		}
		if (m_previewDynamicCamSettings == null)
		{
			m_previewDynamicCamSettings = FillDefaultDynamicCamSettings();
		}
		SwitchTo(m_cameraSwitch.FindActiveCamera());
		BuildDynamicCamSettingsList();
		SetFirstItemFocus(ref m_currentItemUI, m_listItemUI[(int)m_navMode], (m_navMode == NavigationMode.CamGeneralSettingsList) ? m_camGeneralSettingsScrollRect : m_dynamicCamSettingsScrollRect);
		UpdateBottomPanel();
		m_setDefault = false;
		m_dynamicCamSettingsToggle.value = m_dynamicCamCockpit.index > 1 && !m_isXRActive;
		SetDefaultValues();
	}

	protected override void OnDeactivate()
	{
		if (m_cockpitCam != null)
		{
			m_cockpitCam.updUp = true;
		}
		base.OnDeactivate();
	}

	private void Save()
	{
		m_carSettings.SetCameraSettings(m_previewSettings.indentifier, m_previewSettings);
		m_global.dynamicCamParams = m_previewDynamicCamSettings;
		m_global.Save();
	}

	private void SetDefaultValues()
	{
		if (m_valuesStored)
		{
			return;
		}
		NavigationMode[] array = (NavigationMode[])Enum.GetValues(typeof(NavigationMode));
		foreach (NavigationMode navigationMode in array)
		{
			if (navigationMode == NavigationMode.Count)
			{
				continue;
			}
			foreach (BaseListSettingsItem item in m_listItemData[(int)navigationMode])
			{
				item.StoreInitialValue();
			}
		}
		m_valuesStored = true;
	}

	public void SwitchCamera(BaseCamera camera)
	{
		if (!PublicInstancePropertiesEqual(m_previewSettings, m_currentSettings))
		{
			UIMessageBox.ShowComplexDialog(LocalizationKeys.customization_cameras_save_changes_text.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), delegate
			{
				Save();
				SwitchTo(camera);
			}, LocalizationKeys.Yes.Localized(), delegate
			{
				NavResetAll();
				Save();
				SwitchTo(camera);
			}, LocalizationKeys.No.Localized());
		}
		else
		{
			SwitchTo(camera);
		}
	}

	private void BuildSettingsList(BaseSettingsItemController templateItem, List<BaseSettingsItemController> conteinerItemUI, List<BaseListSettingsItem> containerItemData)
	{
		int elementsNeeded = containerItemData.Count;
		if (elementsNeeded <= 0)
		{
			return;
		}
		GOPool.Allocate(templateItem, ref conteinerItemUI, elementsNeeded, delegate(BaseSettingsItemController itemUI)
		{
			int index = conteinerItemUI.IndexOf(itemUI);
			itemUI.SetItem(containerItemData[index]);
			if (containerItemData[index] is HeaderListSettingsItem)
			{
				itemUI.GetComponent<ElementNavigate>().enabled = false;
			}
			itemUI.SetSelected(select: false);
			if (!itemUI.gameObject.activeSelf)
			{
				itemUI.gameObject.SetActive(value: true);
			}
		}, delegate(BaseSettingsItemController itemUI)
		{
			if (conteinerItemUI.IndexOf(itemUI) > elementsNeeded - 1 && itemUI.gameObject.activeSelf)
			{
				itemUI.gameObject.SetActive(value: false);
			}
		});
	}

	private void SwitchTo(BaseCamera camera)
	{
		m_currentCamera = camera;
		string text = m_idCar + camera.identifier;
		m_previewSettings = m_carSettings.GetCameraSettings(text);
		if (m_previewSettings == null)
		{
			m_previewSettings = FillDefaultSettings(text, camera);
		}
		m_changed = false;
		m_rearCam = null;
		if (m_cockpitCam != null)
		{
			m_cockpitCam.updUp = true;
		}
		m_cockpitCam = null;
		m_currentItemUI = null;
		if (!(camera is CARXRearCamera rearCam))
		{
			if (camera is CARXCockpitCamera cARXCockpitCamera)
			{
				selectedCameraName.value = LocalizationKeys.cockpit_camera.Localized();
				LimitsCamera cockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetCockpitLimits(cARXCockpitCamera.identifier);
				if (string.IsNullOrEmpty(cockpitLimits.identifier))
				{
					GameLog.Print(AlertLevel.Error, "System", "Limits for cockpitCam \"" + cARXCockpitCamera.identifier + "\" not found. Set defaults values.");
				}
				if (m_localSettings == null)
				{
					m_localSettings = new HeaderListSettingsItem();
					m_listItemData[0].Add(m_localSettings);
				}
				m_localSettings.title = LocalizationKeys.local_camera_settings.Localized();
				m_cockpitCam = cARXCockpitCamera;
				m_cockpitCam.updUp = false;
				if (m_height == null)
				{
					m_height = new TrackListSettingsItem(cockpitLimits.offsetMin.y, cockpitLimits.offsetMax.y, cockpitLimits.offsetStep, "{0:F2}");
					m_listItemData[0].Add(m_height);
				}
				m_height.title = LocalizationKeys.camera_customizer_offset_y.Localized();
				m_height.value = m_cockpitCam.offset.y;
				TrackListSettingsItem height = m_height;
				height.onDefaultValue = (System.Action)Delegate.Combine(height.onDefaultValue, new System.Action(OnDefaultHeight));
				TrackListSettingsItem height2 = m_height;
				height2.onChangedValue = (System.Action)Delegate.Combine(height2.onChangedValue, new System.Action(OnChangeHeight));
				TrackListSettingsItem height3 = m_height;
				height3.onClick = (System.Action)Delegate.Combine(height3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_height);
				});
				if (m_offsetX == null)
				{
					m_offsetX = new TrackListSettingsItem(cockpitLimits.offsetMin.x, cockpitLimits.offsetMax.x, cockpitLimits.offsetStep, "{0:F2}");
					m_listItemData[0].Add(m_offsetX);
				}
				m_offsetX.title = LocalizationKeys.camera_customizer_offset_x.Localized();
				m_offsetX.value = m_cockpitCam.offset.x;
				TrackListSettingsItem offsetX = m_offsetX;
				offsetX.onDefaultValue = (System.Action)Delegate.Combine(offsetX.onDefaultValue, new System.Action(OnDefaultOffSetX));
				TrackListSettingsItem offsetX2 = m_offsetX;
				offsetX2.onChangedValue = (System.Action)Delegate.Combine(offsetX2.onChangedValue, new System.Action(OnChangeXOffset));
				TrackListSettingsItem offsetX3 = m_offsetX;
				offsetX3.onClick = (System.Action)Delegate.Combine(offsetX3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_offsetX);
				});
				if (m_offsetZ == null)
				{
					m_offsetZ = new TrackListSettingsItem(cockpitLimits.offsetMin.z, cockpitLimits.offsetMax.z, cockpitLimits.offsetStep, "{0:F2}");
					m_listItemData[0].Add(m_offsetZ);
				}
				m_offsetZ.title = LocalizationKeys.camera_customizer_offset_z.Localized();
				m_offsetZ.value = m_cockpitCam.offset.z;
				TrackListSettingsItem offsetZ = m_offsetZ;
				offsetZ.onDefaultValue = (System.Action)Delegate.Combine(offsetZ.onDefaultValue, new System.Action(OnDefaultOffSetZ));
				TrackListSettingsItem offsetZ2 = m_offsetZ;
				offsetZ2.onChangedValue = (System.Action)Delegate.Combine(offsetZ2.onChangedValue, new System.Action(OnChangeZOffset));
				TrackListSettingsItem offsetZ3 = m_offsetZ;
				offsetZ3.onClick = (System.Action)Delegate.Combine(offsetZ3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_offsetZ);
				});
				if (m_pitchAngle == null)
				{
					m_pitchAngle = new TrackListSettingsItem(cockpitLimits.pitch.x, cockpitLimits.pitch.y, cockpitLimits.pitchStep, "{0:F1}");
					m_listItemData[0].Add(m_pitchAngle);
				}
				m_pitchAngle.title = LocalizationKeys.camera_customizer_pitch_angle.Localized();
				m_pitchAngle.value = m_cockpitCam.eulerAngles.x;
				TrackListSettingsItem pitchAngle = m_pitchAngle;
				pitchAngle.onDefaultValue = (System.Action)Delegate.Combine(pitchAngle.onDefaultValue, new System.Action(OnDefaultPitchAngle));
				TrackListSettingsItem pitchAngle2 = m_pitchAngle;
				pitchAngle2.onChangedValue = (System.Action)Delegate.Combine(pitchAngle2.onChangedValue, new System.Action(OnChangePitchAngle));
				TrackListSettingsItem pitchAngle3 = m_pitchAngle;
				pitchAngle3.onClick = (System.Action)Delegate.Combine(pitchAngle3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_pitchAngle);
				});
				if (m_globalSettings == null)
				{
					m_globalSettings = new HeaderListSettingsItem();
					m_listItemData[0].Add(m_globalSettings);
				}
				m_globalSettings.title = LocalizationKeys.global_camera_settings.Localized();
				if (m_fov == null)
				{
					m_fov = new TrackListSettingsItem(20f, 120f, cockpitLimits.fovStep);
					m_listItemData[0].Add(m_fov);
				}
				m_fov.title = LocalizationKeys.settings_graphics_vfov_cockpit.Localized();
				m_fov.value = m_global.cockpitFov;
				TrackListSettingsItem fov = m_fov;
				fov.onDefaultValue = (System.Action)Delegate.Combine(fov.onDefaultValue, new System.Action(OnDefaultFOV));
				TrackListSettingsItem fov2 = m_fov;
				fov2.onChangedValue = (System.Action)Delegate.Combine(fov2.onChangedValue, new System.Action(OnChangeFOV));
				TrackListSettingsItem fov3 = m_fov;
				fov3.onClick = (System.Action)Delegate.Combine(fov3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_fov);
				});
				if (m_dynamicCamCockpit == null)
				{
					m_dynamicCamCockpit = new SwitchListSettingsItem
					{
						title = LocalizationKeys.settings_game_camera_cockpit.Localized()
					};
					m_dynamicCamCockpit.list.AddRange(new string[3]
					{
						LocalizationKeys.control_off.Localized(),
						LocalizationKeys.settings_game_camera_dynamic_mode_legacy.Localized(),
						LocalizationKeys.settings_game_camera_dynamic_mode_improved.Localized()
					});
					m_listItemData[0].Add(m_dynamicCamCockpit);
				}
				m_dynamicCamCockpit.index = (int)(m_global.dynamicCamParams.cockpitCamMode + 1);
				SwitchListSettingsItem dynamicCamCockpit = m_dynamicCamCockpit;
				dynamicCamCockpit.onDefaultValue = (System.Action)Delegate.Combine(dynamicCamCockpit.onDefaultValue, new System.Action(OnDefaultDynamicCam));
				SwitchListSettingsItem dynamicCamCockpit2 = m_dynamicCamCockpit;
				dynamicCamCockpit2.onChangedValue = (System.Action)Delegate.Combine(dynamicCamCockpit2.onChangedValue, new System.Action(OnChangeDynamicCam));
				SwitchListSettingsItem dynamicCamCockpit3 = m_dynamicCamCockpit;
				dynamicCamCockpit3.onClick = (System.Action)Delegate.Combine(dynamicCamCockpit3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_dynamicCamCockpit);
				});
				if (m_hideDriver == null)
				{
					m_hideDriver = new ToggleListSettingsItem
					{
						title = LocalizationKeys.input_action_.AddLocalized("hidedriver")
					};
					m_listItemData[0].Add(m_hideDriver);
				}
				m_hideDriver.value = m_global.hideDriver;
				ToggleListSettingsItem hideDriver = m_hideDriver;
				hideDriver.onChangedValue = (System.Action)Delegate.Combine(hideDriver.onChangedValue, new System.Action(OnChangeDriverVisibility));
				ToggleListSettingsItem hideDriver2 = m_hideDriver;
				hideDriver2.onDefaultValue = (System.Action)Delegate.Combine(hideDriver2.onDefaultValue, new System.Action(OnDefaultDriverVisibility));
				ToggleListSettingsItem hideDriver3 = m_hideDriver;
				hideDriver3.onClick = (System.Action)Delegate.Combine(hideDriver3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_hideDriver);
				});
				if (m_hideWheel == null)
				{
					m_hideWheel = new ToggleListSettingsItem
					{
						title = LocalizationKeys.input_action_.AddLocalized("hidewheel")
					};
					m_listItemData[0].Add(m_hideWheel);
				}
				m_hideWheel.value = m_global.hideWheel;
				m_hideWheelPrevValue = m_global.hideWheel;
				ToggleListSettingsItem hideWheel = m_hideWheel;
				hideWheel.onChangedValue = (System.Action)Delegate.Combine(hideWheel.onChangedValue, new System.Action(OnChangeWheelVisibility));
				ToggleListSettingsItem hideWheel2 = m_hideWheel;
				hideWheel2.onDefaultValue = (System.Action)Delegate.Combine(hideWheel2.onDefaultValue, new System.Action(OnDefaultWheelVisibility));
				ToggleListSettingsItem hideWheel3 = m_hideWheel;
				hideWheel3.onClick = (System.Action)Delegate.Combine(hideWheel3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_hideWheel);
				});
				if (m_hideWheel.value)
				{
					m_hideDriver.value = true;
				}
				m_hideDriver.SetEditingPermission(!m_hideWheel.value);
				if (m_hideWindows == null)
				{
					m_hideWindows = new ToggleListSettingsItem
					{
						title = LocalizationKeys.settings_game_hidewindows.Localized()
					};
					m_listItemData[0].Add(m_hideWindows);
				}
				m_hideWindows.value = m_global.hideWindows;
				ToggleListSettingsItem hideWindows = m_hideWindows;
				hideWindows.onChangedValue = (System.Action)Delegate.Combine(hideWindows.onChangedValue, new System.Action(OnChangeWindowsVisibility));
				ToggleListSettingsItem hideWindows2 = m_hideWindows;
				hideWindows2.onDefaultValue = (System.Action)Delegate.Combine(hideWindows2.onDefaultValue, new System.Action(OnDefaultWindowsVisibility));
				ToggleListSettingsItem hideWindows3 = m_hideWindows;
				hideWindows3.onClick = (System.Action)Delegate.Combine(hideWindows3.onClick, (System.Action)delegate
				{
					CheckSelectedIndex(m_hideWindows);
				});
				m_previewSettings.offset = new Vector3(m_offsetX.value, m_height.value, m_offsetZ.value);
				m_previewSettings.pitch = m_pitchAngle.value;
				m_previewSettings.fov = m_fov.value;
			}
		}
		else
		{
			m_rearCam = rearCam;
		}
		if (!m_valuesStored)
		{
			m_currentSettings = m_previewSettings.Copy();
			m_storedDriverToggle = m_global.hideDriver;
			m_storedWheelToggle = m_global.hideWheel;
			m_storedWindowsToggle = m_global.hideWindows;
		}
		BuildSettingsList(m_prefabItem, m_listItemUI[0], m_listItemData[0]);
		UpdateUIList(m_currentItemUI, m_listItemUI[0], m_listItemData[0]);
	}

	private CarCameraSettings FillDefaultSettings(string key, BaseCamera camera)
	{
		CarCameraSettings carCameraSettings = new CarCameraSettings();
		carCameraSettings.indentifier = key;
		if (!(camera is CARXRearCamera) && camera is CARXCockpitCamera cARXCockpitCamera)
		{
			carCameraSettings.offset = cARXCockpitCamera.defaultOffset;
			carCameraSettings.pitch = cARXCockpitCamera.defaultEulerAngles.x;
			carCameraSettings.fov = cARXCockpitCamera.defaultCameraFOV;
		}
		return carCameraSettings;
	}

	private DynamicCamParameters FillDefaultDynamicCamSettings()
	{
		return m_prefs.globals.dynamicCamParams.Copy();
	}

	private void OnDefaultHeight()
	{
		if (m_cockpitCam != null)
		{
			m_height.value = m_cockpitCam.defaultOffset.y;
			m_previewSettings.offset = new Vector3(m_previewSettings.offset.x, m_height.value, m_previewSettings.offset.z);
		}
	}

	private void OnDefaultOffSetX()
	{
		if (m_cockpitCam != null)
		{
			m_offsetX.value = m_cockpitCam.defaultOffset.x;
			m_previewSettings.offset = new Vector3(m_offsetX.value, m_previewSettings.offset.y, m_previewSettings.offset.z);
			if (m_cameraPlaceObjects != null)
			{
				m_cockpitCam.SetHeadOnCockpit(m_cameraPlaceObjects);
			}
		}
	}

	private void OnDefaultOffSetZ()
	{
		if (m_cockpitCam != null)
		{
			m_offsetZ.value = m_cockpitCam.defaultOffset.z;
			m_previewSettings.offset = new Vector3(m_previewSettings.offset.x, m_previewSettings.offset.y, m_offsetZ.value);
			if (m_cameraPlaceObjects != null)
			{
				m_cockpitCam.SetHeadOnCockpit(m_cameraPlaceObjects);
			}
		}
	}

	private void OnDefaultPitchAngle()
	{
		if (m_cockpitCam != null)
		{
			m_pitchAngle.value = m_cockpitCam.defaultEulerAngles.x;
			m_previewSettings.pitch = m_pitchAngle.value;
		}
	}

	private void OnDefaultFOV()
	{
		if (m_cockpitCam != null)
		{
			m_global.cockpitFov = (int)m_cockpitCam.defaultCameraFOV;
			m_fov.value = m_cockpitCam.defaultCameraFOV;
			m_previewSettings.fov = m_fov.value;
		}
	}

	private void OnDefaultDynamicCam()
	{
		if (m_cockpitCam != null)
		{
			m_dynamicCamCockpit.index = (int)(m_cockpitCam.defaultDynamic + 1);
			m_global.dynamicCamParams.cockpitCamMode = m_cockpitCam.defaultDynamic;
			m_cockpitCam.velocityScale = ((m_dynamicCamCockpit.index > 0) ? m_global.dynamicCamParams.velocityScale : 0f);
			m_currentDynamicCamSettings.cockpitCamMode = (GlobalModel.DynamicCockpitCamMode)(m_dynamicCamCockpit.index - 1);
		}
	}

	private void OnDefaultDriverVisibility()
	{
		m_hideDriver.value = false;
		m_global.hideDriver = false;
	}

	private void OnDefaultWheelVisibility()
	{
		m_hideWheel.value = false;
		m_global.hideWheel = m_hideWheel.value;
	}

	private void OnDefaultWindowsVisibility()
	{
		m_hideWindows.value = false;
		m_global.hideWindows = m_hideWindows.value;
	}

	private void OnChangeHeight()
	{
		m_previewSettings.offset = new Vector3(m_previewSettings.offset.x, m_height.value, m_previewSettings.offset.z);
		if (m_cockpitCam != null)
		{
			m_cockpitCam.offset = m_previewSettings.offset;
			CheckSelectedIndex(m_height);
		}
		m_changed = true;
	}

	private void OnChangeXOffset()
	{
		m_previewSettings.offset = new Vector3(m_offsetX.value, m_previewSettings.offset.y, m_previewSettings.offset.z);
		if (m_cockpitCam != null)
		{
			m_cockpitCam.offset = m_previewSettings.offset;
			if (m_cameraPlaceObjects != null)
			{
				m_cockpitCam.SetHeadOnCockpit(m_cameraPlaceObjects);
				CheckSelectedIndex(m_offsetX);
			}
		}
		m_changed = true;
	}

	private void OnChangeZOffset()
	{
		m_previewSettings.offset = new Vector3(m_previewSettings.offset.x, m_previewSettings.offset.y, m_offsetZ.value);
		if (m_cockpitCam != null)
		{
			m_cockpitCam.offset = m_previewSettings.offset;
			if (m_cameraPlaceObjects != null)
			{
				m_cockpitCam.SetHeadOnCockpit(m_cameraPlaceObjects);
				CheckSelectedIndex(m_offsetZ);
			}
		}
	}

	private void OnChangePitchAngle()
	{
		m_previewSettings.pitch = m_pitchAngle.value;
		if (m_cockpitCam != null)
		{
			m_cockpitCam.eulerAngles = new Vector3(m_previewSettings.pitch, m_cockpitCam.eulerAngles.y, m_cockpitCam.eulerAngles.z);
			CheckSelectedIndex(m_pitchAngle);
		}
	}

	private void OnChangeFOV()
	{
		m_global.cockpitFov = Mathf.RoundToInt(m_fov.value);
		m_previewSettings.fov = m_global.cockpitFov;
		if (m_cockpitCam != null)
		{
			m_cockpitCam.SetFOV(m_previewSettings.fov);
			CheckSelectedIndex(m_fov);
		}
	}

	private void OnChangeDynamicCam()
	{
		if (m_cockpitCam != null)
		{
			m_previewDynamicCamSettings.cockpitCamMode = (GlobalModel.DynamicCockpitCamMode)(m_dynamicCamCockpit.index - 1);
			m_global.dynamicCamParams.cockpitCamMode = m_previewDynamicCamSettings.cockpitCamMode;
			m_cockpitCam.dynamicCameraMode = m_global.dynamicCamParams.cockpitCamMode;
			m_cockpitCam.velocityScale = ((m_global.dynamicCamParams.cockpitCamMode > GlobalModel.DynamicCockpitCamMode.OFF) ? m_global.dynamicCamParams.velocityScale : 0f);
			bool flag = m_dynamicCamCockpit.index > 1;
			ToggleDynamicCamSettings(flag);
			if (!flag)
			{
				CheckSelectedIndex(m_dynamicCamCockpit);
			}
		}
	}

	private void OnChangeDriverVisibility()
	{
		m_global.hideDriver = m_hideDriver.value;
		CheckSelectedIndex(m_hideDriver);
	}

	private void OnChangeWheelVisibility()
	{
		m_global.hideWheel = m_hideWheel.value;
		if (m_hideWheelPrevValue || m_hideWheel.value)
		{
			m_hideDriver.value = m_hideWheel.value;
		}
		m_hideDriver.SetEditingPermission(!m_hideWheel.value);
		OnChangeDriverVisibility();
		FindeItemUI(m_hideDriver, m_listItemUI[0]).UpdateValue();
		CheckSelectedIndex(m_hideWheel);
	}

	private void OnChangeWindowsVisibility()
	{
		m_global.hideWindows = m_hideWindows.value;
		CheckSelectedIndex(m_hideWindows);
	}

	private bool IsSettingsDiffer()
	{
		bool flag = m_storedDriverToggle != m_global.hideDriver || m_storedWheelToggle != m_global.hideWheel || m_storedWindowsToggle != m_global.hideWindows;
		bool flag2 = !PublicInstancePropertiesEqual(m_previewDynamicCamSettings, m_initialDynamicCamSettings);
		return !PublicInstancePropertiesEqual(m_previewSettings, m_currentSettings) || flag2 || flag;
	}

	private IEnumerator WaitOneFrameToOffDefault()
	{
		yield return null;
		m_setDefault = false;
	}

	private void CheckSelectedIndex(BaseListSettingsItem item, NavigationMode navMode = NavigationMode.CamGeneralSettingsList)
	{
		if (!m_setDefault)
		{
			BaseSettingsItemController itemUI = FindeItemUI(item, m_listItemUI[(int)navMode]);
			ScrollRect scrollRect = null;
			switch (navMode)
			{
			case NavigationMode.CamGeneralSettingsList:
				scrollRect = m_camGeneralSettingsScrollRect;
				break;
			case NavigationMode.DynamCamSettingsList:
				scrollRect = m_dynamicCamSettingsScrollRect;
				break;
			}
			ChangeListFocus(navMode);
			SetItemFocus(itemUI, ref m_currentItemUI, scrollRect);
		}
	}

	private void UpdateUIList(BaseSettingsItemController currentItemUI, List<BaseSettingsItemController> containerUI, List<BaseListSettingsItem> containerData)
	{
		for (int i = 0; i < containerData.Count; i++)
		{
			UpdateItemUI(containerData[i], currentItemUI, containerUI);
		}
	}

	private void UpdateItemUI(BaseListSettingsItem itemData, BaseSettingsItemController currentItemUI, List<BaseSettingsItemController> containerUI)
	{
		BaseSettingsItemController baseSettingsItemController = FindeItemUI(itemData, containerUI);
		if (baseSettingsItemController != null)
		{
			if (baseSettingsItemController.selected.value && currentItemUI != baseSettingsItemController)
			{
				baseSettingsItemController.SetSelected(select: false);
			}
			baseSettingsItemController.UpdateValue();
			baseSettingsItemController.gameObject.SetActive(NeedActivateByXr(baseSettingsItemController));
		}
	}

	private bool NeedActivateByXr(BaseSettingsItemController itemController)
	{
		if (!m_isXRActive)
		{
			return true;
		}
		BaseListSettingsItem item = itemController.item;
		if (item != m_dynamicCamCockpit)
		{
			return item != m_fov;
		}
		return false;
	}

	private BaseSettingsItemController FindeItemUI(BaseListSettingsItem itemData, List<BaseSettingsItemController> containerUI)
	{
		if (itemData != null)
		{
			for (int i = 0; i < containerUI.Count; i++)
			{
				if (containerUI[i].item == itemData)
				{
					return containerUI[i];
				}
			}
		}
		return null;
	}

	private void MoveItemSelection(int next, ref BaseSettingsItemController currentItemUI, List<BaseSettingsItemController> containerUI, List<BaseListSettingsItem> containerData, ScrollRect scrollRect)
	{
		if (currentItemUI == null)
		{
			SetFirstItemFocus(ref currentItemUI, containerUI, scrollRect);
			return;
		}
		int num = containerUI.IndexOf(currentItemUI);
		num = Mathf.Max(num + next, 0);
		int num2 = ((next > 0) ? 1 : (-1));
		for (int i = num; i < containerData.Count && i >= 0; i += num2)
		{
			BaseSettingsItemController baseSettingsItemController = containerUI[i];
			if (!(baseSettingsItemController.item is HeaderListSettingsItem) && baseSettingsItemController.gameObject.activeSelf)
			{
				int num3 = num + num2;
				if (num3 >= 0 && num3 < containerData.Count && containerData[num3] is HeaderListSettingsItem)
				{
					SetItemFocus(baseSettingsItemController, ref currentItemUI, scrollRect, containerUI[num3]);
				}
				else
				{
					SetItemFocus(baseSettingsItemController, ref currentItemUI, scrollRect);
				}
				break;
			}
		}
	}

	private void SetFirstItemFocus(ref BaseSettingsItemController currentItemUI, List<BaseSettingsItemController> containerUI, ScrollRect scrollRect)
	{
		if (containerUI.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < containerUI.Count; i++)
		{
			if (!(containerUI[i].item is HeaderListSettingsItem))
			{
				SetItemFocus(containerUI[i], ref currentItemUI, scrollRect);
				break;
			}
		}
	}

	private void SetItemFocus(BaseSettingsItemController itemUI, ref BaseSettingsItemController currentItemUI, ScrollRect scrollRect, BaseSettingsItemController headerUI = null)
	{
		if (itemUI == currentItemUI)
		{
			return;
		}
		if (currentItemUI != null)
		{
			currentItemUI.SetSelected(select: false);
			currentItemUI = null;
		}
		if (itemUI != null)
		{
			itemUI.SetSelected(select: true);
			currentItemUI = itemUI;
			if (headerUI != null && !scrollRect.IsScrollItemVerticalyVisible(headerUI.rectTransform))
			{
				scrollRect.SnapScrollTo(headerUI.rectTransform);
			}
			else if (!scrollRect.IsScrollItemVerticalyVisible(itemUI.rectTransform))
			{
				scrollRect.SnapScrollTo(itemUI.rectTransform);
			}
		}
	}

	public void NavCancel()
	{
		switch (m_navMode)
		{
		case NavigationMode.CamGeneralSettingsList:
			if (IsSettingsDiffer())
			{
				UIMessageBox.ShowComplexDialog(LocalizationKeys.customization_cameras_save_changes_text.Localized(), delegate
				{
				}, LocalizationKeys.Cancel.Localized(), delegate
				{
					Save();
					Hide();
					States.Pop();
					m_valuesStored = false;
				}, LocalizationKeys.Yes.Localized(), delegate
				{
					ResetToPreviousState();
					Save();
					Hide();
					States.Pop();
					m_valuesStored = false;
				}, LocalizationKeys.No.Localized());
			}
			else
			{
				Hide();
				States.Pop();
			}
			break;
		case NavigationMode.DynamCamSettingsList:
			ChangeListFocus(NavigationMode.CamGeneralSettingsList);
			break;
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavUp()
	{
		int next = -1;
		switch (m_navMode)
		{
		case NavigationMode.CamGeneralSettingsList:
			MoveItemSelection(next, ref m_currentItemUI, m_listItemUI[(int)m_navMode], m_listItemData[(int)m_navMode], m_camGeneralSettingsScrollRect);
			break;
		case NavigationMode.DynamCamSettingsList:
			MoveItemSelection(next, ref m_currentItemUI, m_listItemUI[(int)m_navMode], m_listItemData[(int)m_navMode], m_dynamicCamSettingsScrollRect);
			break;
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavDown()
	{
		int next = 1;
		switch (m_navMode)
		{
		case NavigationMode.CamGeneralSettingsList:
			MoveItemSelection(next, ref m_currentItemUI, m_listItemUI[(int)m_navMode], m_listItemData[(int)m_navMode], m_camGeneralSettingsScrollRect);
			break;
		case NavigationMode.DynamCamSettingsList:
			MoveItemSelection(next, ref m_currentItemUI, m_listItemUI[(int)m_navMode], m_listItemData[(int)m_navMode], m_dynamicCamSettingsScrollRect);
			break;
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		BaseSettingsItemController currentItemUI = m_currentItemUI;
		if ((bool)currentItemUI)
		{
			currentItemUI.item.PrevValue();
			currentItemUI.UpdateValue();
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		BaseSettingsItemController currentItemUI = m_currentItemUI;
		if ((bool)currentItemUI)
		{
			currentItemUI.item.NextValue();
			currentItemUI.UpdateValue();
		}
	}

	private void NavResetParam()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_single_to_default.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			ResetParam();
		}, LocalizationKeys.Ok.Localized());
	}

	private void ResetParam()
	{
		BaseSettingsItemController currentItemUI = m_currentItemUI;
		if ((bool)currentItemUI)
		{
			m_setDefault = true;
			currentItemUI.item.ResetToDefault();
			currentItemUI.UpdateValue();
			StartCoroutine(WaitOneFrameToOffDefault());
		}
	}

	private void NavResetAll(bool resetAllCategories = false)
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			ResetAll(resetAllCategories);
		}, LocalizationKeys.Ok.Localized());
	}

	private void ResetAll(bool resetAllCategories = false)
	{
		m_setDefault = true;
		NavigationMode[] array = (resetAllCategories ? ((NavigationMode[])Enum.GetValues(typeof(NavigationMode))) : new NavigationMode[1] { m_navMode });
		foreach (NavigationMode navigationMode in array)
		{
			if (navigationMode == NavigationMode.Count)
			{
				continue;
			}
			foreach (BaseListSettingsItem item in m_listItemData[(int)navigationMode])
			{
				item.ResetToDefault();
			}
			UpdateUIList(m_currentItemUI, m_listItemUI[(int)navigationMode], m_listItemData[(int)navigationMode]);
		}
		ScrollRect scrollRect = null;
		switch (m_navMode)
		{
		case NavigationMode.CamGeneralSettingsList:
			scrollRect = m_camGeneralSettingsScrollRect;
			break;
		case NavigationMode.DynamCamSettingsList:
			scrollRect = m_dynamicCamSettingsScrollRect;
			break;
		}
		SetFirstItemFocus(ref m_currentItemUI, m_listItemUI[(int)m_navMode], scrollRect);
		StartCoroutine(WaitOneFrameToOffDefault());
	}

	private void ResetToPreviousState()
	{
		NavigationMode[] obj = (NavigationMode[])Enum.GetValues(typeof(NavigationMode));
		NavigationMode navMode = m_navMode;
		NavigationMode[] array = obj;
		foreach (NavigationMode navigationMode in array)
		{
			if (navigationMode == NavigationMode.Count)
			{
				continue;
			}
			int num = (int)navigationMode;
			foreach (BaseListSettingsItem item in m_listItemData[num])
			{
				item.ReturnToInitialValue();
				item.onChangedValue.SafeCall();
			}
			UpdateUIList(m_currentItemUI, m_listItemUI[num], m_listItemData[num]);
		}
		m_navMode = navMode;
		ScrollRect scrollRect = m_camGeneralSettingsScrollRect;
		if (m_navMode == NavigationMode.DynamCamSettingsList)
		{
			scrollRect = m_dynamicCamSettingsScrollRect;
		}
		SetFirstItemFocus(ref m_currentItemUI, m_listItemUI[(int)m_navMode], scrollRect);
	}

	public static bool PublicInstancePropertiesEqual<T>(T self, T to, params string[] ignore) where T : class
	{
		if (self != null && to != null)
		{
			Type typeFromHandle = typeof(T);
			PropertyInfo[] cachedProperties = typeFromHandle.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public);
			for (int i = 0; i < cachedProperties.Length; i++)
			{
				if (Array.IndexOf(ignore, cachedProperties[i].Name) == -1)
				{
					PropertyInfo property = typeFromHandle.GetProperty(cachedProperties[i].Name);
					object value = property.GetValue(self, null);
					object value2 = property.GetValue(to, null);
					if (value != value2 && (value == null || !value.Equals(value2)))
					{
						return false;
					}
				}
			}
			return true;
		}
		return self == to;
	}

	public void BuildDynamicCamSettingsList()
	{
		m_listItemData[1].Clear();
		if (m_cockpitCamAimMode == null)
		{
			m_cockpitCamAimMode = new SwitchListSettingsItem
			{
				title = LocalizationKeys.cockpit_camera_aim_mode.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.camAimMode = (DynamicCamParameters.AimMode)m_cockpitCamAimMode.index;
					CheckSelectedIndex(m_cockpitCamAimMode, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamAimMode.index = 0;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamAimMode, NavigationMode.DynamCamSettingsList);
				}
			};
			m_cockpitCamAimMode.list.AddRange(new string[2]
			{
				LocalizationKeys.cockpit_camera_aim_mode_steer.Localized(),
				LocalizationKeys.cockpit_camera_aim_mode_velocity.Localized()
			});
			m_cockpitCamAimMode.index = (int)m_previewDynamicCamSettings.camAimMode;
		}
		if (m_cockpitCamMaxRotSpeed == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationSpeed");
			m_cockpitCamMaxRotSpeed = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step)
			{
				title = LocalizationKeys.cockpit_camera_max_rotation_speed.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.maxRotSpeed = m_cockpitCamMaxRotSpeed.value;
					CheckSelectedIndex(m_cockpitCamMaxRotSpeed, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamMaxRotSpeed.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationSpeed").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamMaxRotSpeed, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.maxRotSpeed
			};
		}
		if (m_cockpitCamMaxRotAngle == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationAngle");
			m_cockpitCamMaxRotAngle = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step)
			{
				title = LocalizationKeys.cockpit_camera_max_rotation_angle.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.maxRotAngle = m_cockpitCamMaxRotAngle.value;
					CheckSelectedIndex(m_cockpitCamMaxRotAngle, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamMaxRotAngle.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationAngle").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamMaxRotAngle, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.maxRotAngle
			};
		}
		if (m_cockpitCamVelScale == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("vectorScale");
			m_cockpitCamVelScale = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step, "{0:F2}")
			{
				title = LocalizationKeys.cockpit_camera_velocity_scale.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.velocityScale = m_cockpitCamVelScale.value;
					CheckSelectedIndex(m_cockpitCamVelScale, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamVelScale.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("vectorScale").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamVelScale, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.velocityScale
			};
		}
		if (m_cockpitCamSmooth == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("rotationSmooth");
			m_cockpitCamSmooth = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step, "{0:F2}")
			{
				title = LocalizationKeys.cockpit_camera_smooth.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.cameraSmooth = m_cockpitCamSmooth.value;
					CheckSelectedIndex(m_cockpitCamSmooth, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamSmooth.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("rotationSmooth").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamSmooth, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.cameraSmooth
			};
		}
		if (m_cockpitCamAccelMovingToggle == null)
		{
			m_cockpitCamAccelMovingToggle = new ToggleListSettingsItem
			{
				title = LocalizationKeys.cockpit_camera_accel_toggle.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.moveWithAccel = m_cockpitCamAccelMovingToggle.value;
					CheckSelectedIndex(m_cockpitCamAccelMovingToggle, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamAccelMovingToggle.value = true;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamAccelMovingToggle, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.moveWithAccel
			};
		}
		if (m_cockpitCamAccelMovingSmooth == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("accelSmooth");
			m_cockpitCamAccelMovingSmooth = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step, "{0:F2}")
			{
				title = LocalizationKeys.cockpit_camera_accel_smooth.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.accelSmooth = m_cockpitCamAccelMovingSmooth.value;
					m_cockpitCam.accelSmooth = m_cockpitCamAccelMovingSmooth.value;
					CheckSelectedIndex(m_cockpitCamAccelMovingSmooth, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamAccelMovingSmooth.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("accelSmooth").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamAccelMovingSmooth, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.accelSmooth
			};
		}
		if (m_cockpitCamDeadZone == null)
		{
			CameraCustomizerLimits.LimitsDynamicCam dynamicCockpitLimits = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("deadzone");
			m_cockpitCamDeadZone = new TrackListSettingsItem(dynamicCockpitLimits.min, dynamicCockpitLimits.max, dynamicCockpitLimits.step, "{0:F2}")
			{
				title = LocalizationKeys.cockpit_camera_deadzone.Localized(),
				onChangedValue = delegate
				{
					m_previewDynamicCamSettings.dynamicCamDeadZone = m_cockpitCamDeadZone.value;
					CheckSelectedIndex(m_cockpitCamDeadZone, NavigationMode.DynamCamSettingsList);
				},
				onDefaultValue = delegate
				{
					m_cockpitCamDeadZone.value = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("deadzone").defaultValue;
				},
				onClick = delegate
				{
					CheckSelectedIndex(m_cockpitCamDeadZone, NavigationMode.DynamCamSettingsList);
				},
				value = m_previewDynamicCamSettings.dynamicCamDeadZone
			};
		}
		m_listItemData[1].AddRange(new BaseListSettingsItem[8] { m_cockpitCamAimMode, m_cockpitCamMaxRotSpeed, m_cockpitCamMaxRotAngle, m_cockpitCamVelScale, m_cockpitCamDeadZone, m_cockpitCamSmooth, m_cockpitCamAccelMovingToggle, m_cockpitCamAccelMovingSmooth });
		m_currentDynamicCamSettings = FillDefaultDynamicCamSettings();
		BuildSettingsList(m_prefabDynamicCamItem, m_listItemUI[1], m_listItemData[1]);
		UpdateUIList(m_currentItemUI, m_listItemUI[1], m_listItemData[1]);
		if (!m_valuesStored)
		{
			m_initialDynamicCamSettings = m_currentDynamicCamSettings.Copy();
		}
	}

	private void ToggleDynamicCamSettings(bool toggle)
	{
		if (m_dynamicCamSettingsToggle.value == toggle)
		{
			if (toggle && !m_setDefault)
			{
				ChangeListFocus(NavigationMode.DynamCamSettingsList);
			}
		}
		else
		{
			m_dynamicCamSettingsToggle.value = toggle;
			NavigationMode navMode = (toggle ? NavigationMode.DynamCamSettingsList : NavigationMode.CamGeneralSettingsList);
			ChangeListFocus(navMode);
		}
	}

	private void ChangeListFocus(NavigationMode navMode)
	{
		m_cachedSelectedItemUI[(int)m_navMode] = m_currentItemUI;
		ScrollRect scrollRect = m_camGeneralSettingsScrollRect;
		switch (navMode)
		{
		case NavigationMode.CamGeneralSettingsList:
			scrollRect = m_camGeneralSettingsScrollRect;
			break;
		case NavigationMode.DynamCamSettingsList:
			scrollRect = m_dynamicCamSettingsScrollRect;
			break;
		}
		m_navMode = navMode;
		if ((bool)m_cachedSelectedItemUI[(int)m_navMode])
		{
			SetItemFocus(m_cachedSelectedItemUI[(int)m_navMode], ref m_currentItemUI, scrollRect);
		}
		else
		{
			SetFirstItemFocus(ref m_currentItemUI, m_listItemUI[(int)m_navMode], scrollRect);
		}
	}

	private void UpdateBottomPanel()
	{
		List<ActionHintData> list = new List<ActionHintData>
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			}
		};
		list.AddRange(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_advanced_reset.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = NavResetParam
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_advanced_reset_all.Localized(),
				type = ActionType.NavAdditionalPrevBtnL2,
				action = delegate
				{
					NavResetAll();
				}
			}
		});
		ShowBottomPanel(list.ToArray());
	}
}
