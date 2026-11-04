using System.Collections;
using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
	private enum MainMenuCamState
	{
		Free = 0,
		ChangeState = 1,
		Lock = 2
	}

	public Vector3 position;

	public Vector3 rotation;

	public float speedLerp;

	private MainMenuCamState state;

	private Vector3 m_startCameraPos;

	private Quaternion m_startCameraRot;

	private CameraRotation m_cameraRotation;

	private Quaternion m_rotationQuaternion;

	public bool disableProcess;

	public void StartCamera()
	{
		StopAllCoroutines();
		if (m_cameraRotation == null)
		{
			m_cameraRotation = GetComponent<CameraRotation>();
		}
		switch (state)
		{
		case MainMenuCamState.Free:
			if (m_cameraRotation != null)
			{
				m_startCameraPos = base.transform.position;
				m_startCameraRot = base.transform.rotation;
			}
			break;
		}
		StartCoroutine(StartLerpToLockPosition());
		m_rotationQuaternion = Quaternion.Euler(rotation);
	}

	public void StopCamera()
	{
		StopAllCoroutines();
		if (state != MainMenuCamState.Free)
		{
			StartCoroutine(StartLerpToFreePosition());
		}
	}

	private IEnumerator StartLerpToFreePosition()
	{
		state = MainMenuCamState.ChangeState;
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime;
			base.transform.position = Vector3.Lerp(base.transform.position, m_startCameraPos, Time.fixedDeltaTime * speedLerp);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, m_startCameraRot, Time.fixedDeltaTime * speedLerp);
			yield return new WaitForEndOfFrame();
		}
		m_cameraRotation.enabled = true;
		state = MainMenuCamState.Free;
	}

	private IEnumerator StartLerpToLockPosition()
	{
		state = MainMenuCamState.ChangeState;
		m_cameraRotation.enabled = false;
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime;
			base.transform.position = Vector3.Lerp(base.transform.position, position, Time.fixedDeltaTime * speedLerp);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, m_rotationQuaternion, Time.fixedDeltaTime * speedLerp);
			yield return new WaitForEndOfFrame();
		}
		state = MainMenuCamState.Lock;
	}
}
