using System;
using Cinemachine;
using UnityEngine;

[Serializable]
public class CustomCameraPreset
{
	public CustomizationCameraPreset stateType;

	public CinemachineVirtualCamera virtualCamera;

	[HideInInspector]
	public VCamCustomSetting customCamSetting;
}
