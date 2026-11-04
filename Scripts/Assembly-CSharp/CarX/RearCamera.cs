using System.Collections.Generic;
using CarX.Internal;
using Cinemachine;
using DI;
using GameInput;
using UnityEngine;

namespace CarX
{
	[AddComponentMenu("")]
	public class RearCamera : BaseCamera
	{
		private class WheelWrap
		{
			private List<Wheel> wheels;

			public WheelWrap(Car m_car)
			{
				wheels = new List<Wheel>();
				if (m_car != null)
				{
					if (m_car.gearType == GearType.GEAR_FWD)
					{
						wheels.Add(m_car.GetWheel(WheelIndex.FrontLeft));
						wheels.Add(m_car.GetWheel(WheelIndex.FrontRight));
						return;
					}
					if (m_car.gearType == GearType.GEAR_RWD)
					{
						wheels.Add(m_car.GetWheel(WheelIndex.RearLeft));
						wheels.Add(m_car.GetWheel(WheelIndex.RearRight));
						return;
					}
					wheels.Add(m_car.GetWheel(WheelIndex.FrontLeft));
					wheels.Add(m_car.GetWheel(WheelIndex.FrontRight));
					wheels.Add(m_car.GetWheel(WheelIndex.RearLeft));
					wheels.Add(m_car.GetWheel(WheelIndex.RearRight));
				}
			}

			public float GetSlipAmount()
			{
				float num = 0f;
				int num2 = 0;
				for (int i = 0; i < wheels.Count; i++)
				{
					float w = wheels[i].w;
					float w2 = wheels[i].w0;
					if (Mathf.Abs(w) > 10f || Mathf.Abs(w2) > 10f)
					{
						num2++;
						num += Mathf.Abs(w2 - w);
					}
				}
				if (num2 <= 0)
				{
					return 0f;
				}
				return num / (float)num2;
			}
		}

		[SerializeField]
		private RaceCar m_target;

		[SerializeField]
		private float m_distance = 7f;

		[SerializeField]
		private float m_cameraFOV = 60f;

		[SerializeField]
		private float m_minDistance = 2f;

		[SerializeField]
		private float m_maxDistance = 12f;

		[SerializeField]
		private float m_height = 1.5f;

		[SerializeField]
		private float m_minHeight = 1.2f;

		[SerializeField]
		private float m_maxHeight = 1.7f;

		[SerializeField]
		private float m_heightSpeed = 10f;

		[SerializeField]
		private float m_alignSpeed = 3f;

		[SerializeField]
		private float m_rotationStiffness = 0.1f;

		[SerializeField]
		private float m_accFactor = 0.4f;

		[SerializeField]
		private float m_accSpeed = 2f;

		[SerializeField]
		private string m_focusPointTag = "Untagged";

		[SerializeField]
		private float m_focusTime;

		[SerializeField]
		private float m_focusSuspensionDist = 4f;

		[SerializeField]
		private float m_focusSuspensionForce = 4f;

		[SerializeField]
		private Vector3 m_focusPointOffset = Vector3.zero;

		[SerializeField]
		private Vector3 m_autoCalcFocusPoint = Vector3.zero;

		[SerializeField]
		private Vector3 m_bottomCalcPoint = Vector3.zero;

		[SerializeField]
		private bool m_detectCollision;

		[SerializeField]
		private LayerMask m_collisionLayers;

		[SerializeField]
		private bool m_allowEffects;

		[SerializeField]
		private float m_distanceShift = -0.5f;

		[SerializeField]
		private bool m_canTwitch = true;

		[SerializeField]
		private bool m_canShake;

		[SerializeField]
		private float m_startSlip = 30f;

		[SerializeField]
		private float m_hFreq_0 = 0.05f;

		[SerializeField]
		private float m_hFreq_1 = 0.01f;

		[SerializeField]
		private float m_hfAmplitude_0 = 0.02f;

		[SerializeField]
		private float m_hfAmplitude_1 = 0.05f;

		[SerializeField]
		private Vector2 m_hfVeloScale_0 = new Vector2(3f, 1f);

		[SerializeField]
		private Vector2 m_hfVeloScale_1 = new Vector2(10f, 1f);

		[SerializeField]
		private float m_hfXYRatio;

		[SerializeField]
		private float m_startVel = 10f;

		[SerializeField]
		private float m_lFreq_0 = 0.1f;

		[SerializeField]
		private float m_lFreq_1 = 0.05f;

		[SerializeField]
		private float m_lfAmplitude_0 = 0.02f;

		[SerializeField]
		private float m_lfAmplitude_1 = 0.05f;

		[SerializeField]
		private float m_lfXYRatio;

