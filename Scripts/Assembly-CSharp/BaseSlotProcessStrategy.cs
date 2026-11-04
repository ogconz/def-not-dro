using Cysharp.Threading.Tasks;
using DB;
using DI;

public abstract class BaseSlotProcessStrategy
{
	protected const int SwapCarTimeoutMs = 20000;

	protected const int WaitLoadTimeoutMs = 10000;

	private GamePrefs m_prefsInternal;

	private BaseModel m_baseModelInternal;

	private PlayerCarFactory m_carFactoryInternal;

	private CarVisualManager m_visualManagerInternal;

	private bool m_isInitialized;

	private int m_carId;

	protected GamePrefs m_prefs
	{
		get
		{
			if (!(m_prefsInternal != null))
			{
				return m_prefsInternal = DependencyInjector.Resolve<GamePrefs>();
			}
			return m_prefsInternal;
		}
	}

	protected BaseModel m_baseModel => m_baseModelInternal ?? (m_baseModelInternal = DependencyInjector.Resolve<BaseModel>());

	protected PlayerCarFactory m_carFactory => m_carFactoryInternal ?? (m_carFactoryInternal = DependencyInjector.Resolve<PlayerCarFactory>());

	protected CarVisualManager m_visualManager => m_visualManagerInternal ?? (m_visualManagerInternal = DependencyInjector.Resolve<CarVisualManager>());

	public int GetCarId()
	{
		return m_carId;
	}

	public void SetCarId(int carId)
	{
		m_carId = carId;
	}

	public virtual RaceCar GetRaceCar()
	{
		return null;
	}

	public virtual bool IsCarLoaded()
	{
		return false;
	}

	public void Activate()
	{
		if (!m_isInitialized)
		{
			OnActivate();
			m_isInitialized = true;
		}
	}

	public void Deactivate()
	{
		if (m_isInitialized)
		{
			OnDeactivate();
			m_isInitialized = false;
		}
	}

	public void Update()
	{
		OnUpdate();
	}

	protected virtual void OnActivate()
	{
	}

	protected virtual void OnDeactivate()
	{
	}

	protected virtual void OnUpdate()
	{
	}

	public virtual async UniTask ConfirmChangesAsync()
	{
	}

	public virtual async UniTask DiscardChangesAsync()
	{
	}

	public virtual async UniTask PreviewSlotForCarAsync(int slotId)
	{
	}
}
