using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Binding;
using Binding.Base;
using Chat;
using GameInput;
using Rewired;
using Rewired.ControllerExtensions;
using UnityEngine;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
public class UIInputBindsSettingsContext : BaseBindingBehaviourTarget
{
	private readonly StringProperty m_deviceName = new StringProperty();

	private readonly BoolProperty m_standaloneBuild = new BoolProperty();

	private readonly BoolProperty m_xboxBuild = new BoolProperty();

	private readonly StringProperty m_accelerometerDescription = new StringProperty();

	private readonly FloatProperty m_steer = new FloatProperty();

	private readonly FloatProperty m_steerLeftSideFill = new FloatProperty();

	private readonly FloatProperty m_steerRightSideFill = new FloatProperty();

	private readonly FloatProperty m_clutch = new FloatProperty();

	private readonly FloatProperty m_brake = new FloatProperty();

	private readonly FloatProperty m_accelerate = new FloatProperty();

	private readonly FloatProperty m_handBrake = new FloatProperty();

	private readonly BoolProperty m_needShowButtonsContainer = new BoolProperty();

	private Controller m_controller;

	[SerializeField]
	private SettingsInputList m_settingsInputList;

	[SerializeField]
	private AxisInputList m_axisList;

	[SerializeField]
	private List<ButtonInputList> m_buttonColumns;

	private ButtonInputList m_selectedButtonColumn;

	private NavigateListType m_currActiveNavigationListType;

	private NavigateListType m_prevActiveContainer = NavigateListType.Axis;

	private List<GameInput.Action> m_actions;

	private InputSettingsListItem m_LastListItemToBind;

	private BindElement m_lastDup;

	private AxisListItem m_lastAxisItem;

	private AxisListItem m_horizontalMouseItem;

	private AxisListItem m_verticalMouseItem;

	private List<ActionHintData> m_actionHintDataList = new List<ActionHintData>();

	private int? m_cachedSelectedBindItemIndex;

	private int? m_cachedSelectedAxisListItemIndex;

	private int? m_cachedSelectedButtonItemIndex;

	private float m_lastDupTime;

	private bool m_isWaitReconnectDevice;

	private bool m_resetControlsDialog;

	private bool m_axisHasBind;

	private WaitForEndOfFrame m_waitForEndOfFrame = new WaitForEndOfFrame();

	private List<ActionType> m_chatActions = new List<ActionType>
	{
		ActionType.OpenTextChat,
		ActionType.OpenTextChatReserve
	};

	private readonly Dictionary<ActionType, ActionType> m_priorityInputSettingsList = new Dictionary<ActionType, ActionType>
	{
		{
			ActionType.Gear6,
			ActionType.Gear7
		},
		{
			ActionType.HideMouse,
			ActionType.ShowInputBar
		}
	};

	private readonly HashSet<ActionType> m_skipValueInputSettingsList = new HashSet<ActionType>();

	public GameInput.InputManager input { get; } = GameInput.InputManager.instance;

