using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatalogCarMarkers", menuName = "Other/CatalogCarMarkers")]
public class CatalogCarMarkers : ScriptableObject
{
	private enum UpdateCarType
	{
		None = -1,
		NewCar = 0,
		RebuiltCar = 1,
		RebuiltBodykit = 2
	}

	[Header("Новые машины")]
	[SerializeField]
	private int[] m_newCarIds;

	[Header("Переделанные машины")]
	[SerializeField]
	private int[] m_rebuiltCarIds;

	[Header("Переделанные бодикиты")]
	[SerializeField]
	private int[] m_rebuiltBodykits;

	private List<(UpdateCarType, int[])> m_listOfArrays = new List<(UpdateCarType, int[])>();

	public int GetUpdateCarType(int carId)
	{
		if (IsCarIntersects(carId, out var updateCarType))
		{
			Debug.LogError($"CarId {carId} intersects with other update types!! Please check CatalogCarMarkers.asset");
		}
		return (int)updateCarType;
	}

	private bool IsCarIntersects(int carId, out UpdateCarType updateCarType)
	{
		updateCarType = UpdateCarType.None;
		InflateListIfNeed();
		int num = 0;
		foreach (var listOfArray in m_listOfArrays)
		{
			int[] item = listOfArray.Item2;
			if (item == null)
			{
				continue;
			}
			for (int i = 0; i < item.Length; i++)
			{
				if (carId == item[i])
				{
					num++;
					(updateCarType, _) = listOfArray;
					break;
				}
			}
		}
		return num > 1;
	}

	private void InflateListIfNeed()
	{
		if (m_listOfArrays.Count == 0)
		{
			m_listOfArrays.Add((UpdateCarType.NewCar, m_newCarIds));
			m_listOfArrays.Add((UpdateCarType.RebuiltCar, m_rebuiltCarIds));
			m_listOfArrays.Add((UpdateCarType.RebuiltBodykit, m_rebuiltBodykits));
		}
	}
}
