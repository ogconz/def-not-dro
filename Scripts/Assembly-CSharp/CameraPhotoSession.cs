using CarX;
using Cinemachine;
using UnityEngine;

public class CameraPhotoSession : BaseCamera
{
	public enum PhotoSessionCameraTarget
	{
		Center = 0,
		FrontLeft = 1,
		FrontRight = 2,
		RearRight = 3,
		RearLeft = 4
	}

	public LayerMask collisionLayers;

	[SerializeField]
	private float lerpSpeed = 5f;

	private Transform m_wheelFrontLeft;

	private Transform m_wheelFrontRight;

	private Transform m_wheelRearLeft;

	private Transform m_wheelRearRight;

	private Transform m_carTransform;

	private Transform m_cachedTransform;

	private Quaternion m_rotation;

	private Transform m_targetTransform;

	private CinemachineVirtualCamera m_virtualCamera;

	private Transform m_cameraTransform;

	private PhotoSessionCameraTarget m_currentTarget;

	private PhotoSessionCameraTarget m_prevTarget;

	private float m_carLengthApproximate;

	private float m_distanceMin;

	private float m_height;

	private float m_x;

	private float m_y;

	private float m_distance = 6f;

	private bool m_forceUpdate = true;

	private bool m_lerpMode;

	private bool m_resetCameraPos;

	private bool m_firstCameraPosChange;

	private const float DISTANCE_MAX = 20f;

	private const float Y_MIN_LIMIT = -3f;

	private const float Y_MAX_LIMIT = 85f;

	private float m_distanceMax;

	private float m_yMinLimit;

	private float m_yMaxLimit;

	public RaceCar targetRaceCar { get; private set; }

	public CinemachineVirtualCamera virtualCamera => m_virtualCamera;

	private void Awake()
	{
		m_cachedTransform = base.transform;
		m_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		m_cameraTransform = m_virtualCamera.transform;
		SetDefaultLimits();
	}

	private void Start()
	{
		m_forceUpdate = true;
	}

	public void SetLimits(float distanceMax, float yMinLimit, float yMaxLimit)
	{
		m_distanceMax = distanceMax;
		m_yMinLimit = yMinLimit;
		m_yMaxLimit = yMaxLimit;
	}

	public void SetDefaultLimits()
	{
		m_distanceMax = 20f;
		m_yMinLimit = -3f;
		m_yMaxLimit = 85f;
	}

	public new void SetFOV(float fov)
	{
		if (m_virtualCamera != null)
		{
			m_virtualCamera.m_Lens.FieldOfView = fov;
		}
	}

	public void SetHeight(float height)
	{
		if (m_firstCameraPosChange)
		{
			m_firstCameraPosChange = false;
			TakePositionFromCamera();
		}
		m_height = height;
		ChangeTarget(m_currentTarget);
		m_lerpMode = false;
	}

	public void SetTilt(float angle)
	{
		if (m_cameraTransform != null)
		{
			m_cameraTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
		}
	}

	public void SetVerticalTilt(float angle)
	{
		if (m_cameraTransform != null)
		{
			m_cameraTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
		}
	}

	public void SetCameraPosition(float xRotation, float yRotation, float distance)
	{
		if (m_forceUpdate)
		{
			m_forceUpdate = false;
		}
		m_x = xRotation;
		m_y = 0f - yRotation;
		m_distance = distance;
		m_y = ClampAngleModulo(m_y, m_yMinLimit, m_yMaxLimit);
		m_distance = Mathf.Clamp(m_distance, m_distanceMin, m_distanceMax);
		Vector3 targetPosition = GetTargetPosition();
		Quaternion quaternion = Quaternion.Euler(m_y, m_x, 0f);
		Vector3 position = targetPosition - quaternion * (Vector3.forward * m_distance);
		ProtectFromCollision(ref position, targetPosition);
		m_cachedTransform.rotation = quaternion;
		m_cachedTransform.position = position;
	}

