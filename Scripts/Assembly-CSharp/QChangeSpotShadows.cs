using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class QChangeSpotShadows : MonoBehaviour
{
	private SpotLightSettingsPass m_spotLightSettings;

	private void Awake()
	{
		CustomPassVolume component = GetComponent<CustomPassVolume>();
		m_spotLightSettings = component.customPasses[0] as SpotLightSettingsPass;
		Refresh(GraphicsQuality.nightSettings.spotShadows, force: true);
	}

	private void Update()
	{
		Refresh(GraphicsQuality.nightSettings.spotShadows);
	}

	private void Refresh(int value, bool force = false)
	{
		if (m_spotLightSettings.maximumSpotShdowsCount != value || force)
		{
			m_spotLightSettings.maximumSpotShdowsCount = value;
		}
	}
}
