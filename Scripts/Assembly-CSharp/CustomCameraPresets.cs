using System;
using System.Collections.Generic;
using Cinemachine;
using Generic.Attributes;
using UnityEngine;

public class CustomCameraPresets : Singleton<CustomCameraPresets>
{
	private GarageCar3dViewContext m_car3dView;

	[ArrayElementTitle("stateType")]
	public List<CustomCameraPreset> presets;

	public Camera vinylPreviewCamera;

	private CameraRotation m_rotation;

	public CustomCameraPreset currentPreset { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		foreach (CustomCameraPreset preset in presets)
		{
			Transform transform = base.transform.Find(preset.stateType.ToString());
			if (!(transform == null))
			{
				preset.virtualCamera = transform.GetComponent<CinemachineVirtualCamera>();
				preset.customCamSetting = transform.GetComponent<VCamCustomSetting>();
			}
		}
	}

	private void Start()
	{
		m_rotation = CameraRotation.instance;
	}

	private CustomCameraPreset GetPreset(CustomizationCameraPreset stateType)
	{
		CustomCameraPreset customCameraPreset = presets.Find((CustomCameraPreset p) => p.stateType == stateType);
		if (customCameraPreset == null)
		{
			customCameraPreset = new CustomCameraPreset();
		}
		return customCameraPreset;
	}

	private CustomizationCameraPreset GetCameraPreset(string presetName)
	{
		string text = presetName.ToLower();
		string[] names = Enum.GetNames(typeof(CustomizationCameraPreset));
		CustomizationCameraPreset result = CustomizationCameraPreset.Chassis;
		for (int i = 0; i < names.Length; i++)
		{
			if (names[i].ToLower() == text)
			{
				result = (CustomizationCameraPreset)i;
				break;
			}
		}
		return result;
	}

	private void DeactivateVirtualCamera()
	{
		foreach (CustomCameraPreset preset in presets)
		{
			if (preset.virtualCamera != null)
			{
				preset.virtualCamera.gameObject.SetActive(value: false);
			}
		}
	}

	private void ActivateVirtualCamera(CustomCameraPreset cameraPreset, bool isOffsetEnabled = false)
	{
		DeactivateVirtualCamera();
		if (cameraPreset.virtualCamera == null)
		{
			ResetToRotation();
			return;
		}
		m_rotation?.SetCustomVirtualCamera();
		cameraPreset.virtualCamera.gameObject.SetActive(value: true);
		if (m_car3dView == null && States.mainCurrent is BaseGameState { contextOwner: { } contextOwner })
		{
			m_car3dView = contextOwner.Get<GarageCar3dViewContext>();
		}
		cameraPreset.customCamSetting?.SetTarget(m_car3dView.GetRaceCar().carX, cameraPreset.stateType, isOffsetEnabled);
	}

	public void ResetToRotation()
	{
		if (currentPreset?.virtualCamera != null)
		{
			CameraRotation.CalcDirectionAndElevation(currentPreset.virtualCamera.transform, out var result);
			m_rotation.SetDirectionAndElevation(result.direction, result.elevation);
			m_rotation.SetIdle();
		}
	}

	public void CameraTo(CustomizationCameraPreset stateType)
	{
		CustomCameraPreset preset = GetPreset(stateType);
		bool isOffsetEnabled = false;
		if (States.mainCurrent is BaseGameState { contextOwner: var contextOwner })
		{
			isOffsetEnabled = contextOwner != null && (contextOwner.IsActive<UIBodyCustomizationContext>() || contextOwner.IsActive<UIPaintCustomizationContext>());
		}
		ActivateVirtualCamera(preset, isOffsetEnabled);
		currentPreset = preset;
	}

	public void CameraTo(string presetName)
	{
		CameraTo(GetCameraPreset(presetName));
	}
}
