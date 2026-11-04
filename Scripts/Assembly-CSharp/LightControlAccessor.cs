using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class LightControlAccessor : MonoBehaviour
{
	private bool m_affectsVolumetric;

	private void Awake()
	{
		HDAdditionalLightData component = GetComponent<HDAdditionalLightData>();
		m_affectsVolumetric = component.affectsVolumetric;
		component.enabled = false;
	}

	private void OnEnable()
	{
		if (DayNightController.instance != null)
		{
			DayNightController.instance.RegisterLight(this);
		}
	}

	private void OnDisable()
	{
		if (DayNightController.instance != null)
		{
			DayNightController.instance.UnRegisterLight(this);
		}
	}
}
