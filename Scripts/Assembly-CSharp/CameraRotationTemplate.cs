using UnityEngine;

public class CameraRotationTemplate : MonoBehaviour
{
	[SerializeField]
	private Vector3 m_position = Vector3.zero;

	[SerializeField]
	private Quaternion m_rotation;

	[SerializeField]
	private float m_speed = 10f;

	public Vector3 position => m_position;

	public Quaternion rotation => m_rotation;

	public float speed => m_speed;

	private void OnEnable()
	{
		if (GarageCameraManager.instance != null)
		{
			GarageCameraManager.instance.ApplyTemplate(this);
		}
	}

	private void OnDisable()
	{
		if (GarageCameraManager.instance != null)
		{
			GarageCameraManager.instance.ResetTransform();
		}
	}
}
