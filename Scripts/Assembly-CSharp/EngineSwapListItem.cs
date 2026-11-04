using DI;
using Inventory;

public class EngineSwapListItem : BaseListItem
{
	private static GamePrefs m_prefs;

	public EngineInfo info { get; private set; }

	public int carId { get; private set; }

	public bool isStock => info.id == 0;

	public EngineSwapListItem(EngineInfo info, int carId)
	{
		this.carId = carId;
		this.info = info;
	}

	static EngineSwapListItem()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
	}

	public int GetCollectedPartsCount()
	{
		return m_prefs.inventory.GetItem(info.partItem)?.count ?? 0;
	}

	public bool IsCollectedPartsCountEnough()
	{
		return GetCollectedPartsCount() >= info.partsToInstall;
	}

	public bool HasInInventory()
	{
		string engineIdFromCar = info.GetEngineIdFromCar(carId);
		InventoryItem item = m_prefs.inventory.GetItem(engineIdFromCar);
		if (item != null)
		{
			return item.count > 0;
		}
		return false;
	}

	public bool IsAbleToInstall()
	{
		if (!isStock && !GameManager.SwapsDLCInstalled && !IsCollectedPartsCountEnough())
		{
			return HasInInventory();
		}
		return true;
	}

	public bool IsInstalled()
	{
		return m_prefs.carSettings.GetEngineIdForCar(carId) == info.id;
	}
}
