using System;
using GameInput;
using HUD;
using Rewired;
using UnityEngine;

public class FreeDroneCameraInput : MonoBehaviour
{
	private FreeDroneCameraInputData m_inputData;

	private ContextOwner m_contextOwner;

	private const float SpeedRotationH = 0.5f;

	private const float SpeedRotationV = 0.5f;

	private void Awake()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState != null)
		{
			m_contextOwner = baseGameState.contextOwner;
		}
	}

	private void OnEnable()
	{
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onTopContextChanged, new Action<BaseContext>(HandleBlockInput));
		}
	}

	public void UpdateInput()
	{
		if (m_inputData != null && !m_inputData.blockedInput)
		{
			MovementInput();
			FovChangersInput();
		}
	}

	private void HandleBlockInput(BaseContext context)
	{
		if (m_inputData != null)
		{
			m_inputData.blockedInput = m_contextOwner.topContext is UIBaseDialogContext || m_contextOwner.topContext is UIRaceStatsContext;
		}
	}

	public void Initialize(FreeDroneCameraInputData inputData)
	{
		m_inputData = inputData;
	}

	private void OnDisable()
	{
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onTopContextChanged, new Action<BaseContext>(HandleBlockInput));
		}
	}

	private void MovementInput()
	{
		m_inputData.movement = GameInput.InputManager.instance.GetActionValue(ActionType.FlyMovement);
		m_inputData.strafe = GameInput.InputManager.instance.GetActionValue(ActionType.FlyStrafe);
		m_inputData.heightUp = GameInput.InputManager.instance.GetActionValue(ActionType.FlyHeightUp);
		m_inputData.heightDown = GameInput.InputManager.instance.GetActionValue(ActionType.FlyHeightDown);
		m_inputData.rotHorizontal = GameInput.InputManager.instance.GetActionValue(ActionType.FlyRotationHorizontal);
		m_inputData.rotVertical = GameInput.InputManager.instance.GetActionValue(ActionType.FlyRotationVertical);
		m_inputData.turboMode = GameInput.InputManager.instance.IsActionHold(ActionType.FlyTurboMode);
		m_inputData.holdRotateButton = Input.GetMouseButton(1);
		if (m_inputData.holdRotateButton && ReInput.controllers.Mouse != null && ReInput.controllers.Mouse.axisCount > 1)
		{
			m_inputData.rotHorizontal = ReInput.controllers.Mouse.GetAxis(0) * 0.5f;
			m_inputData.rotVertical = ReInput.controllers.Mouse.GetAxis(1) * 0.5f;
		}
		m_inputData.changeFOV = GameInput.InputManager.instance.IsActionPressed(ActionType.FlyChangeFOV);
	}

	private void FovChangersInput()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			m_inputData.fovIndex = 0;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			m_inputData.fovIndex = 1;
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			m_inputData.fovIndex = 2;
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			m_inputData.fovIndex = 3;
		}
		if (Input.GetKeyDown(KeyCode.Alpha5))
		{
			m_inputData.fovIndex = 4;
		}
		if (!UIUtils.IsPointerOverGameObject())
		{
			if (Input.mouseScrollDelta.y > 0f)
			{
				m_inputData.IncrementFovIndex(allowWraparoundIndex: false);
			}
			if (Input.mouseScrollDelta.y < 0f)
			{
				m_inputData.DecrementFovIndex(allowWraparoundIndex: false);
			}
		}
		if (m_inputData.changeFOV)
		{
			m_inputData.IncrementFovIndex(allowWraparoundIndex: true);
		}
		m_inputData.changerFov = 0;
	}
}
