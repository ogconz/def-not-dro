using Cinemachine;
using UnityEngine;

namespace CarX
{
	[AddComponentMenu("")]
	public class StaticCamera : BaseCamera
	{
		[SerializeField]
		private RaceCar m_target;

		[SerializeField]
		private bool m_hideCar;

		[SerializeField]
		private string m_staticPointTag = "Untagged";

		[SerializeField]
		private float m_cameraFOV = 60f;

		private Transform m_transform;

		private Transform m_staticPointTransform;

		private CinemachineVirtualCamera m_virtualCamera;

		public string staticPointTag
		{
			get
			{
				return m_staticPointTag;
			}
			set
			{
				m_staticPointTag = value;
				SetTarget(m_target);
			}
		}

		private void Awake()
		{
			m_transform = base.transform;
			m_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		}

		private void Start()
		{
			SetTarget(m_target);
		}

		private void OnEnable()
		{
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = m_cameraFOV;
			}
		}

		private void OnDisable()
		{
			if ((bool)m_virtualCamera)
			{
				m_virtualCamera.m_Lens.FieldOfView = 60f;
			}
		}

		private void LateUpdate()
		{
			if (IsTargetValid(m_target))
			{
				RefreshTargetIfNeeded(ref m_staticPointTransform, m_staticPointTag);
				m_transform.position = m_staticPointTransform.position;
				m_transform.rotation = m_staticPointTransform.rotation;
			}
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
				m_staticPointTransform = m_target.getTransform;
				base.needRefreshTarget = true;
			}
		}
	}
}
