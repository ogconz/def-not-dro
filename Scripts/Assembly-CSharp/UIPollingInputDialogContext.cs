using System.Collections;
using Binding;
using Binding.Base;
using GameInput;
using Rewired;
using UnityEngine;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
public class UIPollingInputDialogContext : BaseBindingBehaviourTarget
{
	private UIInputBindsSettingsContext m_bindsContext;

	private InputSettingsListItem m_itemPolling;

	private bool isPolling;

	private bool m_isAxisClickSoundPlayed;

	private bool m_isAxisLastPollingElement;

	private readonly Property<string> actionText = new StringProperty();

	private readonly Property<string> replaceText = new StringProperty();

	private readonly Property<int> waitTime = new IntProperty();

	private const int m_maxWaitingSeconds = 5;

	private int waitingSeconds;

	public GameInput.InputManager input { get; } = GameInput.InputManager.instance;

	protected override void OnActivate()
	{
		base.OnActivate();
		replaceText.value = "";
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			FixedUpdate();
		}
	}

	private void FixedUpdate()
	{
		if (isPolling)
		{
			PollControllers();
		}
	}

	public void SetData(UIInputBindsSettingsContext bindsContext, InputSettingsListItem itemPolling)
	{
		m_bindsContext = bindsContext;
		m_itemPolling = itemPolling;
		if (itemPolling?.action != null)
		{
			string text = itemPolling.action.actionType.ToString();
			if (itemPolling.action.valueType == ActionValueType.Full)
			{
				text += (m_itemPolling.IsNegative() ? "L" : "R");
			}
			string text2 = LocalizationKeys.input_action_.AddLocalized(text.ToLower()).ToUpper();
			actionText.value = LocalizationKeys.input_settings_textBinding.LocalizedFormat(text2);
		}
		StartCoroutine(WaitPolling());
	}

	private IEnumerator WaitPolling()
	{
		isPolling = false;
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		isPolling = true;
		for (waitingSeconds = 5; waitingSeconds > 0; waitingSeconds--)
		{
			waitTime.value = waitingSeconds;
			yield return new WaitForSecondsRealtime(1f);
		}
		Hide();
	}

	private Action GetActionBind(ControllerPollingInfo pollInfo)
	{
		BindElement bind = input.GetBind(pollInfo.controller, pollInfo.elementType, pollInfo.elementIdentifierName, m_itemPolling.IsNegative());
		if (bind == null)
		{
			return null;
		}
		return GameInput.InputManager.instance.GetActionInfo(bind.bindedAction);
	}

	private void ClearReplaceText()
	{
		replaceText.value = "";
	}

	private void SetReplaceText(ControllerPollingInfo pollinfo)
	{
		Action actionBind = GetActionBind(pollinfo);
		if (actionBind != null)
		{
			string text = actionBind.actionType.ToString();
			if (actionBind.valueType == ActionValueType.Full)
			{
				text += ((actionBind.value > 0f) ? "R" : "L");
			}
			string text2 = LocalizationKeys.input_action_.AddLocalized(text.ToLower()).ToUpper();
			replaceText.value = LocalizationKeys.input_settings_element_busy.LocalizedFormat(text2);
		}
	}

	private void PollControllers()
	{
		if (m_itemPolling == null || m_bindsContext == null)
		{
			return;
		}
		m_isAxisLastPollingElement = false;
		foreach (ControllerPollingInfo item in ReInput.controllers.polling.PollAllControllersForAllElementsDown())
		{
			if (!item.success)
			{
				continue;
			}
			HandlePlayindClickSoundByPollingElem(item.elementType);
			if (item.elementIdentifierName == "ESC")
			{
				m_bindsContext.PollItem(null);
				Hide();
				return;
			}
			Controller controller = item.controller;
			if (controller == m_bindsContext.controller || (controller.type == ControllerType.Mouse && m_bindsContext.controller.type == ControllerType.Keyboard))
			{
				switch (m_bindsContext.TryBind(m_itemPolling, item))
				{
				case BindState.Success:
					ClearReplaceText();
					StopAllCoroutines();
					Hide();
					break;
				case BindState.Busy:
					SetReplaceText(item);
					break;
				case BindState.Failed:
					ClearReplaceText();
					break;
				}
				m_bindsContext.UpdateList();
			}
		}
		if (Mathf.Abs(GameInput.InputManager.instance.lastController.TryGetDS4AccelerationX()) > 0.25f)
		{
			SetGyroscopeBindIfNeed();
			return;
		}
		if (!m_isAxisLastPollingElement)
		{
			m_isAxisClickSoundPlayed = false;
		}
		HotkeyManager.Sleep(0.5f);
	}

	private void SetGyroscopeBindIfNeed()
	{
		foreach (GameInput.InputManager.ControllerElementDescriptor item in input.GetControllerElementsType(input.lastController, ControllerElementType.Axis))
		{
			if (item.name == "Gyroscope Horizontal Axis")
			{
				input.SetBind(input.lastController, ControllerElementType.Axis, m_itemPolling.action, "Gyroscope Horizontal Axis", m_itemPolling.IsNegative());
				Hide();
				m_bindsContext.UpdateItems();
				m_bindsContext.UpdateList();
				break;
			}
		}
	}

	private void HandlePlayindClickSoundByPollingElem(ControllerElementType elementType)
	{
		m_isAxisLastPollingElement = elementType == ControllerElementType.Axis;
		if (!m_isAxisLastPollingElement)
		{
			GameInput.InputManager.PlayClickSound();
		}
		else if (!m_isAxisClickSoundPlayed)
		{
			GameInput.InputManager.PlayClickSound();
			m_isAxisClickSoundPlayed = true;
		}
	}

	[GameHotkey(ActionType.NavStart, false, false)]
	public void NavEmpty()
	{
		Debug.Log("--");
	}
}
