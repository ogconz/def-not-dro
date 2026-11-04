using UnityEngine;

[RequireComponent(typeof(CameraRotationCustom))]
public class CameraTarget : MonoBehaviour
{
	public Transform cameraTarget;

	[SerializeField]
	private Vector3 m_targetOffset;

	private CameraRotationCustom m_cameraRotation;

	public void Awake()
	{
		m_cameraRotation = GetComponent<CameraRotationCustom>();
	}

	private void Start()
	{
		Vector3 position = cameraTarget.position;
		m_cameraRotation.Stop();
		m_cameraRotation.SetCenter(position + m_targetOffset);
	}
}
