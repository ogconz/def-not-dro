using System.Collections.Generic;
using CarX;
using UnityEngine;
using UnityEngine.UI;

public class GarageCameraBinding : MonoBehaviour
{
	private RaceCar m_raceCar;

	public Camera mainCameraGarage;

	private GameObject[] m_garageSurroundings;

	private CameraRotation m_cameraRotation;

	private float m_initialFOV = 60f;

	public RawImage image;

	private RenderTexture m_renderTarget;

	private Vector2 m_lastScreenSize;

	private GarageCar3dViewContext m_garageContext;

	private void OnEnable()
	{
		Init();
		ApplyRenderTarget();
	}

	private void OnDisable()
	{
		if (mainCameraGarage != null)
		{
			mainCameraGarage.backgroundColor = Color.black;
			mainCameraGarage.targetTexture = null;
			mainCameraGarage.fieldOfView = m_initialFOV;
			m_cameraRotation.enabled = true;
			m_cameraRotation.SetIdle();
		}
		if (m_garageSurroundings != null)
		{
			GameObject[] garageSurroundings = m_garageSurroundings;
			foreach (GameObject gameObject in garageSurroundings)
			{
				if (gameObject != null)
				{
					gameObject.SetActive(value: true);
				}
			}
		}
		if (m_renderTarget != null)
		{
			m_renderTarget.Release();
			m_renderTarget = null;
		}
	}

	private void Update()
	{
		if (m_lastScreenSize.x != (float)Screen.width || m_lastScreenSize.y != (float)Screen.height)
		{
			ApplyRenderTarget();
		}
	}

	private void Init()
	{
		if (mainCameraGarage == null)
		{
			GameObject gameObject = GameObject.Find("GarageCamera");
			if (gameObject != null)
			{
				mainCameraGarage = gameObject.GetComponent<Camera>();
			}
			if (mainCameraGarage == null)
			{
				GameLog.Print(AlertLevel.FatalError, "GarageCameraBinding", "Garage camera not found!");
			}
			else
			{
				m_cameraRotation = mainCameraGarage.GetComponent<CameraRotation>();
				if (m_cameraRotation == null)
				{
					GameLog.Print(AlertLevel.FatalError, "GarageCameraBinding", "Camera rotation script not found!");
				}
				m_initialFOV = mainCameraGarage.fieldOfView;
			}
		}
		if (m_garageSurroundings != null)
		{
			return;
		}
		GameObject gameObject2 = GameObject.Find("GarageSurroundings");
		List<GameObject> list = new List<GameObject>(2);
		if (!(gameObject2 != null))
		{
			return;
		}
		for (int i = 0; i < gameObject2.transform.childCount; i++)
		{
			if (!gameObject2.transform.GetChild(i).name.ToLower().Equals("ground"))
			{
				list.Add(gameObject2.transform.GetChild(i).gameObject);
			}
		}
		m_garageSurroundings = list.ToArray();
	}

	private void ApplyRenderTarget()
	{
		m_lastScreenSize = new Vector2(Screen.width, Screen.height);
		if (mainCameraGarage == null || image == null)
		{
			return;
		}
		Rect screenRect = image.rectTransform.GetScreenRect();
		int num = (int)screenRect.size.x;
		int num2 = (int)screenRect.size.y;
		if (m_renderTarget == null)
		{
			m_renderTarget = new RenderTexture(num, num2, 16);
		}
		if (m_renderTarget.width != num || m_renderTarget.height != num2)
		{
			m_renderTarget.Release();
			m_renderTarget = new RenderTexture(num, num2, 16);
		}
		mainCameraGarage.backgroundColor = new Color(1f, 1f, 1f, 0f);
		mainCameraGarage.targetTexture = m_renderTarget;
		mainCameraGarage.fieldOfView = 15f;
		m_cameraRotation.enabled = false;
		mainCameraGarage.transform.position = new Vector3(-5.88f, 0.26f, 5.46f);
		mainCameraGarage.transform.rotation = Quaternion.Euler(-3.5f, 129f, -0.3f);
		if (m_garageSurroundings != null && m_garageSurroundings.Length != 0)
		{
			GameObject[] garageSurroundings = m_garageSurroundings;
			foreach (GameObject gameObject in garageSurroundings)
			{
				if (gameObject != null)
				{
					gameObject.SetActive(value: false);
				}
			}
		}
		image.texture = m_renderTarget;
		image.material.SetTexture("_MainTex", m_renderTarget);
		image.material.mainTexture = m_renderTarget;
		UpdateCameraFOV();
	}

	public void UpdateCameraFOV()
	{
		if (m_garageContext == null || m_raceCar == null)
		{
			m_garageContext = Object.FindObjectOfType<GarageCar3dViewContext>();
		}
		m_raceCar = m_garageContext.GetRaceCar();
		if (!(m_raceCar == null))
		{
			CarDesc desc = new CarDesc();
			m_raceCar.carX.GetCarDesc(ref desc);
			float num = Vector3.Dot(m_raceCar.transform.position - mainCameraGarage.transform.position, mainCameraGarage.transform.forward);
			float num2 = Mathf.Tan(0.5f * (desc.weight.bounds.size.y * 2f) / num);
			mainCameraGarage.fieldOfView = 2f * num2 * 57.29578f;
			mainCameraGarage.transform.rotation.SetLookRotation(m_raceCar.transform.position + desc.weight.bounds.center);
		}
	}
}
