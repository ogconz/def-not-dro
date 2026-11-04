using System;
using DI;
using GameInput;
using PrefsModels;
using Rewired;
using UnityEngine;

namespace CarX.Internal
{
	public class CockpitCamera
	{
		private const float JOYSTICK_DELTA_STEP = 3f;

		private const float JOYSTICK_LERP_COEF = 10f;

		private const float MOUSE_LERP_COEF = 7f;

		private const float TIMEOUT_MOUSE_FALLBACK = 2f;

		private const float FALLBACK_LERP_COEFF = 30f;

		private const float MAX_ANGLE_ROTATION_VIEW = 90f;

		private const float ACCEL_SCALE_TO_HEAD_POS = 0.001f;

		private const float HEADVIEW_VELOCITY_SCALE = 0.7f;

		private const float ORBIT_STICK_INPUT_SCALE = 90f;

		private const float MOUSE_INPUT_SCALE = 0.45f;

		private const float CAMERA_SMOOTH_TWEAK = 0.5f;

		private const float ACCEL_SMOOTH_TIME = 0.08f;

		private const float ACCEL_SMOOTH_TWEAK = 0.5f;

		private const float MIN_ACCEL_SMOOTH = 0.05f;

		private const float ANGLE_EPSILON = 1E-06f;

		private CarX.CockpitCamera m_carXCockpitCam;

		private Vector3 m_headPosition;

		private Vector3 m_position;

		private Vector3 m_accelAccum;

		private Vector3 m_accelVelocity;

		private Vector3 m_headUp = Vector3.up;

		private Vector3 m_headForwardView;

		private Vector3 m_cachedHeadView;

		private Vector3 m_prevDesiredView;

		private Quaternion m_rotation;

		private Quaternion m_prevRotation = Quaternion.identity;

		private bool m_lastUpdateJoystickAcquired;

		private bool m_lastUpdateMouseAcquired;

		private bool m_joystickInputAcquired;

		private bool m_mouseInputAcquired;

		private bool m_isFallbackActive;

		private bool m_lerpAfterJoystick;

		private bool m_isImpovedMode;

		private float m_lastJoystickInputValue;

		private float m_currentRotSpeed;

		private float m_rightSideMaxDeltaAngle;

		private float m_leftSideMaxDeltaAngle;

		private float m_cachedXRotation;

		private float m_lastTimeMouseActive;

		private float m_lastTimeJoystickActive;

		private GamePrefs m_prefs;

		public bool smoothen => m_carXCockpitCam.smoothen;

		public float smoothSpeed => m_carXCockpitCam.smoothSpeed;

		public Vector3 offset => m_carXCockpitCam.offset;

		public float velocityScale => m_carXCockpitCam.velocityScale;

		public Vector3 accelScale => m_carXCockpitCam.accelScale;

		public Vector3 position => m_position;

		public Quaternion rotation => m_rotation;

		public float maxRotSpeed => m_carXCockpitCam.maxRotSpeed;

		public float maxRotAngle => m_carXCockpitCam.maxRotAngle;

		public float cameraSmooth => m_carXCockpitCam.cameraSmooth;

		public float accelSmooth => m_carXCockpitCam.accelSmooth;

		public float dynamicCamDeadZone => m_carXCockpitCam.dynamicCamDeadZone;

		public GlobalModel.DynamicCockpitCamMode mode => m_carXCockpitCam.dynamicCameraMode;

		public bool updUp => m_carXCockpitCam.updUp;

		public bool isInputAcquired
		{
			get
			{
				if (!m_joystickInputAcquired)
				{
					return m_mouseInputAcquired;
				}
				return true;
			}
		}

		public CockpitCamera(CarX.CockpitCamera carXCockpitCam)
		{
			m_carXCockpitCam = carXCockpitCam;
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			CarCameraSwitchHandler.OnChangeCameraViewEvent = (Action<bool>)Delegate.Remove(CarCameraSwitchHandler.OnChangeCameraViewEvent, new Action<bool>(ResetInputRotationFlags));
			CarCameraSwitchHandler.OnChangeCameraViewEvent = (Action<bool>)Delegate.Combine(CarCameraSwitchHandler.OnChangeCameraViewEvent, new Action<bool>(ResetInputRotationFlags));
		}

