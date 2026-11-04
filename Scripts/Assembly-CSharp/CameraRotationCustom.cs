using System;
using System.Collections;
using GameInput;
using UnityEngine;

public class CameraRotationCustom : MonoBehaviour
{
	public struct SphCoord
	{
		public float direction;

		public float elevation;
	}

	private enum State
	{
		Idle = 0,
		ForcedRotation = 1,
		LerpFromForcedRotation = 2,
		ForcedFixPosition = 3,
		RotationToTargetPosition = 4,
		RotationToPositionChangeCenter = 5,
		CustomVirtualCamera = 6
	}

	public float idleSecondsToFullCircle = 60f;

	public float secondsToReturnToIdleFromFixedPosition = 15f;

	public float secondsToReturnToIdleFromForcedRotation = 3f;

	public float holdSecondsToFixPosition = 0.1f;

	public float holdMovementToleranceDegrees = 1f;

	public float maxHorizontalSpeed = 10f;

	public float horizontalSpeedSmoothingFactor01 = 0.7f;

	public float newForcedRotationSmoothFactor = 10f;

	public float minFov = 40f;

	public float maxFov = 60f;

	public float minElevationAngle = 4f;

	public float maxElevationAngle = 70f;

	public bool disableNegativeElevation;

	public float rotationSensitivity = 1.5f;

	public float rotationCenterUpOffset;

	public AnimationCurve curveRotationToPosition = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[HideInInspector]
	public bool canIdle = true;

	private float m_returnToIdleSec = 3f;

	[SerializeField]
	private float m_joystickRotSpeed = 200f;

	private State m_state;

	private float m_partPassedOfStateTime;

	private Vector3 m_rotationCenter;

	[SerializeField]
	private float m_distanceFromCenter;

	private SphCoord m_currentCoord;

	private System.Action[] m_stateUpdates;

	private System.Action[] m_stateTransitions;

	private Vector2 m_forcedRotationScreenStart;

	private SphCoord m_forcedRotationInitialCoord;

	private float m_forcedRotationHoldSeconds;

	private float m_forcedRotationCurrentAngle;

	private float m_forcedRotationCurrentSpeed;

	private Vector3 m_centerPositionStart;

	private Vector3 m_centerPositionEnd;

	private float m_centerDistanceStart;

	private float m_centerDistanceEnd;

	private float m_centerDistanceSetup;

	private SphCoord m_rotationToTargetPositionStart;

	private SphCoord m_rotationToTargetPositionTarget;

	private bool m_idleRotation = true;

	private bool m_skipToRotateByMouse;

	private bool m_useZoom;

	private float m_zoomSpeed = 10f;

	private float m_distanceFromCenterMin = 4f;

	private float m_distanceFromCenterMax = 7f;

	public bool lockMouseRotate;

	public float distanceFromCenter
	{
		get
		{
			return m_distanceFromCenter;
		}
		set
		{
			SetDistanceFromCenter(value);
		}
	}

	public bool idleRotation
	{
		get
		{
			return m_idleRotation;
		}
		set
		{
			if (m_idleRotation != value)
			{
				m_idleRotation = value;
				if (m_idleRotation)
				{
					IdleTransitions();
				}
			}
		}
	}

	private void SetDistanceFromCenter(float dist)
	{
		m_distanceFromCenter = dist;
		bool flag = idleRotation;
		idleRotation = true;
		SetState(State.ForcedRotation);
		Update();
		idleRotation = flag;
	}

	public void Stop()
	{
		SetState(State.ForcedFixPosition);
	}

	private void Empty()
	{
	}

	private void SetStateWithTimeout(State newState, float timeout, State postNewState)
	{
		timeout = Mathf.Clamp(timeout, 0f, 3f);
		if (timeout <= 0f)
		{
			SetState(postNewState);
			return;
		}
		SetState(newState);
		StartCoroutine(TransitionToStateAfter(timeout, postNewState));
	}

	private void SetState(State newState)
	{
		StopAllCoroutines();
		if (m_state != newState)
		{
			if (newState == State.ForcedRotation)
			{
				m_forcedRotationHoldSeconds = 0f;
				m_forcedRotationScreenStart = UnityExtensions.GetFirstTouchOrMousePosition();
				m_forcedRotationInitialCoord = m_currentCoord;
			}
			m_state = newState;
		}
	}

	private IEnumerator TransitionToStateAfter(float timeout, State newState)
	{
		if (!(timeout <= 0f))
		{
			float passed = (m_partPassedOfStateTime = 0f);
			while (passed < timeout)
			{
				passed += Time.deltaTime;
				m_partPassedOfStateTime = passed / timeout;
				yield return null;
			}
			SetState(newState);
		}
	}

