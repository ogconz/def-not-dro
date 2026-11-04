using System;

public class SharingFilterController
{
	private GamePrefs m_prefs;

	public ISharingFilter sharingFilter { get; private set; }

	public SharingFilterController()
	{
	}

	public SharingFilterController(GamePrefs prefs)
	{
		m_prefs = prefs;
	}

	public void InitFilter<T>() where T : ISharingFilter
	{
		sharingFilter = m_prefs.globals.GetSharingFilter<T>();
		if (sharingFilter == null)
		{
			sharingFilter = Activator.CreateInstance<T>();
			ResetFilterToDefault();
		}
		sharingFilter.carId = m_prefs.globals.lastCarId;
	}

	public void ResetFilterToDefault()
	{
		sharingFilter = sharingFilter.GetDefault();
	}

	public void ResetFilterTo(ISharingFilter sharingFilter)
	{
		this.sharingFilter = sharingFilter;
	}

	public bool IsActiveFilter()
	{
		return !sharingFilter.Equals(sharingFilter.GetDefault());
	}

	public void SaveFilter<T>()
	{
		m_prefs.globals.SaveSharingFilter<T>(sharingFilter);
	}

	public void SetFilterValue<K, T>(K filterType, T val)
	{
		sharingFilter.SetFilterValue(filterType, val);
	}

	public T GetFilterValue<K, T>(K filterType)
	{
		return sharingFilter.GetFilterValue<K, T>(filterType);
	}
}
