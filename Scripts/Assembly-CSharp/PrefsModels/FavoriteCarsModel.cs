using System.Collections.Generic;
using SimpleJSON;

namespace PrefsModels
{
	public class FavoriteCarsModel : BasePrefsModel
	{
		[JsonSerialize]
		private List<int> m_cars = new List<int>();

		[JsonIgnore]
		private FavoriteCarsModelDefaultConfig defaultConfig;

		[JsonIgnore]
		public IReadOnlyList<int> cars => m_cars;

		public void AddCar(int carId)
		{
			if (!m_cars.Contains(carId))
			{
				m_cars.Add(carId);
				Save();
			}
		}

		public void RemoveCar(int carId)
		{
			int num = m_cars.FindIndex((int c) => c == carId);
			if (num >= 0 && num < m_cars.Count)
			{
				m_cars.RemoveAt(num);
				Save();
			}
		}

		public bool IsFavorite(int carId)
		{
			return m_cars.Contains(carId);
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (reserveModel is FavoriteCarsModel favoriteCarsModel)
			{
				return m_cars == favoriteCarsModel.m_cars;
			}
			return true;
		}

		private void SetDefaults()
		{
			if (defaultConfig == null)
			{
				defaultConfig = FavoriteCarsModelDefaultConfig.LoadAndInitDB();
			}
			m_cars = new List<int>(defaultConfig.favoriteCarsIds);
		}

		public void ClearAll()
		{
			m_cars.Clear();
			SetDefaults();
			Save();
		}
	}
}
