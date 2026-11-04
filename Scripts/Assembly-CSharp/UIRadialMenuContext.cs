using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using GameInput;
using Rewired;
using UnityEngine;

public class UIRadialMenuContext : BaseBindingBehaviourTarget
{
	private const float FullCircleAngle = 360f;

	private const float OffsetCircleAngle = 450f;

	private const int NoSelectionIndex = -1;

	[SerializeField]
	private List<UIRadialAction> m_radialActionViews = new List<UIRadialAction>();

	[SerializeField]
	private float m_coyoteDeselectTime = 0.15f;

	[SerializeField]
	private float m_stickDeadZone = 0.3f;

	[SerializeField]
	private float m_mouseDeadZone = 0.3f;

	[SerializeField]
	private float m_mouseMovementThreshold = 5f;

	[SerializeField]
	private RectTransform m_centerDeadZone;

	private Vector2 m_initialMousePosition;

	private int m_currentSelectedIndex;

	private bool m_isSelectionStarted;

	private StringProperty m_actionText = new StringProperty();

	private BoolProperty m_isSelectedCenter = new BoolProperty();

	public List<UIRadialAction> radialActionViews => m_radialActionViews;

	public StringProperty actionText => m_actionText;

	public bool IsAnyActionSelected()
	{
		foreach (UIRadialAction radialActionView in m_radialActionViews)
		{
			if (radialActionView.isSelected)
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		CameraSwitch.instance.rotateMode = false;
		foreach (UIRadialAction radialActionView in m_radialActionViews)
		{
			radialActionView.UpdateHintVisibility();
			radialActionView.UpdateSelection(isSelected: false);
		}
		m_initialMousePosition = Vector2.zero;
		m_isSelectionStarted = false;
		m_isSelectedCenter.value = true;
		m_currentSelectedIndex = -1;
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		EnableCameraRotationAsync().Forget();
		bool flag = GameInput.InputManager.instance.IsActionReleased(ActionType.ShowRadialMenu);
		if (m_currentSelectedIndex != -1 && flag)
		{
			m_radialActionViews[m_currentSelectedIndex].PerformAction();
		}
	}

	private void Update()
	{
		HandleSelectionInput();
	}

	private void HandleSelectionInput()
	{
		if (GameInput.InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis))
		{
			HandleGamepadInput(axis);
		}
		else
		{
			HandleMouseInput();
		}
	}

	private void HandleGamepadInput(Vector2 currentAxis2D)
	{
		if (currentAxis2D.sqrMagnitude < m_stickDeadZone)
		{
			UpdateSelectionAsync(-1).Forget();
			m_isSelectionStarted = false;
		}
		else
		{
			m_isSelectionStarted = true;
			ProcessSelection(currentAxis2D);
		}
	}

	private void HandleMouseInput()
	{
		Vector2 screenPosition = ReInput.controllers.Mouse.screenPosition;
		if (!m_isSelectionStarted)
		{
			if (m_initialMousePosition == Vector2.zero)
			{
				m_initialMousePosition = screenPosition;
			}
			if ((m_initialMousePosition - screenPosition).sqrMagnitude < m_mouseMovementThreshold)
			{
				UpdateSelectionAsync(-1).Forget();
				HandleMouseClick();
				return;
			}
			m_isSelectionStarted = true;
		}
		Vector2 cursorRelativeToRect = GetCursorRelativeToRect(m_centerDeadZone, screenPosition);
		if (cursorRelativeToRect.sqrMagnitude <= m_mouseDeadZone)
		{
			UpdateSelectionAsync(-1).Forget();
			m_isSelectionStarted = false;
			HandleMouseClick();
		}
		else
		{
			ProcessSelection(cursorRelativeToRect);
			HandleMouseClick();
		}
	}

	private void ProcessSelection(Vector2 currentAxis2D)
	{
		float angle = Mathf.Atan2(currentAxis2D.y, currentAxis2D.x) * 57.29578f;
		int selectedIndex = GetSelectedIndex(angle);
		if (selectedIndex != m_currentSelectedIndex)
		{
			UpdateSelectionAsync(selectedIndex).Forget();
		}
	}

	private void HandleMouseClick()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
		{
			if (m_currentSelectedIndex != -1)
			{
				m_radialActionViews[m_currentSelectedIndex].PerformAction(forceHide: true);
			}
			else
			{
				Hide();
			}
		}
	}

	private int GetSelectedIndex(float angle)
	{
		float num = 360f / (float)m_radialActionViews.Count;
		angle = (450f - angle) % 360f;
		if (angle < 0f)
		{
			angle += 360f;
		}
		int num2 = Mathf.FloorToInt(angle / num);
		if (num2 >= m_radialActionViews.Count)
		{
			return -1;
		}
		return num2;
	}

	private async UniTaskVoid UpdateSelectionAsync(int newIndex)
	{
		if (m_currentSelectedIndex != -1)
		{
			int previousIndex = m_currentSelectedIndex;
			if (newIndex == -1)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(m_coyoteDeselectTime), ignoreTimeScale: true);
				if (m_currentSelectedIndex != previousIndex)
				{
					return;
				}
			}
			m_radialActionViews[previousIndex].UpdateSelection(isSelected: false);
			m_isSelectedCenter.value = true;
		}
		if (newIndex != -1)
		{
			m_radialActionViews[newIndex].UpdateSelection(isSelected: true);
			m_isSelectedCenter.value = false;
		}
		m_currentSelectedIndex = newIndex;
	}

	public static Vector2 GetCursorRelativeToRect(RectTransform rt, Vector2 screenPoint)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, null, out var localPoint);
		float num = rt.rect.width * 0.5f;
		float num2 = rt.rect.height * 0.5f;
		Vector2 result = default(Vector2);
		result.x = ((num > 0f) ? (localPoint.x / num) : 0f);
		result.y = ((num2 > 0f) ? (localPoint.y / num2) : 0f);
		return result;
	}

	private async UniTaskVoid EnableCameraRotationAsync()
	{
		await UniTask.Delay(TimeSpan.FromSeconds(m_coyoteDeselectTime), ignoreTimeScale: true);
		CameraSwitch.instance.rotateMode = true;
	}
}
