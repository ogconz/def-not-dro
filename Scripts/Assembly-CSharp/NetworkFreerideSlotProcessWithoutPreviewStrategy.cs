using Cysharp.Threading.Tasks;

public class NetworkFreerideSlotProcessWithoutPreviewStrategy : NetworkFreerideSlotProcessStrategy
{
	protected override async UniTask UpdateTyreVisual(int adhLimit)
	{
		int activeSlotId = base.m_prefs.carSettings.GetActiveSlotId(GetCarId());
		await base.PreviewSlotForCarAsync(activeSlotId);
		await base.UpdateTyreVisual(adhLimit);
	}

	public override async UniTask PreviewSlotForCarAsync(int slotId)
	{
		int carId = GetCarId();
		RecordSlotForCar(carId, slotId);
		base.m_prefs.carSettings.SetActiveSlotId(carId, slotId);
	}
}
