using Cysharp.Threading.Tasks;

public class SingleXDSSlotProcessStrategy : FreerideSlotProcessStrategy
{
	protected override void OnActivate()
	{
		m_raceState = States.mainCurrent as BaseRaceState;
		m_ghostRecorder = m_raceState.ghostRecorder;
		m_carSlotsContext = m_raceState.contextOwner.Get<UICarSlotsContext>();
		SaveOriginalCar();
	}

	protected override void OnDeactivate()
	{
	}

	public override async UniTask ConfirmChangesAsync()
	{
		if (IsCarLoaded())
		{
			m_carSlotsContext.Hide();
			m_raceState.contextOwner.Hide<UICarSelectorContext>();
			m_raceState.contextOwner.Hide<UIXDSPreCombatContext>();
			m_raceState.contextOwner.Get<UIXDSPreCombatContext>().onCarSelected.SafeCall(GetCarId());
		}
	}

	public override async UniTask DiscardChangesAsync()
	{
		await RevertChangesAndRestoreCarAsync();
		m_carSlotsContext.Hide();
	}
}
