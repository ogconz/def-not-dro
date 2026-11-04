using ArrayExtension;
using CarModelSystem;
using CustomizationDR2.Enums;
using SimpleJSON;

namespace SharedConfigs_DR2
{
	public class BodyPartItem : BasePartItem
	{
		public string name;

		public string description;

		public string icon;

		public string prefab;

		public TypeID partType;

		public bool paintable;

		public int[] linkedParts = new int[0];

		public int[] exceptedParts = new int[0];

		[JsonIgnore]
		public bool hasLinkedParts
		{
			get
			{
				if (linkedParts != null)
				{
					return linkedParts.Length != 0;
				}
				return false;
			}
		}

		[JsonIgnore]
		public bool hasExceptedParts
		{
			get
			{
				if (exceptedParts != null)
				{
					return exceptedParts.Length != 0;
				}
				return false;
			}
		}

		public BodyPartItem()
		{
		}

		public BodyPartItem(int id, int cost, int installationCost, CurrencyType currencyType, bool defaultLocked, string name, string description, string icon, string prefab, TypeID partType, bool paintable)
			: base(installationCost, defaultLocked)
		{
			base.id = id;
			base.cost = cost;
			base.currencyType = currencyType;
			this.name = name;
			this.icon = icon;
			this.prefab = prefab;
			this.partType = partType;
			this.paintable = paintable;
		}

		public static bool PaintableCategory(TypeID partType)
		{
			if (partType != BodyPartTypeIDs.LightsFront && partType != BodyPartTypeIDs.LightsRear)
			{
				return partType != BodyPartTypeIDs.Exhaust;
			}
			return false;
		}

		public bool IsLinkedPart(int id)
		{
			if (linkedParts != null)
			{
				return linkedParts.Contains(id);
			}
			return false;
		}

		public bool IsExceptedPart(int id)
		{
			if (exceptedParts != null)
			{
				return exceptedParts.Contains(id);
			}
			return false;
		}
	}
}
