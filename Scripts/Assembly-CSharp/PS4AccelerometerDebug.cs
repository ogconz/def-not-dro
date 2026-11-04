using System;
using GameInput;
using Rewired;
using Rewired.ControllerExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PS4AccelerometerDebug : MonoBehaviour
{
	[SerializeField]
	private Slider m_xSlider;

	[SerializeField]
	private Slider m_ySlider;

	[SerializeField]
	private Slider m_zSlider;

	[SerializeField]
	private TMP_Text m_rawValueX;

	[SerializeField]
	private TMP_Text m_rawValueY;

	[SerializeField]
	private TMP_Text m_rawValueZ;

	[SerializeField]
	private TMP_Text m_angularValueX;

	[SerializeField]
	private TMP_Text m_angularValueY;

	[SerializeField]
	private TMP_Text m_angularValueZ;

	private Vector3 m_accelerometerValue;

	private void Start()
	{
		GameConsole.SetCmd("gyroDebugInfo", delegate
		{
			SetVisible(!base.gameObject.activeSelf);
		});
		SetVisible(visible: false);
	}

	private void SetVisible(bool visible)
	{
		base.gameObject.SetActive(visible);
	}

	private void Update()
	{
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if (!(instance == null))
		{
			m_accelerometerValue = GetDualshockAcceleration(instance.lastController);
			UpdateSliders();
			UpdateTexts();
		}
	}

	private void UpdateSliders()
	{
		m_xSlider.value = m_accelerometerValue.x;
		m_ySlider.value = m_accelerometerValue.y;
		m_zSlider.value = m_accelerometerValue.z;
	}

	private void UpdateTexts()
	{
		m_rawValueX.text = Math.Round(m_accelerometerValue.x, 2).ToString();
		m_rawValueY.text = Math.Round(m_accelerometerValue.y, 2).ToString();
		m_rawValueZ.text = Math.Round(m_accelerometerValue.x, 2).ToString();
		m_angularValueX.text = Math.Round(ValueToAngle(m_accelerometerValue.x), 2).ToString();
		m_angularValueY.text = Math.Round(ValueToAngle(m_accelerometerValue.y), 2).ToString();
		m_angularValueZ.text = Math.Round(ValueToAngle(m_accelerometerValue.z), 2).ToString();
	}

	private float ValueToAngle(float value)
	{
		return 57.29578f * value * MathF.PI / 2f;
	}

	private Vector3 GetDualshockAcceleration(Controller controller)
	{
		if (controller == null)
		{
			return Vector3.zero;
		}
		return controller.GetExtension<IDualShock4Extension>()?.GetAccelerometerValue() ?? Vector3.zero;
	}
}
