using CarX;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public static class FMODCarX
{
	private static bool m_cameraViewIsValid = true;

	private static PARAMETER_ID m_cameraViewParameterId;

	public static void Initialize()
	{
		PARAMETER_DESCRIPTION parameter;
		RESULT parameterDescriptionByName = RuntimeManager.StudioSystem.getParameterDescriptionByName("CameraView", out parameter);
		if (parameterDescriptionByName != RESULT.OK)
		{
			UnityEngine.Debug.LogError($"FMOD get parameter description CameraView failed: {parameterDescriptionByName}");
			m_cameraViewIsValid = false;
		}
		else
		{
			m_cameraViewParameterId = parameter.id;
			m_cameraViewIsValid = true;
		}
		if (CameraSwitch.instance != null)
		{
			CameraSwitch.instance.onCameraChanged += OnCameraChanged;
		}
	}

	public static float GetCurrentCameraViewParamValue()
	{
		if (CameraSwitch.instance == null || CameraSwitch.instance.targetRaceCar == null)
		{
			return 2f;
		}
		Car carX = CameraSwitch.instance.targetRaceCar.carX;
		if (CameraSwitch.instance.CheckForOutsideView(carX))
		{
			return CameraSwitch.instance.CheckForFrontStaticView(carX) ? 0f : 2f;
		}
		return 1f;
	}

	private static void OnCameraChanged(CameraSwitch.ECameraType camType)
	{
		if (m_cameraViewIsValid)
		{
			float currentCameraViewParamValue = GetCurrentCameraViewParamValue();
			RESULT rESULT = RuntimeManager.StudioSystem.setParameterByID(m_cameraViewParameterId, currentCameraViewParamValue);
			if (rESULT != RESULT.OK)
			{
				UnityEngine.Debug.LogError($"FMOD set parameter CameraView failed: {rESULT}");
			}
		}
	}
}
