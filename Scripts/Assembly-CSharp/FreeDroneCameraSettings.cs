public class FreeDroneCameraSettings
{
	private float m_speedMovement;

	private float m_speedMultiplier;

	private float m_movementDamping;

	private float m_dampingSpeed;

	private float m_speedRotation;

	private float m_dampRotation;

	private float m_fov;

	private float m_amplitudeGain;

	private float m_frequencyGain;

	private float m_targetDamping;

	private float m_lockHeadTime;

	public float speedMovement
	{
		get
		{
			return m_speedMovement;
		}
		set
		{
			m_speedMovement = value;
		}
	}

	public float speedMultiplier
	{
		get
		{
			return m_speedMultiplier;
		}
		set
		{
			m_speedMultiplier = value;
		}
	}

	public float movementDamping
	{
		get
		{
			return m_movementDamping;
		}
		set
		{
			m_movementDamping = value;
		}
	}

	public float dampingSpeed
	{
		get
		{
			return m_dampingSpeed;
		}
		set
		{
			m_dampingSpeed = value;
		}
	}

	public float speedRotation
	{
		get
		{
			return m_speedRotation;
		}
		set
		{
			m_speedRotation = value;
		}
	}

	public float dampRotation
	{
		get
		{
			return m_dampRotation;
		}
		set
		{
			m_dampRotation = value;
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

	public float amplitudeGain
	{
		get
		{
			return m_amplitudeGain;
		}
		set
		{
			m_amplitudeGain = value;
		}
	}

	public float frequencyGain
	{
		get
		{
			return m_frequencyGain;
		}
		set
		{
			m_frequencyGain = value;
		}
	}

	public float targetDamping
	{
		get
		{
			return m_targetDamping;
		}
		set
		{
			m_targetDamping = value;
		}
	}

	public float lockHeadTime
	{
		get
		{
			return m_lockHeadTime;
		}
		set
		{
			m_lockHeadTime = value;
		}
	}

	public FreeDroneCameraSettings Copy()
	{
		return MemberwiseClone() as FreeDroneCameraSettings;
	}
}