	public void UpdateCameraPosition(float deltaXRotation, float deltaYRotation, float deltaDistance)
	{
		bool flag = deltaXRotation != 0f || deltaYRotation != 0f || deltaDistance != 0f;
		if (m_forceUpdate || m_lerpMode)
		{
			m_forceUpdate = false;
			if (flag)
			{
				m_lerpMode = false;
				m_resetCameraPos = true;
			}
		}
		else if (!flag)
		{
			return;
		}
		if (!m_lerpMode && m_resetCameraPos)
		{
			m_resetCameraPos = false;
			m_firstCameraPosChange = false;
			TakePositionFromCamera();
		}
		m_x += deltaXRotation;
		m_y -= deltaYRotation;
		m_y = ClampAngleModulo(m_y, m_yMinLimit, m_yMaxLimit);
		m_distance += deltaDistance;
		m_distance = Mathf.Clamp(m_distance, m_distanceMin, m_distanceMax);
		Vector3 targetPosition = GetTargetPosition();
		Vector3 vector = ((m_currentTarget == PhotoSessionCameraTarget.Center) ? m_cachedTransform.forward : (m_carTransform.position - targetPosition));
		Quaternion quaternion = (m_lerpMode ? Quaternion.LookRotation(vector.normalized * m_distance) : Quaternion.Euler(m_y, m_x, 0f));
		Vector3 position = targetPosition - quaternion * (Vector3.forward * m_distance);
		ProtectFromCollision(ref position, targetPosition);
		if (m_lerpMode)
		{
			m_cachedTransform.rotation = Quaternion.Lerp(m_cachedTransform.rotation, quaternion, Time.unscaledDeltaTime * lerpSpeed);
			m_cachedTransform.position = Vector3.Lerp(m_cachedTransform.position, position, Time.unscaledDeltaTime * lerpSpeed);
		}
		else
		{
			m_cachedTransform.rotation = quaternion;
			m_cachedTransform.position = position;
		}
	}

	public void UpdateCameraPositionPhotoMode(float deltaXRotation, float deltaYRotation, float deltaDistance)
	{
		bool flag = deltaXRotation != 0f || deltaYRotation != 0f || deltaDistance != 0f;
		if (m_forceUpdate || m_lerpMode)
		{
			m_forceUpdate = false;
			if (flag)
			{
				m_lerpMode = false;
				m_resetCameraPos = true;
			}
		}
		if (flag && m_resetCameraPos)
		{
			m_resetCameraPos = false;
			m_firstCameraPosChange = false;
			TakePositionFromCamera();
		}
		Vector3 vector = new Vector3(m_targetTransform.position.x, Mathf.Max(Mathf.Max(m_wheelFrontRight.position.y, m_wheelFrontLeft.position.y), Mathf.Max(m_wheelRearRight.position.y, m_wheelRearLeft.position.y)), m_targetTransform.position.z);
		m_rotation.eulerAngles = new Vector3(ClampAngle(m_rotation.eulerAngles.x - deltaYRotation, m_yMinLimit, m_yMaxLimit), m_rotation.eulerAngles.y + deltaXRotation, m_rotation.eulerAngles.z);
		m_distance += deltaDistance;
		m_distance = Mathf.Clamp(m_distance, m_distanceMin, m_distanceMax);
		Vector3 vector2 = ((m_currentTarget == PhotoSessionCameraTarget.Center) ? m_cachedTransform.forward : (m_carTransform.position - vector));
		if (m_lerpMode)
		{
			m_rotation.eulerAngles = Quaternion.LookRotation(vector2.normalized * m_distance).eulerAngles;
		}
		Vector3 position = vector - m_rotation * (Vector3.forward * m_distance);
		position.y += m_height;
		ProtectFromCollision(ref position, vector);
		float t = Time.unscaledDeltaTime * lerpSpeed;
		if (m_lerpMode)
		{
			m_cachedTransform.rotation = Quaternion.Lerp(m_cachedTransform.rotation, m_rotation, t);
			m_cachedTransform.position = Vector3.Lerp(m_cachedTransform.position, position, t);
		}
		else
		{
			m_cachedTransform.rotation = m_rotation;
			m_cachedTransform.position = position;
		}
	}

	public void UpdateCameraPositionWithoutLerp(float deltaXRotation, float deltaYRotation, float deltaDistance)
	{
		m_x += deltaXRotation;
		m_y -= deltaYRotation;
		m_y = ClampAngleModulo(m_y, m_yMinLimit, m_yMaxLimit);
		m_distance += deltaDistance;
		m_distance = Mathf.Clamp(m_distance, m_distanceMin, m_distanceMax);
		Vector3 targetPosition = GetTargetPosition();
		Quaternion quaternion = Quaternion.Euler(m_y, m_x, 0f);
		Vector3 position = targetPosition - quaternion * (Vector3.forward * m_distance);
		ProtectFromCollision(ref position, targetPosition);
		m_cachedTransform.SetPositionAndRotation(position, quaternion);
	}

