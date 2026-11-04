using CarX;
using Minimaps;
using TMPro;
using UnityEngine;

public class InteriorMinimap : MonoBehaviour
{
	[SerializeField]
	private InteriorMiniMapVisualizer m_visualizer;

	[SerializeField]
	private TextMeshProUGUI m_mapName;

	[SerializeField]
	private GameObject m_interiorMinimapRoot;

	private void OnEnable()
	{
		if (!(States.mainCurrent is BaseRaceState))
		{
			m_interiorMinimapRoot.SetActive(value: false);
			return;
		}
		m_mapName.text = MinimapProvider.instance.trackName;
		m_visualizer.Initialize();
		SetInteriorMinimapVisibility();
		if (CameraSwitch.instance != null)
		{
			CameraSwitch.instance.onCameraChanged += OnCameraSwitchHandler;
		}
	}

	private void OnDisable()
	{
		if (CameraSwitch.instance != null)
		{
			CameraSwitch.instance.onCameraChanged -= OnCameraSwitchHandler;
		}
	}

	private void OnCameraSwitchHandler(CameraSwitch.ECameraType _)
	{
		SetInteriorMinimapVisibility();
	}

	private void SetInteriorMinimapVisibility()
	{
		m_interiorMinimapRoot.SetActive(GetVisibilityValid());
	}

	private bool GetVisibilityValid()
	{
		if (IsCameraValid())
		{
			return IsRaceCarValid();
		}
		return false;
	}

	private bool IsCameraValid()
	{
		return CameraSwitch.instance.FindActiveCamera() is CockpitCamera;
	}

	private bool IsRaceCarValid()
	{
		return m_visualizer.assignedCar == CameraSwitch.instance.targetRaceCar;
	}
}
