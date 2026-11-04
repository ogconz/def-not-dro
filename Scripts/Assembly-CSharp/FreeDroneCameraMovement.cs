using System;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(FreeDroneCameraInput))]
public class FreeDroneCameraMovement : MonoBehaviour
{
	private const float COLLIDER_CHECK_DISTANCE = 0.65f;

	private const float GROUND_CHECK_DISTANCE = 100f;

	private const float MAX_CAMERA_HEIGHT = 500f;

	private const int FOV_DEFAULT_INDEX = 3;

	public Action<float> onChangedFOV;

	public LayerMask collisionLayers;

	public float mainSpeed = 100f;

	public float shiftAdd = 250f;

	public float maxShift = 1000f;

	public float speedDamping = 50f;

	public Transform targetFollowing;

	[Range(5f, 50f)]
	[SerializeField]
	private float m_fovScrollingSpeed = 25f;

	[SerializeField]
	private LayerMask m_groundLayers;

	[SerializeField]
	private Transform m_targetAnchor;

	private float m_totalRun = 1f;

	private float m_speedMultiplier;

	private FreeDroneCameraInput m_inputComponent;

	private FreeDroneCameraInputData m_inputData;

	private FreeDroneCamera m_droneCamera;

	private CinemachineVirtualCamera m_freeCam;

	private Vector2 m_rotationValues = Vector2.zero;

	private Vector3 m_velocityAxis = Vector3.zero;

	private Vector3 m_accelerationLerp = Vector3.zero;

	private Vector3 m_signs = Vector3.zero;

	private float m_currFrameCount;

	private bool m_autofocusMode;

	private bool m_trackingMode;

	private CinemachineBrain.UpdateMethod? m_prevUpdateType;

	private CinemachineBrain m_cinemachineBrain;

	private bool m_ignoreCollisions;

	private RaycastHit[] m_hitInfo = new RaycastHit[4];

	public Transform targetAnchor => m_targetAnchor;

	private void Awake()
	{
		m_freeCam = base.transform.GetComponentInChildren<CinemachineVirtualCamera>();
		if (m_freeCam == null)
		{
			Debug.LogError($"GameObject {base.transform} don't have CinemachineVirtualCamera component");
			return;
		}
		m_droneCamera = GetComponent<FreeDroneCamera>();
		m_cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
		if (m_targetAnchor == null)
		{
			m_targetAnchor = base.transform.Find("TargetAnchor");
			if (m_targetAnchor == null)
			{
				Debug.LogError("Target anchor for " + base.gameObject.name + " not found");
			}
		}
	}

	private void OnEnable()
	{
		SetCinemachineBrainUpdateMethod(CinemachineBrain.UpdateMethod.ManualUpdate);
	}

	private void OnDisable()
	{
		SetCinemachineBrainUpdateMethod(m_prevUpdateType);
	}

	private void SetCinemachineBrainUpdateMethod(CinemachineBrain.UpdateMethod? updateMethod)
	{
		if ((bool)m_cinemachineBrain && updateMethod.HasValue)
		{
			m_prevUpdateType = m_cinemachineBrain.m_UpdateMethod;
			m_cinemachineBrain.m_UpdateMethod = updateMethod.Value;
		}
	}

	private void Start()
	{
		PlayerCarControl.instance.SetBlockControl(value: true);
	}

