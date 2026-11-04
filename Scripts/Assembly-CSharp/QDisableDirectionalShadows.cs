using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class QDisableDirectionalShadows : MonoBehaviour
{
	private HDAdditionalLightData m_lightData;

	private bool m_shadows = true;

	public bool nightSettingsShadows
	{
		get
		{
			if (!GraphicsQuality.nightSettings.shadows && (bool)DayNightController.instance)
			{
				return DayNightController.instance.IsDayLight;
			}
			return true;
		}
	}

	private void Awake()
	{
		m_lightData = GetComponent<HDAdditionalLightData>();
		Refresh(nightSettingsShadows, force: true);
	}

	private void Refresh(bool value, bool force = false)
	{
		if (value != m_shadows || force)
		{
			m_shadows = value;
			m_lightData.EnableShadows(m_shadows);
		}
	}

	private void Update()
	{
		Refresh(nightSettingsShadows);
	}
}