		[SerializeField]
		private Vector3 m_focusShiftAmpl = new Vector3(0f, -0.1f, 3f);

		[SerializeField]
		private bool m_calcFocusPointOffset;

		[SerializeField]
		private bool m_addFocusShift = true;

		[SerializeField]
		private bool m_calcDistanceFromBumper;

		[SerializeField]
		private bool m_calcHeightFromBottomCar;

		private Transform m_transform;

		private Transform m_cameraTransform;

		private Transform m_focusTransform;

		private Vector3 m_focusSpeed;

		private CarX.Internal.RearCamera m_cameraCtrl;

		private CinemachineVirtualCamera m_virtualCamera;

		private WheelWrap m_wheelWrap;

		private bool m_effectIsOn;

		private bool m_effectIsOnLocal;

		private Vector3 m_focus;

		private Vector3 m_focusDelta = Vector3.zero;

		private Vector3 m_twitchAtDrift;

		private Vector3 m_twitchAtVelo;

		private float timer_0;

		private float timer_1;

		private Vector3 m_smoothVelocity;

		private float m_smoothFreq = 0.08f;

		private Vector3 m_focusShift = Vector3.zero;

		private float m_focusDampSpeed;

		private bool m_resetInterpolation;

		private GamePrefs m_prefs;

		private float m_distanceFromBumper;

		private float m_curFocusTime;

		private bool isLoadedTargetModel;

		private bool m_firstEnter = true;

		public RaceCar target => m_target;

		public LayerMask layerCollision => m_collisionLayers;

		public float focusTime => m_focusTime;

		public bool effectIsOn
		{
			get
			{
				return m_effectIsOn;
			}
			set
			{
				m_effectIsOn = value;
				ResetParams();
			}
		}

		public bool effectIsOnLocal
		{
			get
			{
				return m_effectIsOnLocal;
			}
			set
			{
				m_effectIsOnLocal = value;
			}
		}

		public bool shakeCamOn
		{
			get
			{
				if (m_canShake)
				{
					return m_canTwitch;
				}
				return false;
			}
			set
			{
				m_canShake = value;
				ResetParams();
			}
		}

		public float rotationY
		{
			get
			{
				return m_cameraCtrl.m_posX;
			}
			set
			{
				m_cameraCtrl.m_posX = value;
			}
		}

		public float speedRotationOrbit => m_cameraCtrl.xSpeedControlRotate;

		public float distance => m_cameraCtrl.distance;

		private void Awake()
		{
			m_transform = base.transform;
			m_cameraCtrl = new CarX.Internal.RearCamera();
			m_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
			m_cameraTransform = ((m_virtualCamera != null) ? m_virtualCamera.transform : null);
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			ResetParams();
		}

		private void Start()
		{
			SetTarget(m_target);
		}

		private void OnEnable()
		{
			if (m_focusTime == 0f)
			{
				ResetFocus();
			}
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_cameraFOV;
			}
		}

		public void ResetFocus()
		{
			m_curFocusTime = 0f;
			m_focusSpeed = Vector3.zero;
		}

