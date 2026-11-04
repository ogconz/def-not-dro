using System;
using UnityEngine;

[Serializable]
public class PhotoCamParameters
{
	[SerializeField]
	private int m_cameraMode;

	[SerializeField]
	private float m_speedCameraGate;

	[SerializeField]
	private float m_chromaticAberration;

	[SerializeField]
	private float m_cameraFocus;

	[SerializeField]
	private float m_exposition = 0.5f;

	[SerializeField]
	private float m_aperture;

	[SerializeField]
	private float m_сontrast = 0.5f;

	[SerializeField]
	private float m_saturation = 0.5f;

	[SerializeField]
	private float m_brightness = 0.5f;

	[SerializeField]
	private float m_vignetting;

	[SerializeField]
	private float m_temperature = 0.5f;

	[SerializeField]
	private int m_pilot = 1;

	public int cameraMode
	{
		get
		{
			return m_cameraMode;
		}
		set
		{
			m_cameraMode = value;
		}
	}

	public float speedCameraGate
	{
		get
		{
			return m_speedCameraGate;
		}
		set
		{
			m_speedCameraGate = value;
		}
	}

	public float chromaticAberration
	{
		get
		{
			return m_chromaticAberration;
		}
		set
		{
			m_chromaticAberration = value;
		}
	}

	public float cameraFocus
	{
		get
		{
			return m_cameraFocus;
		}
		set
		{
			m_cameraFocus = value;
		}
	}

	public float exposition
	{
		get
		{
			return m_exposition;
		}
		set
		{
			m_exposition = value;
		}
	}

	public float aperture
	{
		get
		{
			return m_aperture;
		}
		set
		{
			m_aperture = value;
		}
	}

	public float contrast
	{
		get
		{
			return m_сontrast;
		}
		set
		{
			m_сontrast = value;
		}
	}

	public float saturation
	{
		get
		{
			return m_saturation;
		}
		set
		{
			m_saturation = value;
		}
	}

	public float brightness
	{
		get
		{
			return m_brightness;
		}
		set
		{
			m_brightness = value;
		}
	}

	public float vignetting
	{
		get
		{
			return m_vignetting;
		}
		set
		{
			m_vignetting = value;
		}
	}

	public float temperature
	{
		get
		{
			return m_temperature;
		}
		set
		{
			m_temperature = value;
		}
	}

	public int pilot
	{
		get
		{
			return m_pilot;
		}
		set
		{
			m_pilot = value;
		}
	}
}
