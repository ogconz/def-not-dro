using System;
using System.Collections.Generic;
using CarX;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class EngineGraph : MonoBehaviour
{
	private const float GraphAxisYStep = 500f;

	[SerializeField]
	private UILineRenderer m_torqueRenderer;

	[SerializeField]
	private UILineRenderer m_hpRenderer;

	[SerializeField]
	private UILineRenderer m_revLimiterRenderer;

	[SerializeField]
	private Transform m_hpPeak;

	[SerializeField]
	private Transform m_torquePeak;

	[SerializeField]
	private Transform m_start;

	[SerializeField]
	private Transform m_end;

	[Space]
	[SerializeField]
	private float m_rpmMin;

	[SerializeField]
	private float m_rpmMax;

	[SerializeField]
	private float m_rpmCutoff;

	[Space]
	[SerializeField]
	private int m_steps;

	public void SetCar(Car car, out float maxGraphAxisY)
	{
		if (m_rpmMin == m_rpmMax || (m_start.localPosition - m_end.localPosition).sqrMagnitude < float.Epsilon)
		{
			Debug.LogWarning("EngineGraph -> SetCar: Invalid settings!");
			maxGraphAxisY = 0f;
			return;
		}
		Vector3 vector = m_end.localPosition - m_start.localPosition;
		List<Vector2> list = new List<Vector2>();
		List<Vector2> list2 = new List<Vector2>();
		car.CalcMaxPowerMaxTorque(out var max_hp, out var max_hp_rpm, out var max_tq, out var max_tq_rpm);
		max_tq = Car.ConvertNMtoLbFt(max_tq);
		maxGraphAxisY = Mathf.Max(max_tq, max_hp);
		maxGraphAxisY = (float)Math.Ceiling(maxGraphAxisY / 500f) * 500f;
		if (maxGraphAxisY < float.Epsilon)
		{
			return;
		}
		Vector3 localPosition = m_start.localPosition;
		for (int i = 0; i < m_steps; i++)
		{
			float num = m_rpmMin + m_rpmMax / (float)m_steps * ((float)i + 1f);
			if (!(num < m_rpmCutoff))
			{
				float wtorq;
				float hp;
				float whp;
				float torq;
				if (num > car.engineRevLimiter || num > m_rpmMax)
				{
					float num2 = Mathf.Min(m_rpmMax, car.engineRevLimiter);
					car.GetDynoGraphAtRPM(num2, out torq, out wtorq, out hp, out whp);
					torq = Car.ConvertNMtoLbFt(torq);
					list.Add(new Vector3(m_start.localPosition.x + (num2 - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + hp / maxGraphAxisY * vector.y));
					list2.Add(new Vector3(localPosition.x + (num2 - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + torq / maxGraphAxisY * vector.y));
					break;
				}
				car.GetDynoGraphAtRPM(num, out torq, out wtorq, out hp, out whp);
				torq = Car.ConvertNMtoLbFt(torq);
				list.Add(new Vector3(m_start.localPosition.x + (num - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + hp / maxGraphAxisY * vector.y));
				list2.Add(new Vector3(localPosition.x + (num - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + torq / maxGraphAxisY * vector.y));
			}
		}
		if (car.engineRevLimiter <= m_rpmMax)
		{
			if (!m_revLimiterRenderer.enabled)
			{
				m_revLimiterRenderer.enabled = true;
			}
			float x = localPosition.x + (car.engineRevLimiter - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x;
			m_revLimiterRenderer.Points = new Vector2[2]
			{
				new Vector2(x, localPosition.y),
				new Vector2(x, m_end.localPosition.y)
			};
		}
		else
		{
			if (m_revLimiterRenderer.enabled)
			{
				m_revLimiterRenderer.enabled = false;
			}
			m_revLimiterRenderer.Points = Array.Empty<Vector2>();
		}
		m_hpPeak.localPosition = new Vector2(localPosition.x + (max_hp_rpm - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + max_hp / maxGraphAxisY * vector.y);
		m_torquePeak.localPosition = new Vector2(localPosition.x + (max_tq_rpm - m_rpmMin) / (m_rpmMax - m_rpmMin) * vector.x, localPosition.y + max_tq / maxGraphAxisY * vector.y);
		m_hpRenderer.Points = list.ToArray();
		m_torqueRenderer.Points = list2.ToArray();
	}

	private void Awake()
	{
		m_torqueRenderer.enabled = true;
		m_hpRenderer.enabled = true;
		m_revLimiterRenderer.enabled = true;
		m_torqueRenderer.material = new UnityEngine.Material(Shader.Find("Sprites/Default"));
		m_hpRenderer.material = new UnityEngine.Material(Shader.Find("Sprites/Default"));
		m_revLimiterRenderer.material = new UnityEngine.Material(Shader.Find("Sprites/Default"));
	}
}