	public Controller controller => m_controller;

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		m_currActiveNavigationListType = NavigateListType.Binds;
		if (m_settingsInputList != null && m_settingsInputList.GetCount() > 0)
		{
			m_settingsInputList.selectedIndex = 0;
		}
		m_prevActiveContainer = NavigateListType.Axis;
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.settings_category_steer_title.Localized());
		UpdateBottomPanel();
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		m_xboxBuild.value = false;
		m_standaloneBuild.value = true;
		m_accelerometerDescription.value = LocalizationKeys.input_settings_accelerometer_description.Localized();
	}

	private void InitButtonsListByControllerMeta(Controller controller)
	{
		foreach (ButtonInputList buttonColumn in m_buttonColumns)
		{
			buttonColumn.Clear();
		}
		if (controller.GetMetaInfo() == null)
		{
			return;
		}
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if (instance == null)
		{
			return;
		}
		List<GameInput.InputManager.ControllerElementDescriptor> controllerElementsType = instance.GetControllerElementsType(controller, ControllerElementType.Button);
		if (controllerElementsType == null)
		{
			return;
		}
		if (controllerElementsType.Count <= 0)
		{
			m_needShowButtonsContainer.value = false;
			return;
		}
		m_needShowButtonsContainer.value = true;
		int num = 0;
		int i = 0;
		foreach (GameInput.InputManager.ControllerElementDescriptor item2 in controllerElementsType)
		{
			num++;
			if (item2.element is Controller.Button button)
			{
				m_buttonColumns[i].AddItem(new ButtonListItem(this, num.ToString(), item2.name, button, controller));
				i++;
				if (i > m_buttonColumns.Count - 1)
				{
					i = 0;
				}
			}
		}
		for (; i != 0 && i < m_buttonColumns.Count; i++)
		{
			ButtonListItem item = new ButtonListItem(this, null, null, null, controller, needHide: true);
			m_buttonColumns[i].AddItem(item);
		}
		TimeController.StartNow(BuildNavigationList(NavigateListType.Buttons));
	}

	private void InitAxisListByControllerMeta(Controller controller)
	{
		m_axisList.Clear();
		if (controller.GetMetaInfo() == null)
		{
			return;
		}
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if (instance == null)
		{
			return;
		}
		List<GameInput.InputManager.ControllerElementDescriptor> list = new List<GameInput.InputManager.ControllerElementDescriptor>();
		if (controller.type == ControllerType.Keyboard)
		{
			controller = instance.activeControllers.Keys.ToList().Find((Controller controller2) => controller2.type == ControllerType.Mouse);
			if (controller == null)
			{
				return;
			}
		}
		list = instance.GetControllerElementsType(controller, ControllerElementType.Axis);
		if (list == null)
		{
			return;
		}
		int num = 0;
		foreach (GameInput.InputManager.ControllerElementDescriptor item in list)
		{
			num++;
			if (!(item.element is Controller.Axis axis))
			{
				continue;
			}
			AxisListItem axisListItem = new AxisListItem(this, num.ToString(), item.name, axis, controller);
			if (controller.type == ControllerType.Mouse)
			{
				if (item.name.ToLower().Equals("mouse horizontal"))
				{
					m_horizontalMouseItem = axisListItem;
				}
				else if (item.name.ToLower().Equals("mouse vertical"))
				{
					m_verticalMouseItem = axisListItem;
				}
			}
			m_axisList.AddItem(axisListItem);
		}
		TimeController.StartNow(BuildNavigationList(NavigateListType.Axis));
	}

	private IEnumerator BuildNavigationList(NavigateListType targetList)
	{
		yield return m_waitForEndOfFrame;
		switch (targetList)
		{
		case NavigateListType.Binds:
			BuildBindsList();
			break;
		case NavigateListType.Axis:
			m_axisList.RecalcList();
			m_axisList.ScrollTo(0);
			break;
		case NavigateListType.Buttons:
		{
			foreach (ButtonInputList buttonColumn in m_buttonColumns)
			{
				buttonColumn.RecalcList();
				buttonColumn.ScrollTo(0);
			}
			break;
		}
		}
	}

	private void Update()
	{
		UpdateAxisItemsListInput();
		UpdateButtonsItemsListInput();
		UpdateInputPanel();
	}

	private void UpdateAxisItemsListInput()
	{
		foreach (AxisListItem item in m_axisList.items)
		{
			if (item.axis == null)
			{
				continue;
			}
			float num = 0f;
			if (item.device.type == ControllerType.Mouse)
			{
				HandleMouseAxisItemSliderValues(item);
				num = item.currentNormalizedAxis;
			}
			else
			{
				if (item.title == "Gyroscope Horizontal Axis")
				{
					item.currentNormalizedAxis = GameInput.InputManager.instance.lastController.TryGetDS4AccelerationX();
				}
				else
				{
					item.currentNormalizedAxis = item.axis.value;
				}
				num = item.currentNormalizedAxis * 1000f;
			}
			item.currentAxis = (int)MathTools.Round(num, 0.10000000149011612, MathTools.RoundMode.HalfUp);
		}
		m_axisList.UpdateList();
	}

	private void UpdateButtonsItemsListInput()
	{
		foreach (ButtonInputList buttonColumn in m_buttonColumns)
		{
			foreach (ButtonListItem item in buttonColumn.items)
			{
				if (item.button != null)
				{
					item.isPressed = item.button.pressure > 0f;
				}
			}
			buttonColumn.UpdateList();
		}
	}

	private void UpdateInputPanel()
	{
		m_steer.value = input.GetActionValue(ActionType.Turn);
		m_steerLeftSideFill.value = 0f - Mathf.Min(0f, m_steer.value);
		m_steerRightSideFill.value = Mathf.Max(0f, m_steer.value);
		m_clutch.value = input.GetActionValue(ActionType.Clutch);
		m_brake.value = input.GetActionValue(ActionType.Brake);
		m_accelerate.value = input.GetActionValue(ActionType.Accelerate);
		m_handBrake.value = input.GetActionValue(ActionType.HandBrake);
	}

	private void HandleMouseAxisItemSliderValues(AxisListItem axisItem)
	{
		float num = (float)Screen.width / 2f;
		float num2 = (float)Screen.height / 2f;
		if (axisItem == m_horizontalMouseItem)
		{
			axisItem.currentNormalizedAxis = Mathf.Clamp(ReInput.controllers.Mouse.screenPosition.x - num, 0f - num, num);
		}
		else if (axisItem == m_verticalMouseItem)
		{
			axisItem.currentNormalizedAxis = Mathf.Clamp(ReInput.controllers.Mouse.screenPosition.y - num2, 0f - num2, num2);
		}
		else
		{
			axisItem.currentNormalizedAxis = axisItem.axis.value * 100f;
		}
	}

	private void EngineStartBindsHandler()
	{
		if (m_settingsInputList == null)
		{
			return;
		}
		GameInput.InputManager.TryShowBindStarterIgnitionMsg(needShow: false, 10f, controller, delegate(ActionType targetAction, bool isNeedShowMessage)
		{
			if (isNeedShowMessage)
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.empty_bind_ignition_starter.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
				{
					TimeController.StartNow(ScrollToActionTypeCoroutine(targetAction));
				}, LocalizationKeys.nav_hint_input_settings_bind.Localized());
			}
		});
	}

	protected override void OnDeactivate()
	{
		foreach (ButtonInputList buttonColumn in m_buttonColumns)
		{
			buttonColumn.ClearSelection();
		}
		m_selectedButtonColumn = null;
		m_axisList.ClearSelection();
		m_settingsInputList.ClearSelection();
		GameInput.InputManager.instance.Save();
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		base.OnDeactivate();
	}

	private IEnumerator ScrollToActionTypeCoroutine(ActionType actionType)
	{
		yield return m_waitForEndOfFrame;
		int count = m_settingsInputList.GetCount();
		for (int i = 0; i < count; i++)
		{
			if (m_settingsInputList[i].action.actionType == actionType)
			{
				m_settingsInputList.selectedIndex = i;
				break;
			}
		}
		m_settingsInputList.ScrollTo(m_settingsInputList.selectedIndex);
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		if (m_controller.id == args.controllerId)
		{
			IDualShock4Extension extension = m_controller.GetExtension<IDualShock4Extension>();
			string text = args.name;
			if (extension != null)
			{
				text = LocalizationKeys.dualshock_gamepad_name.Localized();
			}
			if (m_controller.GetExtension<DualSenseExtension>() != null)
			{
				text = LocalizationKeys.ps5_dualsense_gamepad_name.Localized();
			}
			if (m_resetControlsDialog)
			{
				base.owner.Hide<UIBaseDialogContext>();
			}
			base.owner.Hide<UIPollingInputDialogContext>();
			UIMessageBox.ShowDialog(LocalizationKeys.input_settings_device_disconnect.LocalizedFormat(text), delegate
			{
				m_isWaitReconnectDevice = false;
				Hide();
			}, new DialogButtonData[1]
			{
				new DialogButtonData
				{
					text = LocalizationKeys.Ok.Localized(),
					action = delegate
					{
						m_isWaitReconnectDevice = false;
						Hide();
					}
				}
			});
			m_isWaitReconnectDevice = true;
		}
	}

	private void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		if (m_isWaitReconnectDevice && m_controller.id == args.controllerId)
		{
			UIMessageBox.HideBaseDialog();
		}
	}

	private bool CanAddChatAction(GameInput.Action action)
	{
		if (action == null)
		{
			return false;
		}
		if (!m_chatActions.Contains(action.actionType))
		{
			return true;
		}
		return ChatOverlay.IsInitialized();
	}

	private void BuildBindsList()
	{
		if (m_settingsInputList == null)
		{
			return;
		}
		m_settingsInputList.Clear();
		if (m_actions == null)
		{
			m_actions = (from x in input.GetActions()
				where !GameInput.Action.IsSystemAction(x.actionType) && x.actionType != ActionType.ErrorAction
				select x).ToList();
		}
		foreach (GameInput.Action action in m_actions)
		{
			if (!CanAddChatAction(action))
			{
				continue;
			}
			if (action.valueType == ActionValueType.Full)
			{
				m_settingsInputList.AddItem(new InputSettingsListItem(this, action, action.actionType.ToString() + "L", ButtonDisplayMode.EXCLUDE_POSITIVE, !m_standaloneBuild.value));
				m_settingsInputList.AddItem(new InputSettingsListItem(this, action, action.actionType.ToString() + "R", ButtonDisplayMode.EXCLUDE_NEGATIVE, !m_standaloneBuild.value));
			}
			else
			{
				if (action.actionType == ActionType.VoiceChat || m_skipValueInputSettingsList.Contains(action.actionType))
				{
					continue;
				}
				if (m_priorityInputSettingsList.TryGetValue(action.actionType, out var next))
				{
					int index = m_actions.FindIndex((GameInput.Action b) => b.actionType == next);
					m_settingsInputList.AddItem(new InputSettingsListItem(this, action, action.actionType.ToString(), ButtonDisplayMode.ALL, !m_standaloneBuild.value));
					m_settingsInputList.AddItem(new InputSettingsListItem(this, m_actions[index], m_actions[index].actionType.ToString(), ButtonDisplayMode.ALL, !m_standaloneBuild.value));
					m_skipValueInputSettingsList.Add(m_actions[index].actionType);
				}
				else
				{
					m_settingsInputList.AddItem(new InputSettingsListItem(this, action, action.actionType.ToString(), ButtonDisplayMode.ALL, !m_standaloneBuild.value));
				}
			}
		}
		m_skipValueInputSettingsList.Clear();
		m_settingsInputList.RecalcList();
		m_settingsInputList.selectedIndex = 0;
		m_settingsInputList.ScrollTo(0);
		UpdateBottomPanel();
	}

	private void UpdateBottomPanel()
	{
		m_actionHintDataList.Clear();
		m_actionHintDataList.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = NavCancel
		});
		switch (m_currActiveNavigationListType)
		{
		case NavigateListType.Binds:
			BuildBottomPanelForBindsList();
			break;
		case NavigateListType.Axis:
			BuildBottomPanelForAvailableControllerElementsList(m_axisList);
			break;
		case NavigateListType.Buttons:
			BuildBottomPanelForAvailableControllerElementsList(m_selectedButtonColumn);
			break;
		}
		ShowBottomPanel(m_actionHintDataList.ToArray());
	}

	private void BuildBottomPanelForAvailableControllerElementsList<T>(IListView<T> listViewItems) where T : BaseListItem
	{
		if (listViewItems.GetCount() == 0)
		{
			return;
		}
		T val = listViewItems.GetSelectedItem();
		if (val == null)
		{
			return;
		}
		if (GetInputSettingsListItemByItem(val) != null)
		{
			m_actionHintDataList.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = NavSelect
			});
			return;
		}
		AxisListItem axisListItem = val as AxisListItem;
		if (axisListItem != null)
		{
			InputSettingsListItem settingsListItemToBind = m_settingsInputList.GetItem(m_cachedSelectedBindItemIndex.GetValueOrDefault());
			m_actionHintDataList.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_bind.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					ProcessBindingAxisItem(axisListItem, settingsListItemToBind);
				}
			});
		}
	}

	private void BuildBottomPanelForBindsList()
	{
		if (m_settingsInputList.GetCount() == 0)
		{
			return;
		}
		m_actionHintDataList.AddRange(new ActionHintData[3]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_advanced_reset.Localized(),
				type = ActionType.NavDPadLeft,
				action = NavReset
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_bind.Localized(),
				type = ActionType.NavSelectBtnA,
				action = NavSelect
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_remove_bind.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = delegate
				{
					RemoveBind(m_settingsInputList.GetSelectedItem());
				}
			}
		});
		int selectedIndex = m_settingsInputList.selectedIndex;
		if (selectedIndex >= 0 && selectedIndex < m_settingsInputList.GetCount() - 1 && m_settingsInputList[selectedIndex].bind?.elementAxis != null)
		{
			m_actionHintDataList.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_calibration.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = NavCalibration
			});
		}
		if (m_prevActiveContainer == NavigateListType.Axis && m_axisList.GetCount() > 0)
		{
			m_actionHintDataList.Add(new ActionHintData
			{
				hint = LocalizationKeys.allowed_axises.Localized(),
				type = ActionType.NavDPadRight,
				action = delegate
				{
					ChangeNavInputTo(NavigateListType.Axis);
				}
			});
		}
		if (m_prevActiveContainer == NavigateListType.Buttons && m_buttonColumns.Count > 0)
		{
			m_actionHintDataList.Add(new ActionHintData
			{
				hint = LocalizationKeys.allowed_buttons.Localized(),
				type = ActionType.NavDPadRight,
				action = delegate
				{
					ChangeNavInputTo(NavigateListType.Buttons);
				}
			});
		}
	}

	public void BuildSettingsList(Controller newController)
	{
		if (newController == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "Controller for settings is empty!");
			return;
		}
		m_controller = newController;
		m_deviceName.value = m_controller.Name();
		if (m_controller.GetExtension<IDualShock4Extension>() != null)
		{
			m_deviceName.value = LocalizationKeys.dualshock_gamepad_name.Localized();
		}
		if (m_controller.GetExtension<DualSenseExtension>() != null)
		{
			m_deviceName.value = LocalizationKeys.ps5_dualsense_gamepad_name.Localized();
		}
		if (m_standaloneBuild.value)
		{
			InitAxisListByControllerMeta(newController);
			if (m_controller.type != ControllerType.Keyboard)
			{
				InitButtonsListByControllerMeta(newController);
			}
			else
			{
				foreach (ButtonInputList buttonColumn in m_buttonColumns)
				{
					buttonColumn.Clear();
				}
				m_needShowButtonsContainer.value = false;
			}
		}
		EngineStartBindsHandler();
		TimeController.StartNow(BuildNavigationList(NavigateListType.Binds));
	}

	public void OnClickSettingsItem()
	{
		if (m_currActiveNavigationListType != NavigateListType.Binds)
		{
			m_cachedSelectedBindItemIndex = null;
			ChangeNavInputTo(NavigateListType.Binds);
		}
		else
		{
			UpdateBottomPanel();
		}
	}

	private List<InputSettingsListItem> GetInputSettingsListItemsActionType(ActionType actionType)
	{
		return m_settingsInputList.FindAll((InputSettingsListItem item) => item.action.actionType == actionType);
	}

	public BindState TryBind(InputSettingsListItem listItem, ControllerPollingInfo pollInfo)
	{
		if (listItem == null)
		{
			return BindState.Failed;
		}
		if (!IsControllerBindAllowed(pollInfo.elementIdentifierName))
		{
			return BindState.CantBind;
		}
		BindElement bindElement = listItem.bind?.baseElement;
		bool num = bindElement != null && bindElement.IsBindElementName(pollInfo.elementIdentifierName) && bindElement.deviceInfo != null && bindElement.deviceInfo.rewiredType == pollInfo.controllerType;
		bool flag = false;
		if (num)
		{
			input.RemoveElement(pollInfo.controller, pollInfo.elementType, listItem.action.actionType, pollInfo.elementIdentifierName);
			PollItem(null);
			flag = true;
		}
		else
		{
			if (pollInfo.elementType == ControllerElementType.Button)
			{
				if (bindElement != null && bindElement.deviceInfo != null && ((bindElement.deviceInfo.rewiredType == ControllerType.Keyboard && pollInfo.controllerType == ControllerType.Mouse) || (bindElement.deviceInfo.rewiredType == ControllerType.Mouse && pollInfo.controllerType == ControllerType.Keyboard)))
				{
					input.RemoveElement(bindElement.deviceInfo.rewiredController, ControllerElementType.Button, bindElement.bindedAction, bindElement.elementName);
					PollItem(null);
				}
				BindButtonType bindButtonType = ((!listItem.IsNegative()) ? BindButtonType.Positive : BindButtonType.Negative);
				BindButtonElement bindButtonElement = input.GetBind(pollInfo.controller, pollInfo.elementType, pollInfo.elementIdentifierName, listItem.IsNegative()) as BindButtonElement;
				bool num2 = bindButtonElement != null && (bindButtonElement.bindedAction != listItem.action.actionType || bindButtonElement.bindType != bindButtonType);
				bool flag2 = m_LastListItemToBind == listItem && bindButtonElement == m_lastDup && Time.unscaledTime > m_lastDupTime + 0.1f && Time.unscaledTime < m_lastDupTime + 3f;
				if (num2 && !flag2)
				{
					m_lastDupTime = Time.unscaledTime;
					m_lastDup = bindButtonElement;
					m_LastListItemToBind = listItem;
					return BindState.Busy;
				}
				input.SetBind(pollInfo.controller, pollInfo.elementType, listItem.action, pollInfo.elementIdentifierName, listItem.IsNegative());
				PollItem(null);
				flag = true;
			}
			if (pollInfo.elementType == ControllerElementType.Axis)
			{
				BindElement bind = input.GetBind(pollInfo.controller, pollInfo.elementType, pollInfo.elementIdentifierName, listItem.IsNegative());
				bool flag3 = bind != null && bind.bindedAction != listItem.action.actionType;
				bool flag4 = false;
				if (bind != null)
				{
					flag4 = GameInput.Action.IsGearChangeAction(bind.bindedAction) && GameInput.Action.IsGearChangeAction(listItem.action.actionType);
					foreach (GameInput.Action action in m_actions)
					{
						if (flag3)
						{
							break;
						}
						foreach (BindElement activeBind in action.activeBinds)
						{
							if (activeBind.elementId == bind.elementId && activeBind.elementName == bind.elementName)
							{
								flag3 = true;
								break;
							}
						}
					}
				}
				bool flag5 = m_LastListItemToBind == listItem && bind == m_lastDup && Time.unscaledTime > m_lastDupTime + 0.1f && Time.unscaledTime < m_lastDupTime + 3f;
				if (flag3 && !flag5 && !flag4)
				{
					m_lastDupTime = Time.unscaledTime;
					m_lastDup = bind;
					m_LastListItemToBind = listItem;
					return BindState.Busy;
				}
				input.SetBind(pollInfo.controller, pollInfo.elementType, listItem.action, pollInfo.elementIdentifierName, listItem.IsNegative());
				if (pollInfo.controllerType == ControllerType.Mouse)
				{
					TryRemoveAndBindOppositeAction(listItem, pollInfo);
				}
				flag = true;
			}
		}
		if (flag)
		{
			UpdateItems();
			return BindState.Success;
		}
		return BindState.Failed;
	}

	private static bool IsControllerBindAllowed(string button)
	{
		switch (button)
		{
		case "Start":
		case "Menu":
		case "Option":
		case "Options":
		case "Options Button":
		case "OPTIONS button":
		case "+ Button":
		case "AltGr":
			return false;
		default:
			return true;
		}
	}

	private void TryRemoveAndBindOppositeAction(InputSettingsListItem listItem, ControllerPollingInfo pollingInfo)
	{
		bool num = listItem.action.actionType == ActionType.GearUp;
		bool flag = listItem.action.actionType == ActionType.GearDown;
		ActionType actionType = ActionType.ErrorAction;
		if (num || flag)
		{
			actionType = (flag ? ActionType.GearUp : ActionType.GearDown);
		}
		else if (listItem.action.actionType == ActionType.Turn)
		{
			ClearAllSettingsListItemsContainActionType(ActionType.Turn);
		}
		if (actionType == ActionType.ErrorAction)
		{
			return;
		}
		input.RemoveElement(pollingInfo.controller, pollingInfo.elementType, actionType, pollingInfo.elementIdentifierName);
		foreach (InputSettingsListItem item in GetInputSettingsListItemsActionType(actionType))
		{
			input.SetBind(pollingInfo.controller, pollingInfo.elementType, item.action, pollingInfo.elementIdentifierName, item.IsNegative());
		}
	}

	public void PollItem(InputSettingsListItem listItem)
	{
		if (listItem != null)
		{
			ShowPollingDialog(listItem);
		}
	}

	public void UpdateItems()
	{
		foreach (InputSettingsListItem item in m_settingsInputList.items)
		{
			item.UpdateBindsName();
		}
		UpdateList();
		UpdateBottomPanel();
	}

	public void UpdateList()
	{
		m_settingsInputList.UpdateList();
	}

	public void ClearItem(InputSettingsListItem item)
	{
		if (item?.bind != null)
		{
			input.RemoveElement(controller, (item.bind.baseElement is BindButtonElement) ? ControllerElementType.Button : ControllerElementType.Axis, item.action.actionType, item.bind.baseElement.elementName);
			if (controller.type == ControllerType.Keyboard)
			{
				input.RemoveElement(ReInput.controllers.Mouse, (item.bind.baseElement is BindButtonElement) ? ControllerElementType.Button : ControllerElementType.Axis, item.action.actionType, item.bind.baseElement.elementName);
			}
			item.UpdateBindsName();
			UpdateList();
		}
	}

	public void ShowPollingDialog(InputSettingsListItem listItem)
	{
		base.owner.Push<UIPollingInputDialogContext>().SetData(this, listItem);
	}

	public void ShowCalibrationDialog(InputSettingsListItem listItem)
	{
		if (listItem?.bind?.elementAxis != null)
		{
			UICalibrationInputDialogContext uICalibrationInputDialogContext = base.owner.Push<UICalibrationInputDialogContext>();
			uICalibrationInputDialogContext.SetBindData(this, listItem);
			uICalibrationInputDialogContext.onDeactivate = (System.Action)Delegate.Remove(uICalibrationInputDialogContext.onDeactivate, new System.Action(UpdateBottomPanel));
			uICalibrationInputDialogContext.onDeactivate = (System.Action)Delegate.Combine(uICalibrationInputDialogContext.onDeactivate, new System.Action(UpdateBottomPanel));
		}
	}

	public void NavCancel()
	{
		if (m_currActiveNavigationListType != NavigateListType.Binds)
		{
			ChangeNavInputTo(NavigateListType.Binds);
		}
		else
		{
			Hide();
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		if (m_currActiveNavigationListType == NavigateListType.Binds)
		{
			NavigateListType navigateListType = ((m_prevActiveContainer == NavigateListType.Axis) ? NavigateListType.Axis : NavigateListType.Buttons);
			ChangeNavInputTo(navigateListType);
		}
		else
		{
			if (m_currActiveNavigationListType != NavigateListType.Buttons)
			{
				return;
			}
			int num = m_buttonColumns.IndexOf(m_selectedButtonColumn);
			if (num != m_buttonColumns.Count - 1)
			{
				ButtonInputList buttonInputList = m_buttonColumns[num];
				ButtonInputList buttonInputList2 = m_buttonColumns[num + 1];
				ButtonListItem item = buttonInputList2.GetItem(buttonInputList.selectedIndex);
				m_selectedButtonColumn = buttonInputList2;
				if (item == null || item.needHide)
				{
					m_selectedButtonColumn.selectedIndex = buttonInputList.selectedIndex - 1;
				}
				else
				{
					m_selectedButtonColumn.selectedIndex = buttonInputList.selectedIndex;
				}
				buttonInputList.ClearSelection();
				UpdateBottomPanel();
			}
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		if (m_currActiveNavigationListType == NavigateListType.Axis || (m_currActiveNavigationListType == NavigateListType.Buttons && m_buttonColumns.IndexOf(m_selectedButtonColumn) == 0))
		{
			ChangeNavInputTo(NavigateListType.Binds);
		}
		else if (m_currActiveNavigationListType == NavigateListType.Buttons)
		{
			int num = m_buttonColumns.IndexOf(m_selectedButtonColumn);
			if (num == 0)
			{
				ChangeNavInputTo(NavigateListType.Binds);
				return;
			}
			ButtonInputList buttonInputList = m_buttonColumns[num];
			m_selectedButtonColumn = m_buttonColumns[num - 1];
			m_selectedButtonColumn.selectedIndex = buttonInputList.selectedIndex;
			buttonInputList.ClearSelection();
			UpdateBottomPanel();
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		switch (m_currActiveNavigationListType)
		{
		case NavigateListType.Binds:
			if (m_settingsInputList.GetCount() > 0 && m_settingsInputList.selectedIndex > 0)
			{
				m_settingsInputList.selectedIndex--;
			}
			break;
		case NavigateListType.Axis:
			if (m_axisList.GetCount() > 0 && m_axisList.selectedIndex > 0)
			{
				m_axisList.selectedIndex--;
			}
			break;
		case NavigateListType.Buttons:
			if (m_selectedButtonColumn.selectedIndex - 1 < 0)
			{
				ChangeNavInputTo(NavigateListType.Axis);
			}
			else
			{
				m_selectedButtonColumn.selectedIndex--;
			}
			break;
		}
		UpdateBottomPanel();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		switch (m_currActiveNavigationListType)
		{
		case NavigateListType.Binds:
			if (m_settingsInputList.selectedIndex < m_settingsInputList.GetCount() - 1)
			{
				m_settingsInputList.selectedIndex++;
			}
			break;
		case NavigateListType.Axis:
			if (m_axisList.selectedIndex >= m_axisList.GetCount() - 1 && m_needShowButtonsContainer.value)
			{
				ChangeNavInputTo(NavigateListType.Buttons);
			}
			else
			{
				m_axisList.selectedIndex++;
			}
			break;
		case NavigateListType.Buttons:
		{
			ButtonListItem item = m_selectedButtonColumn.GetItem(m_selectedButtonColumn.selectedIndex + 1);
			if (item == null || item.needHide)
			{
				return;
			}
			if (m_selectedButtonColumn.selectedIndex < m_selectedButtonColumn.GetCount() - 1)
			{
				m_selectedButtonColumn.selectedIndex++;
			}
			break;
		}
		}
		UpdateBottomPanel();
	}

	public void NavReset()
	{
		m_resetControlsDialog = true;
		UIMessageBox.ShowQuestion(LocalizationKeys.settings_input_reset_question.Localized(), delegate
		{
			m_resetControlsDialog = false;
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			if (m_controller.type == ControllerType.Mouse || m_controller.type == ControllerType.Keyboard)
			{
				GameInput.InputManager.instance.DeviceReset(ReInput.controllers.Mouse);
				GameInput.InputManager.instance.DeviceReset(ReInput.controllers.Keyboard);
			}
			else
			{
				GameInput.InputManager.instance.DeviceReset(m_controller);
			}
			HotkeyManager.Sleep();
			BuildBindsList();
			m_resetControlsDialog = false;
		}, LocalizationKeys.Ok.Localized());
	}

	private void ProcessBindingAxisItem(AxisListItem axisListItem, InputSettingsListItem inputSettingsListItemToBind)
	{
		if (axisListItem != null && inputSettingsListItemToBind != null)
		{
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (!(instance == null))
			{
				inputSettingsListItemToBind.ClearBind();
				instance.SetBind(axisListItem.device, ControllerElementType.Axis, inputSettingsListItemToBind.action, axisListItem.title);
				UpdateItems();
			}
		}
	}

	public void NavSelect()
	{
		switch (m_currActiveNavigationListType)
		{
		case NavigateListType.Binds:
			if (m_settingsInputList.GetCount() > 0)
			{
				m_settingsInputList[m_settingsInputList.selectedIndex].PollBind();
			}
			break;
		case NavigateListType.Axis:
		{
			AxisListItem axisListItem = m_axisList.GetSelectedItem();
			if (axisListItem != null)
			{
				OnSelectAxisItem(axisListItem, useHotkey: true);
			}
			break;
		}
		case NavigateListType.Buttons:
		{
			ButtonListItem buttonListItem = m_selectedButtonColumn.GetSelectedItem();
			if (buttonListItem != null)
			{
				OnSelectButtonItem(buttonListItem, useHotkey: true);
			}
			break;
		}
		}
	}

	private InputSettingsListItem GetInputSettingsListItemByItem(BaseListItem listItem)
	{
		if (listItem == null)
		{
			return null;
		}
		BindElement targetBindElement = null;
		if (listItem is AxisListItem axisListItem)
		{
			targetBindElement = GameInput.InputManager.instance.GetBind(axisListItem.device, ControllerElementType.Axis, axisListItem.title);
		}
		else if (listItem is ButtonListItem buttonListItem)
		{
			targetBindElement = GameInput.InputManager.instance.GetBind(buttonListItem.device, ControllerElementType.Button, buttonListItem.title);
		}
		if (targetBindElement == null)
		{
			return null;
		}
		return m_settingsInputList.Find((InputSettingsListItem item) => item.GetElementName() == targetBindElement.elementName);
	}

	private ButtonInputList GetButtonColumnByItem(ButtonListItem buttonItem)
	{
		return m_buttonColumns.Find((ButtonInputList list) => list.Find((ButtonListItem item) => item == buttonItem) != null);
	}

	public void OnSelectAxisItem(AxisListItem axisItem, bool useHotkey, bool isDoubleClick = false)
	{
		if (useHotkey)
		{
			InputSettingsListItem inputSettingsListItemByItem = GetInputSettingsListItemByItem(axisItem);
			if (inputSettingsListItemByItem != null)
			{
				m_cachedSelectedBindItemIndex = m_settingsInputList.IndexOf(inputSettingsListItemByItem);
			}
			ChangeNavInputTo(NavigateListType.Binds);
		}
		else if (m_currActiveNavigationListType != NavigateListType.Axis)
		{
			m_cachedSelectedAxisListItemIndex = m_axisList.selectedIndex;
			ChangeNavInputTo(NavigateListType.Axis);
		}
		UpdateBottomPanel();
		if (isDoubleClick)
		{
			InputSettingsListItem item = m_settingsInputList.GetItem(m_cachedSelectedBindItemIndex.GetValueOrDefault());
			ProcessBindingAxisItem(axisItem, item);
		}
	}

	public void OnSelectButtonItem(ButtonListItem buttonItem, bool useHotkey)
	{
		if (useHotkey)
		{
			InputSettingsListItem inputSettingsListItemByItem = GetInputSettingsListItemByItem(buttonItem);
			if (inputSettingsListItemByItem != null)
			{
				m_cachedSelectedBindItemIndex = m_settingsInputList.IndexOf(inputSettingsListItemByItem);
			}
			ChangeNavInputTo(NavigateListType.Binds);
		}
		else if (m_currActiveNavigationListType != NavigateListType.Buttons)
		{
			ButtonInputList buttonColumnByItem = GetButtonColumnByItem(buttonItem);
			if (buttonColumnByItem != null)
			{
				m_selectedButtonColumn = buttonColumnByItem;
				m_cachedSelectedButtonItemIndex = buttonColumnByItem.selectedIndex;
			}
			ChangeNavInputTo(NavigateListType.Buttons);
		}
		else if (m_currActiveNavigationListType == NavigateListType.Buttons)
		{
			ButtonInputList buttonColumnByItem2 = GetButtonColumnByItem(buttonItem);
			if (buttonColumnByItem2 != null && m_selectedButtonColumn != buttonColumnByItem2)
			{
				m_selectedButtonColumn.ClearSelection();
				m_selectedButtonColumn = buttonColumnByItem2;
			}
		}
		UpdateBottomPanel();
	}

	private void ChangeNavInputTo(NavigateListType navigateListType)
	{
		m_prevActiveContainer = m_currActiveNavigationListType;
		switch (navigateListType)
		{
		case NavigateListType.Binds:
			m_currActiveNavigationListType = NavigateListType.Binds;
			m_axisList.ClearSelection();
			if (m_selectedButtonColumn != null)
			{
				m_selectedButtonColumn.ClearSelection();
			}
			if (m_cachedSelectedBindItemIndex.HasValue)
			{
				m_settingsInputList.selectedIndex = m_cachedSelectedBindItemIndex.GetValueOrDefault();
				m_cachedSelectedBindItemIndex = null;
			}
			break;
		case NavigateListType.Axis:
			m_currActiveNavigationListType = NavigateListType.Axis;
			if (m_prevActiveContainer == NavigateListType.Binds)
			{
				if (!m_cachedSelectedBindItemIndex.HasValue)
				{
					m_cachedSelectedBindItemIndex = m_settingsInputList.GetSelectedIndex();
				}
				m_settingsInputList.ClearSelection();
				m_axisList.selectedIndex = 0;
			}
			else if (m_prevActiveContainer == NavigateListType.Buttons)
			{
				m_selectedButtonColumn.ClearSelection();
				m_axisList.selectedIndex = m_axisList.GetCount() - 1;
			}
			if (m_cachedSelectedAxisListItemIndex.HasValue)
			{
				m_axisList.selectedIndex = m_cachedSelectedAxisListItemIndex.Value;
				m_cachedSelectedAxisListItemIndex = null;
			}
			break;
		case NavigateListType.Buttons:
		{
			m_settingsInputList.ClearSelection();
			m_axisList.ClearSelection();
			m_currActiveNavigationListType = NavigateListType.Buttons;
			bool flag = false;
			foreach (ButtonInputList buttonColumn in m_buttonColumns)
			{
				buttonColumn.ClearSelection();
				if (!flag && !m_cachedSelectedButtonItemIndex.HasValue)
				{
					buttonColumn.selectedIndex = 0;
					m_selectedButtonColumn = buttonColumn;
					flag = true;
				}
				else if (buttonColumn == m_selectedButtonColumn && m_cachedSelectedButtonItemIndex.HasValue)
				{
					buttonColumn.selectedIndex = m_cachedSelectedButtonItemIndex.Value;
				}
			}
			m_cachedSelectedButtonItemIndex = null;
			break;
		}
		}
		UpdateBottomPanel();
	}

	public void NavCalibration()
	{
		if (m_settingsInputList.GetCount() > 0)
		{
			ShowCalibrationDialog(m_settingsInputList[m_settingsInputList.selectedIndex]);
		}
	}

	public void RemoveBind(InputSettingsListItem inputSettingsListItem)
	{
		if (m_settingsInputList.GetCount() > 0)
		{
			ActionType actionType = inputSettingsListItem.action.actionType;
			if (inputSettingsListItem.bind?.elementAxis != null)
			{
				switch (actionType)
				{
				case ActionType.Turn:
					ClearAllSettingsListItemsContainActionType(ActionType.Turn);
					break;
				case ActionType.GearUp:
				case ActionType.GearDown:
					ClearItem(m_settingsInputList.Find((InputSettingsListItem item) => item.action.actionType == ActionType.GearUp));
					ClearItem(m_settingsInputList.Find((InputSettingsListItem item) => item.action.actionType == ActionType.GearDown));
					break;
				default:
					ClearItem(inputSettingsListItem);
					break;
				}
			}
			else
			{
				ClearItem(inputSettingsListItem);
			}
		}
		UpdateBottomPanel();
	}

	private void ClearAllSettingsListItemsContainActionType(ActionType actionTypeToRemove)
	{
		List<InputSettingsListItem> inputSettingsListItemsActionType = GetInputSettingsListItemsActionType(actionTypeToRemove);
		if (inputSettingsListItemsActionType == null)
		{
			return;
		}
		foreach (InputSettingsListItem item in inputSettingsListItemsActionType)
		{
			ClearItem(item);
		}
	}
}