	public void Initialize(FreeDroneCameraInputData inputData)
	{
		m_inputData = inputData;
		m_inputComponent = GetComponent<FreeDroneCameraInput>();
		m_inputComponent.Initialize(inputData);
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			CalcDroneCamera();
		}
	}

	private void FixedUpdate()
	{
		CalcDroneCamera();
	}

	private void LateUpdate()
	{
		m_cinemachineBrain.ManualUpdate();
	}

	private void CalcDroneCamera()
	{
		if (m_freeCam == null || m_targetAnchor == null)
		{
			return;
		}
		m_inputComponent.UpdateInput();
		if (targetFollowing == null && (bool)PlayerCarControl.instance.car)
		{
			targetFollowing = PlayerCarControl.instance.car.transform;
			if (m_autofocusMode)
			{
				m_freeCam.m_LookAt = targetFollowing;
			}
		}
		Vector3 vector = Vector3.zero;
		if (m_trackingMode)
		{
			Vector3 position = targetFollowing.position;
			vector = position - base.transform.position;
			base.transform.position = position;
			if (m_currFrameCount + 5f > (float)Time.frameCount)
			{
				m_targetAnchor.position = position - targetFollowing.forward * 4f + Vector3.up * 2f;
			}
		}
		Vector3 signs = m_signs;
		float totalRun = m_totalRun;
		float speedMultiplier = m_speedMultiplier;
		Vector3 accelerationLerp = m_accelerationLerp;
		Vector3 vector2 = new Vector3(m_inputData.strafe, 0f, m_inputData.movement);
		accelerationLerp.x = Mathf.MoveTowards(accelerationLerp.x, Mathf.Abs(vector2.x), speedDamping * Time.unscaledDeltaTime);
		accelerationLerp.z = Mathf.MoveTowards(accelerationLerp.z, Mathf.Abs(vector2.z), speedDamping * Time.unscaledDeltaTime);
		if (Mathf.Abs(vector2.x) > Mathf.Epsilon)
		{
			signs.x = Mathf.Sign(vector2.x);
			m_velocityAxis.x = Mathf.MoveTowards(m_velocityAxis.x, accelerationLerp.x * signs.x, 2f * Time.unscaledDeltaTime);
		}
		else
		{
			m_velocityAxis.x = Mathf.MoveTowards(m_velocityAxis.x, 0f, Time.unscaledDeltaTime);
		}
		if (Mathf.Abs(vector2.z) > Mathf.Epsilon)
		{
			signs.z = Mathf.Sign(vector2.z);
			m_velocityAxis.z = Mathf.MoveTowards(m_velocityAxis.z, accelerationLerp.z * signs.z, 2f * Time.unscaledDeltaTime);
		}
		else
		{
			m_velocityAxis.z = Mathf.MoveTowards(m_velocityAxis.z, 0f, Time.unscaledDeltaTime);
		}
		vector2 = m_velocityAxis;
		if (m_inputData.turboMode)
		{
			totalRun = Mathf.MoveTowards(totalRun, shiftAdd, Time.unscaledDeltaTime);
			float num = totalRun * mainSpeed;
			vector2 *= num;
			vector2.x = Mathf.Clamp(vector2.x, 0f - maxShift, maxShift);
			vector2.y = Mathf.Clamp(vector2.y, 0f - maxShift, maxShift);
			vector2.z = Mathf.Clamp(vector2.z, 0f - maxShift, maxShift);
			speedMultiplier = num * Time.unscaledDeltaTime;
			speedMultiplier = Mathf.Clamp(speedMultiplier, 0f - maxShift, maxShift);
		}
		else
		{
			totalRun = Mathf.MoveTowards(totalRun, 1f, Time.unscaledDeltaTime * 2f);
			vector2 *= mainSpeed * totalRun;
			speedMultiplier = mainSpeed * totalRun * Time.unscaledDeltaTime;
		}
		vector2 *= Time.unscaledDeltaTime;
		Vector3 position2 = m_targetAnchor.position;
		Vector3 vector3 = position2 + m_targetAnchor.TransformDirection(vector2);
		vector3.y = position2.y;
		float b = m_inputData.heightUp - m_inputData.heightDown;
		accelerationLerp.y = Mathf.Lerp(accelerationLerp.y, b, speedDamping * 2f * Time.unscaledDeltaTime);
		float num2 = accelerationLerp.y * speedMultiplier * 0.3f;
		vector3.y += num2;
		Vector3 vector4 = vector + (vector3 - position2);
		if (m_trackingMode)
		{
			speedMultiplier = vector4.magnitude * 3f;
		}
		m_signs = signs;
		m_targetAnchor.position = vector3;
		m_totalRun = totalRun;
		m_speedMultiplier = speedMultiplier;
		m_accelerationLerp = accelerationLerp;
		SetFov();
	}

	private void CheckOnGround()
	{
		Vector3 position = m_targetAnchor.position;
		if (Physics.RaycastNonAlloc(position, Vector3.down, m_hitInfo, 100f, m_groundLayers) != 0)
		{
			return;
		}
		int num = Physics.RaycastNonAlloc(position + Vector3.up * 100f, Vector3.down, m_hitInfo, 100f, m_groundLayers);
		if (num <= 0)
		{
			return;
		}
		RaycastHit raycastHit = m_hitInfo[0];
		for (int i = 1; i < num; i++)
		{
			if (m_hitInfo[i].point.y < raycastHit.point.y)
			{
				raycastHit = m_hitInfo[i];
			}
		}
		m_targetAnchor.position = raycastHit.point + Vector3.up * 0.65f;
	}

	private bool IsCollided(RaycastHit[] hitInfo, int length)
	{
		for (int i = 0; i < length; i++)
		{
			RaycastHit raycastHit = hitInfo[i];
			if (raycastHit.transform != null && raycastHit.transform != m_targetAnchor.transform)
			{
				return true;
			}
		}
		return false;
	}

	public void ReverseMoving(Vector3 direction)
	{
		m_targetAnchor.position += direction * 0.65f * 0.5f;
	}

	public void DisableTrackingModeByCollision()
	{
		if (m_trackingMode && !m_ignoreCollisions)
		{
			m_droneCamera.SwitchTrackingMode(trackingParam: false);
		}
	}

	public void IgnoreCollision()
	{
		m_ignoreCollisions = true;
	}

	public void ResetRelative()
	{
		if (!(targetFollowing == null))
		{
			Vector3 localPosition = targetAnchor.position - targetFollowing.position;
			base.transform.position = targetAnchor.position;
			targetAnchor.localPosition = localPosition;
		}
	}

	public void SetPosition(Vector3 positionParam)
	{
		m_targetAnchor.position = positionParam;
	}

	public void LookAt(Vector3 lookAtPosition)
	{
		m_targetAnchor.LookAt(lookAtPosition, Vector3.up);
		m_rotationValues.x = 0f - m_targetAnchor.rotation.eulerAngles.x;
		m_rotationValues.y = m_targetAnchor.rotation.eulerAngles.y;
	}

	public void SwitchFollowing(bool autofocusParam)
	{
		m_autofocusMode = autofocusParam;
	}

	public void SwitchTracking(bool trackingParam)
	{
		if (trackingParam)
		{
			m_currFrameCount = Time.frameCount;
		}
		m_trackingMode = trackingParam;
	}

	public void SetFov(float fovParam)
	{
		m_freeCam.m_Lens.FieldOfView = fovParam;
		m_inputData.fovChanged = false;
		onChangedFOV.SafeCall(fovParam);
	}

	public float GetFov()
	{
		return m_freeCam.m_Lens.FieldOfView;
	}

	private void SetFov()
	{
		if (m_inputData.fovChanged)
		{
			SetFov(m_inputData.fovValues[Mathf.Clamp(m_inputData.fovIndex, 0, m_inputData.fovValues.Keys.Count - 1)]);
		}
		if (m_inputData.changerFov != 0)
		{
			SetFov(GetFov() + (float)m_inputData.changerFov * m_fovScrollingSpeed * Time.unscaledDeltaTime);
		}
	}

	public void SetDefaultFovIndex()
	{
		m_inputData.fovIndex = 3;
	}
}
