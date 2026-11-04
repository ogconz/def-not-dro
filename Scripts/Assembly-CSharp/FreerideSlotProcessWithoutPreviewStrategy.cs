using Cysharp.Threading.Tasks;

public class FreerideSlotProcessWithoutPreviewStrategy : FreerideSlotProcessStrategy
{
	public override async UniTask ConfirmChangesAsync()
	{
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		await base.PreviewSlotForCarAsync(activeSlotId);
		if (States.current is InGameMenuState)
		{
			States.Pop();
		}
	}

	public override async UniTask PreviewSlotForCarAsync(int slotId)
	{
		int carId = GetCarId();
		RecordSlotForCar(carId, slotId);
		base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
	}
}
