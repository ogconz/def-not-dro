using System.Collections;
using System.Collections.Generic;
using CarX.Internal;
using Cinemachine;
using SyncMultiplayer;
using UnityEngine;

namespace CarX
{
	[AddComponentMenu("")]
	public class FollowCamera : BaseCamera
	{
		private enum LockMode
		{
			SingleLock = 0,
			MassLock = 1
		}

		[SerializeField]
		private RaceCar m_target;

		[SerializeField]
		private CinemachineVirtualCamera m_virtualCamera;

		[SerializeField]
		private Camera m_camera;

		[SerializeField]
		private float m_trackingPointUpdateRate = 0.5f;

		[SerializeField]
		private float m_changeTrackingPointThreshold = 2f;

		[SerializeField]
		private string m_focusPointTag = "Untagged";

		[SerializeField]
		private string m_trackingCameraPointTag = "Untagged";

		private float m_fieldOfViewStore;

		private float m_nearClipPlaneStore;

		private Transform m_transform;

		private Transform m_focusTransform;

		private Vector3 m_focusPointTarget;

		private float m_focusLockedEndTime;

		private List<Transform> m_trackingCameraPoints = new List<Transform>();

		private int m_curPntIndex;

		private float m_lastCamChangedTime;

		[SerializeField]
		private float m_carFieldOfView = 12f;

		private float m_carFieldOfViewCurrent = 12f;

		private float m_carFieldOfViewTarget = 12f;

		private CarX.Internal.FollowCamera m_cameraCtrl;

		public bool trackingCustomPoints;

		private List<Transform> m_trackingCustomCameraPoints = new List<Transform>();

		private int m_curCustomPntIndex;

		private bool m_handsSwitched;

		private int m_updown;

		private LockMode m_lockMode;

		private Vector3 m_TargetVelocityOffset;

		private Vector3 m_CurrentVelocityOffset;

		private Vector3 m_FollowAngles;

		private Vector3 m_FollowVelocity;

		private Quaternion m_OriginalRotation;

		private bool m_OriginalRotationInvalid = true;

		[SerializeField]
		private float m_FollowTime = 0.2f;

		[SerializeField]
		private float m_FollowSpeed = 5f;

		[SerializeField]
		private float m_SwaySpeed = 0.5f;

		[SerializeField]
		private float m_BaseSwayAmount = 0.5f;

		[SerializeField]
		private float m_TrackingSwayAmount = 0.5f;

		[Range(-1f, 1f)]
		[SerializeField]
		private float m_TrackingBias;

		public void AddCustomCameraPoint(Transform positionTransform)
		{
			m_trackingCustomCameraPoints.Add(positionTransform);
		}

		public void RemoveCustomCameraPoint(Transform positionTransform)
		{
			m_trackingCustomCameraPoints.Remove(positionTransform);
		}

		public void ClearCustomCameraPoints()
		{
			m_trackingCustomCameraPoints.Clear();
		}

		public bool IsExistsCustomPoints()
		{
			return m_trackingCustomCameraPoints.Count > 0;
		}

		public override void SetTarget(RaceCar target)
		{
			m_lastCamChangedTime = 0f;
			m_target = target;
			if ((bool)m_target)
			{
				m_focusTransform = m_target.getTransform;
				m_focusPointTarget = m_focusTransform.position;
				base.needRefreshTarget = true;
			}
			if (base.enabled)
			{
				UpdateTrackingPoint();
				InstantApplyFocus();
			}
		}

		private void Reset()
		{
			m_virtualCamera = base.transform.root.GetComponentInChildren<CinemachineVirtualCamera>();
		}

		private void Awake()
		{
			m_transform = base.transform;
			m_cameraCtrl = new CarX.Internal.FollowCamera();
		}

		private void Start()
		{
			SetTarget(m_target);
		}

