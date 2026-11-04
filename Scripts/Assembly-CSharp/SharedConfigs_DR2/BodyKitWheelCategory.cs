using System.Collections.Generic;

namespace SharedConfigs_DR2
{
	public class BodyKitWheelCategory
	{
		public string categoryKey;

		public List<BodyKitPartItem> wheelParts;

		public BodyKitWheelCategory()
		{
			wheelParts = new List<BodyKitPartItem>();
		}

		public int GetWheelPartSort(int id)
		{
			return wheelParts.Find((BodyKitPartItem part) => part.id == id)?.sort ?? 0;
		}
	}
}
