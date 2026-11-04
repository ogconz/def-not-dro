using DI;
using UnityEngine;

public abstract class CarInteriorPart : MonoBehaviour
{
	public enum CarPart
	{
		DrivingWheel = 0,
		Speedometer = 1,
		Tachometer = 2,
		Gear = 3,
		Turbo = 4
	}

	public CarPart part;

	protected const string FORMAT_N0 = "N0";

	protected const string FORMAT_TEXT = "0.00";

	protected ICarCharacteristic m_characteristic;

	protected GamePrefs m_prefs;

	private RaceCar m_raceCar;

	private void Awake()
	{
		m_raceCar = GetComponentInParent<RaceCar>();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		if (!m_raceCar)
		{
			base.enabled = false;
			return;
		}
		Init();
		if (m_characteristic == null)
		{
			base.enabled = false;
			return;
		}
		m_characteristic.Init(m_raceCar);
		UpdateValue();
	}

	private void Start()
	{
	}

	protected abstract void Init();

	protected abstract void UpdateValue();

	private void Update()
	{
		UpdateValue();
	}

	protected virtual void OnDestroy()
	{
		m_characteristic?.Destroy();
	}
}
