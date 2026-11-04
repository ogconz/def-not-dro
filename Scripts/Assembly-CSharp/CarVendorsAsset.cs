using System.Collections.Generic;
using UnityEngine;

public class CarVendorsAsset : ScriptableObject
{
	private const string DbPath = "DB/CarVendorsAsset";

	[SerializeField]
	private List<CarVendor> m_vendors = new List<CarVendor>();

	private readonly Dictionary<int, CarVendor> m_vendorsDictionary = new Dictionary<int, CarVendor>();

	private static CarVendorsAsset m_db;

	public IReadOnlyCollection<CarVendor> Vendors => m_vendors;

	public static CarVendorsAsset LoadAndInitDB()
	{
		if (m_db == null)
		{
			m_db = ResourcesWrapper.Load<CarVendorsAsset>("DB/CarVendorsAsset");
		}
		if (m_db == null)
		{
			GameLog.LogError("Unable to load car vendors, DB is null");
			return null;
		}
		foreach (CarVendor vendor in m_db.m_vendors)
		{
			m_db.m_vendorsDictionary[vendor.id] = vendor;
		}
		return m_db;
	}

	public CarVendor Find(int id)
	{
		if (!m_vendorsDictionary.TryGetValue(id, out var value))
		{
			value = m_vendors.Find((CarVendor x) => x.id == id);
			m_vendorsDictionary.Add(id, value);
		}
		return value;
	}
}
