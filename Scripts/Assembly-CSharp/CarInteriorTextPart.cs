using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
[AddComponentMenu("Scripts/Car/CarInteriorTextPart")]
public class CarInteriorTextPart : CarInteriorPart
{
	[SerializeField]
	private bool m_showInPercent;

	[SerializeField]
	private int m_decimalPlaces;

	private Text m_text;

	private float m_lastValue;

	private float m_diff;

	protected override void Init()
	{
		m_text = GetComponent<Text>();
		m_lastValue = -1f;
		m_diff = 1f / Mathf.Pow(10f, m_decimalPlaces);
		switch (part)
		{
		case CarPart.Speedometer:
			m_characteristic = new SpeedCharacteristic(m_prefs);
			break;
		case CarPart.Tachometer:
			m_characteristic = new TachoCharacteristic();
			break;
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
		if (m_decimalPlaces > 0)
		{
			float value = m_characteristic.GetValue();
			if (Math.Abs(m_lastValue - value) > m_diff)
			{
				m_text.text = value.ToString("0.00");
				m_lastValue = value;
			}
		}
		else if (m_showInPercent)
		{
			m_text.text = Mathf.RoundToInt(m_characteristic.GetFillValue() * 100f).ToString("N0");
		}
		else
		{
			m_text.text = m_characteristic.GetStringValue();
		}
	}
}
