using SimpleJSON;

namespace Inventory
{
	public class InventoryItem
	{
		[JsonSerialize]
		public string id;

		[JsonSerialize]
		public int count;

		[JsonSerialize]
		public bool permanent;
	}
}