		public void UpdateCamera(bool improvedModeActive, Transform target, Vector3 localPlacePoint, Vector3 localVel, Vector3 localAcc, float dt, bool instant = false)
		{
			m_isImpovedMode = improvedModeActive;
			if (m_isImpovedMode)
			{
				UpdateImproveCameraMode(target, localPlacePoint, localVel, localAcc, dt);
			}
			else
			{
				UpdateLegacyCameraMode(target, localPlacePoint, localVel, localAcc, dt);
			}
			Quaternion headRotation = Quaternion.LookRotation(m_headForwardView, m_headUp);
			TryModifyRotationUsingInput(ref headRotation, dt);
			if (m_isImpovedMode && !isInputAcquired)
			{
				ApplyImproveEffectsToHeadRotation(ref headRotation, dt, instant);
			}
			ApplyRotation(headRotation, target);
			m_prevRotation = headRotation;
			m_lastUpdateMouseAcquired = m_mouseInputAcquired;
			m_lastUpdateJoystickAcquired = m_joystickInputAcquired;
		}

		public void ResetInputRotationFlags(bool isCamOutside)
		{
			if (isCamOutside)
			{
				m_prevRotation = Quaternion.identity;
				m_joystickInputAcquired = false;
				m_mouseInputAcquired = false;
				m_lastUpdateJoystickAcquired = false;
				m_lastUpdateMouseAcquired = false;
			}
		}

		private void TryModifyRotationUsingInput(ref Quaternion rotation, float dt)
		{
			bool flag = States.current is BaseRaceState baseRaceState && baseRaceState.raceContext != null && baseRaceState.raceContext.isActive;
			bool flag2 = false;
			flag2 = XRModeManager.IsXRActive();
			if (flag && !flag2 && IsInputRotationAcquired(out var inputRotVal, dt))
			{
				rotation = GetRotationByInputValue(inputRotVal, dt);
			}
			CheckIfNeedActivateFallback();
			if (m_isFallbackActive || !flag)
			{
				if ((!m_lerpAfterJoystick && Time.time - m_lastTimeMouseActive < 2f) || Time.timeScale == 0f)
				{
					rotation = m_prevRotation;
					return;
				}
				rotation = GetFallbackRotationOnDisabledInput();
				m_lastJoystickInputValue = 0f;
			}
		}

		private Quaternion GetFallbackRotationOnDisabledInput()
		{
			float num = (m_lerpAfterJoystick ? m_lastTimeJoystickActive : (m_lastTimeMouseActive + 2f));
			float num2 = Time.time - num;
			float dt = num2 / (30f * (1f + num2));
			LerpDesiredViewFromPrevious(ref m_headForwardView, dt, useSlerp: true);
			return Quaternion.LookRotation(m_headForwardView, m_headUp);
		}

		private void CheckIfNeedActivateFallback()
		{
			if (m_lastUpdateJoystickAcquired && !m_joystickInputAcquired)
			{
				m_lerpAfterJoystick = true;
				m_isFallbackActive = true;
			}
			if (m_lastUpdateMouseAcquired && !m_mouseInputAcquired)
			{
				m_lerpAfterJoystick = false;
				m_isFallbackActive = true;
			}
			if (Mathf.Abs(Vector3.Dot(m_prevDesiredView, m_headForwardView) - 1f) < 1E-06f || isInputAcquired)
			{
				m_isFallbackActive = false;
			}
		}

		private void UpdateLegacyCameraMode(Transform target, Vector3 localPlacePoint, Vector3 localVel, Vector3 localAcc, float dt)
		{
			localPlacePoint += offset;
			if (updUp)
			{
				m_headUp = m_headPosition - localPlacePoint + Vector3.up * offset.y;
				m_headUp.x *= -1f;
				m_headUp.z *= 0.5f;
				MathExt.FastNormalize(ref m_headUp);
			}
			localVel.z = Mathf.Max(localVel.z, 0f);
			float sqrMagnitude = localVel.sqrMagnitude;
			if (sqrMagnitude > 100f)
			{
				float num = 1f / Mathf.Sqrt(sqrMagnitude);
				localVel *= num;
			}
			else
			{
				localVel *= 0.1f;
			}
			m_headForwardView = Vector3.forward + localVel * 0.7f;
			m_headForwardView -= m_headUp * Vector3.Dot(m_headForwardView, m_headUp);
			UpdateHeadPositionBy(target, localPlacePoint, localAcc, isLegacyMode: true, dt);
		}

