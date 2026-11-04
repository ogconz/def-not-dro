using UnityEngine;

public class TiltHorizon : MonoBehaviour
{
	[SerializeField]
	private float m_smooth = 0.2f;

	[SerializeField]
	private float m_linearity = 0.6f;

	private Transform m_transform;

	private float m_currentTilt;

	private float m_tiltVelocity;

	private CameraSwitch m_cameraSwitch;

	public float smooth
	{
		get
		{
			return m_smooth;
		}
		set
		{
			m_smooth = value;
		}
	}

	private void Awake()
	{
		m_transform = base.transform;
		m_cameraSwitch = GetComponent<CameraSwitch>();
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f && m_cameraSwitch.cameraType != CameraSwitch.ECameraType.PhotoSession)
		{
			float num = 0f - Input.acceleration.x;
			float num2 = Mathf.Sign(num);
			num = Mathf.Lerp(num * num, Mathf.Abs(num), m_linearity) * num2;
			m_currentTilt = Mathf.SmoothDamp(m_currentTilt, num, ref m_tiltVelocity, m_smooth, float.MaxValue, deltaTime);
			m_transform.rotation *= Quaternion.Euler(0f, 0f, m_currentTilt * 75f);
		}
	}
}
