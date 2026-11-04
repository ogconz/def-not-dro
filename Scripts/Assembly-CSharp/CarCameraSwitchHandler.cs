using System;
using System.Collections.Generic;
using SyncMultiplayer;
using UnityEngine;

[AddComponentMenu("Scripts/Car/CarCameraSwitchHandler")]
public class CarCameraSwitchHandler : MonoBehaviour
{
	public enum ObjectType
	{
		Unknown = 0,
		Interior = 1,
		CockpitPlace = 2
	}

	public class ViewObject
	{
		public ObjectType type;

		public GameObject data;
	}

	public static Action<bool> OnChangeCameraViewEvent;

	public CameraPartMarker[] cameraPlaceObjects;

	private List<CameraPartMarker> m_cameraPlaceObjects = new List<CameraPartMarker>();

	private RaceCar m_car;

	private bool? m_lastOutside;

	public void Clear()
	{
		m_cameraPlaceObjects.Clear();
		m_cameraPlaceObjects.AddRange(cameraPlaceObjects);
	}

	public void AddCameraPlaceObject(CameraPartMarker go, ObjectType type = ObjectType.Unknown)
	{
		m_cameraPlaceObjects.Add(go);
	}

	public bool GetLastOutsideValue()
	{
		if (!m_lastOutside.HasValue)
		{
			return true;
		}
		return m_lastOutside.Value;
	}

	public void ForcedChangeDriverHead(bool active)
	{
		CameraPartMarker[] array = cameraPlaceObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(active);
		}
	}

	public void OnChangeCameraView(bool outside)
	{
		CARXCockpitCamera cARXCockpitCamera = CameraSwitch.instance.FindActiveCamera() as CARXCockpitCamera;
		if (cARXCockpitCamera != null && !outside)
		{
			cARXCockpitCamera.SetHeadOnCockpit(cameraPlaceObjects);
			if (cameraPlaceObjects.Length != 0)
			{
				m_lastOutside = cameraPlaceObjects[0].isActiveAndEnabled;
			}
		}
		else if (outside)
		{
			if (cameraPlaceObjects.Length != 0 && cameraPlaceObjects[0].isActiveAndEnabled != outside)
			{
				m_lastOutside = cameraPlaceObjects[0].isActiveAndEnabled;
			}
			ForcedChangeDriverHead(active: false);
		}
		else
		{
			ForcedChangeDriverHead(!GetLastOutsideValue());
		}
		if (PlayerCarControl.instance.car != null)
		{
			PlayerCarControl.instance.car.UpdateDriverVisibility();
			PlayerCarControl.instance.car.UpdateWindowsVisability();
		}
		else if (m_car != null)
		{
			m_car.UpdateDriverVisibility();
			m_car.UpdateWindowsVisability();
		}
		OnChangeCameraViewEvent.SafeCall(outside);
	}

	private void SwitchCameraHandler(CameraSwitch.ECameraType cameraType)
	{
		OnChangeCameraView(GetLastOutsideValue());
	}

	private void Start()
	{
		m_car = GetComponentInParent<RaceCar>();
		if (!(NetworkController.InstanceGame.LocalPlayer.userCar != m_car))
		{
			bool isOutsideCamera = CameraSwitch.instance.CheckForOutsideView(m_car.carX);
			EnterChangeCameraViewOnCarLoad(isOutsideCamera);
		}
	}

	private void EnterChangeCameraViewOnCarLoad(bool isOutsideCamera)
	{
		if (m_car.IsVisualLoaded)
		{
			if (CameraSwitch.instance != null)
			{
				OnChangeCameraView(isOutsideCamera);
			}
			else
			{
				OnChangeCameraView(GetLastOutsideValue());
			}
		}
		else
		{
			m_car.OnCarVisualLoadFinishEvent += delegate
			{
				OnChangeCameraView(isOutsideCamera);
			};
		}
	}

	private void OnDestroy()
	{
		if (CameraSwitch.instance != null)
		{
			CameraSwitch.instance.onCameraChanged -= SwitchCameraHandler;
		}
	}
}