		private void UpdateImproveCameraMode(Transform carTarget, Vector3 localPlacePoint, Vector3 localVel, Vector3 localAcc, float dt)
		{
			m_headUp = Vector3.up;
			localPlacePoint += offset;
			if (localVel.z < 0f)
			{
				localVel.z *= -1f;
			}
			m_headForwardView = CalculateParametrisedFwd(localVel, velocityScale);
			UpdateHeadPositionBy(carTarget, localPlacePoint, localAcc, isLegacyMode: false, dt);
		}

		private void ApplyImproveEffectsToHeadRotation(ref Quaternion headRotation, float dt, bool instant)
		{
			if (!m_isFallbackActive)
			{
				ClampRotationY(ref headRotation, maxRotAngle);
			}
			if (dynamicCamDeadZone > 0f && !instant)
			{
				headRotation = ApplyDeadZoneSmooth(m_prevRotation, headRotation, dynamicCamDeadZone);
			}
			if (!instant && !m_isFallbackActive)
			{
				headRotation = ApplySmoothDamp(m_prevRotation, headRotation, Vector3.up, ref m_currentRotSpeed, maxRotSpeed, 0.5f * cameraSmooth, dt);
			}
		}

		private bool IsInputRotationAcquired(out float inputRotVal, float dt)
		{
			inputRotVal = 0f;
			m_mouseInputAcquired = false;
			m_joystickInputAcquired = false;
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (instance == null)
			{
				return false;
			}
			CameraSwitch instance2 = CameraSwitch.instance;
			if (instance2 != null && !instance2.rotateMode)
			{
				return false;
			}
			if (instance.IsLastActiveJoystickAxis2D(m_prefs.globals.cameraOrbitStick, out var axis))
			{
				dt *= 3f;
				inputRotVal = m_lastJoystickInputValue + Mathf.Clamp(axis.x - m_lastJoystickInputValue, 0f - dt, dt);
				m_lastJoystickInputValue = inputRotVal;
				m_joystickInputAcquired = axis != Vector2.zero;
				if (m_joystickInputAcquired)
				{
					m_lastTimeJoystickActive = Time.time;
				}
			}
			else if (instance.GetActionValue(ActionType.FreeCamView) > 0f)
			{
				inputRotVal = ReInput.controllers.Mouse.GetAxis(0);
				m_lastTimeMouseActive = Time.time;
				m_mouseInputAcquired = true;
			}
			return isInputAcquired;
		}

		private void UpdateHeadPositionBy(Transform carTarget, Vector3 localPlacePoint, Vector3 localAcc, bool isLegacyMode, float dt)
		{
			m_headPosition = localPlacePoint;
			ModifyOffsetLocalHeadPositionByAccel(localAcc, isLegacyMode, dt);
			m_position = carTarget.TransformPoint(m_headPosition);
		}

		private void ModifyOffsetLocalHeadPositionByAccel(Vector3 localAcc, bool isLegacyMode, float dt)
		{
			if (isLegacyMode)
			{
				m_accelAccum = Vector3.SmoothDamp(m_accelAccum, localAcc, ref m_accelVelocity, 0.08f, float.MaxValue, dt);
				m_headPosition.x += m_accelAccum.x * 0.001f * accelScale.x;
				m_headPosition.y -= m_accelAccum.y * 0.001f * accelScale.y;
				m_headPosition.z -= m_accelAccum.z * 0.001f * accelScale.z;
			}
			else
			{
				float smoothTime = Mathf.Max(0.05f, 0.5f * accelSmooth);
				m_accelAccum = Vector3.SmoothDamp(m_accelAccum, localAcc, ref m_accelVelocity, smoothTime, float.MaxValue, dt);
				m_headPosition.z -= m_accelAccum.z * 0.001f * accelScale.z;
			}
		}

		private void ApplyRotation(Quaternion rotation, Transform carTarget)
		{
			m_rotation = carTarget.rotation * rotation * m_carXCockpitCam.eulerQuaternion;
		}

		private Quaternion GetRotationByInputValue(float inputValue, float dt)
		{
			if (!m_lastUpdateJoystickAcquired && !m_lastUpdateMouseAcquired)
			{
				Vector3 headViewToStartRotate = GetHeadViewToStartRotate();
				CacheViewOnFirtsAcquireInput(headViewToStartRotate);
				CalculateEstimatedAnglesByView(headViewToStartRotate);
			}
			Vector3 view = Quaternion.AngleAxis(GetTargetAngleByInputValue(inputValue), Vector3.up) * m_cachedHeadView;
			LerpDesiredViewFromPrevious(ref view, dt);
			Quaternion result = Quaternion.LookRotation(view, m_headUp);
			ClampRotationY(ref result, 90f);
			return result;
		}