		private void OnEnable()
		{
			m_lastCamChangedTime = 0f;
			if ((bool)m_virtualCamera)
			{
				m_fieldOfViewStore = m_virtualCamera.m_Lens.FieldOfView;
				m_nearClipPlaneStore = m_virtualCamera.m_Lens.NearClipPlane;
			}
			m_handsSwitched = false;
			m_curPntIndex = -1;
			m_curCustomPntIndex = -1;
			FindTrackingPoints();
			StartCoroutine(UpdateTrackingPointCoroutine(0f, m_trackingPointUpdateRate));
			InstantApplyFocus();
			if (XRModeManager.IsXRActive())
			{
				m_carFieldOfView = 120f;
			}
		}

		private void OnDisable()
		{
			StopAllCoroutines();
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_fieldOfViewStore;
				m_virtualCamera.m_Lens.NearClipPlane = m_nearClipPlaneStore;
			}
		}

		private IEnumerator UpdateTrackingPointCoroutine(float time, float repeatRate)
		{
			yield return new WaitForSeconds(time);
			while (true)
			{
				UpdateTrackingPoint();
				yield return new WaitForSeconds(repeatRate);
			}
		}

		private void LateUpdate()
		{
			if (!IsTargetValid(m_target) || !m_virtualCamera)
			{
				return;
			}
			RefreshTargetIfNeeded(ref m_focusTransform, m_focusPointTag);
			m_carFieldOfViewTarget = m_carFieldOfView;
			m_TargetVelocityOffset = Vector3.zero;
			if (m_lockMode == LockMode.SingleLock)
			{
				m_TargetVelocityOffset = m_target.getRigidbody.velocity;
				m_focusPointTarget = m_focusTransform.position;
			}
			else
			{
				m_focusPointTarget = m_focusTransform.position;
				if (m_target.networkPlayer != null && m_target.networkPlayer.attachedRoom != null)
				{
					SubroomInfo attachedRoom = m_target.networkPlayer.attachedRoom;
					if (attachedRoom.runtimePlayers.Count > 1)
					{
						m_focusPointTarget = Vector3.zero;
						foreach (NetworkPlayer runtimePlayer in attachedRoom.runtimePlayers)
						{
							Vector3 position = runtimePlayer.userCar.transform.position;
							m_TargetVelocityOffset += runtimePlayer.userCar.getRigidbody.velocity;
							m_focusPointTarget += position;
						}
						m_focusPointTarget /= (float)attachedRoom.runtimePlayers.Count;
						m_TargetVelocityOffset /= (float)attachedRoom.runtimePlayers.Count;
					}
				}
			}
			m_CurrentVelocityOffset = (m_OriginalRotationInvalid ? m_TargetVelocityOffset : Vector3.Lerp(m_CurrentVelocityOffset, m_TargetVelocityOffset, Time.deltaTime * m_FollowSpeed));
			float magnitude = m_CurrentVelocityOffset.magnitude;
			if (magnitude > 0.01f)
			{
				_ = magnitude / 2f;
				_ = 5f;
			}
			if (m_OriginalRotationInvalid)
			{
				m_OriginalRotationInvalid = false;
				m_transform.rotation = Quaternion.LookRotation(m_focusPointTarget - m_transform.position);
				m_OriginalRotation = m_transform.localRotation;
				m_FollowVelocity = Vector3.zero;
				m_FollowAngles = Vector3.zero;
			}
			Vector2 vector = new Vector2(361f, 361f);
			base.transform.localRotation = m_OriginalRotation;
			Vector3 vector2 = base.transform.InverseTransformPoint(m_focusPointTarget);
			float value = Mathf.Atan2(vector2.x, vector2.z) * 57.29578f;
			value = Mathf.Clamp(value, (0f - vector.y) * 0.5f, vector.y * 0.5f);
			base.transform.localRotation = m_OriginalRotation * Quaternion.Euler(0f, value, 0f);
			vector2 = base.transform.InverseTransformPoint(m_focusPointTarget);
			float value2 = Mathf.Atan2(vector2.y, vector2.z) * 57.29578f;
			value2 = Mathf.Clamp(value2, (0f - vector.x) * 0.5f, vector.x * 0.5f);
			Vector3 followAngles = new Vector3(m_FollowAngles.x + Mathf.DeltaAngle(m_FollowAngles.x, value2), m_FollowAngles.y + Mathf.DeltaAngle(m_FollowAngles.y, value));
			m_FollowAngles = followAngles;
			Vector3 eulerAngles = (m_OriginalRotation * Quaternion.Euler(0f - m_FollowAngles.x, m_FollowAngles.y, 0f)).eulerAngles;
			base.transform.localRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, 0f);
			Bounds bounds = new Bounds(m_focusPointTarget, Vector3.one * 3f);
			if (m_target.networkPlayer != null && m_target.networkPlayer.attachedRoom != null)
			{
				SubroomInfo attachedRoom2 = m_target.networkPlayer.attachedRoom;
				if (attachedRoom2.runtimePlayers.Count > 1)
				{
					foreach (NetworkPlayer runtimePlayer2 in attachedRoom2.runtimePlayers)
					{
						Vector3 position2 = runtimePlayer2.userCar.transform.position;
						bounds.Encapsulate(new Bounds(position2, Vector3.one * 3f));
					}
					Matrix4x4 inverse = Matrix4x4.TRS(bounds.center - m_transform.forward * bounds.extents.magnitude, Quaternion.LookRotation(m_transform.forward, Vector3.up), Vector3.one).inverse;
					Bounds bounds2 = new Bounds(inverse.MultiplyPoint3x4(m_focusPointTarget), Vector3.one * 3f);
					Bounds bounds3 = new Bounds(m_camera.WorldToScreenPoint(m_focusPointTarget), Vector3.one * 3f);
					foreach (NetworkPlayer runtimePlayer3 in attachedRoom2.runtimePlayers)
					{
						Vector3 position3 = runtimePlayer3.userCar.transform.position;
						bounds2.Encapsulate(new Bounds(inverse.MultiplyPoint3x4(position3), Vector3.one * 3f));
						bounds3.Encapsulate(new Bounds(m_camera.WorldToScreenPoint(position3), Vector3.one * 3f));
					}
					LockMode lockMode = m_lockMode;
					Vector4 vector3 = new Vector4(-0.25f * (float)Screen.width, -0.25f * (float)Screen.height);
					vector3.z = (float)Screen.width - vector3.x;
					vector3.w = (float)Screen.height - vector3.y;
					if (bounds3.min.x < vector3.x || bounds3.min.y < vector3.y || bounds3.max.x > vector3.z || bounds3.max.y > vector3.w || bounds.extents.magnitude > 40f)
					{
						m_lockMode = LockMode.SingleLock;
					}
					else if (m_lockMode == LockMode.SingleLock)
					{
						if (bounds3.min.x > 0f && bounds3.min.y > 0f && bounds3.max.x < (float)Screen.width && bounds3.max.y < (float)Screen.height && bounds.extents.magnitude < 30f)
						{
							m_lockMode = LockMode.MassLock;
						}
					}
					else
					{
						m_lockMode = LockMode.MassLock;
					}
					if (lockMode != m_lockMode)
					{
						if (Time.time < m_focusLockedEndTime)
						{
							m_lockMode = lockMode;
						}
						else
						{
							m_focusLockedEndTime = Time.time + 3f;
						}
					}
					switch (m_lockMode)
					{
					case LockMode.SingleLock:
						m_carFieldOfViewTarget = m_carFieldOfView;
						break;
					case LockMode.MassLock:
						m_carFieldOfViewTarget = Mathf.Max(bounds2.size.x / (0.5f * m_camera.aspect), bounds2.size.y / 0.5f);
						break;
					}
				}
			}
			float num = Mathf.PerlinNoise(0f, Time.time * m_SwaySpeed) - 0.5f;
			float num2 = Mathf.PerlinNoise(0f, Time.time * m_SwaySpeed + 100f) - 0.5f;
			num *= m_BaseSwayAmount;
			num2 *= m_BaseSwayAmount;
			float num3 = Mathf.PerlinNoise(0f, Time.time * m_SwaySpeed) - 0.5f + m_TrackingBias;
			float num4 = Mathf.PerlinNoise(0f, Time.time * m_SwaySpeed + 100f) - 0.5f + m_TrackingBias;
			num3 *= (0f - m_TrackingSwayAmount) * m_FollowVelocity.x;
			num4 *= m_TrackingSwayAmount * m_FollowVelocity.y;
			base.transform.Rotate(num + num3, num2 + num4, 0f);
			m_carFieldOfViewCurrent = Mathf.MoveTowards(m_carFieldOfViewCurrent, m_carFieldOfViewTarget, Time.unscaledDeltaTime * 60f);
			m_cameraCtrl.Update(m_carFieldOfViewCurrent, m_focusPointTarget, m_transform.position, Time.deltaTime);
			m_virtualCamera.m_Lens.NearClipPlane = m_cameraCtrl.nearClipPlane;
			m_virtualCamera.m_Lens.FieldOfView = m_cameraCtrl.fieldOfView;
			m_updown = 0;
			if (Input.GetKey(KeyCode.PageDown))
			{
				m_updown = -1;
			}
			else if (Input.GetKey(KeyCode.PageUp))
			{
				m_updown = 1;
			}
			if (m_updown != 0)
			{
				m_handsSwitched = true;
			}
		}

		public void InstantApplyFocus()
		{
			m_OriginalRotationInvalid = true;
			LateUpdate();
			m_carFieldOfViewCurrent = m_carFieldOfViewTarget;
		}

		public void SetTrackPoints(List<Transform> points)
		{
			if (points != null)
			{
				points.Sort((Transform lhs, Transform rhs) => lhs.name.CompareTo(rhs.name));
				m_trackingCameraPoints.Clear();
				m_trackingCameraPoints.AddRange(points);
			}
			else
			{
				m_trackingCameraPoints.Clear();
			}
		}

		private void FindTrackingPoints()
		{
			if (m_trackingCameraPoints.Count != 0 && !m_trackingCameraPoints.Exists((Transform e) => e == null))
			{
				return;
			}
			List<Transform> list = new List<Transform>();
			if (!m_trackingCameraPointTag.Equals("Untagged"))
			{
				GameObject[] array = GameObject.FindGameObjectsWithTag(m_trackingCameraPointTag);
				foreach (GameObject gameObject in array)
				{
					list.Add(gameObject.transform);
				}
			}
			SetTrackPoints(list);
		}

		private void CalcCameraPoint(ref List<Transform> points, ref int index)
		{
			if (Time.time < m_lastCamChangedTime + 2f || !m_target || points.Count == 0)
			{
				return;
			}
			Vector3 focusPointTarget = m_focusPointTarget;
			float num = float.MaxValue;
			if (index != -1 && !m_handsSwitched && points[index] != null)
			{
				num = Vector3.SqrMagnitude(focusPointTarget - points[index].position) - m_changeTrackingPointThreshold * m_changeTrackingPointThreshold;
			}
			int num2 = -1;
			if (!m_handsSwitched)
			{
				int i = 0;
				for (int count = points.Count; i < count; i++)
				{
					Transform transform = points[i];
					if (!(transform == null))
					{
						float num3 = Vector3.SqrMagnitude(focusPointTarget - transform.position);
						if (num > num3)
						{
							num2 = i;
							num = num3;
						}
					}
				}
			}
			else
			{
				num2 = index;
				num2 += m_updown;
				if (num2 >= points.Count)
				{
					num2 = 0;
				}
				if (num2 < 0)
				{
					num2 = points.Count - 1;
				}
			}
			if (num2 != -1)
			{
				bool num4 = index != num2;
				index = num2;
				Transform transform2 = points[index];
				if (transform2 != null)
				{
					m_transform.position = transform2.position;
				}
				if (num4)
				{
					m_lastCamChangedTime = Time.time;
					m_cameraCtrl.Reset();
					InstantApplyFocus();
				}
			}
			m_updown = 0;
		}

		private void UpdateTrackingPoint()
		{
			if (m_trackingCustomCameraPoints.Count > 0)
			{
				CalcCameraPoint(ref m_trackingCustomCameraPoints, ref m_curCustomPntIndex);
			}
			else
			{
				CalcCameraPoint(ref m_trackingCameraPoints, ref m_curPntIndex);
			}
		}
	}
}
