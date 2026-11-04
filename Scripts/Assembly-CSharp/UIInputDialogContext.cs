using System;
using System.Collections;
using GameInput;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIInputDialogContext : UIBaseDialogContext
{
	private const string BIND_NAV_NAME = "AWSDSpaceRight ArrowLeft ArrowReturn";

	private const string BIND_RETURN_NAME = "ReturnCrossA Button";

	[SerializeField]
	private TMP_InputField m_input;

	[SerializeField]
	private TMP_Text m_placeholder;

	private BaseEventData m_baseEventData;

	private GameInput.InputManager m_inputManager;

	private ControllerType m_activeControllerType;

	private WaitForEndOfFrame m_endOfFrame = new WaitForEndOfFrame();

	public TMP_InputField input => m_input;

	public string text
	{
		get
		{
			return m_input.text;
		}
		set
		{
			m_input.text = value;
		}
	}

	public string placeHolder
	{
		get
		{
			return m_placeholder.text;
		}
		set
		{
			if (value != null)
			{
				m_placeholder.text = value;
			}
		}
	}

	public InputField.ContentType contentType
	{
		get
		{
			return (InputField.ContentType)input.contentType;
		}
		set
		{
			input.contentType = (TMP_InputField.ContentType)value;
		}
	}

	private bool m_isKeyboardOrMouseActive
	{
		get
		{
			if (m_activeControllerType != ControllerType.Keyboard)
			{
				return m_activeControllerType == ControllerType.Mouse;
			}
			return true;
		}
	}

	protected override void OnActivate()
	{
		m_inputManager = GameInput.InputManager.instance;
		GameInput.InputManager inputManager = m_inputManager;
		inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(inputManager.onChangeActiveDevice, new Action<Controller>(ChangeFocusInputOnChangeActiveDevice));
		m_activeControllerType = m_inputManager.lastController.type;
		m_baseEventData = new BaseEventData(EventSystem.current);
		SetFocusInputField(m_isKeyboardOrMouseActive);
		TimeController.StartNow(SelectFirstElementAsync());
	}

	private IEnumerator SelectFirstElementAsync()
	{
		yield return m_endOfFrame;
		indexNavigation.value = 0;
	}

	protected override void OnDeactivate()
	{
		GameInput.InputManager inputManager = m_inputManager;
		inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(inputManager.onChangeActiveDevice, new Action<Controller>(ChangeFocusInputOnChangeActiveDevice));
		base.OnDeactivate();
	}

	private void ChangeFocusInputOnChangeActiveDevice(Controller controller)
	{
		m_activeControllerType = controller.type;
	}

	private void Update()
	{
		UpdateLockInput();
	}

	private void UpdateLockInput()
	{
		if (m_input.text.Contains(" ") && input.isFocused)
		{
			if (GameInput.InputManager.GetLockedInputObject() != m_input)
			{
				GameInput.InputManager.LockInput(m_input);
			}
		}
		else if (GameInput.InputManager.GetLockedInputObject() == m_input)
		{
			GameInput.InputManager.LockInput(null);
		}
	}

	private void SetFocusInputField(bool needSelect)
	{
		if (needSelect)
		{
			input.OnSelect(m_baseEventData);
		}
		else
		{
			input.OnDeselect(m_baseEventData);
		}
	}

	public override void NavigateLeft()
	{
		if (CheckBinds(ActionType.NavLeft, "AWSDSpaceRight ArrowLeft ArrowReturn", notContain: true))
		{
			base.NavigateLeft();
		}
		else if (!input.isFocused)
		{
			base.NavigateLeft();
		}
	}

	public override void NavigateRight()
	{
		if (CheckBinds(ActionType.NavRight, "AWSDSpaceRight ArrowLeft ArrowReturn", notContain: true))
		{
			base.NavigateRight();
		}
		else if (!input.isFocused)
		{
			base.NavigateRight();
		}
	}

	private bool CheckBinds(ActionType bindType, string textBindNav, bool notContain)
	{
		if ((bool)input && m_inputManager != null && input.isFocused && m_isKeyboardOrMouseActive)
		{
			BindElement actionActiveBind = m_inputManager.GetActionActiveBind(bindType);
			if (actionActiveBind != null)
			{
				if (notContain)
				{
					if (!textBindNav.Contains(actionActiveBind.elementId))
					{
						return true;
					}
				}
				else if (textBindNav.Contains(actionActiveBind.elementId))
				{
					return true;
				}
			}
		}
		return false;
	}
}
