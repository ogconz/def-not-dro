using System.Collections.Generic;
using UnityEngine;

public class FreeCamerasManager : MonoBehaviour
{
	private List<GameObject> m_freeCameras = new List<GameObject>();

	private void Awake()
	{
		this.RemoveCloneFromName();
	}

	public GameObject InitCamera(Vector3 position)
	{
		GameObject gameObject = GetCameraByPosition(position);
		if (gameObject == null)
		{
			gameObject = new GameObject("Camera");
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.position = position;
			m_freeCameras.Add(gameObject.gameObject);
		}
		return gameObject;
	}

	public void DestroyByIndex(int index)
	{
		if (index >= 0 && index < m_freeCameras.Count)
		{
			Object.Destroy(m_freeCameras[index]);
			m_freeCameras[index] = null;
		}
	}

	private GameObject GetCameraByPosition(Vector3 position)
	{
		foreach (GameObject freeCamera in m_freeCameras)
		{
			if (!(freeCamera == null) && !(freeCamera.transform == null) && freeCamera.transform.position == position)
			{
				return freeCamera;
			}
		}
		return null;
	}

	public void ClearDeactivatedCameras()
	{
		m_freeCameras.RemoveAll((GameObject camera) => camera == null);
	}
}