	private void TakePositionFromCamera()
	{
		Vector3 vector = m_carTransform.position - m_cachedTransform.position;
		float num = Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z);
		if (num > 0f)
		{
			SetCameraPosition(Mathf.Atan2(vector.x, vector.z) * 57.29578f, Mathf.Atan(vector.y / num) * 57.29578f, m_distance);
		}
	}

	private void ProtectFromCollision(ref Vector3 position, Vector3 target)
	{
		Vector3 vector = position - target;
		float magnitude = vector.magnitude;
		Vector3 direction = vector * (1f / magnitude);
		float num = m_virtualCamera.m_Lens.NearClipPlane + 0.001f;
		Vector3 vector2 = Vector3.zero;
		float num2 = 0f;
		if (Physics.SphereCast(target, num, direction, out var hitInfo, magnitude, collisionLayers))
		{
			float num3 = num - Vector3.Dot(position - hitInfo.point, hitInfo.normal);
			if (num3 > 0f)
			{
				num2 = num3;
				vector2 = hitInfo.normal;
			}
		}
		if (num2 > 0f)
		{
			vector2 = num2 * vector2.normalized;
			Vector3 vector3 = target;
			vector3.y = position.y - 0.2f;
			Vector3 vector4 = position + vector2 - vector3;
			float magnitude2 = vector4.magnitude;
			if (magnitude2 < m_distanceMin)
			{
				vector4 *= m_distanceMin / (magnitude2 + 0.001f);
				vector2 = vector3 + vector4 - position;
			}
			position += vector2;
			position = target + (position - target).normalized * Mathf.Clamp(hitInfo.distance, m_distanceMin, m_distanceMax);
		}
	}

	private float ClampAngleModulo(float angle, float min, float max)
	{
		while (angle < -360f)
		{
			angle += 360f;
		}
		while (angle > 360f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}

	private float ClampAngle(float angle, float min, float max)
	{
		float num = Mathf.Floor((angle - ((min + max) * 0.5f - 180f)) / 360f) * 360f;
		return Mathf.Clamp(angle, min + num, max + num);
	}

	public override void SetTarget(RaceCar car)
	{
		if (!BaseCar.IsNull(car))
		{
			targetRaceCar = car;
			CARXCar component = car.GetComponent<CARXCar>();
			if (!(m_carTransform == component.getTransform))
			{
				m_wheelFrontLeft = component.GetWheelTransform(WheelIndex.FrontLeft);
				m_wheelFrontRight = component.GetWheelTransform(WheelIndex.FrontRight);
				m_wheelRearLeft = component.GetWheelTransform(WheelIndex.RearLeft);
				m_wheelRearRight = component.GetWheelTransform(WheelIndex.RearRight);
				m_carTransform = component.getTransform;
				CarDesc desc = null;
				component.GetCarDesc(ref desc);
				Vector3 extents = desc.weight.bounds.extents;
				m_carLengthApproximate = Mathf.Sqrt(extents.x * extents.x + extents.z * extents.z) * 2f;
				ChangeTarget(m_currentTarget);
			}
		}
	}

	public PhotoSessionCameraTarget PrevTarget()
	{
		PhotoSessionCameraTarget photoSessionCameraTarget = ((m_currentTarget != PhotoSessionCameraTarget.RearLeft) ? (m_currentTarget + 1) : PhotoSessionCameraTarget.Center);
		ChangeTarget(photoSessionCameraTarget);
		return photoSessionCameraTarget;
	}

	public PhotoSessionCameraTarget NextTarget()
	{
		PhotoSessionCameraTarget photoSessionCameraTarget = ((m_currentTarget == PhotoSessionCameraTarget.Center) ? PhotoSessionCameraTarget.RearLeft : (m_currentTarget - 1));
		ChangeTarget(photoSessionCameraTarget);
		return photoSessionCameraTarget;
	}

	public void ChangeTarget(PhotoSessionCameraTarget target)
	{
		switch (target)
		{
		case PhotoSessionCameraTarget.Center:
			SetTargetVector(m_carTransform);
			m_distanceMin = m_carLengthApproximate / 2f;
			break;
		case PhotoSessionCameraTarget.FrontLeft:
			SetTargetVector(m_wheelFrontLeft);
			m_distanceMin = m_carLengthApproximate * 0.85f;
			break;
		case PhotoSessionCameraTarget.FrontRight:
			SetTargetVector(m_wheelFrontRight);
			m_distanceMin = m_carLengthApproximate * 0.85f;
			break;
		case PhotoSessionCameraTarget.RearLeft:
			SetTargetVector(m_wheelRearLeft);
			m_distanceMin = m_carLengthApproximate * 0.85f;
			break;
		case PhotoSessionCameraTarget.RearRight:
			SetTargetVector(m_wheelRearRight);
			m_distanceMin = m_carLengthApproximate * 0.85f;
			break;
		}
		m_prevTarget = m_currentTarget;
		m_currentTarget = target;
		m_firstCameraPosChange = m_currentTarget != m_prevTarget;
		m_forceUpdate = true;
		m_lerpMode = true;
	}

	private Vector3 GetTargetPosition()
	{
		Vector3 vector = (m_targetTransform ? m_targetTransform.position : m_carTransform.position);
		return new Vector3(vector.x, m_wheelRearRight.position.y + m_height, vector.z);
	}

	private void SetTargetVector(Transform tr)
	{
		if (tr != null)
		{
			m_targetTransform = tr;
			m_lerpMode = true;
		}
	}
}
