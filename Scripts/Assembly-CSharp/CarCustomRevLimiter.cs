using CarX;

public class CarCustomRevLimiter : CARXBaseScript
{
	private const float maxRPM = 15000f;

	public float revBias = 200f;

	public bool smoothMode;

	private RaceCar m_raceCar;

	private Car m_carx;

	private float m_carXrevBias = 50f;

	private float m_revLim = 1000f;

	private float m_gateRPM = 15000f;

	private FMODPopExhaust[] m_popSounds = new FMODPopExhaust[0];

	public RaceCar raceCar => m_raceCar ?? (m_raceCar = GetComponentInParent<RaceCar>());

	protected override bool OnEnableHandler()
	{
		if (!raceCar)
		{
			return false;
		}
		m_carx = raceCar.carX;
		Singletone<Simulator>.instance.OnPreSimulationEvent += RevControl;
		GameConsole.SetCmd("x_rcarxBias", delegate
		{
			int.TryParse(GameConsole.cmdArgs[1], out var result);
			m_carXrevBias = result;
		});
		GameConsole.SetCmd("x_rBias", delegate
		{
			int.TryParse(GameConsole.cmdArgs[1], out var result);
			revBias = result;
		});
		GameConsole.SetCmd("x_rSmooth", delegate
		{
			int.TryParse(GameConsole.cmdArgs[1], out var result);
			smoothMode = result != 0;
		});
		return base.OnEnableHandler();
	}

	protected void Start()
	{
		m_popSounds = raceCar.GetComponentsInChildren<FMODPopExhaust>();
	}

	protected override void OnDisableHandler()
	{
		if ((bool)m_carx)
		{
			Singletone<Simulator>.instance.OnPreSimulationEvent -= RevControl;
		}
		base.OnDisableHandler();
	}

	private void Play()
	{
		FMODPopExhaust[] popSounds = m_popSounds;
		foreach (FMODPopExhaust fMODPopExhaust in popSounds)
		{
			if (fMODPopExhaust.enabled)
			{
				fMODPopExhaust.PlayRevLimit();
			}
		}
	}

	protected void RevControl()
	{
		m_revLim = m_carx.engineRevLimiter;
		if (m_carx.rpm >= m_revLim - m_carXrevBias)
		{
			m_carx.rpm = m_revLim - m_carXrevBias;
			m_gateRPM = m_revLim - revBias;
			Play();
		}
		if (m_carx.rpm > m_gateRPM)
		{
			m_carx.accelerate = (smoothMode ? (m_carx.accelerate * (m_revLim - m_carx.rpm) / m_gateRPM) : 0f);
		}
		else
		{
			m_gateRPM = 15000f;
		}
	}
}