		private Vector3 GetHeadViewToStartRotate()
		{
			if (m_isImpovedMode && (dynamicCamDeadZone > 0f || cameraSmooth > 0f))
			{
				return m_prevRotation * Vector3.forward;
			}
			if (!m_isFallbackActive)
			{
				return m_headForwardView;
			}
			return m_prevDesiredView;
		}

		private void CacheViewOnFirtsAcquireInput(Vector3 view)
		{
			m_cachedHeadView = view;
			m_prevDesiredView = view;
			m_cachedXRotation = 0f;
		}

		private void LerpDesiredViewFromPrevious(ref Vector3 view, float dt, bool useSlerp = false)
		{
			float t = dt * (m_mouseInputAcquired ? 7f : 10f);
			view = (useSlerp ? Vector3.Slerp(m_prevDesiredView, view, t) : Vector3.Lerp(m_prevDesiredView, view, t));
			m_prevDesiredView = new Vector3(view.x, view.y, Mathf.Abs(view.z));
		}

		private float GetTargetAngleByInputValue(float inputValue)
		{
			float scaledInputValue = GetScaledInputValue(inputValue);
			float result;
			if (m_mouseInputAcquired)
			{
				result = (m_cachedXRotation = Mathf.Clamp(m_cachedXRotation + scaledInputValue, m_leftSideMaxDeltaAngle, m_rightSideMaxDeltaAngle));
			}
			else
			{
				scaledInputValue *= ((scaledInputValue < 0f) ? (0f - m_leftSideMaxDeltaAngle / 90f) : (m_rightSideMaxDeltaAngle / 90f));
				result = Mathf.Clamp(scaledInputValue, m_leftSideMaxDeltaAngle, m_rightSideMaxDeltaAngle);
			}
			return result;
		}

		private float GetScaledInputValue(float inputValue)
		{
			return inputValue * (m_mouseInputAcquired ? 0.45f : 90f);
		}

		private void CalculateEstimatedAnglesByView(Vector3 view)
		{
			float num = Vector3.Angle(view, Vector3.forward);
			num *= Mathf.Sign(view.x);
			m_leftSideMaxDeltaAngle = -90f - num;
			m_rightSideMaxDeltaAngle = 90f - num;
		}

		private Vector3 CalculateParametrisedFwd(Vector3 localVel, float velScale)
		{
			Vector3 v = Vector3.Lerp(MathExt.vForward, localVel, velScale);
			v -= Vector3.up * Vector3.Dot(v, Vector3.up);
			MathExt.FastNormalizeSafe(ref v);
			return v;
		}

		private Quaternion ApplyDeadZoneSmooth(Quaternion currentRotation, Quaternion desiredRotation, float deadZone)
		{
			float value = 1f;
			deadZone = Mathf.Clamp01(deadZone);
			if (deadZone > 0f && maxRotAngle > 0f)
			{
				value = Vector3.Angle(currentRotation * Vector3.forward, desiredRotation * Vector3.forward) / (deadZone * maxRotAngle);
				value *= value;
			}
			value = Mathf.Clamp01(value);
			return Quaternion.Lerp(currentRotation, desiredRotation, value);
		}

		private Quaternion ApplySmoothDamp(Quaternion currentRotation, Quaternion desiredRotation, Vector3 axis, ref float currentSpeed, float maxSpeed, float smoothParam, float dt)
		{
			if (dt == 0f)
			{
				return currentRotation;
			}
			smoothParam = Mathf.Max(smoothParam, 0.025f);
			float current = Vector3.SignedAngle(Vector3.forward, currentRotation * Vector3.forward, axis);
			float target = Vector3.SignedAngle(Vector3.forward, desiredRotation * Vector3.forward, axis);
			return Quaternion.AngleAxis(Mathf.SmoothDampAngle(current, target, ref currentSpeed, smoothParam, maxSpeed, dt), axis);
		}

		private void ClampRotationY(ref Quaternion rotation, float maxRotAngle)
		{
			maxRotAngle = Mathf.Abs(maxRotAngle);
			float y = rotation.eulerAngles.y;
			y = ((y > 180f) ? (y - 360f) : y);
			if (y > maxRotAngle)
			{
				y = maxRotAngle;
			}
			else
			{
				if (!(y < 0f - maxRotAngle))
				{
					return;
				}
				y = 0f - maxRotAngle;
			}
			rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, y, rotation.eulerAngles.z);
		}
	}
}
