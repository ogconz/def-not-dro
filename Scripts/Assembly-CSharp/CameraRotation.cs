using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using GameInput;
using GameOverlay;
using UnityEngine;

public class CameraRotation : MonoBehaviour
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

	private const float FovLerpDuration = 0.1f;

	private const float HeightLerpDuration = 0.1f;

	private const float DistanceLerpDuration = 0.1f;

	public float idleSecondsToFullCircle = 60f;

	public float secondsToReturnToIdleFromFixedPosition = 15f;

	public float secondsToReturnToIdleFromForcedRotation = 3f;

	public float holdSecondsToFixPosition = 0.1f;

	public float holdMovementToleranceDegrees = 1f;

	public float maxHorizontalSpeed = 10f;

	public float horizontalSpeedSmoothingFactor01 = 0.7f;

	public float newForcedRotationSmoothFactor = 10f;

	public float fov;

	public float minFov = 14f;

	public float maxFov = 50f;

	public float defaultFov = 50f;

	public bool allowGamepadZoom;

	public bool useFovZoom;

	public bool useZoom;

	public float minElevationAngle = 4f;

	public float maxElevationAngle = 70f;

	public bool disableNegativeElevation;

	public float rotationSensitivity = 1.5f;

	public float rotationCenterUpOffset;

	public float minRotationCenterUpOffset = 0.28f;

	public float maxRotationCenterUpOffset = 0.55f;

	public AnimationCurve curveRotationToPosition = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	private float m_distanceFromCenterMax = 7f;

	[HideInInspector]
	public bool canIdle = true;

	private float m_returnToIdleSec = 3f;

	[SerializeField]
	private float m_joystikRotSpeed = 200f;

	private State m_state;

	private float m_partPassedOfStateTime;

	private Vector3 m_rotationCenter;

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

	private float m_fovZoomSpeed = 5f;

	private float m_distanceZoomSpeed = 18f;

	private Camera m_camera;

	private CinemachineBrain m_cinemachine;

	[SerializeField]
	private float m_distanceFromCenter;

	public static CameraRotation instance { get; private set; }

	public float distanceFromCenterMin { get; set; } = 4f;

	public bool disableReturnToIdle
	{
		set
		{
			m_returnToIdleSec = (value ? 0f : secondsToReturnToIdleFromForcedRotation);
		}
	}

	public bool skipToRotateByMouse => m_skipToRotateByMouse;

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

	public void SetFov(float lerp)
	{
		if (!(m_camera == null))
		{
			float startValue = fov;
			float endValue = Mathf.Clamp01(lerp);
			DOTween.To(Setter, startValue, endValue, 0.1f).Play();
			SetCenterUpOffset(lerp);
		}
		void Setter(float value)
		{
			fov = value;
			m_camera.fieldOfView = Mathf.Lerp(minFov, maxFov, fov);
		}
	}

	public void ResetFov()
	{
		SetFov((defaultFov - minFov) / (maxFov - minFov));
	}

	public void SetCenterUpOffset(float lerp)
	{
		float startValue = rotationCenterUpOffset;
		float endValue = Mathf.Lerp(minRotationCenterUpOffset, maxRotationCenterUpOffset, Mathf.Clamp01(lerp));
		DOTween.To(Setter, startValue, endValue, 0.1f).Play();
		void Setter(float value)
		{
			rotationCenterUpOffset = value;
			SetDirectionAndElevation(m_currentCoord.direction, m_currentCoord.elevation);
		}
	}

	public void ResetToMaxDistance()
	{
		DOTween.To(delegate(float value)
		{
			SetDistanceFromCenter(value);
			GetCurrentDirectionAndElevation(out m_currentCoord);
			SetDirectionAndElevation(m_currentCoord.direction, m_currentCoord.elevation);
		}, distanceFromCenter, m_distanceFromCenterMax, 0.1f).Play();
	}

	public void SetDistanceFromCenterSmooth(float newDistance)
	{
		DOTween.To(Setter, m_distanceFromCenter, newDistance, 0.1f).Play();
		void Setter(float value)
		{
			m_distanceFromCenter = Mathf.Clamp(value, distanceFromCenterMin, m_distanceFromCenterMax);
			GetCurrentDirectionAndElevation(out m_currentCoord);
			SetDirectionAndElevation(m_currentCoord.direction, m_currentCoord.elevation);
		}
	}

	public void SetDistanceFromCenter(float dist)
	{
		m_distanceFromCenter = dist;
		bool flag = idleRotation;
		idleRotation = true;
		SetState(State.ForcedRotation);
		Update();
		idleRotation = flag;
	}

	public void RotateToPosition(Quaternion quaternion, float duration)
	{
		Vector3 eulerAngles = quaternion.eulerAngles;
		RotateToPosition(new SphCoord
		{
			direction = eulerAngles.y,
			elevation = eulerAngles.x
		}, duration);
	}

	public void RotateToPosition(SphCoord targetCoord, float duration)
	{
		PrepareRotateToPosition(targetCoord, duration);
		SetStateWithTimeout(State.RotationToTargetPosition, duration, State.ForcedFixPosition);
	}

	private void PrepareRotateToPosition(SphCoord targetCoord, float duration)
	{
		m_rotationToTargetPositionTarget = targetCoord;
		GetCurrentDirectionAndElevation(out m_rotationToTargetPositionStart);
		if (Mathf.Abs(m_rotationToTargetPositionTarget.direction - m_rotationToTargetPositionStart.direction) > 180f)
		{
			m_rotationToTargetPositionTarget.direction -= 360f;
		}
	}

	public void RotateToPositionChangeCenterAndDistance(SphCoord position, Vector3 centerShift, float distanceCoeff, float duration, bool blockForceRotation = true)
	{
		PrepareRotateToPosition(position, duration);
		m_centerDistanceStart = m_distanceFromCenter;
		m_centerDistanceEnd = m_centerDistanceSetup * distanceCoeff;
		m_centerPositionStart = m_rotationCenter;
		m_centerPositionEnd = m_centerPositionStart + centerShift;
		State postNewState = (blockForceRotation ? State.RotationToPositionChangeCenter : State.LerpFromForcedRotation);
		SetStateWithTimeout(State.RotationToPositionChangeCenter, duration, postNewState);
	}

	public void RotateToPositionSetCenterAndDistance(SphCoord position, Vector3 newCenter, float distanceCoeff, float duration, bool blockForceRotation = true)
	{
		RotateToPositionChangeCenterAndDistance(position, newCenter - m_rotationCenter, distanceCoeff, duration, blockForceRotation);
	}

	public void RotateToPositionResetCenterAndDistance(SphCoord position, float duration, bool stopAfter = false)
	{
		PrepareRotateToPosition(position, duration);
		m_centerPositionStart = m_rotationCenter;
		m_centerDistanceStart = m_distanceFromCenter;
		m_centerDistanceEnd = m_centerDistanceSetup;
		ResetCenter();
		m_centerPositionEnd = m_rotationCenter;
		SetStateWithTimeout(State.RotationToPositionChangeCenter, duration, stopAfter ? State.ForcedFixPosition : State.LerpFromForcedRotation);
	}

	public void SetIdle()
	{
		SetState(State.Idle);
	}

	public void SetCustomVirtualCamera()
	{
		SetState(State.CustomVirtualCamera);
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
		float passed = (m_partPassedOfStateTime = 0f);
		while (passed < timeout)
		{
			passed += Time.deltaTime;
			m_partPassedOfStateTime = passed / timeout;
			yield return null;
		}
		SetState(newState);
	}

	private void IdleRotation()
	{
		if (canIdle)
		{
			float num = 360f / idleSecondsToFullCircle;
			SetDirectionAndElevation(m_currentCoord.direction + num * Time.deltaTime, m_currentCoord.elevation);
		}
	}

	private void ForcedRotationUpdate()
	{
		if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis))
		{
			Vector2 vector = axis * Time.deltaTime * m_joystikRotSpeed;
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
		if (Input.GetMouseButton(0))
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
		float b = 360f / idleSecondsToFullCircle;
		float num = Mathf.Lerp(m_forcedRotationCurrentSpeed, b, Mathf.Pow(m_partPassedOfStateTime, 0.1f));
		SetDirectionAndElevation(m_currentCoord.direction + num * Time.deltaTime, m_currentCoord.elevation);
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

	private void IdleTransitions()
	{
		if (m_state != State.RotationToTargetPosition && idleRotation && !m_skipToRotateByMouse)
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

	public void GetCurrentDirectionAndElevation(out SphCoord result)
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

	private void SetDirectionAndElevation(float direction, float elevation, bool withOffset = true)
	{
		Vector3 vector = Quaternion.Euler(elevation, direction, 0f) * Vector3.forward;
		Vector3 position = (withOffset ? GetRotationCenterWithOffset() : m_rotationCenter) - vector * m_distanceFromCenter;
		base.transform.position = position;
		Vector3 vector2 = (withOffset ? GetRotationCenterWithOffset() : m_rotationCenter);
		base.transform.rotation = Quaternion.LookRotation(vector2 - base.transform.position);
		m_currentCoord.direction = direction;
		m_currentCoord.elevation = elevation;
	}

	public void SetDirectionAndElevation(float direction, float elevation)
	{
		Vector3 vector = Quaternion.Euler(elevation, direction, 0f) * Vector3.forward;
		Vector3 position = GetRotationCenterWithOffset() - vector * m_distanceFromCenter;
		if (disableNegativeElevation && position.y < 0.15f)
		{
			position.y = 0.15f;
		}
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

	private void ResetCenter()
	{
		GameObject gameObject = GameObject.Find("CarPositionMarker");
		SetCenter((gameObject == null) ? base.transform.position : gameObject.transform.position);
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
		instance = this;
		m_returnToIdleSec = secondsToReturnToIdleFromForcedRotation;
		m_camera = GetComponentInChildren<Camera>();
		m_cinemachine = GetComponentInChildren<CinemachineBrain>();
		if (!(m_camera == null) && !(m_cinemachine == null))
		{
			ResetFov();
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
			ResetCenter();
			m_distanceFromCenterMax = m_distanceFromCenter;
			m_centerDistanceSetup = m_distanceFromCenter;
		}
	}

	private void OnDestroy()
	{
		instance = null;
	}

	private void Update()
	{
		if (Overlay.instance.IsResetInput())
		{
			Input.ResetInputAxes();
			return;
		}
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
		float mouseScrollDelta = GetMouseScrollDelta();
		float num = ((mouseScrollDelta != 0f) ? mouseScrollDelta : GetGamepadZoomDelta());
		if (num != 0f)
		{
			if (useFovZoom)
			{
				SetFov(fov + num * 0.02f * m_fovZoomSpeed);
			}
			else if (useZoom)
			{
				float num2 = num * 0.02f * m_distanceZoomSpeed;
				SetDistanceFromCenterSmooth(m_distanceFromCenter + num2);
			}
		}
	}

	private float GetMouseScrollDelta()
	{
		if (UIUtils.IsPointerOverGameObject())
		{
			return 0f;
		}
		return 0f - Input.mouseScrollDelta.y;
	}

	private float GetGamepadZoomDelta()
	{
		if (!allowGamepadZoom)
		{
			return 0f;
		}
		InputManager inputManager = InputManager.instance;
		return 0f + inputManager.GetActionValue(ActionType.ZoomOut) * 0.5f - inputManager.GetActionValue(ActionType.ZoomIn) * 0.5f;
	}
}
