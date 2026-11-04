using UnityEngine;

public class CarCameraSettings
{
	public string indentifier;

	[SerializeField]
	private Vector3 m_offset;

	[SerializeField]
	private float m_pitch;

	[SerializeField]
	private float m_fov;

	public Vector3 offset
	{
		get
		{
			return m_offset;
		}
		set
		{
			m_offset = value;
		}
	}

	public float pitch
	{
		get
		{
			return m_pitch;
		}
		set
		{
			m_pitch = value;
		}
	}

	public float fov
	{
		get
		{
			return m_fov;
		}
		set
		{
			m_fov = value;
		}
	}

	public CarCameraSettings Copy()
	{
		return MemberwiseClone() as CarCameraSettings;
	}
}
