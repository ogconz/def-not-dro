using System.Collections.Generic;
using DB;
using DB.Meta;
using UnityEngine;

public class CarItemsPool
{
	private const string PREFAB_PATH = "Prefabs/UI/Contexts/UICarSelectorCarItem";

	private readonly Dictionary<int, UICarSelectorCar> m_carItems = new Dictionary<int, UICarSelectorCar>();

	private readonly List<PlayerCar> m_cars = new List<PlayerCar>();

	private GameObject m_carsPoolObject;

	public IReadOnlyDictionary<int, UICarSelectorCar> carItems => m_carItems;

	public IReadOnlyCollection<PlayerCar> cars => m_cars;

	public void Initialize(BaseModel model)
	{
		if (m_carsPoolObject != null)
		{
			return;
		}
		GameObject gameObject = ResourcesWrapper.Load<GameObject>("Prefabs/UI/Contexts/UICarSelectorCarItem");
		if (gameObject == null)
		{
			GameLog.LogError("[CarItemsPool]: Unable to load carItem prefab");
			return;
		}
		m_carsPoolObject = new GameObject("CarUIItemsPool");
		m_carsPoolObject.ResetPRS();
		Object.DontDestroyOnLoad(m_carsPoolObject);
		m_cars.Clear();
		m_cars.AddRange(model.QueryAll<PlayerCar>());
		foreach (PlayerCar car in m_cars)
		{
			GameObject gameObject2 = Object.Instantiate(gameObject, m_carsPoolObject.transform);
			UICarSelectorCar component = gameObject2.GetComponent<UICarSelectorCar>();
			component.SetMeta(car);
			gameObject2.SetActive(value: false);
			m_carItems.Add(car.id, component);
		}
	}

	public UICarSelectorCar Get(int carId)
	{
		carItems.TryGetValue(carId, out var value);
		if (value != null && value.gameObject != null)
		{
			value.gameObject.SetActive(value: true);
			value.ResetPRS();
			return value;
		}
		return null;
	}

	public void ReturnToPool(UICarSelectorCar carItem)
	{
		carItem.gameObject.SetActive(value: false);
		if (m_carsPoolObject != null)
		{
			carItem.transform.SetParent(m_carsPoolObject.transform);
		}
	}
}
