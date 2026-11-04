using GameInput;
using Rewired;
using UnityEngine;

public class CameraInputController
{
	public struct SphCoord
	{
		public float direction;

		public float elevation;
	}

	private const float XSpeed = 250f;

	private const float YSpeed = 200f;

	private const float ScrollSpeed = 60f;

	private readonly float m_invertedDPI;

	private readonly float m_deviceZoomScale = 2f;

	private const float rotationSmoothFactor = 10f;

	private const float rotationSensitivity = 1f;

	private const float maxDeltaDegree = 10f;

	private Vector2 m_initialScreenPosition;

	private SphCoord m_initialCoord;

	private SphCoord m_prevCoord;

	private SphCoord m_currentCoord;

	private bool m_firstUpdate = true;

	public CameraInputController()
	{
		float invertedDPI = 0f;
		m_invertedDPI = invertedDPI;
	}

	public void CalcStandalone(float dT, ref float deltaXRotation, ref float deltaYRotation, ref float deltaDistance)
	{
		if (GameInput.InputManager.IsInputLocked())
		{
			return;
		}
		if (GameInput.InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis))
		{
			Vector2 vector = axis * dT * 200f;
			if (vector.sqrMagnitude > 0f)
			{
				m_currentCoord.direction += vector.x * -1f;
				m_currentCoord.elevation -= vector.y;
				deltaXRotation = m_currentCoord.direction - m_prevCoord.direction;
				deltaYRotation = m_currentCoord.elevation - m_prevCoord.elevation;
				m_prevCoord = m_currentCoord;
				deltaXRotation = Mathf.Clamp(deltaXRotation, -10f, 10f);
				deltaYRotation = Mathf.Clamp(deltaYRotation, -10f, 10f);
			}
		}
		else if (Input.GetMouseButton(0))
		{
			RotationUpdate(dT);
			deltaXRotation = m_currentCoord.direction - m_prevCoord.direction;
			deltaYRotation = m_currentCoord.elevation - m_prevCoord.elevation;
			m_prevCoord = m_currentCoord;
			deltaXRotation = Mathf.Clamp(deltaXRotation, -10f, 10f);
			deltaYRotation = Mathf.Clamp(deltaYRotation, -10f, 10f);
		}
		else
		{
			m_firstUpdate = true;
		}
		Mouse mouse = ReInput.controllers.Mouse;
		if (mouse.enabled && mouse.axisCount > 2)
		{
			deltaDistance -= Mathf.Clamp(mouse.GetAxisRaw(2) * 0.3f, -1f, 1f) * 60f * dT;
		}
	}

	public void CalcMobile(float dT, ref float deltaXRotation, ref float deltaYRotation, ref float deltaDistance)
	{
		switch (Input.touchCount)
		{
		case 1:
			if (IsTouching(Input.GetTouch(0)))
			{
				RotationUpdate(dT);
				deltaXRotation = m_currentCoord.direction - m_prevCoord.direction;
				deltaYRotation = m_currentCoord.elevation - m_prevCoord.elevation;
				m_prevCoord = m_currentCoord;
				deltaXRotation = Mathf.Clamp(deltaXRotation, -10f, 10f);
				deltaYRotation = Mathf.Clamp(deltaYRotation, -10f, 10f);
			}
			else
			{
				m_firstUpdate = true;
			}
			break;
		case 2:
		{
			Touch touch = Input.GetTouch(0);
			Touch touch2 = Input.GetTouch(1);
			Vector2 normalized = (touch2.position - touch.position).normalized;
			float num = Vector2.Dot(touch.deltaPosition, normalized) + Vector2.Dot(touch2.deltaPosition, -normalized);
			num *= m_invertedDPI * ((touch.deltaTime > 0f) ? (dT / touch.deltaTime) : 1f);
			num *= m_deviceZoomScale;
			deltaDistance = Mathf.Clamp(num, -1f, 1f) * 60f * dT;
			m_firstUpdate = true;
			break;
		}
		default:
			m_firstUpdate = true;
			break;
		}
	}

	private void RotationUpdate(float dT)
	{
		Vector2 firstTouchOrMousePosition = UnityExtensions.GetFirstTouchOrMousePosition();
		if (m_firstUpdate)
		{
			m_initialScreenPosition = firstTouchOrMousePosition;
			m_currentCoord.direction = 0f;
			m_currentCoord.elevation = 0f;
			m_initialCoord = m_currentCoord;
			m_prevCoord = m_currentCoord;
			m_firstUpdate = false;
		}
		float elevation = 70f * (firstTouchOrMousePosition.y - m_initialScreenPosition.y) / (float)Screen.height * 1f + m_initialCoord.elevation;
		float direction = m_initialCoord.direction;
		direction += 360f * (firstTouchOrMousePosition.x - m_initialScreenPosition.x) / (float)Screen.width * 1f;
		float direction2 = Mathf.Lerp(m_currentCoord.direction, direction, 10f * dT);
		m_currentCoord.direction = direction2;
		m_currentCoord.elevation = elevation;
	}

	public static bool IsTouching(Touch touch)
	{
		if (touch.phase != TouchPhase.Began && touch.phase != TouchPhase.Moved)
		{
			return touch.phase == TouchPhase.Stationary;
		}
		return true;
	}
}