	public void IdleRotation()
	{
		if (canIdle && !(idleSecondsToFullCircle <= 0f))
		{
			float num = 360f / idleSecondsToFullCircle;
			SetDirectionAndElevation(m_currentCoord.direction + num * Time.deltaTime, m_currentCoord.elevation);
		}
	}

	private void ForcedRotationUpdate()
	{
		if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) && !lockMouseRotate)
		{
			Vector2 vector = axis * Time.deltaTime * m_joystickRotSpeed;
			if (vector.sqrMagnitude > 0f)
			{
				SphCoord result = default(SphCoord);
				GetCurrentDirectionAndElevation(out result);
				result.direction += vector.x * -1f;
				result.elevation = Mathf.Clamp(result.elevation + vector.y, minElevationAngle, maxElevationAngle);
				SetDirectionAndElevation(result.direction, result.elevation);
				m_forcedRotationCurrentSpeed = 0f;
				m_forcedRotationCurrentAngle = 0f;
				m_forcedRotationHoldSeconds += Time.deltaTime;
			}
		}
		if (Input.GetMouseButton(0) && !lockMouseRotate)
		{
			Vector2 firstTouchOrMousePosition = UnityExtensions.GetFirstTouchOrMousePosition();
			float elevation = Mathf.Clamp((0f - Mathf.Abs(maxElevationAngle - minElevationAngle)) * (firstTouchOrMousePosition.y - m_forcedRotationScreenStart.y) / (float)Screen.height * rotationSensitivity + m_forcedRotationInitialCoord.elevation, minElevationAngle, maxElevationAngle);
			float direction = m_forcedRotationInitialCoord.direction;
			direction += 360f * (firstTouchOrMousePosition.x - m_forcedRotationScreenStart.x) / (float)Screen.width * rotationSensitivity;
			float num = Mathf.Lerp(m_currentCoord.direction, direction, newForcedRotationSmoothFactor * Time.deltaTime);
			float num2 = num - m_currentCoord.direction;
			m_forcedRotationCurrentSpeed = Mathf.Lerp(m_forcedRotationCurrentSpeed, num2 / Time.deltaTime, horizontalSpeedSmoothingFactor01);
			if (Mathf.Abs(num2) <= holdMovementToleranceDegrees)
			{
				m_forcedRotationHoldSeconds += Time.deltaTime;
			}
			else
			{
				m_forcedRotationHoldSeconds = 0f;
			}
			m_forcedRotationCurrentAngle = num;
			SetDirectionAndElevation(m_forcedRotationCurrentAngle, elevation);
		}
	}

	private void LerpFromForcedRotationUpdate()
	{
		if (!(idleSecondsToFullCircle <= 0f))
		{
			float b = 360f / idleSecondsToFullCircle;
			float num = Mathf.Lerp(m_forcedRotationCurrentSpeed, b, Mathf.Pow(m_partPassedOfStateTime, 0.1f));
			SetDirectionAndElevation(m_currentCoord.direction + num * Time.deltaTime, m_currentCoord.elevation);
		}
	}

	private void RotationToTargetPositionUpdate()
	{
		SetDirectionAndElevation(Mathf.Lerp(m_rotationToTargetPositionStart.direction, m_rotationToTargetPositionTarget.direction, m_partPassedOfStateTime), Mathf.Lerp(m_rotationToTargetPositionStart.elevation, m_rotationToTargetPositionTarget.elevation, m_partPassedOfStateTime));
	}

	private void CurveRotationToTargetPositionUpdate(float time)
	{
		SetDirectionAndElevation(Mathf.Lerp(m_rotationToTargetPositionStart.direction, m_rotationToTargetPositionTarget.direction, time), Mathf.Lerp(m_rotationToTargetPositionStart.elevation, m_rotationToTargetPositionTarget.elevation, time));
	}

	private void RotationToPositionChangeCenterUpdate()
	{
		float num = curveRotationToPosition.Evaluate(m_partPassedOfStateTime);
		SetCenter(Vector3.Lerp(m_centerPositionStart, m_centerPositionEnd, num));
		m_distanceFromCenter = Mathf.Lerp(m_centerDistanceStart, m_centerDistanceEnd, num);
		CurveRotationToTargetPositionUpdate(num);
	}

	public void IdleTransitions()
	{
		if (idleRotation && !m_skipToRotateByMouse)
		{
			if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) && axis != Vector2.zero)
			{
				SetState(State.ForcedRotation);
			}
			if (!UIUtils.IsPointerOverGameObject() && (Input.touchCount > 0 || Input.GetMouseButton(0)))
			{
				SetState(State.ForcedRotation);
			}
		}
	}

	private void ForcedRotationTransitions()
	{
		InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis);
		if (!Input.GetMouseButton(0) && !(axis != Vector2.zero))
		{
			if (m_forcedRotationHoldSeconds >= holdSecondsToFixPosition)
			{
				SetStateWithTimeout(State.ForcedFixPosition, secondsToReturnToIdleFromFixedPosition, State.Idle);
			}
			if (Mathf.Approximately(m_returnToIdleSec, float.MaxValue))
			{
				SetStateWithTimeout(State.LerpFromForcedRotation, 3f, State.ForcedFixPosition);
			}
			else
			{
				SetStateWithTimeout(State.LerpFromForcedRotation, m_returnToIdleSec, State.Idle);
			}
		}
	}

	private void GetCurrentDirectionAndElevation(out SphCoord result)
	{
		CalcDirectionAndElevation(base.transform, out result);
	}

	public static void CalcDirectionAndElevation(Transform tr, out SphCoord result)
	{
		Vector3 forward = tr.forward;
		Vector3 vector = forward;
		vector.y = 0f;
		result.direction = Vector3.Angle(Vector3.forward, vector) * Mathf.Sign(vector.x);
		result.elevation = (0f - Vector3.Angle(vector, forward)) * Mathf.Sign(forward.y);
	}

	public void SetDirectionAndElevation(float direction, float elevation)
	{
		Vector3 vector = Quaternion.Euler(elevation, direction, 0f) * Vector3.forward;
		Vector3 position = GetRotationCenterWithOffset() - vector * m_distanceFromCenter;
		position.y = (disableNegativeElevation ? Mathf.Max(position.y, 0.15f) : position.y);
		base.transform.position = position;
		base.transform.LookAt(GetRotationCenterWithOffset());
		m_currentCoord.direction = direction;
		m_currentCoord.elevation = elevation;
	}

	private Vector3 GetRotationCenterWithOffset()
	{
		return GetRotationCenterWithOffset(m_rotationCenter);
	}

	private Vector3 GetRotationCenterWithOffset(Vector3 center)
	{
		return center + Vector3.up * rotationCenterUpOffset;
	}

	public void SetCenter(Vector3 center)
	{
		m_rotationCenter = center;
		m_distanceFromCenter = Vector3.Distance(GetRotationCenterWithOffset(), base.transform.position);
		base.transform.LookAt(GetRotationCenterWithOffset());
		GetCurrentDirectionAndElevation(out m_currentCoord);
	}

	private void Awake()
	{
		m_returnToIdleSec = secondsToReturnToIdleFromForcedRotation;
		if (!(GetComponentInChildren<Camera>() == null))
		{
			Array values = Enum.GetValues(typeof(State));
			m_stateUpdates = new System.Action[values.Length];
			m_stateUpdates[0] = IdleRotation;
			m_stateUpdates[1] = ForcedRotationUpdate;
			m_stateUpdates[2] = LerpFromForcedRotationUpdate;
			m_stateUpdates[3] = Empty;
			m_stateUpdates[4] = RotationToTargetPositionUpdate;
			m_stateUpdates[5] = RotationToPositionChangeCenterUpdate;
			m_stateUpdates[6] = Empty;
			m_stateTransitions = new System.Action[values.Length];
			m_stateTransitions[0] = IdleTransitions;
			m_stateTransitions[1] = ForcedRotationTransitions;
			m_stateTransitions[2] = IdleTransitions;
			m_stateTransitions[3] = IdleTransitions;
			m_stateTransitions[4] = IdleTransitions;
			m_stateTransitions[5] = Empty;
			m_stateTransitions[6] = IdleTransitions;
			m_centerDistanceSetup = m_distanceFromCenter;
		}
	}

	private void Update()
	{
		RegisterMouseRotationState();
		int state = (int)m_state;
		if (state >= 0)
		{
			if (state < m_stateUpdates.Length)
			{
				m_stateUpdates[state].Call();
			}
			if (state < m_stateTransitions.Length)
			{
				m_stateTransitions[state].Call();
			}
		}
	}

	private void RegisterMouseRotationState()
	{
		if (Input.GetMouseButtonDown(0) && UIUtils.IsPointerOverGameObject())
		{
			m_skipToRotateByMouse = true;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			m_skipToRotateByMouse = false;
		}
	}

	private void LateUpdate()
	{
		if (!UIUtils.IsPointerOverGameObject() && m_useZoom && base.enabled)
		{
			float num = 0f - Input.mouseScrollDelta.y;
			if (num != 0f)
			{
				m_distanceFromCenter = Mathf.Clamp(m_distanceFromCenter + num * Time.deltaTime * m_zoomSpeed, m_distanceFromCenterMin, m_distanceFromCenterMax);
				GetCurrentDirectionAndElevation(out m_currentCoord);
				SetDirectionAndElevation(m_currentCoord.direction, m_currentCoord.elevation);
			}
		}
	}
}
