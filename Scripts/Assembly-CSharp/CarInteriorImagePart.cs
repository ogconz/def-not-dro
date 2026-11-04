using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[AddComponentMenu("Scripts/Car/CarInteriorImagePart")]
public class CarInteriorImagePart : CarInteriorPart
{
	[Range(0f, 1f)]
	[SerializeField]
	private float m_maxFill = 1f;

	[SerializeField]
	private bool m_forceUseSerialiedMaxFill;

	[SerializeField]
	private bool m_useCustomRevLimiterValue;

	[SerializeField]
	private float m_customRevLimiterValue;

	private Image m_image;

	protected override void Init()
	{
		m_image = GetComponent<Image>();
		switch (part)
		{
		case CarPart.Speedometer:
			m_characteristic = new SpeedCharacteristic(m_prefs);
			break;
		case CarPart.Tachometer:
		{
			float? maxRevLimiterValue = null;
			if (m_useCustomRevLimiterValue)
			{
				maxRevLimiterValue = m_customRevLimiterValue;
			}
			m_characteristic = new TachoCharacteristic(maxRevLimiterValue);
			break;
		}
		case CarPart.Gear:
			m_characteristic = new GearCharacteristic();
			break;
		case CarPart.Turbo:
			m_characteristic = new TurboCharacteristic();
			break;
		}
	}

	protected override void UpdateValue()
	{
		float num = m_maxFill;
		if (!m_forceUseSerialiedMaxFill && m_characteristic.maxFillVal != 0f)
		{
			num = m_characteristic.maxFillVal;
		}
		m_image.fillAmount = Mathf.Clamp(num * m_characteristic.GetFillValue(), 0f, num);
	}
}
