using UnityEngine;

[AddComponentMenu("Scripts/Generic/Util/SetupRenderSettings")]
public class SetupRenderSettings : MonoBehaviour
{
	[SerializeField]
	private Color m_ambientLight;

	[SerializeField]
	private float m_flareStrength;

	[SerializeField]
	private bool m_fog;

	[SerializeField]
	private Color m_fogColor;

	[SerializeField]
	private float m_fogDensity;

	[SerializeField]
	private float m_fogEndDistance;

	[SerializeField]
	private FogMode m_fogMode;

	[SerializeField]
	private float m_fogStartDistance;

	[SerializeField]
	private float m_haloStrength;

	[SerializeField]
	private Material m_skybox;

	[SerializeField]
	private LightmapsMode m_lightmapsMode;

	[ContextMenu("Capture")]
	private void Capture()
	{
		m_ambientLight = RenderSettings.ambientLight;
		m_flareStrength = RenderSettings.flareStrength;
		m_fog = RenderSettings.fog;
		m_fogColor = RenderSettings.fogColor;
		m_fogDensity = RenderSettings.fogDensity;
		m_fogEndDistance = RenderSettings.fogEndDistance;
		m_fogMode = RenderSettings.fogMode;
		m_fogStartDistance = RenderSettings.fogStartDistance;
		m_haloStrength = RenderSettings.haloStrength;
		m_skybox = RenderSettings.skybox;
		m_lightmapsMode = LightmapSettings.lightmapsMode;
	}

	[ContextMenu("Apply")]
	private void Apply()
	{
		RenderSettings.ambientLight = m_ambientLight;
		RenderSettings.flareStrength = m_flareStrength;
		RenderSettings.fog = m_fog;
		RenderSettings.fogColor = m_fogColor;
		RenderSettings.fogDensity = m_fogDensity;
		RenderSettings.fogEndDistance = m_fogEndDistance;
		RenderSettings.fogMode = m_fogMode;
		RenderSettings.fogStartDistance = m_fogStartDistance;
		RenderSettings.haloStrength = m_haloStrength;
		RenderSettings.skybox = m_skybox;
		LightmapSettings.lightmapsMode = m_lightmapsMode;
	}

	private void Start()
	{
		Apply();
	}
}
