using Cinemachine;
using UnityEngine;

public class FreeDroneCameraRotation : MonoBehaviour
{
	public float speedRotation;

	public float rotateForce;

	private CinemachineVirtualCamera m_freeCam;

	private FreeDroneCameraInputData m_inputData;

	private Transform m_targetAnchor;

	private Vector2 m_rotationValues;

	private bool m_autofocusMode;

	private void Awake()
	{
		m_freeCam = base.transform.GetComponentInChildren<CinemachineVirtualCamera>();
		if (m_freeCam == null)
		{
			Debug.LogError($"GameObject {base.transform} don't have CinemachineVirtualCamera component");
		}
		else if (m_targetAnchor == null)
		{
			m_targetAnchor = base.transform.Find("TargetAnchor");
			if (m_targetAnchor == null)
			{
				Debug.LogError("Target anchor for " + base.gameObject.name + " not found");
			}
		}
	}

	public void Initialize(FreeDroneCameraInputData inputData)
	{
		m_inputData = inputData;
	}

	public void SwitchFollowing(bool autofocusParam)
	{
		m_autofocusMode = autofocusParam;
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			CameraRotation();
		}
	}

	private void FixedUpdate()
	{
		CameraRotation();
	}

	private void CameraRotation()
	{
		if (m_autofocusMode)
		{
			Quaternion rotation = m_freeCam.transform.rotation;
			m_targetAnchor.rotation = rotation;
			m_rotationValues.x = 0f - rotation.eulerAngles.x;
			m_rotationValues.y = rotation.eulerAngles.y;
			return;
		}
		m_rotationValues.x += m_inputData.rotVertical * speedRotation;
		float y = m_rotationValues.y;
		m_rotationValues.y += m_inputData.rotHorizontal * speedRotation;
		m_rotationValues.x = Mathf.Clamp(m_rotationValues.x, -90f, 90f);
		if (m_rotationValues.y > 360f)
		{
			m_rotationValues.y -= 360f;
		}
		if (m_rotationValues.y < -360f)
		{
			m_rotationValues.y += 360f;
		}
		float y2 = m_targetAnchor.eulerAngles.y;
		float f = Mathf.DeltaAngle(y2, y);
		float f2 = Mathf.DeltaAngle(y2, m_rotationValues.y);
		float num = Mathf.Sign(f);
		float num2 = Mathf.Sign(f2);
		if (num != num2 && Mathf.Abs(f) > 30f)
		{
			m_rotationValues.y = y2 + num * 179f;
		}
		Quaternion b = Quaternion.Euler(0f - m_rotationValues.x, m_rotationValues.y, 0f);
		Vector3 eulerAngles = Quaternion.Slerp(m_targetAnchor.rotation, b, Time.unscaledDeltaTime * rotateForce).eulerAngles;
		m_targetAnchor.eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, 0f);
	}
}
