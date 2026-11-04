using CarX.Internal;
using Cinemachine;
using PrefsModels;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CarX
{
	[AddComponentMenu("")]
	public class CockpitCamera : BaseCamera
	{
		[SerializeField]
		private RaceCar m_target;

		[SerializeField]
		private Vector3 m_offset = new Vector3(0f, 0.5f, 0f);

		[SerializeField]
		private Vector3 m_eulerAngles = Vector3.zero;

		private Quaternion m_eulerQuaternion = Quaternion.identity;

		[SerializeField]
		private Vector3 m_accelScale = new Vector3(1f, 2.5f, 5f);

		[SerializeField]
		private float m_cameraFOV = 60f;

		[SerializeField]
		private string m_placePointTag = "Untagged";

		[SerializeField]
		private bool m_smoothen;

		[SerializeField]
		private float m_smoothSpeed = 0.5f;

		[SerializeField]
		private float m_maxRotSpeed = 360f;

		[SerializeField]
		private float m_maxRotAngle = 45f;

		[SerializeField]
		private float m_cameraSmooth;

		[SerializeField]
		private float m_velocityScale = 0.7f;

		[SerializeField]
		private float m_accelSmooth = 0.1f;

		[SerializeField]
		private float m_dynamicCamDeadZone;

		private Transform m_transform;

		private Transform m_placeTransform;

		private CarX.Internal.CockpitCamera m_cameraCtrl;

		private CinemachineVirtualCamera m_virtualCamera;

		private bool m_firstEnter = true;

		private bool m_updUp = true;

		private bool m_useAccel = true;

		private HDAdditionalCameraData m_additionalCameraData;

		private FrameSettingsOverrideMask m_frameSettingsOverrideMask;

		private bool m_defaultTWMVState;

		private DynamicCamParameters.AimMode m_dynamicCameraAimingMode;

		private GlobalModel.DynamicCockpitCamMode m_dynamicCamMode = GlobalModel.DynamicCockpitCamMode.OFF;

		private float? m_fOnDisableTimeMark;

		public Transform placeTransform => m_placeTransform;

		public Vector3 defaultOffset { get; private set; }

		public Vector3 defaultEulerAngles { get; private set; }

		public float defaultCameraFOV { get; private set; }

		public GlobalModel.DynamicCockpitCamMode defaultDynamic { get; private set; }

		public Vector3 offset
		{
			get
			{
				return m_offset;
			}
			set
			{
				m_offset = value;
			}
		}

		public float velocityScale
		{
			get
			{
				return m_velocityScale;
			}
			set
			{
				m_velocityScale = value;
			}
		}

		public Vector3 accelScale
		{
			get
			{
				return m_accelScale;
			}
			set
			{
				m_accelScale = value;
			}
		}

		public Vector3 eulerAngles
		{
			get
			{
				return m_eulerAngles;
			}
			set
			{
				m_eulerAngles = value;
				m_eulerQuaternion = Quaternion.Euler(m_eulerAngles);
			}
		}

		public Quaternion eulerQuaternion => m_eulerQuaternion;

		public string placePointTag
		{
			get
			{
				return m_placePointTag;
			}
			set
			{
				m_placePointTag = value;
				SetTarget(m_target);
			}
		}

		public float FOV => m_cameraFOV;

		public float maxRotSpeed
		{
			get
			{
				return m_maxRotSpeed;
			}
			set
			{
				m_maxRotSpeed = value;
			}
		}

		public float maxRotAngle
		{
			get
			{
				return m_maxRotAngle;
			}
			set
			{
				m_maxRotAngle = value;
			}
		}

		public float cameraSmooth
		{
			get
			{
				return m_cameraSmooth;
			}
			set
			{
				m_cameraSmooth = value;
			}
		}

		public float accelSmooth
		{
			get
			{
				return m_accelSmooth;
			}
			set
			{
				m_accelSmooth = value;
			}
		}

		public GlobalModel.DynamicCockpitCamMode dynamicCameraMode
		{
			get
			{
				return m_dynamicCamMode;
			}
			set
			{
				m_dynamicCamMode = value;
			}
		}

		public DynamicCamParameters.AimMode dynamicCameraAimingMode
		{
			get
			{
				return m_dynamicCameraAimingMode;
			}
			set
			{
				m_dynamicCameraAimingMode = value;
			}
		}

		public bool useAccel
		{
			get
			{
				return m_useAccel;
			}
			set
			{
				m_useAccel = value;
			}
		}

		public float dynamicCamDeadZone
		{
			get
			{
				return m_dynamicCamDeadZone;
			}
			set
			{
				m_dynamicCamDeadZone = value;
			}
		}

		public bool smoothen => m_smoothen;

		public float smoothSpeed => m_smoothSpeed;

		public bool updUp
		{
			get
			{
				return m_updUp;
			}
			set
			{
				m_updUp = value;
			}
		}

		private void Awake()
		{
			m_transform = base.transform;
			m_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
			SetDefaults();
			m_cameraCtrl = new CarX.Internal.CockpitCamera(this);
			GameObject gameObject = Camera.main.gameObject;
			m_additionalCameraData = gameObject.GetComponent<HDAdditionalCameraData>();
			m_additionalCameraData.customRenderingSettings = true;
			m_frameSettingsOverrideMask = m_additionalCameraData.renderingPathCustomFrameSettingsOverrideMask;
			m_frameSettingsOverrideMask.mask[16u] = true;
			m_defaultTWMVState = m_additionalCameraData.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector);
			m_defaultTWMVState = true;
			m_additionalCameraData.renderingPathCustomFrameSettingsOverrideMask = m_frameSettingsOverrideMask;
			m_additionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.TransparentsWriteMotionVector, m_defaultTWMVState);
		}

		private void Start()
		{
			SetTarget(m_target);
		}

		private void OnEnable()
		{
			m_additionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.TransparentsWriteMotionVector, value: false);
			if (!m_fOnDisableTimeMark.HasValue)
			{
				m_fOnDisableTimeMark = Time.maximumDeltaTime;
			}
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_cameraFOV;
			}
		}

		private void OnDisable()
		{
			m_additionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.TransparentsWriteMotionVector, m_defaultTWMVState);
			m_fOnDisableTimeMark = Time.time;
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = 60f;
			}
		}

		private void LateUpdate()
		{
			if (IsTargetValid(m_target))
			{
				RefreshTargetIfNeeded(ref m_placeTransform, m_placePointTag);
				float dt = (m_fOnDisableTimeMark.HasValue ? (Time.time - m_fOnDisableTimeMark.Value) : Time.deltaTime);
				if (m_firstEnter)
				{
					m_firstEnter = false;
					updUp = false;
				}
				Vector3 localVel = Vector3.zero;
				Vector3 localAcc = Vector3.zero;
				if (dynamicCameraMode > GlobalModel.DynamicCockpitCamMode.OFF)
				{
					Car carX = m_target.carX;
					bool flag = dynamicCameraMode == GlobalModel.DynamicCockpitCamMode.Legacy;
					localVel = CalculateDesiredForward(dynamicCameraAimingMode == DynamicCamParameters.AimMode.Velocity || flag, flag);
					localAcc = ((flag || m_useAccel) ? new Vector3(carX.sideAccel, carX.vertAccel, carX.longAccel) : Vector3.zero);
				}
				bool improvedModeActive = dynamicCameraMode == GlobalModel.DynamicCockpitCamMode.Improved;
				m_cameraCtrl.UpdateCamera(improvedModeActive, m_target.getTransform, m_placeTransform.localPosition, localVel, localAcc, dt, m_fOnDisableTimeMark.HasValue);
				m_transform.SetPositionAndRotation(m_cameraCtrl.position, m_cameraCtrl.rotation);
				if (m_fOnDisableTimeMark.HasValue)
				{
					m_fOnDisableTimeMark = null;
				}
			}
		}

		protected override void SetDefaults()
		{
			defaultDynamic = GlobalModel.DynamicCockpitCamMode.OFF;
			defaultCameraFOV = m_cameraFOV;
			defaultEulerAngles = m_eulerAngles;
			defaultOffset = m_offset;
			maxRotAngle = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationAngle").defaultValue;
			maxRotSpeed = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("maxRotationSpeed").defaultValue;
			accelSmooth = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("accelSmooth").defaultValue;
			cameraSmooth = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("rotationSmooth").defaultValue;
			dynamicCamDeadZone = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("deadzone").defaultValue;
			velocityScale = ScriptableSingleton<CameraCustomizerLimits>.instance.GetDynamicCockpitLimits("vectorScale").defaultValue;
			dynamicCameraMode = GlobalModel.DynamicCockpitCamMode.Legacy;
			dynamicCameraAimingMode = DynamicCamParameters.AimMode.Steer;
			useAccel = false;
		}

		public override void ResetToDefaults()
		{
			dynamicCameraMode = defaultDynamic;
			SetFOV(defaultCameraFOV);
			m_eulerAngles = defaultEulerAngles;
			m_offset = defaultOffset;
		}

		public override void SetFOV(float fov)
		{
			m_cameraFOV = fov;
			if (base.isActiveAndEnabled && (bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_cameraFOV;
			}
		}

		public override void SetTarget(RaceCar target)
		{
			m_target = target;
			if ((bool)m_target)
			{
				m_placeTransform = m_target.getTransform;
				base.needRefreshTarget = true;
			}
		}

		public void SetHeadOnCockpit(CameraPartMarker[] cameraPlaceObjects)
		{
			float num = offset.x;
			float num2 = offset.z;
			if (PlayerCarControl.instance.car != m_target)
			{
				num = 0f;
				num2 = 0f;
			}
			foreach (CameraPartMarker cameraPartMarker in cameraPlaceObjects)
			{
				bool flag = m_placePointTag == cameraPartMarker.placePointTag;
				if (num <= -0.2f || num >= 0.2f || num2 <= -0.2f || num2 >= 0.2f || !flag)
				{
					cameraPartMarker.SetActive(active: false);
				}
				else if ((num > -0.2f && num < 0.2f) || (num2 > -0.2f && num2 < 0.2f) || flag)
				{
					cameraPartMarker.SetActive(active: true);
				}
			}
		}

		private Vector3 CalculateDesiredForward(bool velocityBased, bool raw = false)
		{
			Vector3 forward = Vector3.forward;
			if (velocityBased)
			{
				Vector3 vector = m_target.getTransform.InverseTransformDirection(m_target.carX.velocity);
				forward = vector;
				if (!raw)
				{
					vector.y = 0f;
					float sqrMagnitude = vector.sqrMagnitude;
					if (sqrMagnitude > 25f)
					{
						forward = vector / Mathf.Sqrt(sqrMagnitude);
					}
					else
					{
						vector.z = Mathf.Max(vector.z, 0f - vector.z);
						if (sqrMagnitude > 0f)
						{
							vector /= Mathf.Sqrt(sqrMagnitude);
						}
						forward = Vector3.Lerp(Vector3.forward, vector, sqrMagnitude / 25f);
					}
				}
			}
			else
			{
				forward = Quaternion.AngleAxis((0f - m_target.GetSteerSmoothDamp()) * maxRotAngle, Vector3.up) * Vector3.forward;
			}
			return forward;
		}
	}
}