		private void OnDisable()
		{
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = 60f;
			}
		}

		private void OnValidate()
		{
			ResetParams();
		}

		private void Update()
		{
			if ((bool)m_target && !m_target.carModel.IsLoading() && !isLoadedTargetModel && !m_target.carModel.IsLoading())
			{
				isLoadedTargetModel = true;
				SetTarget(m_target);
			}
		}

		private void LateUpdate()
		{
			if (!IsTargetValid(m_target))
			{
				return;
			}
			float deltaTime = Time.deltaTime;
			RefreshTargetIfNeeded(ref m_focusTransform, m_focusPointTag);
			if (m_focusTransform == null)
			{
				return;
			}
			Car carX = m_target.carX;
			ModifyFocusPoint(m_focusTransform, carX.velocity);
			m_focus = m_focusPointOffset;
			if (effectIsOnLocal)
			{
				m_focus += m_focusShift;
			}
			m_cameraCtrl.Update(m_focusTransform, m_focus, carX.velocity, Mathf.Min(deltaTime, 0.1f));
			Vector3 position = m_transform.position;
			Quaternion rotation = m_transform.rotation;
			Vector3 position2 = m_target.getTransform.position;
			Quaternion rotation2 = m_target.getTransform.rotation;
			Vector3 position3 = m_cameraCtrl.position;
			Quaternion rotation3 = m_cameraCtrl.rotation;
			float sqrMagnitude = (position - position2).sqrMagnitude;
			m_curFocusTime = Mathf.Lerp(m_curFocusTime, (sqrMagnitude < m_focusSuspensionDist * m_focusSuspensionDist) ? 0f : m_focusTime, deltaTime * m_focusSuspensionForce);
			if (m_resetInterpolation)
			{
				m_resetInterpolation = false;
			}
			else
			{
				float num = Quaternion.Angle(rotation, rotation2);
				if (m_focusTime > 0f && num > 30f && InputRotationActive())
				{
					m_curFocusTime -= Mathf.Lerp(0f, m_curFocusTime + 1f, deltaTime);
					if (m_curFocusTime < 0f)
					{
						m_curFocusTime = 0f;
					}
				}
				if (m_firstEnter && m_focusTime > 0f)
				{
					float y = m_cameraCtrl.position.y;
					float y2 = m_target.getTransform.position.y;
					Vector3 vector = new Vector3(0f, y - y2, 0f);
					position3 = m_target.getTransform.position + vector;
					m_firstEnter = false;
				}
				else
				{
					position3 = Vector3.SmoothDamp(position, position3, ref m_focusSpeed, m_curFocusTime);
					if (shakeCamOn && deltaTime > 0f)
					{
						position3 += m_focusDelta;
					}
				}
			}
			m_transform.SetPositionAndRotation(position3, rotation3);
		}

		private bool InputRotationActive()
		{
			if (InputManager.instance.IsLastActiveJoystickAxis2D(m_prefs.globals.cameraOrbitStick, out var axis))
			{
				return axis != Vector2.zero;
			}
			return Input.GetMouseButton(1);
		}

		public override void SetFOV(float fov)
		{
			m_cameraFOV = fov;
			if (base.isActiveAndEnabled && (bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_cameraFOV;
			}
		}

		public override void ResetInterpolation()
		{
			m_cameraCtrl.SkipFirstInterpolation();
			m_resetInterpolation = true;
		}

		public override void SetTarget(RaceCar target)
		{
			m_target = target;
			isLoadedTargetModel = false;
			if (!m_target)
			{
				return;
			}
			m_wheelWrap = new WheelWrap(m_target.carX);
			m_focusTransform = m_target.getTransform;
			if (!m_target.carModel.IsLoading())
			{
				CarDesc desc = new CarDesc();
				target.carX.GetCarDesc(ref desc);
				isLoadedTargetModel = true;
				if (m_calcFocusPointOffset)
				{
					Vector3 center = desc.weight.bounds.center;
					m_autoCalcFocusPoint = center;
				}
				else
				{
					m_autoCalcFocusPoint = Vector3.zero;
				}
				if (m_calcHeightFromBottomCar)
				{
					m_bottomCalcPoint = desc.weight.bounds.center;
					m_bottomCalcPoint.y = desc.weight.bounds.min.y;
				}
				else
				{
					m_bottomCalcPoint = Vector3.zero;
				}
				if (m_calcDistanceFromBumper)
				{
					m_distanceFromBumper = desc.weight.bounds.size.z * 0.5f;
					m_cameraCtrl.distance = (effectIsOnLocal ? (m_distanceFromBumper + m_distance + m_distanceShift) : (m_distanceFromBumper + m_distance));
				}
				else
				{
					m_cameraCtrl.distance = (effectIsOnLocal ? (m_distance + m_distanceShift) : m_distance);
				}
				m_cameraCtrl.focusPoint = m_autoCalcFocusPoint;
				m_cameraCtrl.bottomPoint = m_bottomCalcPoint;
				m_cameraCtrl.autoCalcFocusPoint = m_calcFocusPointOffset;
			}
			base.needRefreshTarget = true;
			ResetParams();
		}

		private void ResetParams()
		{
			if (m_cameraCtrl != null)
			{
				effectIsOnLocal = m_allowEffects && m_effectIsOn && (m_target == null || !m_target.isNetworkCar);
				m_cameraCtrl.detectCollision = m_detectCollision;
				m_cameraCtrl.collisionLayers = m_collisionLayers;
				if (m_calcDistanceFromBumper)
				{
					m_cameraCtrl.distance = (effectIsOnLocal ? (m_distanceFromBumper + m_distance + m_distanceShift) : (m_distanceFromBumper + m_distance));
				}
				else
				{
					m_cameraCtrl.distance = (effectIsOnLocal ? (m_distance + m_distanceShift) : m_distance);
				}
				m_cameraCtrl.minDistance = m_minDistance;
				m_cameraCtrl.maxDistance = m_maxDistance;
				m_cameraCtrl.height = m_height;
				m_cameraCtrl.minHeight = m_minHeight;
				m_cameraCtrl.maxHeight = m_maxHeight;
				m_cameraCtrl.heightSpeed = m_heightSpeed;
				m_cameraCtrl.alignSpeed = m_alignSpeed;
				m_cameraCtrl.rotationStiffness = (effectIsOnLocal ? m_rotationStiffness : 0.1f);
				m_cameraCtrl.accFactor = m_accFactor;
				m_cameraCtrl.accSpeed = m_accSpeed;
			}
		}

		private void ModifyFocusPoint(Transform target, Vector3 velocity)
		{
			if (!effectIsOnLocal && !shakeCamOn)
			{
				m_focusDelta = Vector3.zero;
				m_focusShift = Vector3.zero;
				return;
			}
			float magnitude = velocity.magnitude;
			Vector3 vector = Vector3.zero;
			bool flag = true;
			if (m_wheelWrap != null)
			{
				float slipAmount = m_wheelWrap.GetSlipAmount();
				if (slipAmount > m_startSlip)
				{
					flag = false;
					if (Time.time > timer_0)
					{
						float num = Interpolate(m_hFreq_0, m_hFreq_1, slipAmount, m_startSlip, 100f);
						timer_0 = Time.time + num;
						float num2 = Interpolate(m_hfAmplitude_0, m_hfAmplitude_1, slipAmount, m_startSlip, 100f);
						num2 *= Interpolate(m_hfVeloScale_0.y, m_hfVeloScale_1.y, magnitude, m_hfVeloScale_0.x, m_hfVeloScale_1.x);
						Vector3 vector2 = target.up * RandomSign();
						Vector3 vector3 = target.right * RandomSign();
						if (shakeCamOn)
						{
							m_twitchAtDrift = (vector2 * (1f - m_hfXYRatio) + vector3 * m_hfXYRatio).normalized * num2;
						}
						else
						{
							m_twitchAtDrift = Vector3.zero;
						}
						m_smoothFreq = num;
					}
				}
				else
				{
					m_twitchAtDrift = Vector3.zero;
				}
				vector = m_twitchAtDrift;
			}
			if (flag && magnitude > m_startVel)
			{
				if (Time.time > timer_1)
				{
					float num3 = Interpolate(m_lFreq_0, m_lFreq_1, magnitude, m_startVel, 30f);
					timer_1 = Time.time + num3;
					float num4 = Interpolate(m_lfAmplitude_0, m_lfAmplitude_1, magnitude, m_startVel, 30f);
					Vector3 vector4 = target.up * RandomSign();
					Vector3 vector5 = target.right * RandomSign();
					if (shakeCamOn)
					{
						m_twitchAtVelo = (vector4 * (1f - m_lfXYRatio) + vector5 * m_lfXYRatio).normalized * num4;
					}
					else
					{
						m_twitchAtVelo = Vector3.zero;
					}
					m_smoothFreq = num3;
				}
			}
			else
			{
				m_twitchAtVelo = Vector3.zero;
			}
			vector += m_twitchAtVelo;
			m_focusDelta = Vector3.SmoothDamp(m_focusDelta, vector, ref m_smoothVelocity, m_smoothFreq);
			float num5 = 0f;
			if (magnitude > m_startVel)
			{
				Vector3 forward = target.forward;
				forward.y = 0f;
				Vector3 forward2 = m_transform.forward;
				forward2.y = 0f;
				float magnitude2 = Vector3.Cross(forward.normalized, forward2.normalized).magnitude;
				num5 = Mathf.Clamp01(magnitude * 0.036f) * magnitude2;
			}
			if (m_addFocusShift)
			{
				float deltaTime = Time.deltaTime;
				if (deltaTime > 0f)
				{
					m_focusShift.x = 0f;
					m_focusShift.y = Mathf.Lerp(m_focusShift.y, num5 * m_focusShiftAmpl.y, 2f * deltaTime);
					m_focusShift.z = Mathf.SmoothDamp(m_focusShift.z, num5 * m_focusShiftAmpl.z, ref m_focusDampSpeed, 1f);
				}
			}
		}

		private float Interpolate(float val_0, float val_1, float prop, float prop_0, float prop_1)
		{
			if (prop_0 == prop_1)
			{
				return val_1;
			}
			float value = val_0 + (val_1 - val_0) * (prop - prop_0) / (prop_1 - prop_0);
			float min = Mathf.Min(val_0, val_1);
			float max = Mathf.Max(val_0, val_1);
			return Mathf.Clamp(value, min, max);
		}

		private int RandomSign()
		{
			if (Random.Range(1, 10) % 2 != 0)
			{
				return -1;
			}
			return 1;
		}

		private void OnDrawGizmos()
		{
			if (m_target != null)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawSphere(m_target.transform.TransformPoint(m_autoCalcFocusPoint), 0.15f);
			}
		}
	}
}
