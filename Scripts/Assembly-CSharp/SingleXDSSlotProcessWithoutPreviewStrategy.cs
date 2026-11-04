using Cysharp.Threading.Tasks;

public class SingleXDSSlotProcessWithoutPreviewStrategy : SingleXDSSlotProcessStrategy
{
	public override async UniTask ConfirmChangesAsync()
	{
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		await base.PreviewSlotForCarAsync(activeSlotId);
		await base.ConfirmChangesAsync();
	}

	public override async UniTask PreviewSlotForCarAsync(int slotId)
	{
		int carId = GetCarId();
		RecordSlotForCar(carId, slotId);
		base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
	}
}
