using System;
using System.Collections.Generic;

namespace LocalCaching.Model
{
	[Serializable]
	public class CarProfileCacheContainer
	{
		public int version = 1;

		public List<CarProfileCache> carProfile = new List<CarProfileCache>();

		public CarProfileCacheContainer()
		{
		}

		public CarProfileCacheContainer(int ver, List<CarProfileCache> cache)
		{
			version = ver;
			carProfile = cache;
		}

		public Dictionary<int, CarProfileCache> GetAsDictionary()
		{
			Dictionary<int, CarProfileCache> dictionary = new Dictionary<int, CarProfileCache>();
			foreach (CarProfileCache item in carProfile)
			{
				dictionary.Add(item.id, item);
			}
			return dictionary;
		}
	}
}
