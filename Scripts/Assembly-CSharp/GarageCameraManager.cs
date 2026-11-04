using UnityEngine;

public class GarageCameraManager : MonoBehaviour
{
	public static GarageCameraManager m_instance;

	private Vector3 defaultPosition;

	private Quaternion defaultRotation;

	private CameraRotationTemplate m_currentTemplate;

	private Vector3 targetPosition;

	private Quaternion targetRotation;

	private float targetSpeed;

	[SerializeField]
	private float defaultSpeed = 5f;

	public static GarageCameraManager instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = Object.FindObjectOfType<GarageCameraManager>();
			}
			return m_instance;
		}
	}

	private void Awake()
	{
		if (m_instance == null)
		{
			m_instance = this;
		}
	}

	private void Start()
	{
		defaultPosition = base.transform.localPosition;
		defaultRotation = base.transform.localRotation;
		ResetTransform();
	}

	private void Update()
	{
		base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, targetPosition, Time.deltaTime * targetSpeed);
		base.transform.localRotation = Quaternion.Lerp(base.transform.localRotation, targetRotation, Time.deltaTime * targetSpeed);
	}

	public void ResetTransform()
	{
		targetPosition = defaultPosition;
		targetRotation = defaultRotation;
		targetSpeed = defaultSpeed;
		m_currentTemplate = null;
	}

	public void ApplyTemplate(CameraRotationTemplate template)
	{
		targetPosition = template.position;
		targetRotation = template.rotation;
		targetSpeed = template.speed;
		m_currentTemplate = template;
	}
}
