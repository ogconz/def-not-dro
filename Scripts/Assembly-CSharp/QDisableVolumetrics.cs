using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class QDisableVolumetrics : MonoBehaviour
{
	private HDAdditionalCameraData m_additionalCameraData;

	private FrameSettingsOverrideMask m_frameSettingsOverrideMask;

	private bool m_volumetrics = true;

	private void Awake()
	{
		m_additionalCameraData = GetComponent<HDAdditionalCameraData>();
		m_additionalCameraData.customRenderingSettings = true;
		m_frameSettingsOverrideMask = m_additionalCameraData.renderingPathCustomFrameSettingsOverrideMask;
		m_frameSettingsOverrideMask.mask[27u] = true;
		m_frameSettingsOverrideMask.mask[28u] = true;
		m_additionalCameraData.renderingPathCustomFrameSettingsOverrideMask = m_frameSettingsOverrideMask;
		Refresh(GraphicsQuality.nightSettings.volumetric, force: true);
	}

	private void Refresh(bool value, bool force = false)
	{
		if (m_volumetrics != value || force)
		{
			m_volumetrics = value;
			m_additionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Volumetrics, m_volumetrics);
		}
	}

	private void Update()
	{
		Refresh(GraphicsQuality.nightSettings.volumetric);
	}
}
