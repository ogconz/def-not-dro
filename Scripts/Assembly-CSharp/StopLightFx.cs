using CarX;
using UnityEngine;

[AddComponentMenu("Car/StopLightFx")]
public class StopLightFx : MonoBehaviour
{
	private Car m_car;

	private float m_intensity;

	private MaterialSetter m_materialSetter;

	private Car car
	{
		get
		{
			if (!m_car)
			{
				m_car = GetComponentInParent<Car>();
			}
			return m_car;
		}
	}

	private void OnEnable()
	{
		if (m_car == null)
		{
			m_car = GetComponentInParent<Car>();
		}
		m_materialSetter = new MaterialSetter(base.gameObject, new string[1] { "_Intensity" }, includeChildren: true);
	}

	private void OnDisable()
	{
		m_materialSetter = null;
	}

	private void Start()
	{
		SetIntensity(0f);
	}

	private void Update()
	{
		if (!(m_car == null))
		{
			float num = ((car.brake > 0.05f) ? 1f : 0f);
			if (num != m_intensity)
			{
				m_intensity += Mathf.Sign(num - m_intensity) * Time.deltaTime * 10f;
				m_intensity = Mathf.Clamp01(m_intensity);
				SetIntensity(m_intensity);
			}
		}
	}

	private void SetIntensity(float intensity)
	{
		m_materialSetter.SetFloat(0, intensity);
	}
}
