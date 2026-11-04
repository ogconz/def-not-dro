using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarX;
using DI;
using GameInput;
using GameOverlay;
using PrefsModels;
using Rewired;
using SyncMultiplayer;
using UnityEngine;

[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UIInputSettingsContext : BaseBindingBehaviourTarget
{
	public enum CategoryGroup
	{
		Device = 1,
		AdvancedSettings = -1,
		Both = 0
	}

	[Dependency]
	protected GamePrefs m_prefs;

	protected GlobalModel m_globals;

	[SerializeField]
	private AssistPostprocessGraph m_postprocessGraph;

	public SwitcherActions switcherActions = new SwitcherActions();

	private BoolProperty m_showDeviceTypeContainer = new BoolProperty();

	private IntProperty m_listGroupVisibilityParam = new IntProperty(1);

	private Property<CategoryGroup> m_currActiveListGroup = new Property<CategoryGroup>(CategoryGroup.Device);

	private BoolProperty m_needShowDescriptionPanel = new BoolProperty(startValue: true);

	private BoolProperty m_isConsole = new BoolProperty();

	private StringProperty m_descriptionTitle = new StringProperty();

	private StringProperty m_descriptionMain = new StringProperty();

	private StringProperty m_vibroFFBTitle = new StringProperty();

	private List<ActionHintData> m_actionBottomHints = new List<ActionHintData>();

	private string m_descriptionDeviceSettingsTitle;

	private string m_descriptionDeviceSettings;

	private string m_descriptionAdvancedSettingsTitle;

	private string m_descriptionAdvancedSettings;

	private SettingsDevicesList m_devicesList;

	private Controller m_lastController;

	private ControllerMetaInfo m_lastFXControllerMeta;

	public SettingsGameList advancedSettingsList;

	private SwitchListSettingsItem m_steeringDeviceTypeScheme;

	private SwitchListSettingsItem m_gearboxType;

	private SwitchListSettingsItem m_clutchType;

	private SwitchListSettingsItem m_holdIgnition;

	private ToggleListSettingsItem m_steeringAssist;

	private SwitchListSettingsItem m_steeringAngle;

	private ESteeringWheelAngle[] m_steerAngleValue;

	private ToggleListSettingsItem m_vibration;

	private TrackListSettingsItem m_feedback;

	private TrackListSettingsItem m_autoCenterPar;

	private SwitchListSettingsItem m_presetType;

	private SwitchListSettingsItem m_moveMouseAxisType;

	private TrackListSettingsItem m_stabilization;

	private TrackListSettingsItem m_prediction;

	private TrackListSettingsItem m_inertia;

	private TrackListSettingsItem m_acceleration;

	private TrackListSettingsItem m_smoothing;

	private ToggleListSettingsItem m_postprocess;

	private TrackListSettingsItem m_postprocessX;

	private TrackListSettingsItem m_postprocessY;

	private TrackListSettingsItem m_steeringDeadzone;

	private TrackListSettingsItem m_steeringLinearity;

	private TrackListSettingsItem m_accLinearity;

	private TrackListSettingsItem m_breakLinearity;

	private TrackListSettingsItem m_handbrakeLinearity;

	private TrackListSettingsItem m_clutchLinearity;

	private TrackListSettingsItem m_cursorAcceleration;

	private GameInput.DeviceType m_currSteeringDeviceType = GameInput.DeviceType.Unknown;

	private static GameInput.DeviceType[] m_allowedControllerDeviceType = new GameInput.DeviceType[3]
	{
		GameInput.DeviceType.Keyboard,
		GameInput.DeviceType.Gamepad,
		GameInput.DeviceType.Wheel
	};

	private ControlsSetup.Setup m_buttonsSetup;

	private ControlsSetup.Setup m_axisSetup;

	private ControlsSetup.Setup m_wheelSetup;

	private ControlsSetup.Setup m_logitechWheelSetup;

	private ControlsSetup.Setup m_currentUsedSetup;

	private StaticWheelPreset m_currentStaticWheelPreset;

	private ControlsSetup.FloatPair m_currentFloatPair;

	private ControlsSetup.IntPair m_currentIntPair;

	private bool m_isLogitechActiveWheel;

	private bool m_isWheelPlugged;

	private string m_prevWheelControllerName;

	private bool m_customWheelPresetForceSelected;

	private static List<ParamFloat> m_unlockedFloatParams = new List<ParamFloat>
	{
		ParamFloat.FFPredictWk,
		ParamFloat.FFPredictSk,
		ParamFloat.SteeringWheelAccelFF,
		ParamFloat.FFPostProcess_x,
		ParamFloat.FFPostProcess_y,
		ParamFloat.FFAccel,
		ParamFloat.FFAutoCenterPar
	};

	private static List<ParamInt> m_unlockedIntParams = new List<ParamInt>
	{
		ParamInt.SteerSmoothLevel,
		ParamInt.FFPostProcess,
		ParamInt.FFLogitechSpeedMod
	};

	private bool m_skipAccelOverride = true;

	private static bool m_isThrustmasterT248PrevPlugged = false;

	private bool m_forceSelectWheelPresetItem = true;

	private bool m_forceUpdatePostProcessItems;

	private (SettingsDeviceItemViewModel prevFFBdevice, SettingsDeviceItemViewModel newFFBdevice) m_cachedFFBDevices;

	private const int DEFAULT_ACC_T248 = 1;

	public static InputSettings.Parameters inputSettings => GameInput.InputManager.settings.Params;

	public static ControlsSetup controlsSetup => ScriptableSingleton<ControlsSetup>.instance;

	public bool isWheelDeviceTypeStaticPreset
	{
		get
		{
			if (inputSettings.currentWheelPresetType != WheelPresetType.Custom)
			{
				return isWheelDeviceType;
			}
			return false;
		}
	}

	public bool isWheelDeviceTypeCustomPreset
	{
		get
		{
			if (inputSettings.currentWheelPresetType == WheelPresetType.Custom)
			{
				return isWheelDeviceType;
			}
			return false;
		}
	}

	public bool isWheelDeviceType
	{
		get
		{
			if (m_currentUsedSetup.steeringMode != SteeringMode.Wheel)
			{
				return m_currentUsedSetup.steeringMode == SteeringMode.LogitechWheel;
			}
			return true;
		}
	}

	public bool isNowOpenDeviceListGroup => m_listGroupVisibilityParam.value >= 0;

	private bool m_networkRoomAssistRestricted
	{
		get
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame != null && instanceGame.CurrentRoom != null)
			{
				return m_prefs.globals.lastRoomAssist != 0;
			}
			return false;
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		BringForwardByDepth();
		m_globals = m_prefs.globals;
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.settings_category_steer_title.Localized());
		InitLocalizationBindings();
		switcherActions.ClearActions();
		switcherActions.AddActions(new HeaderActionData[2]
		{
			new HeaderActionData
			{
				text = LocalizationKeys.input_settings_devices_category.Localized(),
				action = delegate
				{
					SetActiveListGroup(CategoryGroup.Device);
				}
			},
			new HeaderActionData
			{
				text = LocalizationKeys.input_settings_devices_advanced.Localized(),
				action = delegate
				{
					SetActiveListGroup(CategoryGroup.AdvancedSettings);
				}
			}
		});
		switcherActions.Select(0);
		UpdateListFromControllers();
		if (m_devicesList != null)
		{
			SettingsDevicesList devicesList = m_devicesList;
			devicesList.onClick = (Action<int>)Delegate.Remove(devicesList.onClick, new Action<int>(OnClickDeviceItem));
			SettingsDevicesList devicesList2 = m_devicesList;
			devicesList2.onClick = (Action<int>)Delegate.Combine(devicesList2.onClick, new Action<int>(OnClickDeviceItem));
		}
		GameInput.InputManager instance = GameInput.InputManager.instance;
		instance.onDeviceAdd = (Action<Controller>)Delegate.Combine(instance.onDeviceAdd, new Action<Controller>(OnControllerConnected));
		GameInput.InputManager instance2 = GameInput.InputManager.instance;
		instance2.onDeviceRemove = (Action<Controller>)Delegate.Combine(instance2.onDeviceRemove, new Action<Controller>(OnControllerDisconnected));
		GameInput.InputManager instance3 = GameInput.InputManager.instance;
		instance3.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(instance3.onChangeActiveDevice, new Action<Controller>(OnActiveWheelChange));
		m_showDeviceTypeContainer.value = true;
		HandleDeviceTypeSettingChoose();
		SteeringMode lastSteeringMode = GameInput.InputManager.instance.GetLastSteeringMode();
		if (lastSteeringMode != SteeringMode.Axis && lastSteeringMode != SteeringMode.Buttons)
		{
			m_isWheelPlugged = true;
			m_isLogitechActiveWheel = lastSteeringMode == SteeringMode.LogitechWheel;
			inputSettings.isLogitechLastSteeringDevice = m_isLogitechActiveWheel;
		}
		else
		{
			m_isWheelPlugged = false;
		}
		TryGetUnlockedPhysicsParams();
		TryGetStaticWheelPreset();
		BuildAdvancedInputSettings();
		BringForwardByDepth();
		m_currActiveListGroup.OnValueChanged += OnActiveListChange;
		SwitchToDevicesCategory();
		SettingsGameList settingsGameList = advancedSettingsList;
		settingsGameList.onChangeSelectedIndex = (System.Action)Delegate.Combine(settingsGameList.onChangeSelectedIndex, new System.Action(OnSelectedAdvancedItemChanged));
		m_postprocessGraph.DrawGraph(m_postprocessX.value, m_postprocessY.value);
		m_postprocessGraph.SetVisible(visible: false);
	}

	private void OnClickDeviceItem(int index)
	{
		m_currActiveListGroup.value = CategoryGroup.Device;
	}

	private void OnActiveListChange()
	{
		if (m_currActiveListGroup.value == CategoryGroup.Device)
		{
			advancedSettingsList.selectedIndex = -1;
			m_devicesList.selectedIndex = 0;
		}
		else if (m_currActiveListGroup.value == CategoryGroup.AdvancedSettings)
		{
			m_devicesList.selectedIndex = -1;
			advancedSettingsList.selectedIndex = 0;
		}
		UpdateBottomPanel();
	}

	private void TryGetUnlockedPhysicsParams()
	{
		if (inputSettings.defaultUnlockedPhysicsParams == null)
		{
			StoreDefaultPhysicsParamsFromControlsSetup();
			UploadDefaultPhysicsParamsToActual();
		}
		else if (IsControlsSetupChanged())
		{
			StoreDefaultPhysicsParamsFromControlsSetup();
		}
		TryLoadNewPhysicsParams();
		LoadSetupsRefs();
		TryOverrideAccelThrustT248();
		TryApplyNonCustomPresetBySteeringMode(GetSteeringModeByCurrentControllerType());
		m_skipAccelOverride = true;
	}

	private void TryGetStaticWheelPreset()
	{
		WheelPresetType savedWheelPreset = inputSettings.currentWheelPresetType;
		m_currentStaticWheelPreset = inputSettings.staticWheelPresets.Find((StaticWheelPreset preset) => preset.wheelPreset == savedWheelPreset);
	}

	private SteeringMode GetSteeringModeByCurrentControllerType()
	{
		switch (m_currSteeringDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			return SteeringMode.Buttons;
		case GameInput.DeviceType.Gamepad:
			return SteeringMode.Axis;
		case GameInput.DeviceType.Wheel:
			if (!m_isWheelPlugged)
			{
				if (!inputSettings.isLogitechLastSteeringDevice)
				{
					return SteeringMode.Wheel;
				}
				return SteeringMode.LogitechWheel;
			}
			if (!m_isLogitechActiveWheel)
			{
				return SteeringMode.Wheel;
			}
			return SteeringMode.LogitechWheel;
		default:
			return SteeringMode.Buttons;
		}
	}

	private void LoadSetupsRefs(bool forceButtonsSteeringMode = false)
	{
		m_currentFloatPair = null;
		m_currentIntPair = null;
		SteeringMode steeringMode = (forceButtonsSteeringMode ? SteeringMode.Buttons : GetSteeringModeByCurrentControllerType());
		foreach (ControlsSetup.Setup customUnlockedPhysicsParam in inputSettings.customUnlockedPhysicsParams)
		{
			switch (customUnlockedPhysicsParam.steeringMode)
			{
			case SteeringMode.Buttons:
				m_buttonsSetup = customUnlockedPhysicsParam;
				break;
			case SteeringMode.Axis:
				m_axisSetup = customUnlockedPhysicsParam;
				break;
			case SteeringMode.Wheel:
				m_wheelSetup = customUnlockedPhysicsParam;
				break;
			case SteeringMode.LogitechWheel:
				m_logitechWheelSetup = customUnlockedPhysicsParam;
				break;
			}
			if (steeringMode == customUnlockedPhysicsParam.steeringMode)
			{
				m_currentUsedSetup = customUnlockedPhysicsParam;
			}
		}
	}

	private void HandleDeviceTypeSettingChoose()
	{
		if (inputSettings.steeringDeviceType.HasValue)
		{
			m_currSteeringDeviceType = inputSettings.steeringDeviceType.GetValueOrDefault();
		}
		else
		{
			m_currSteeringDeviceType = GameInput.DeviceType.Keyboard;
		}
	}

	private void TryLoadNewPhysicsParams()
	{
		List<ControlsSetup.Setup> defaultUnlockedPhysicsParams = inputSettings.defaultUnlockedPhysicsParams;
		List<ControlsSetup.Setup> customUnlockedPhysicsParams = inputSettings.customUnlockedPhysicsParams;
		if (defaultUnlockedPhysicsParams == null || customUnlockedPhysicsParams == null)
		{
			return;
		}
		foreach (ControlsSetup.Setup savedDefaultSetup in defaultUnlockedPhysicsParams)
		{
			List<ParamFloat> list = m_unlockedFloatParams.FindAll((ParamFloat unlockedParam) => !savedDefaultSetup.floatParams.Exists((ControlsSetup.FloatPair floatPair2) => floatPair2.paramType == unlockedParam));
			List<ParamInt> list2 = m_unlockedIntParams.FindAll((ParamInt unlockedIntParam) => !savedDefaultSetup.intParams.Exists((ControlsSetup.IntPair intPair2) => intPair2.paramType == unlockedIntParam));
			if (list == null || list2 == null)
			{
				continue;
			}
			ControlsSetup.Setup setup = controlsSetup.custom.Find((ControlsSetup.Setup setup3) => setup3.steeringMode == savedDefaultSetup.steeringMode);
			if (setup == null)
			{
				continue;
			}
			ControlsSetup.Setup setup2 = customUnlockedPhysicsParams.Find((ControlsSetup.Setup setup3) => setup3.steeringMode == savedDefaultSetup.steeringMode);
			if (setup2 == null)
			{
				continue;
			}
			foreach (ParamFloat lostFloatParam in list)
			{
				ControlsSetup.FloatPair floatPair = setup.floatParams.Find((ControlsSetup.FloatPair floatPair2) => floatPair2.paramType == lostFloatParam);
				if (floatPair != null)
				{
					savedDefaultSetup.floatParams.Add(new ControlsSetup.FloatPair
					{
						value = floatPair.value,
						paramType = floatPair.paramType
					});
					setup2.floatParams.Add(new ControlsSetup.FloatPair
					{
						value = floatPair.value,
						paramType = floatPair.paramType
					});
				}
			}
			foreach (ParamInt lostIntParam in list2)
			{
				ControlsSetup.IntPair intPair = setup.intParams.Find((ControlsSetup.IntPair intParams) => intParams.paramType == lostIntParam);
				if (intPair != null)
				{
					savedDefaultSetup.intParams.Add(new ControlsSetup.IntPair
					{
						value = intPair.value,
						paramType = intPair.paramType
					});
					setup2.intParams.Add(new ControlsSetup.IntPair
					{
						value = intPair.value,
						paramType = intPair.paramType
					});
				}
			}
		}
	}

	private bool IsControlsSetupChanged()
	{
		if (inputSettings.defaultUnlockedPhysicsParams.Count != controlsSetup.custom.Count)
		{
			return true;
		}
		foreach (ControlsSetup.Setup customDefaultStoredSetup in inputSettings.defaultUnlockedPhysicsParams)
		{
			ControlsSetup.Setup setup = controlsSetup.custom.Find((ControlsSetup.Setup setup2) => setup2.steeringMode == customDefaultStoredSetup.steeringMode);
			if (setup == null)
			{
				return true;
			}
			foreach (ControlsSetup.FloatPair floatPair in customDefaultStoredSetup.floatParams)
			{
				ControlsSetup.FloatPair floatPair2 = setup.floatParams.Find((ControlsSetup.FloatPair floatPairItem) => floatPairItem.paramType == floatPair.paramType);
				if (floatPair2 == null)
				{
					return true;
				}
				if (floatPair2.value != floatPair.value)
				{
					return true;
				}
			}
			foreach (ControlsSetup.IntPair intPair in customDefaultStoredSetup.intParams)
			{
				ControlsSetup.IntPair intPair2 = setup.intParams.Find((ControlsSetup.IntPair intPairItem) => intPairItem.paramType == intPair.paramType);
				if (intPair2 == null)
				{
					return true;
				}
				if (intPair2.value != intPair.value)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static List<ControlsSetup.FloatPair> GetUnlockedFloatParamsCopyFromSetup(ControlsSetup.Setup setup)
	{
		List<ControlsSetup.FloatPair> list = new List<ControlsSetup.FloatPair>();
		foreach (ControlsSetup.FloatPair floatParam in setup.floatParams)
		{
			if (IsUnlockedFloatParam(floatParam))
			{
				list.Add(new ControlsSetup.FloatPair
				{
					paramType = floatParam.paramType,
					value = floatParam.value
				});
			}
		}
		return list;
	}

	private static List<ControlsSetup.IntPair> GetUnlockedIntParamsCopyFromSetup(ControlsSetup.Setup setup)
	{
		List<ControlsSetup.IntPair> list = new List<ControlsSetup.IntPair>();
		foreach (ControlsSetup.IntPair intParam in setup.intParams)
		{
			if (IsUnlockedIntParam(intParam))
			{
				list.Add(new ControlsSetup.IntPair
				{
					paramType = intParam.paramType,
					value = intParam.value
				});
			}
		}
		return list;
	}

	private static ControlsSetup.Setup CopySetup(ControlsSetup.Setup fromSetup)
	{
		return new ControlsSetup.Setup
		{
			steeringMode = fromSetup.steeringMode,
			floatParams = GetUnlockedFloatParamsCopyFromSetup(fromSetup),
			intParams = GetUnlockedIntParamsCopyFromSetup(fromSetup)
		};
	}

	private static void UploadDefaultPhysicsParamsToActual(SteeringMode? targetSteeringMode = null)
	{
		if (inputSettings.defaultUnlockedPhysicsParams == null)
		{
			return;
		}
		if (targetSteeringMode.HasValue)
		{
			ControlsSetup.Setup setup = inputSettings.defaultUnlockedPhysicsParams.Find((ControlsSetup.Setup setup2) => setup2.steeringMode == targetSteeringMode);
			if (setup != null)
			{
				inputSettings.customUnlockedPhysicsParams.RemoveAll((ControlsSetup.Setup setup2) => setup2.steeringMode == targetSteeringMode);
				ControlsSetup.Setup item = CopySetup(setup);
				inputSettings.customUnlockedPhysicsParams.Add(item);
			}
			return;
		}
		inputSettings.customUnlockedPhysicsParams = new List<ControlsSetup.Setup>();
		foreach (ControlsSetup.Setup defaultUnlockedPhysicsParam in inputSettings.defaultUnlockedPhysicsParams)
		{
			ControlsSetup.Setup item2 = CopySetup(defaultUnlockedPhysicsParam);
			inputSettings.customUnlockedPhysicsParams.Add(item2);
		}
	}

	private static void StoreDefaultPhysicsParamsFromControlsSetup()
	{
		inputSettings.defaultUnlockedPhysicsParams = new List<ControlsSetup.Setup>();
		foreach (ControlsSetup.Setup item in controlsSetup.custom)
		{
			ControlsSetup.Setup setup = new ControlsSetup.Setup();
			setup.steeringMode = item.steeringMode;
			setup.floatParams = GetUnlockedFloatParamsCopyFromSetup(item);
			setup.intParams = GetUnlockedIntParamsCopyFromSetup(item);
			inputSettings.defaultUnlockedPhysicsParams.Add(setup);
		}
	}

	private static bool IsUnlockedFloatParam(ControlsSetup.FloatPair floatPair)
	{
		if (floatPair == null)
		{
			return false;
		}
		return m_unlockedFloatParams.Exists((ParamFloat floatParam) => floatParam == floatPair.paramType);
	}

	private static bool IsUnlockedIntParam(ControlsSetup.IntPair intPair)
	{
		if (intPair == null)
		{
			return false;
		}
		return m_unlockedIntParams.Exists((ParamInt intParam) => intParam == intPair.paramType);
	}

	private void UpdateListFromControllers()
	{
		if (m_devicesList == null)
		{
			m_devicesList = GetComponentInChildren<SettingsDevicesList>();
		}
		m_devicesList?.Clear();
		foreach (Controller controller in ReInput.controllers.Controllers)
		{
			TryAddControllerToSettingsList(controller);
		}
		StartCoroutine(RefreshDevicesList());
	}

	protected override void OnDeactivate()
	{
		if (Enum.GetValues(typeof(GameInput.DeviceType)) is GameInput.DeviceType[] array)
		{
			GameInput.DeviceType[] array2 = array;
			foreach (GameInput.DeviceType deviceType in array2)
			{
				if (deviceType != inputSettings.steeringDeviceType)
				{
					ForceSetDevicesEnableFFB(deviceType, enable: false);
				}
			}
		}
		if (inputSettings.steeringDeviceType.HasValue)
		{
			ForceSetDevicesEnableFFB(inputSettings.steeringDeviceType.GetValueOrDefault(), enable: true);
		}
		SettingsGameList settingsGameList = advancedSettingsList;
		settingsGameList.onChangeSelectedIndex = (System.Action)Delegate.Remove(settingsGameList.onChangeSelectedIndex, new System.Action(OnSelectedAdvancedItemChanged));
		base.owner.Hide<UIBackgroundContext>();
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if (instance != null)
		{
			GameInput.Action[] actions = instance.GetActions();
			foreach (GameInput.Action action in actions)
			{
				if (GameInput.Action.IsSteeringDeviceDependentActionType(action.actionType))
				{
					action.lastActiveBind = null;
				}
			}
			instance.Save();
			instance.onDeviceAdd = (Action<Controller>)Delegate.Remove(instance.onDeviceAdd, new Action<Controller>(OnControllerConnected));
			instance.onDeviceRemove = (Action<Controller>)Delegate.Remove(instance.onDeviceRemove, new Action<Controller>(OnControllerDisconnected));
			instance.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(instance.onChangeActiveDevice, new Action<Controller>(OnActiveWheelChange));
		}
		ScriptableSingleton<ControlsSetup>.instance.ApplyParams(GameInput.InputManager.instance.GetLastSteeringMode());
		m_lastController = null;
		m_lastFXControllerMeta = null;
		m_currActiveListGroup.OnValueChanged -= OnActiveListChange;
		base.OnDeactivate();
	}

	private void Update()
	{
		Controller lastActiveController = ReInput.controllers.GetLastActiveController();
		ControllerMetaInfo curFXDevice = GameInput.InputManager.instance.CurFXDevice;
		if ((lastActiveController == m_lastController && curFXDevice == m_lastFXControllerMeta) || (m_lastController != null && lastActiveController.type == ControllerType.Mouse))
		{
			return;
		}
		m_lastController = lastActiveController;
		m_lastFXControllerMeta = curFXDevice;
		foreach (SettingsDeviceItemViewModel item in m_devicesList.items)
		{
			item.UpdateDeviceState();
		}
		m_devicesList.UpdateList();
	}

	private void OnSelectedAdvancedItemChanged()
	{
		if (!isNowOpenDeviceListGroup)
		{
			BaseListSettingsItem baseListSettingsItem = advancedSettingsList.GetSelectedItem();
			SetDescriptionPanelByText(baseListSettingsItem?.description ?? m_descriptionAdvancedSettings);
			m_postprocessGraph.SetVisible(baseListSettingsItem == m_postprocessX || baseListSettingsItem == m_postprocessY);
		}
	}

	private IEnumerator RefreshDevicesList()
	{
		yield return new WaitForEndOfFrame();
		m_devicesList.RecalcList();
		m_devicesList.ScrollTo(0);
		if (m_currActiveListGroup.value == CategoryGroup.Device)
		{
			m_devicesList.selectedIndex = 0;
		}
	}

	public void ShowBinds(Controller controller)
	{
		base.owner.HideStackAndPush<UIInputBindsSettingsContext>().BuildSettingsList(controller);
	}

	private void OnControllerConnected(Controller controller)
	{
		TryAddControllerToSettingsList(controller);
		m_devicesList.RecalcList();
		TimeController.AddCallback(2f, delegate
		{
			UpdateDeviceItems();
			m_devicesList.UpdateList();
		});
	}

	private void UpdateDeviceItems()
	{
		if (!(m_devicesList != null))
		{
			return;
		}
		foreach (SettingsDeviceItemViewModel item in m_devicesList.items)
		{
			item.UpdateItemInfo();
		}
	}

	private void TryAddControllerToSettingsList(Controller controller)
	{
		if (controller.enabled && controller.isConnected && Overlay.instance.IsNativePlatformController(controller) && controller.type != ControllerType.Mouse)
		{
			SettingsDeviceItemViewModel settingsDeviceItemViewModel = new SettingsDeviceItemViewModel(controller, this);
			settingsDeviceItemViewModel.onChangedDeviceType = (Action<SettingsDeviceItemViewModel>)Delegate.Remove(settingsDeviceItemViewModel.onChangedDeviceType, new Action<SettingsDeviceItemViewModel>(HandleDeviceTypeSwitch));
			settingsDeviceItemViewModel.onChangedDeviceType = (Action<SettingsDeviceItemViewModel>)Delegate.Combine(settingsDeviceItemViewModel.onChangedDeviceType, new Action<SettingsDeviceItemViewModel>(HandleDeviceTypeSwitch));
			m_devicesList.AddItem(settingsDeviceItemViewModel);
		}
	}

	private InputSettings.DeviceProfile TryGetDeviceProfileByDeviceItem(SettingsDeviceItemViewModel deviceItem)
	{
		if (!GameInput.InputManager.instance.activeControllers.TryGetValue(deviceItem.device, out var value))
		{
			return null;
		}
		GameInput.InputManager.settings.devices.TryGetValue(value, out var value2);
		return value2;
	}

	private void HandleDeviceTypeSwitch(SettingsDeviceItemViewModel deviceItemViewModel)
	{
		if (deviceItemViewModel == null)
		{
			return;
		}
		GameInput.DeviceType deviceType = deviceItemViewModel.deviceMeta.deviceType;
		InputSettings.DeviceProfile deviceProfile = TryGetDeviceProfileByDeviceItem(deviceItemViewModel);
		if (deviceProfile != null)
		{
			deviceProfile.deviceType = deviceType;
			deviceItemViewModel.UpdateDeviceState();
		}
		if (deviceType == GameInput.DeviceType.Wheel)
		{
			m_devicesList.ForEach(delegate(SettingsDeviceItemViewModel item)
			{
				if (item != deviceItemViewModel)
				{
					ControllerMetaInfo deviceMeta = item.deviceMeta;
					if (deviceMeta.deviceType == GameInput.DeviceType.Wheel)
					{
						deviceMeta.deviceType = GameInput.DeviceType.Gamepad;
						InputSettings.DeviceProfile deviceProfile2 = TryGetDeviceProfileByDeviceItem(item);
						if (deviceProfile2 != null)
						{
							deviceProfile2.deviceType = GameInput.DeviceType.Gamepad;
						}
					}
					item.UpdateDeviceState();
					item.UpdateDeviceType();
				}
			});
		}
		m_devicesList.UpdateList();
	}

	private void OnControllerDisconnected(Controller controller)
	{
		UpdateListFromControllers();
		int num = -1;
		for (int i = 0; i < m_devicesList.GetCount(); i++)
		{
			if (m_devicesList[i].deviceMeta.rewiredController == controller)
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			m_devicesList.RemoveItem(num);
			m_devicesList.RecalcList();
		}
		if (GameInput.InputManager.instance.IsThrusmasterT248LastSteeringDeviceOrController(controller))
		{
			m_isThrustmasterT248PrevPlugged = false;
		}
	}

	private void OnActiveWheelChange(Controller controller)
	{
		if (controller == null)
		{
			return;
		}
		ControllerMetaInfo metaInfo = controller.GetMetaInfo();
		if (metaInfo == null)
		{
			return;
		}
		if (metaInfo.deviceType != GameInput.DeviceType.Wheel)
		{
			m_isWheelPlugged = false;
		}
		else if (!string.Equals(m_prevWheelControllerName, metaInfo.name))
		{
			m_prevWheelControllerName = metaInfo.name;
			inputSettings.isLogitechLastSteeringDevice = GameInput.InputManager.instance.IsControllerLogitechWheel(controller);
			m_isWheelPlugged = true;
			m_isLogitechActiveWheel = inputSettings.isLogitechLastSteeringDevice;
			if (m_currSteeringDeviceType == GameInput.DeviceType.Wheel)
			{
				TryOverrideAccelThrustT248(controller);
				TryApplyNonCustomPresetBySteeringMode(m_isLogitechActiveWheel ? SteeringMode.LogitechWheel : SteeringMode.Wheel, null, controller);
				m_skipAccelOverride = true;
				m_forceSelectWheelPresetItem = false;
				OnWheelPresetChange();
				ReloadPostProcessItems();
			}
		}
	}

	private void ReloadPostProcessItems()
	{
		if (!(advancedSettingsList == null))
		{
			advancedSettingsList.RemoveItem(m_postprocessX, shouldScroll: false);
			advancedSettingsList.RemoveItem(m_postprocessY, shouldScroll: false);
			m_forceUpdatePostProcessItems = true;
			OnPostProcessChange();
			m_forceUpdatePostProcessItems = false;
		}
	}

	public void UpdateBottomPanel()
	{
		m_actionBottomHints.Clear();
		switch (m_currActiveListGroup.value)
		{
		case CategoryGroup.Device:
			m_actionBottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			});
			m_actionBottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_device_settings.Localized(),
				type = ActionType.NavSelectBtnA,
				action = NavSelect
			});
			break;
		case CategoryGroup.AdvancedSettings:
			m_actionBottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			});
			if (m_listGroupVisibilityParam.value != 0)
			{
				m_actionBottomHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_reset.Localized(),
					type = ActionType.NavBtnExpand,
					action = ResetAdditional
				});
			}
			if (advancedSettingsList.GetCount() > 0)
			{
				advancedSettingsList.selectedIndex = 0;
			}
			break;
		}
		if (isNowOpenDeviceListGroup)
		{
			m_actionBottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_reset.Localized(),
				type = ActionType.NavBtnExpand,
				action = ResetDevices
			});
		}
		ShowBottomPanel(m_actionBottomHints);
	}

	private void SetActiveListGroup(CategoryGroup categoryGroup)
	{
		if (m_currActiveListGroup.value != categoryGroup)
		{
			switch (categoryGroup)
			{
			case CategoryGroup.Device:
				SwitchToDevicesCategory();
				break;
			case CategoryGroup.AdvancedSettings:
				SwitchToDevicesAdvanced();
				break;
			}
		}
	}

	public void SwitchToDevicesCategory()
	{
		m_listGroupVisibilityParam.value = 0;
		m_currActiveListGroup.value = CategoryGroup.Device;
		m_isConsole.value = false;
		LoadListSettingsItems();
		SetDescriptionPanelByText(m_descriptionDeviceSettings, m_descriptionDeviceSettingsTitle);
		UpdateBottomPanel();
	}

	public void SwitchToDevicesAdvanced()
	{
		m_listGroupVisibilityParam.value = -1;
		m_currActiveListGroup.value = CategoryGroup.AdvancedSettings;
		LoadListSettingsItems();
		SetDescriptionPanelByText(m_descriptionAdvancedSettings, m_descriptionAdvancedSettingsTitle);
		UpdateBottomPanel();
	}

	private void InitLocalizationBindings()
	{
		m_descriptionDeviceSettingsTitle = LocalizationKeys.input_settings_device_description_title.Localized();
		m_descriptionDeviceSettings = LocalizationKeys.input_settings_devices_main.Localized();
		m_descriptionAdvancedSettingsTitle = LocalizationKeys.input_settings_description_title.Localized();
		m_descriptionAdvancedSettings = LocalizationKeys.input_settings_devices_advanced.Localized();
		m_vibroFFBTitle.value = string.Empty;
	}

	private void SetDescriptionPanelByText(string main, string title = null)
	{
		if (title != null)
		{
			m_descriptionTitle.value = title;
		}
		m_descriptionMain.value = main;
	}

	public void SwitchPrevPage()
	{
		switcherActions.OnPrevPage();
	}

	public void SwitchNextPage()
	{
		switcherActions.OnNextPage();
	}

	private void BuildAdvancedInputSettings()
	{
		HandleDeviceTypeSettingChoose();
		InitListSettingsItems();
		LoadListSettingsItems();
	}

	private void InitListSettingsItems()
	{
		InitUnlockedPhysicsParams();
		InitGeneralItems();
		if (m_currSteeringDeviceType == GameInput.DeviceType.Gamepad)
		{
			InitGamepadItems();
		}
		else if (m_currSteeringDeviceType == GameInput.DeviceType.Wheel)
		{
			InitWheelItems();
		}
	}

	private void InitUnlockedPhysicsParams()
	{
		if (m_stabilization == null)
		{
			m_stabilization = new TrackListSettingsItem(0f, 3f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_stabilization.Localized()
			};
			TrackListSettingsItem stabilization = m_stabilization;
			stabilization.onChangedValue = (System.Action)Delegate.Combine(stabilization.onChangedValue, new System.Action(OnStabilizationChange));
		}
		string text = LocalizationKeys.settings_input_recommended_value.Localized();
		text += ": \n";
		string text2 = LocalizationKeys.settings_input_stabilization_desc.Localized();
		switch (m_currSteeringDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			text = text + LocalizationKeys.settings_input_keyboard.Localized() + ": 1\n";
			text += text2.Substring(text2.IndexOf("{1}") + 3);
			break;
		case GameInput.DeviceType.Gamepad:
			text = text + LocalizationKeys.settings_input_gamepad.Localized() + ": 1\n";
			text += text2.Substring(text2.IndexOf("{1}") + 3);
			break;
		case GameInput.DeviceType.Wheel:
			text += LocalizationKeys.settings_input_stabilization_desc.LocalizedFormat(' ', ' ');
			break;
		}
		if (m_moveMouseAxisType == null)
		{
			m_moveMouseAxisType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_move_mouse_axis.Localized(),
				description = LocalizationKeys.settings_input_move_mouse_axis_desc.Localized()
			};
			m_moveMouseAxisType.list.Add(LocalizationKeys.settings_input_move_mouse_axis_on_delta.Localized());
			m_moveMouseAxisType.list.Add(LocalizationKeys.settings_input_move_mouse_axis_on_coord.Localized());
			if (m_globals.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
			{
				m_moveMouseAxisType.NextValue();
			}
			else
			{
				m_moveMouseAxisType.PrevValue();
			}
			SwitchListSettingsItem moveMouseAxisType = m_moveMouseAxisType;
			moveMouseAxisType.onChangedValue = (System.Action)Delegate.Combine(moveMouseAxisType.onChangedValue, new System.Action(OnMoveMouseAxisChange));
		}
		m_stabilization.description = text;
		if (m_prediction == null)
		{
			m_prediction = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_prediction.Localized()
			};
			TrackListSettingsItem prediction = m_prediction;
			prediction.onChangedValue = (System.Action)Delegate.Combine(prediction.onChangedValue, new System.Action(OnPredictionChange));
		}
		string text3 = LocalizationKeys.settings_input_prediction_desc.Localized();
		text3 = text3 + "\n\n" + LocalizationKeys.settings_input_recommended_value.Localized() + "\n";
		switch (m_currSteeringDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			text3 = text3 + LocalizationKeys.settings_input_keyboard.Localized() + ": 1";
			break;
		case GameInput.DeviceType.Gamepad:
			text3 = text3 + LocalizationKeys.settings_input_gamepad.Localized() + ": 1";
			break;
		case GameInput.DeviceType.Wheel:
			text3 = text3 + LocalizationKeys.bind_wheel.Localized() + ": 0 - 0.5";
			break;
		}
		m_prediction.description = text3;
		if (m_acceleration == null)
		{
			m_acceleration = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_acceleration.Localized(),
				description = LocalizationKeys.settings_input_acceleration_desc.Localized()
			};
			TrackListSettingsItem acceleration = m_acceleration;
			acceleration.onChangedValue = (System.Action)Delegate.Combine(acceleration.onChangedValue, new System.Action(OnAccelerationChange));
		}
		if (m_smoothing == null)
		{
			m_smoothing = new TrackListSettingsItem(0f, 3f, 1f)
			{
				title = LocalizationKeys.settings_input_smoothing.Localized(),
				description = LocalizationKeys.settings_input_smoothing_desc.Localized()
			};
			TrackListSettingsItem smoothing = m_smoothing;
			smoothing.onChangedValue = (System.Action)Delegate.Combine(smoothing.onChangedValue, new System.Action(OnSmoothingChange));
		}
		if (m_inertia == null)
		{
			m_inertia = new TrackListSettingsItem(0f, 2f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_inertia.Localized(),
				description = LocalizationKeys.settings_input_inertia_desc.Localized()
			};
			TrackListSettingsItem inertia = m_inertia;
			inertia.onChangedValue = (System.Action)Delegate.Combine(inertia.onChangedValue, new System.Action(OnInertiaChange));
		}
		if (m_autoCenterPar == null)
		{
			m_autoCenterPar = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_autocenterpar.Localized()
			};
			TrackListSettingsItem autoCenterPar = m_autoCenterPar;
			autoCenterPar.onChangedValue = (System.Action)Delegate.Combine(autoCenterPar.onChangedValue, new System.Action(OnAutoCenterParChange));
		}
		if (m_postprocess == null)
		{
			m_postprocess = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_input_postprocess.Localized(),
				description = LocalizationKeys.settings_input_postprocess_desc.Localized()
			};
			ToggleListSettingsItem postprocess = m_postprocess;
			postprocess.onChangedValue = (System.Action)Delegate.Combine(postprocess.onChangedValue, new System.Action(OnPostProcessChange));
		}
		if (m_postprocessX == null)
		{
			m_postprocessX = new TrackListSettingsItem(0.05f, 0.9f, 0.05f, "{0:F2}")
			{
				title = LocalizationKeys.settings_input_postprocess_x.Localized()
			};
			TrackListSettingsItem postprocessX = m_postprocessX;
			postprocessX.onChangedValue = (System.Action)Delegate.Combine(postprocessX.onChangedValue, new System.Action(OnPostProcessXChange));
		}
		if (m_postprocessY == null)
		{
			m_postprocessY = new TrackListSettingsItem(0f, 0.9f, 0.05f, "{0:F2}")
			{
				title = LocalizationKeys.settings_input_postprocess_y.Localized()
			};
			TrackListSettingsItem postprocessY = m_postprocessY;
			postprocessY.onChangedValue = (System.Action)Delegate.Combine(postprocessY.onChangedValue, new System.Action(OnPostProcessYChange));
		}
		if (!m_skipAccelOverride)
		{
			TryApplyNonCustomPresetBySteeringMode(GetSteeringModeByCurrentControllerType());
			TryOverrideAccelThrustT248();
		}
		TryLoadValuesToPresetDependentParams();
		LoadValuesToNonPresetDependentParams();
	}

	private void TryLoadValuesToPresetDependentParams()
	{
		if (m_customWheelPresetForceSelected)
		{
			return;
		}
		m_skipAccelOverride = false;
		List<ControlsSetup.FloatPair> list = new List<ControlsSetup.FloatPair>();
		List<ControlsSetup.IntPair> list2 = new List<ControlsSetup.IntPair>();
		bool flag = false;
		if (isWheelDeviceTypeStaticPreset)
		{
			list = m_currentStaticWheelPreset.floatParams;
			list2 = m_currentStaticWheelPreset.intParams;
		}
		else
		{
			list = m_currentUsedSetup.floatParams;
			list2 = m_currentUsedSetup.intParams;
			flag = isWheelDeviceType;
		}
		flag &= !m_isThrustmasterT248PrevPlugged;
		ControlsSetup.FloatPair floatPair = null;
		foreach (ControlsSetup.FloatPair item in list)
		{
			if (item.paramType == ParamFloat.FFPredictWk)
			{
				m_stabilization.value = item.value;
			}
			if (item.paramType == ParamFloat.FFPredictSk)
			{
				m_prediction.value = item.value;
			}
			if (item.paramType == ParamFloat.SteeringWheelAccelFF)
			{
				m_inertia.value = item.value;
			}
			if (item.paramType == ParamFloat.FFAccel)
			{
				m_acceleration.value = item.value;
				floatPair = item;
			}
		}
		foreach (ControlsSetup.IntPair item2 in list2)
		{
			if (item2.paramType == ParamInt.SteerSmoothLevel)
			{
				m_smoothing.value = item2.value;
			}
			if (item2.paramType == ParamInt.FFLogitechSpeedMod && flag)
			{
				if (m_currentUsedSetup.steeringMode == SteeringMode.Wheel && !inputSettings.wheelParams.isThrustAccelOverrided)
				{
					m_acceleration.value = item2.value;
					floatPair.value = item2.value;
					inputSettings.wheelParams.isThrustAccelOverrided = true;
				}
				else if (m_currentUsedSetup.steeringMode == SteeringMode.LogitechWheel && !inputSettings.wheelParams.isLogitechAccelOverrided)
				{
					m_acceleration.value = item2.value;
					floatPair.value = item2.value;
					inputSettings.wheelParams.isLogitechAccelOverrided = true;
				}
			}
		}
	}

	private void LoadValuesToNonPresetDependentParams()
	{
		if (m_currentUsedSetup == null)
		{
			return;
		}
		foreach (ControlsSetup.FloatPair floatParam in m_currentUsedSetup.floatParams)
		{
			if (floatParam.paramType == ParamFloat.FFPostProcess_x)
			{
				m_postprocessX.value = floatParam.value;
			}
			if (floatParam.paramType == ParamFloat.FFPostProcess_y)
			{
				m_postprocessY.value = floatParam.value;
			}
			if (floatParam.paramType == ParamFloat.FFAutoCenterPar)
			{
				m_autoCenterPar.value = floatParam.value;
			}
		}
		foreach (ControlsSetup.IntPair intParam in m_currentUsedSetup.intParams)
		{
			if (intParam.paramType == ParamInt.FFPostProcess)
			{
				m_postprocess.value = intParam.value == 1;
			}
		}
	}

	public static void TryOverrideAccelThrustT248(Controller pluggedController = null, bool needCreateSaveData = true)
	{
		_ = GameInput.InputManager.instance;
		InputSettings.Parameters parameters = GameInput.InputManager.settings.Params;
		if (!GameInput.InputManager.instance.IsThrusmasterT248LastSteeringDeviceOrController(pluggedController))
		{
			return;
		}
		if (!needCreateSaveData)
		{
			ScriptableSingleton<ControlsSetup>.instance.OverrideParamFloat(SteeringMode.Wheel, ParamFloat.FFAccel, 1f);
			return;
		}
		m_isThrustmasterT248PrevPlugged = true;
		if (parameters.wheelParams.isThrust248AccelParamOverrided)
		{
			return;
		}
		if (parameters.defaultUnlockedPhysicsParams == null)
		{
			StoreDefaultPhysicsParamsFromControlsSetup();
		}
		if (parameters.customUnlockedPhysicsParams == null)
		{
			UploadDefaultPhysicsParamsToActual();
		}
		ControlsSetup.Setup setup = parameters.customUnlockedPhysicsParams.Find((ControlsSetup.Setup setup2) => setup2.steeringMode == SteeringMode.Wheel);
		if (setup != null)
		{
			ControlsSetup.FloatPair floatPair = setup.floatParams.Find((ControlsSetup.FloatPair floatPair2) => floatPair2.paramType == ParamFloat.FFAccel);
			if (floatPair != null)
			{
				floatPair.value = 1f;
				parameters.wheelParams.isThrust248AccelParamOverrided = true;
			}
		}
	}

	public static void TryApplyNonCustomPresetBySteeringMode(SteeringMode steeringMode, Action<List<ControlsSetup.FloatPair>, List<ControlsSetup.IntPair>> ApplyPhysics = null, Controller pluggedController = null)
	{
		InputSettings.Parameters inputSettings = GameInput.InputManager.settings.Params;
		List<ControlsSetup.Setup> customUnlockedPhysicsParams = inputSettings.customUnlockedPhysicsParams;
		if (customUnlockedPhysicsParams == null)
		{
			return;
		}
		ControlsSetup.Setup setup = customUnlockedPhysicsParams.Find((ControlsSetup.Setup setup2) => setup2.steeringMode == steeringMode);
		if ((steeringMode != SteeringMode.Wheel && steeringMode != SteeringMode.LogitechWheel) || inputSettings.currentWheelPresetType == WheelPresetType.Custom)
		{
			return;
		}
		StaticWheelPreset staticWheelPreset = inputSettings.staticWheelPresets.Find((StaticWheelPreset preset) => preset.wheelPreset == inputSettings.currentWheelPresetType);
		if (staticWheelPreset != null)
		{
			ControlsSetup.IntPair intPair = setup.intParams.Find((ControlsSetup.IntPair intPair3) => intPair3.paramType == ParamInt.FFLogitechSpeedMod);
			ControlsSetup.IntPair intPair2 = staticWheelPreset.intParams.Find((ControlsSetup.IntPair intPair3) => intPair3.paramType == ParamInt.FFLogitechSpeedMod);
			if (intPair2 != null && intPair != null)
			{
				bool flag = false;
				flag = GameInput.InputManager.instance.IsThrusmasterT248LastSteeringDeviceOrController(pluggedController) || m_isThrustmasterT248PrevPlugged;
				intPair2.value = (flag ? 1 : intPair.value);
			}
			ControlsSetup.FloatPair floatPair = staticWheelPreset.floatParams.Find((ControlsSetup.FloatPair floatPair2) => floatPair2.paramType == ParamFloat.FFAccel);
			if (floatPair != null && intPair2 != null)
			{
				floatPair.value = intPair2.value;
			}
			ApplyPhysics?.Invoke(staticWheelPreset.floatParams, staticWheelPreset.intParams);
		}
	}

	private void LoadListSettingsItems()
	{
		advancedSettingsList.Clear();
		if (m_listGroupVisibilityParam.value == 0)
		{
			m_steeringDeviceTypeScheme.SetNarrowItem();
			advancedSettingsList.AddItem(m_steeringDeviceTypeScheme);
			advancedSettingsList.RecalcList();
			return;
		}
		m_steeringDeviceTypeScheme.width = m_steeringDeviceTypeScheme.defaultWidth;
		advancedSettingsList.AddItem(m_gearboxType);
		advancedSettingsList.AddItem(m_clutchType);
		advancedSettingsList.AddItem(m_holdIgnition);
		advancedSettingsList.AddItem(m_steeringDeviceTypeScheme);
		advancedSettingsList.AddItem(m_steeringAngle);
		switch (m_currSteeringDeviceType)
		{
		case GameInput.DeviceType.Gamepad:
			LoadGamepadItems();
			break;
		case GameInput.DeviceType.Wheel:
			LoadWheelItems();
			break;
		default:
			advancedSettingsList.AddItem(m_moveMouseAxisType);
			advancedSettingsList.AddItem(m_prediction);
			advancedSettingsList.AddItem(m_stabilization);
			advancedSettingsList.AddItem(m_steeringAssist);
			break;
		}
		advancedSettingsList.AddItem(m_steeringLinearity);
		advancedSettingsList.AddItem(m_accLinearity);
		advancedSettingsList.AddItem(m_breakLinearity);
		advancedSettingsList.AddItem(m_handbrakeLinearity);
		advancedSettingsList.AddItem(m_clutchLinearity);
		advancedSettingsList.AddItem(m_steeringDeadzone);
		advancedSettingsList.RecalcList();
	}

	private void LoadGamepadItems()
	{
		advancedSettingsList.AddItem(m_prediction);
		advancedSettingsList.AddItem(m_stabilization);
		advancedSettingsList.AddItem(m_vibration);
		advancedSettingsList.AddItem(m_steeringAssist);
	}

	private void LoadWheelItems()
	{
		advancedSettingsList.AddItem(m_feedback);
		advancedSettingsList.AddItem(m_autoCenterPar);
		advancedSettingsList.AddItem(m_steeringAssist);
		advancedSettingsList.AddItem(m_presetType);
		advancedSettingsList.AddItem(m_prediction);
		advancedSettingsList.AddItem(m_stabilization);
		advancedSettingsList.AddItem(m_smoothing);
		advancedSettingsList.AddItem(m_acceleration);
		advancedSettingsList.AddItem(m_inertia);
		advancedSettingsList.AddItem(m_postprocess);
		if (m_postprocess.value)
		{
			advancedSettingsList.AddItem(m_postprocessX);
			advancedSettingsList.AddItem(m_postprocessY);
		}
	}

	private void OnSteerAssistentChange()
	{
		if (m_networkRoomAssistRestricted)
		{
			m_steeringAssist.value = false;
			GUICommonGodVoice.ShowText(LocalizationKeys.input_settings_no_available_assistant_in_current_room.Localized());
		}
		GlobalModel.SteeringAssist steeringAssist = (m_steeringAssist.value ? GlobalModel.SteeringAssist.AssistV2 : GlobalModel.SteeringAssist.None);
		switch (m_currSteeringDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			m_prefs.globals.steeringAssistentKeyboard = steeringAssist;
			break;
		case GameInput.DeviceType.Gamepad:
			m_prefs.globals.steeringAssistentGamepad = steeringAssist;
			break;
		case GameInput.DeviceType.Wheel:
			m_prefs.globals.steeringAssistentWheel = steeringAssist;
			break;
		}
		m_prefs.globals.Save();
		advancedSettingsList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_steeringAssist);
	}

	private void OnControllerTypeChange()
	{
		m_currSteeringDeviceType = m_allowedControllerDeviceType[m_steeringDeviceTypeScheme.index];
		inputSettings.steeringDeviceType = m_currSteeringDeviceType;
		LoadSetupsRefs();
		BuildAdvancedInputSettings();
		UpdateDeviceItems();
		m_devicesList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_steeringDeviceTypeScheme);
	}

	private void OnAutomaticGearboxChange()
	{
		bool flag = m_gearboxType.index == 0;
		inputSettings.automaticGearboxEnabled = flag;
		if (flag)
		{
			m_clutchType.index = 0;
			inputSettings.automaticClutchEnabled = true;
			m_clutchType.SetBlockToEditing(value: true);
		}
		else
		{
			m_clutchType.SetBlockToEditing(value: false);
		}
		GameEvents.GameSettingsInputUpdate();
		advancedSettingsList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_gearboxType);
	}

	private void OnAutomaticClutchChange()
	{
		bool flag = m_clutchType.index == 0;
		inputSettings.automaticClutchEnabled = flag;
		GameInput.InputManager.TryShowBindStarterIgnitionMsg(!flag, 5f);
		GameEvents.GameSettingsInputUpdate();
		advancedSettingsList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_clutchType);
	}

	private void OnVibrationChange()
	{
		inputSettings.gamepadParams.vibrationEnabled = m_vibration.value;
		advancedSettingsList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_vibration);
	}

	private void OnHoldIgnitionChange()
	{
		bool flag = m_holdIgnition.index == 1;
		inputSettings.holdIgnition = flag;
		GameInput.InputManager.TryShowBindStarterIgnitionMsg(flag, 5f);
		advancedSettingsList.UpdateList();
		advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_holdIgnition);
	}

	private void OnWheelPresetChange()
	{
		WheelPresetType newWheelPreset = (WheelPresetType)m_presetType.index;
		if (newWheelPreset != WheelPresetType.Custom)
		{
			m_customWheelPresetForceSelected = false;
		}
		inputSettings.currentWheelPresetType = newWheelPreset;
		m_currentStaticWheelPreset = inputSettings.staticWheelPresets.Find((StaticWheelPreset preset) => preset.wheelPreset == newWheelPreset);
		LoadSetupsRefs();
		InitUnlockedPhysicsParams();
		advancedSettingsList.UpdateList();
		if (m_forceSelectWheelPresetItem)
		{
			advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_presetType);
		}
	}

	private void OnStabilizationChange()
	{
		if (isWheelDeviceTypeStaticPreset)
		{
			TrySetCustomWheelPresetOnEditingParam(ParamFloat.FFPredictWk, m_stabilization.value);
		}
		m_forceSelectWheelPresetItem = true;
		HandleSliderDiscretizationByTrackItem(m_stabilization);
		if (isWheelDeviceTypeCustomPreset || !isWheelDeviceType)
		{
			TrySetCurrentFloatPairByParamType(ParamFloat.FFPredictWk, m_stabilization.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnPredictionChange()
	{
		if (isWheelDeviceTypeStaticPreset)
		{
			TrySetCustomWheelPresetOnEditingParam(ParamFloat.FFPredictSk, m_prediction.value);
		}
		m_forceSelectWheelPresetItem = true;
		HandleSliderDiscretizationByTrackItem(m_prediction);
		if (isWheelDeviceTypeCustomPreset || !isWheelDeviceType)
		{
			TrySetCurrentFloatPairByParamType(ParamFloat.FFPredictSk, m_prediction.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnMoveMouseAxisChange()
	{
		m_globals.moveMouseAxisType = ((m_globals.moveMouseAxisType != GlobalModel.MouseMoveType.Coord) ? GlobalModel.MouseMoveType.Coord : GlobalModel.MouseMoveType.Delta);
		advancedSettingsList.UpdateList();
	}

	private void OnInertiaChange()
	{
		if (isWheelDeviceTypeStaticPreset)
		{
			TrySetCustomWheelPresetOnEditingParam(ParamFloat.SteeringWheelAccelFF, m_inertia.value);
		}
		m_forceSelectWheelPresetItem = true;
		HandleSliderDiscretizationByTrackItem(m_inertia);
		if (isWheelDeviceTypeCustomPreset || !isWheelDeviceType)
		{
			TrySetCurrentFloatPairByParamType(ParamFloat.SteeringWheelAccelFF, m_inertia.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnAccelerationChange()
	{
		if (isWheelDeviceTypeStaticPreset)
		{
			TrySetCustomWheelPresetOnEditingParam(ParamFloat.FFAccel, m_acceleration.value);
		}
		m_forceSelectWheelPresetItem = true;
		HandleSliderDiscretizationByTrackItem(m_acceleration);
		if (isWheelDeviceTypeCustomPreset || !isWheelDeviceType)
		{
			TrySetCurrentFloatPairByParamType(ParamFloat.FFAccel, m_acceleration.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnSmoothingChange()
	{
		if (isWheelDeviceTypeStaticPreset)
		{
			TrySetCustomWheelPresetOnEditingParam(ParamInt.SteerSmoothLevel, (int)m_smoothing.value);
		}
		m_forceSelectWheelPresetItem = true;
		HandleSliderDiscretizationByTrackItem(m_smoothing);
		if (isWheelDeviceTypeCustomPreset || !isWheelDeviceType)
		{
			TrySetCurrentIntPairByParamType(ParamInt.SteerSmoothLevel, (int)m_smoothing.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnAutoCenterParChange()
	{
		HandleSliderDiscretizationByTrackItem(m_autoCenterPar);
		if (isWheelDeviceType)
		{
			TrySetCurrentFloatPairByParamType(ParamFloat.FFAutoCenterPar, m_autoCenterPar.value);
		}
		advancedSettingsList.UpdateList();
	}

	private void OnPostProcessChange()
	{
		if (m_listGroupVisibilityParam.value == 0)
		{
			return;
		}
		if (m_currentIntPair == null || m_currentIntPair.paramType != ParamInt.FFPostProcess)
		{
			m_currentIntPair = m_currentUsedSetup.intParams.Find((ControlsSetup.IntPair intPair) => intPair.paramType == ParamInt.FFPostProcess);
		}
		bool num = m_currentIntPair.value == 1;
		m_currentIntPair.value = (m_postprocess.value ? 1 : 0);
		m_postprocessGraph.DrawGraph(m_postprocessX.value, m_postprocessY.value);
		advancedSettingsList.UpdateList();
		if (num != m_postprocess.value || m_forceUpdatePostProcessItems)
		{
			if (m_postprocess.value)
			{
				int num2 = advancedSettingsList.IndexOf(m_postprocess);
				advancedSettingsList.InsertItem(++num2, m_postprocessX);
				advancedSettingsList.InsertItem(++num2, m_postprocessY);
			}
			else
			{
				advancedSettingsList.RemoveItem(m_postprocessX, shouldScroll: false);
				advancedSettingsList.RemoveItem(m_postprocessY, shouldScroll: false);
			}
			advancedSettingsList.ApplyChangeList();
			if (!m_forceUpdatePostProcessItems)
			{
				advancedSettingsList.ScrollTo(m_postprocess);
			}
		}
		if (!m_forceUpdatePostProcessItems)
		{
			advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_postprocess);
		}
	}

	private void OnPostProcessXChange()
	{
		HandleSliderDiscretizationByTrackItem(m_postprocessX);
		TrySetCurrentFloatPairByParamType(ParamFloat.FFPostProcess_x, m_postprocessX.value);
		m_postprocessGraph.DrawGraph(m_postprocessX.value, m_postprocessY.value);
		advancedSettingsList.UpdateList();
	}

	private void OnPostProcessYChange()
	{
		HandleSliderDiscretizationByTrackItem(m_postprocessY);
		TrySetCurrentFloatPairByParamType(ParamFloat.FFPostProcess_y, m_postprocessY.value);
		m_postprocessGraph.DrawGraph(m_postprocessX.value, m_postprocessY.value);
		advancedSettingsList.UpdateList();
	}

	private void TrySetCustomWheelPresetOnEditingParam(ParamInt targetParamInt, int value)
	{
		if (m_currentStaticWheelPreset != null)
		{
			ControlsSetup.IntPair intPair = m_currentStaticWheelPreset.intParams.Find((ControlsSetup.IntPair intPair2) => intPair2.paramType == targetParamInt);
			if (intPair != null && !Mathf.Approximately(intPair.value, value))
			{
				ForceSetCustomWheelPreset();
			}
		}
	}

	private void TrySetCustomWheelPresetOnEditingParam(ParamFloat targetParamFloat, float value)
	{
		if (m_currentStaticWheelPreset != null)
		{
			ControlsSetup.FloatPair floatPair = m_currentStaticWheelPreset.floatParams.Find((ControlsSetup.FloatPair floatPair2) => floatPair2.paramType == targetParamFloat);
			if (floatPair != null && !Mathf.Approximately(floatPair.value, value))
			{
				ForceSetCustomWheelPreset();
			}
		}
	}

	private void ForceSetCustomWheelPreset()
	{
		if (m_presetType != null)
		{
			m_customWheelPresetForceSelected = true;
			m_presetType.index = 0;
			m_forceSelectWheelPresetItem = false;
			OnWheelPresetChange();
			OnPredictionChange();
			OnStabilizationChange();
			OnSmoothingChange();
			OnAccelerationChange();
			OnInertiaChange();
		}
	}

	private void HandleSliderDiscretizationByTrackItem(TrackListSettingsItem trackListSettingsItem)
	{
		if (trackListSettingsItem != null && !Mathf.Approximately(trackListSettingsItem.step, 0f))
		{
			float num = trackListSettingsItem.value / trackListSettingsItem.step;
			if ((double)num != Math.Truncate(num))
			{
				trackListSettingsItem.value = (float)Mathf.RoundToInt(num) * trackListSettingsItem.step;
			}
		}
	}

	private void TrySetCurrentIntPairByParamType(ParamInt targetParamInt, int value)
	{
		if (m_currentIntPair == null || m_currentIntPair.paramType != targetParamInt)
		{
			m_currentIntPair = m_currentUsedSetup.intParams.Find((ControlsSetup.IntPair intPair) => intPair.paramType == targetParamInt);
		}
		if (m_currentIntPair != null)
		{
			m_currentIntPair.value = value;
		}
	}

	private void TrySetCurrentFloatPairByParamType(ParamFloat targetParamFloat, float value)
	{
		if (m_currentFloatPair == null || m_currentFloatPair.paramType != targetParamFloat)
		{
			m_currentFloatPair = m_currentUsedSetup.floatParams.Find((ControlsSetup.FloatPair floatPair) => floatPair.paramType == targetParamFloat);
		}
		if (m_currentFloatPair != null)
		{
			m_currentFloatPair.value = value;
		}
	}

	private void OnForceFeedBackChange()
	{
		inputSettings.wheelParams.forceFeedback = m_feedback.value;
		advancedSettingsList.UpdateList();
	}

	private void OnSteeringDeadzoneChange()
	{
		inputSettings.steeringDeadZone = m_steeringDeadzone.value;
		advancedSettingsList.UpdateList();
	}

	private void OnLinearityChange()
	{
		inputSettings.steeringLinearity = m_steeringLinearity.value;
		inputSettings.steeringLinearity = m_steeringLinearity.value;
		inputSettings.accLinearity = m_accLinearity.value;
		inputSettings.brakeLinearity = m_breakLinearity.value;
		inputSettings.clutchLinearity = m_clutchLinearity.value;
		inputSettings.handbrakeLinearity = m_handbrakeLinearity.value;
		advancedSettingsList.UpdateList();
	}

	private void OnChangeSteeringAngle()
	{
		ESteeringWheelAngle eSteeringWheelAngle = m_steerAngleValue[m_steeringAngle.index];
		if (inputSettings.wheelParams.steeringWheelAngle != eSteeringWheelAngle)
		{
			inputSettings.wheelParams.steeringWheelAngle = eSteeringWheelAngle;
			advancedSettingsList.UpdateList();
			if (DependencyInjector.Resolve<WheelForceFeedbackSupport>().currentFeedbackSystem is DirectInputFeedback directInputFeedback && !directInputFeedback.IsWheelAngleRangeSupported())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.input_settings_need_wheel_calibration.Localized(), 3f);
			}
			advancedSettingsList.selectedIndex = advancedSettingsList.IndexOf(m_steeringAngle);
		}
	}

	public void ResetAdditional()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			ResetParamsBySelectedDevice(m_currSteeringDeviceType);
			BuildAdvancedInputSettings();
			if (advancedSettingsList.GetCount() > 0)
			{
				advancedSettingsList.selectedIndex = 0;
			}
			m_prefs.globals.Save();
			GameInput.InputManager.instance.Save();
			GameEvents.GameSettingsInputUpdate();
		}, LocalizationKeys.Ok.Localized());
	}

	private void ResetDevices()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			m_devicesList.ForEach(delegate(SettingsDeviceItemViewModel deviceItem)
			{
				deviceItem.deviceMeta.SetDefaultDeviceType();
			});
			UpdateDeviceItems();
			m_devicesList.UpdateList();
			m_prefs.globals.Save();
			GameInput.InputManager.instance.Save();
		}, LocalizationKeys.Ok.Localized());
	}

	private void ResetParamsBySelectedDevice(GameInput.DeviceType controllerDeviceType)
	{
		InputSettings.GamepadParams gamepadParams = inputSettings.gamepadParams;
		InputSettings.WheelParams wheelParams = inputSettings.wheelParams;
		List<ControlsSetup.Setup> defaultUnlockedPhysicsParams = inputSettings.defaultUnlockedPhysicsParams;
		List<ControlsSetup.Setup> customUnlockedPhysicsParams = inputSettings.customUnlockedPhysicsParams;
		List<StaticWheelPreset> staticWheelPresets = inputSettings.staticWheelPresets;
		bool isLogitechLastSteeringDevice = inputSettings.isLogitechLastSteeringDevice;
		Action<bool> autoClutchChanged = inputSettings.autoClutchChanged;
		GameInput.InputManager.settings.Params = new InputSettings.Parameters();
		inputSettings.autoClutchChanged = autoClutchChanged;
		inputSettings.defaultUnlockedPhysicsParams = defaultUnlockedPhysicsParams;
		inputSettings.customUnlockedPhysicsParams = customUnlockedPhysicsParams;
		inputSettings.staticWheelPresets = staticWheelPresets;
		inputSettings.isLogitechLastSteeringDevice = isLogitechLastSteeringDevice;
		wheelParams.steeringWheelAngle = ESteeringWheelAngle.R900;
		wheelParams.steeringDeadzone = 0f;
		switch (controllerDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			inputSettings.gamepadParams = gamepadParams;
			inputSettings.wheelParams = wheelParams;
			if (!m_networkRoomAssistRestricted)
			{
				m_prefs.globals.steeringAssistentKeyboard = GlobalModel.SteeringAssist.AssistV2;
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.input_settings_no_available_assistant_in_current_room.Localized());
			}
			break;
		case GameInput.DeviceType.Gamepad:
			inputSettings.wheelParams = wheelParams;
			if (!m_networkRoomAssistRestricted)
			{
				m_prefs.globals.steeringAssistentGamepad = GlobalModel.SteeringAssist.AssistV2;
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.input_settings_no_available_assistant_in_current_room.Localized());
			}
			break;
		case GameInput.DeviceType.Wheel:
		{
			System.Action steeringWheelAngleChanged = wheelParams.SteeringWheelAngleChanged;
			inputSettings.gamepadParams = gamepadParams;
			m_prefs.globals.steeringAssistentWheel = GlobalModel.SteeringAssist.None;
			inputSettings.wheelParams.SteeringWheelAngleChanged = steeringWheelAngleChanged;
			if (GetSteeringModeByCurrentControllerType() == SteeringMode.LogitechWheel)
			{
				inputSettings.wheelParams.isThrust248AccelParamOverrided = wheelParams.isThrust248AccelParamOverrided;
			}
			break;
		}
		}
		m_customWheelPresetForceSelected = false;
		UploadDefaultPhysicsParamsToActual(GetSteeringModeByCurrentControllerType());
		LoadSetupsRefs(forceButtonsSteeringMode: true);
	}

	private void InitControllerSettingItem()
	{
		if (m_steeringDeviceTypeScheme == null)
		{
			m_steeringDeviceTypeScheme = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_steering_device_type_choose.Localized()
			};
			m_steeringDeviceTypeScheme.list.AddRange(new string[3]
			{
				LocalizationKeys.settings_input_keyboard.Localized() + "/" + LocalizationKeys.settings_input_mouse.Localized(),
				LocalizationKeys.settings_input_gamepad.Localized(),
				LocalizationKeys.bind_wheel.Localized()
			});
			SwitchListSettingsItem steeringDeviceTypeScheme = m_steeringDeviceTypeScheme;
			steeringDeviceTypeScheme.onChangedValue = (System.Action)Delegate.Combine(steeringDeviceTypeScheme.onChangedValue, new System.Action(OnControllerTypeChange));
			SwitchListSettingsItem steeringDeviceTypeScheme2 = m_steeringDeviceTypeScheme;
			steeringDeviceTypeScheme2.onClick = (System.Action)Delegate.Combine(steeringDeviceTypeScheme2.onClick, (System.Action)delegate
			{
				m_currActiveListGroup.value = CategoryGroup.Device;
			});
		}
		m_steeringDeviceTypeScheme.description = LocalizationKeys.settings_input_steering_device_type_choose_description.Localized();
		m_steeringDeviceTypeScheme.index = Array.IndexOf(m_allowedControllerDeviceType, m_currSteeringDeviceType);
	}

	private void InitGeneralItems()
	{
		InitControllerSettingItem();
		if (m_gearboxType == null)
		{
			m_gearboxType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_gearbox_type.Localized()
			};
			m_gearboxType.list.AddRange(new string[2]
			{
				LocalizationKeys.settings_input_gearbox_auto.Localized(),
				LocalizationKeys.settings_input_gearbox_manual.Localized()
			});
			SwitchListSettingsItem gearboxType = m_gearboxType;
			gearboxType.onChangedValue = (System.Action)Delegate.Combine(gearboxType.onChangedValue, new System.Action(OnAutomaticGearboxChange));
		}
		m_gearboxType.index = ((!inputSettings.automaticGearboxEnabled) ? 1 : 0);
		if (m_clutchType == null)
		{
			m_clutchType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_clutch_type.Localized()
			};
			m_clutchType.list.AddRange(new string[2]
			{
				LocalizationKeys.settings_input_clutch_auto.Localized(),
				LocalizationKeys.settings_input_clutch_manual.Localized()
			});
			SwitchListSettingsItem clutchType = m_clutchType;
			clutchType.onChangedValue = (System.Action)Delegate.Combine(clutchType.onChangedValue, new System.Action(OnAutomaticClutchChange));
		}
		if (inputSettings.automaticGearboxEnabled)
		{
			m_clutchType.index = 0;
			inputSettings.automaticClutchEnabled = true;
			m_clutchType.SetBlockToEditing(value: true);
		}
		else
		{
			m_clutchType.index = ((!inputSettings.automaticClutchEnabled) ? 1 : 0);
		}
		if (m_holdIgnition == null)
		{
			m_holdIgnition = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_ignition_type.Localized()
			};
			m_holdIgnition.list.AddRange(new string[2]
			{
				LocalizationKeys.settings_input_ignition_simplified.Localized(),
				LocalizationKeys.settings_input_ignition_cockpit.Localized()
			});
			SwitchListSettingsItem holdIgnition = m_holdIgnition;
			holdIgnition.onChangedValue = (System.Action)Delegate.Combine(holdIgnition.onChangedValue, new System.Action(OnHoldIgnitionChange));
		}
		m_holdIgnition.index = (inputSettings.holdIgnition ? 1 : 0);
		if (m_steeringLinearity == null)
		{
			m_steeringLinearity = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_wheelLinearity.Localized()
			};
			TrackListSettingsItem steeringLinearity = m_steeringLinearity;
			steeringLinearity.onChangedValue = (System.Action)Delegate.Combine(steeringLinearity.onChangedValue, new System.Action(OnLinearityChange));
		}
		m_steeringLinearity.value = inputSettings.steeringLinearity;
		if (m_accLinearity == null)
		{
			m_accLinearity = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_accLinearity.Localized()
			};
			TrackListSettingsItem accLinearity = m_accLinearity;
			accLinearity.onChangedValue = (System.Action)Delegate.Combine(accLinearity.onChangedValue, new System.Action(OnLinearityChange));
		}
		m_accLinearity.value = inputSettings.accLinearity;
		if (m_breakLinearity == null)
		{
			m_breakLinearity = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_brakeLinearity.Localized()
			};
			TrackListSettingsItem breakLinearity = m_breakLinearity;
			breakLinearity.onChangedValue = (System.Action)Delegate.Combine(breakLinearity.onChangedValue, new System.Action(OnLinearityChange));
		}
		m_breakLinearity.value = inputSettings.brakeLinearity;
		if (m_handbrakeLinearity == null)
		{
			m_handbrakeLinearity = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_handbrakeLinearity.Localized()
			};
			TrackListSettingsItem handbrakeLinearity = m_handbrakeLinearity;
			handbrakeLinearity.onChangedValue = (System.Action)Delegate.Combine(handbrakeLinearity.onChangedValue, new System.Action(OnLinearityChange));
		}
		m_handbrakeLinearity.value = inputSettings.handbrakeLinearity;
		if (m_clutchLinearity == null)
		{
			m_clutchLinearity = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_clutchLinearity.Localized()
			};
			TrackListSettingsItem clutchLinearity = m_clutchLinearity;
			clutchLinearity.onChangedValue = (System.Action)Delegate.Combine(clutchLinearity.onChangedValue, new System.Action(OnLinearityChange));
		}
		m_clutchLinearity.value = inputSettings.clutchLinearity;
		if (m_steeringDeadzone == null)
		{
			m_steeringDeadzone = new TrackListSettingsItem(0f, 1f, 0.1f, "{0:F1}")
			{
				title = LocalizationKeys.settings_input_wheeldeadzone.Localized()
			};
			TrackListSettingsItem steeringDeadzone = m_steeringDeadzone;
			steeringDeadzone.onChangedValue = (System.Action)Delegate.Combine(steeringDeadzone.onChangedValue, new System.Action(OnSteeringDeadzoneChange));
		}
		m_steeringDeadzone.value = inputSettings.steeringDeadZone;
		if (m_steeringAssist == null)
		{
			m_steeringAssist = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_controls_st_assist.Localized()
			};
			ToggleListSettingsItem steeringAssist = m_steeringAssist;
			steeringAssist.onChangedValue = (System.Action)Delegate.Combine(steeringAssist.onChangedValue, new System.Action(OnSteerAssistentChange));
		}
		GameInput.DeviceType currSteeringDeviceType = m_currSteeringDeviceType;
		GlobalModel.SteeringAssist steeringAssist2 = default(GlobalModel.SteeringAssist);
		switch (currSteeringDeviceType)
		{
		case GameInput.DeviceType.Keyboard:
			steeringAssist2 = m_prefs.globals.steeringAssistentKeyboard;
			break;
		case GameInput.DeviceType.Gamepad:
			steeringAssist2 = m_prefs.globals.steeringAssistentGamepad;
			break;
		case GameInput.DeviceType.Wheel:
			steeringAssist2 = m_prefs.globals.steeringAssistentWheel;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(currSteeringDeviceType);
			break;
		}
		GlobalModel.SteeringAssist steeringAssist3 = steeringAssist2;
		m_steeringAssist.value = steeringAssist3 == GlobalModel.SteeringAssist.AssistV2;
		if (m_steeringAngle == null)
		{
			m_steeringAngle = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_game_steer_w_angle.Localized()
			};
			m_steerAngleValue = (ESteeringWheelAngle[])Enum.GetValues(typeof(ESteeringWheelAngle));
			ESteeringWheelAngle[] steerAngleValue = m_steerAngleValue;
			foreach (ESteeringWheelAngle eSteeringWheelAngle in steerAngleValue)
			{
				m_steeringAngle.list.Add((int)eSteeringWheelAngle);
			}
			SwitchListSettingsItem steeringAngle = m_steeringAngle;
			steeringAngle.onChangedValue = (System.Action)Delegate.Combine(steeringAngle.onChangedValue, new System.Action(OnChangeSteeringAngle));
		}
		for (int j = 0; j < m_steerAngleValue.Length; j++)
		{
			if (m_steerAngleValue[j] == inputSettings.wheelParams.steeringWheelAngle)
			{
				m_steeringAngle.index = j;
				OnChangeSteeringAngle();
				break;
			}
		}
	}

	private void InitGamepadItems()
	{
		if (m_vibration == null)
		{
			string title = LocalizationKeys.settings_input_vibration_xbox.Localized();
			m_vibration = new ToggleListSettingsItem
			{
				title = title
			};
			ToggleListSettingsItem vibration = m_vibration;
			vibration.onChangedValue = (System.Action)Delegate.Combine(vibration.onChangedValue, new System.Action(OnVibrationChange));
		}
		m_vibration.value = inputSettings.gamepadParams.vibrationEnabled;
	}

	private void InitWheelItems()
	{
		if (m_presetType == null)
		{
			m_presetType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_input_preset_type.Localized()
			};
			m_presetType.list.AddRange(new string[3]
			{
				LocalizationKeys.settings_input_preset_custom.Localized(),
				LocalizationKeys.settings_input_preset_beginner.Localized(),
				LocalizationKeys.settings_input_preset_pro.Localized()
			});
			SwitchListSettingsItem presetType = m_presetType;
			presetType.onChangedValue = (System.Action)Delegate.Combine(presetType.onChangedValue, new System.Action(OnWheelPresetChange));
		}
		m_presetType.index = (int)inputSettings.currentWheelPresetType;
		if (m_feedback == null)
		{
			m_feedback = new TrackListSettingsItem(0f, 1f, 0.01f, "{0:F2}")
			{
				title = LocalizationKeys.settings_input_feedback.Localized()
			};
			TrackListSettingsItem feedback = m_feedback;
			feedback.onChangedValue = (System.Action)Delegate.Combine(feedback.onChangedValue, new System.Action(OnForceFeedBackChange));
		}
		m_feedback.value = inputSettings.wheelParams.forceFeedback;
	}

	public void NavCancel()
	{
		if (m_currSteeringDeviceType != GameInput.DeviceType.Wheel || WheelsOrPedalsContainsBindedAccelAction())
		{
			ProcessExit();
		}
	}

	private bool WheelsOrPedalsContainsBindedAccelAction()
	{
		if (m_devicesList.Find((SettingsDeviceItemViewModel deviceItem) => deviceItem.deviceMeta.deviceType == GameInput.DeviceType.Wheel) != null && GameInput.InputManager.instance.GetActionInfo(ActionType.Accelerate).activeBinds.Find((BindElement bindItem) => bindItem.deviceInfo.deviceType == GameInput.DeviceType.Wheel || bindItem.deviceInfo.deviceType == GameInput.DeviceType.Pedals) == null)
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.settings_info_empty_accel_bind_with_wheel_connected.Localized(), ProcessExit, LocalizationKeys.Exit.Localized(), null, LocalizationKeys.Cancel.Localized());
			return false;
		}
		return true;
	}

	private void ProcessExit()
	{
		Hide();
		if (States.mainCurrent is GarageGUIState garageGUIState)
		{
			UIMainMenuContext uIMainMenuContext = garageGUIState.contextOwner.Get<UIMainMenuContext>();
			if (uIMainMenuContext != null)
			{
				uIMainMenuContext.OnSwitchToSettings();
			}
		}
	}

	public void NavSelect()
	{
		if (m_currActiveListGroup.value != CategoryGroup.AdvancedSettings)
		{
			m_devicesList[m_devicesList.selectedIndex].OnMissClick();
		}
	}

	private void ForceSetDevicesEnableFFB(GameInput.DeviceType specificDeviceType, bool enable)
	{
		if (DependencyInjector.Resolve<WheelForceFeedbackSupport>() == null)
		{
			return;
		}
		GameInput.InputManager instance = GameInput.InputManager.instance;
		int count = m_devicesList.GetCount();
		for (int i = 0; i < count; i++)
		{
			SettingsDeviceItemViewModel settingsDeviceItemViewModel = m_devicesList[i];
			if (settingsDeviceItemViewModel.deviceMeta.deviceType == specificDeviceType)
			{
				instance.SetDeviceDisableFFB(settingsDeviceItemViewModel.deviceMeta, !enable);
			}
		}
		ApplyFFBChanges();
	}

	private void ApplyFFBChanges()
	{
		m_devicesList.UpdateList();
		GameInput.InputManager.instance?.Save();
	}

	[GameHotkey(ActionType.NavNextPage, false, false)]
	public void OnSwitchNextPage()
	{
		switcherActions.OnNextPage();
	}

	[GameHotkey(ActionType.NavPrevPage, false, false)]
	public void OnSwitchPrevPage()
	{
		switcherActions.OnPrevPage();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigateListDown()
	{
		switch (m_currActiveListGroup.value)
		{
		case CategoryGroup.Device:
			if (!(m_devicesList == null))
			{
				if (m_devicesList.selectedIndex + 1 >= m_devicesList.GetCount())
				{
					m_devicesList.selectedIndex = m_devicesList.GetCount() - 1;
				}
				else
				{
					m_devicesList.selectedIndex++;
				}
			}
			break;
		case CategoryGroup.AdvancedSettings:
			NavigateAdvancedDown();
			break;
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigateListUp()
	{
		switch (m_currActiveListGroup.value)
		{
		case CategoryGroup.Device:
			if (m_devicesList == null)
			{
				break;
			}
			if (m_devicesList.selectedIndex - 1 < 0)
			{
				if (m_listGroupVisibilityParam.value == 0)
				{
					m_currActiveListGroup.value = CategoryGroup.AdvancedSettings;
				}
				else
				{
					m_devicesList.selectedIndex = 0;
				}
			}
			else
			{
				m_devicesList.selectedIndex--;
			}
			break;
		case CategoryGroup.AdvancedSettings:
			NavigateAdvancedUp();
			break;
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigateLeft()
	{
		switch (m_currActiveListGroup.value)
		{
		case CategoryGroup.Device:
			m_devicesList.GetSelectedItem()?.PrevDeviceTypeValue();
			break;
		case CategoryGroup.AdvancedSettings:
			NavigateAdvancedLeft();
			break;
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigateRight()
	{
		switch (m_currActiveListGroup.value)
		{
		case CategoryGroup.Device:
			m_devicesList.GetSelectedItem()?.NextDeviceTypeValue();
			break;
		case CategoryGroup.AdvancedSettings:
			NavigateAdvancedRight();
			break;
		}
	}

	private void NavigateAdvancedDown()
	{
		if (!(advancedSettingsList == null))
		{
			if (advancedSettingsList.GetCount() == 0)
			{
				advancedSettingsList.selectedIndex = -1;
			}
			else if (advancedSettingsList.selectedIndex + 1 >= advancedSettingsList.GetCount() && m_listGroupVisibilityParam.value == 0)
			{
				m_currActiveListGroup.value = CategoryGroup.Device;
			}
			else
			{
				advancedSettingsList.selectedIndex++;
			}
		}
	}

	private void NavigateAdvancedUp()
	{
		if (advancedSettingsList == null)
		{
			return;
		}
		if (advancedSettingsList.GetCount() == 0)
		{
			advancedSettingsList.selectedIndex = -1;
			return;
		}
		advancedSettingsList.selectedIndex--;
		if (advancedSettingsList.selectedIndex < 0)
		{
			advancedSettingsList.selectedIndex = 0;
		}
	}

	private void NavigateAdvancedLeft()
	{
		if (!(advancedSettingsList == null))
		{
			if (advancedSettingsList.selectedIndex > -1)
			{
				advancedSettingsList.items[advancedSettingsList.selectedIndex].PrevValue();
			}
			advancedSettingsList.UpdateList();
		}
	}

	private void NavigateAdvancedRight()
	{
		if (!(advancedSettingsList == null))
		{
			if (advancedSettingsList.selectedIndex > -1)
			{
				advancedSettingsList.items[advancedSettingsList.selectedIndex].NextValue();
			}
			advancedSettingsList.UpdateList();
		}
	}
}
