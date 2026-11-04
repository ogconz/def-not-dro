using System;
using PrefsModels;
using UnityEngine;

[Serializable]
public class DynamicCamParameters
{
	public enum AimMode
	{
		Steer = 0,
		Velocity = 1
	}

	[SerializeField]
	private AimMode m_camAimMode;

	[SerializeField]
	private bool m_moveWithAccel = true;

	[SerializeField]
	private float m_velocityScale = 0.4f;

	[SerializeField]
	private float m_maxRotSpeed = 360f;

	[SerializeField]
	private float m_maxRotAngle = 30f;

	[SerializeField]
	private float m_cameraSmooth;

	[SerializeField]
	private float m_accelSmooth = 0.2f;

	[SerializeField]
	private float m_dynamicCamDeadZone;

	[SerializeField]
	private GlobalModel.DynamicCockpitCamMode m_cockpitCamMode;

	public AimMode camAimMode
	{
		get
		{
			return m_camAimMode;
		}
		set
		{
			m_camAimMode = value;
		}
	}

	public float velocityScale
	{
		get
		{
			return m_velocityScale;
		}
		set
		{
			m_velocityScale = value;
		}
	}

	public float maxRotSpeed
	{
		get
		{
			return m_maxRotSpeed;
		}
		set
		{
			m_maxRotSpeed = value;
		}
	}

	public float maxRotAngle
	{
		get
		{
			return m_maxRotAngle;
		}
		set
		{
			m_maxRotAngle = value;
		}
	}

	public float cameraSmooth
	{
		get
		{
			return m_cameraSmooth;
		}
		set
		{
			m_cameraSmooth = value;
		}
	}

	public float accelSmooth
	{
		get
		{
			return m_accelSmooth;
		}
		set
		{
			m_accelSmooth = value;
		}
	}

	public float dynamicCamDeadZone
	{
		get
		{
			return m_dynamicCamDeadZone;
		}
		set
		{
			m_dynamicCamDeadZone = value;
		}
	}

	public bool moveWithAccel
	{
		get
		{
			return m_moveWithAccel;
		}
		set
		{
			m_moveWithAccel = value;
		}
	}

	public GlobalModel.DynamicCockpitCamMode cockpitCamMode
	{
		get
		{
			return m_cockpitCamMode;
		}
		set
		{
			m_cockpitCamMode = value;
		}
	}

	public virtual DynamicCamParameters Copy()
	{
		return MemberwiseClone() as DynamicCamParameters;
	}
}
