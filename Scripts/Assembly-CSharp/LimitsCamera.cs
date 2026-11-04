using System;
using UnityEngine;

[Serializable]
public class LimitsCamera
{
	[SerializeField]
	private string m_identifier;

	[SerializeField]
	private Vector3 m_offsetMin = -Vector3.one;

	[SerializeField]
	private Vector3 m_offsetMax = Vector3.one;

	[SerializeField]
	private float m_offsetStep = 0.1f;

	[SerializeField]
	private Vector2 m_pitch = new Vector2(-20f, 20f);

	[SerializeField]
	private float m_pitchStep = 0.1f;

	[SerializeField]
	private Vector2 m_fov = new Vector2(20f, 120f);

	[SerializeField]
	private float m_fovStep = 1f;

	public string identifier => m_identifier;

	public Vector3 offsetMin => m_offsetMin;

	public Vector3 offsetMax => m_offsetMax;

	public float offsetStep => m_offsetStep;

	public Vector2 pitch => m_pitch;

	public float pitchStep => m_pitchStep;

	public Vector2 fov => m_fov;

	public float fovStep => m_fovStep;
}
