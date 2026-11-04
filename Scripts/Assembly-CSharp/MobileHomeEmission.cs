using UnityEngine;

public class MobileHomeEmission : MonoBehaviour
{
	[SerializeField]
	private Renderer m_renderer;

	private Material m_material;

	private const string INTENSITY_PROP = "_EmissiveIntensity";

	private const string COLOR_PROP = "_EmissiveColor";

	private DayNightController m_dayNightCtrl;

	private void Awake()
	{
		m_material = m_renderer.material;
	}

	private void Start()
	{
		m_dayNightCtrl = DayNightController.instance;
		if ((bool)m_dayNightCtrl)
		{
			m_dayNightCtrl.OnChangeDayNightMode.AddListener(OnChangeDayNightModeHandler);
		}
	}

	private void OnChangeDayNightModeHandler()
	{
		Color color = Color.white * m_material.GetFloat("_EmissiveIntensity");
		m_material.SetColor("_EmissiveColor", DayNightController.instance.IsDayLight ? Color.black : color);
	}

	private void OnDestroy()
	{
		Object.Destroy(m_material);
		if ((bool)m_dayNightCtrl)
		{
			m_dayNightCtrl.OnChangeDayNightMode.RemoveListener(OnChangeDayNightModeHandler);
		}
	}
}
