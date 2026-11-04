using System.Collections.Generic;
using CustomizationDR2.Enums;
using UnityEngine;

namespace SharedConfigs_DR2
{
	public class BodyKitItem : BaseBlockableItem
	{
		public List<BodyKitPartItem> bodyParts;

		public List<BodyKitWheelCategory> wheelCategories;

		public List<int> defaultBodyParts;

		public Dictionary<CarWheelAxles, int> defaultWheelParts;

		public string name;

		public string description;

		public string icon;

		public float frontAxleWidth;

		public float rearAxleWidth;

		public float baseVerticalOffset;

		public int sort;

		public int installationCost;

		public bool defaultLocked;

		public CurrencyType installationCurrencyType;

		public BodyKitItem()
		{
			bodyParts = new List<BodyKitPartItem>();
			wheelCategories = new List<BodyKitWheelCategory>();
			defaultBodyParts = new List<int>();
			defaultWheelParts = new Dictionary<CarWheelAxles, int>();
			base.currencyType = CurrencyType.Coins;
			installationCurrencyType = CurrencyType.Coins;
		}

		public BodyKitItem(int id, int cost, int installationCost, CurrencyType currencyType, CurrencyType installationCurrencyType, bool defaultLocked, string name, string description, string icon, float frontAxleWidth, float rearAxleWidth, float baseVerticalOffset, int sort)
			: this()
		{
			base.id = id;
			base.cost = cost;
			this.installationCost = installationCost;
			base.currencyType = currencyType;
			this.installationCurrencyType = installationCurrencyType;
			this.defaultLocked = defaultLocked;
			this.name = name;
			this.description = description;
			this.icon = icon;
			this.frontAxleWidth = frontAxleWidth;
			this.rearAxleWidth = rearAxleWidth;
			this.baseVerticalOffset = baseVerticalOffset;
			this.sort = sort;
		}

		public List<BodyKitPartItem> GetDefaultBodyParts()
		{
			return bodyParts.FindAll((BodyKitPartItem part) => IsDefaultBodyPart(part.id));
		}

		public int GetBodyPartSort(int id)
		{
			return bodyParts.Find((BodyKitPartItem part) => part.id == id)?.sort ?? 0;
		}

		public bool IsDefaultBodyPart(int paramId)
		{
			return defaultBodyParts.Contains(paramId);
		}

		public bool IsDefaultWheelPart(int id)
		{
			foreach (KeyValuePair<CarWheelAxles, int> defaultWheelPart in defaultWheelParts)
			{
				if (defaultWheelPart.Value == id)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsDefaultWheelPart(int paramId, out List<CarWheelAxles> axles)
		{
			axles = new List<CarWheelAxles>();
			foreach (KeyValuePair<CarWheelAxles, int> defaultWheelPart in defaultWheelParts)
			{
				if (defaultWheelPart.Value == paramId)
				{
					axles.Add(defaultWheelPart.Key);
				}
			}
			return axles.Count > 0;
		}

		public bool IsDefaultWheelPart(int paramId, CarWheelAxles axle)
		{
			if (defaultWheelParts.TryGetValue(axle, out var value))
			{
				return value == paramId;
			}
			return false;
		}

		public bool TryGetDefaultWheelParts(out BodyKitPartItem frontWheelPart, out BodyKitPartItem rearWheelPart)
		{
			frontWheelPart = null;
			rearWheelPart = null;
			for (int i = 0; i < wheelCategories.Count; i++)
			{
				BodyKitWheelCategory bodyKitWheelCategory = wheelCategories[i];
				for (int j = 0; j < bodyKitWheelCategory.wheelParts.Count; j++)
				{
					BodyKitPartItem bodyKitPartItem = bodyKitWheelCategory.wheelParts[j];
					if (IsDefaultWheelPart(bodyKitPartItem.id, CarWheelAxles.Front))
					{
						frontWheelPart = bodyKitPartItem;
					}
					if (IsDefaultWheelPart(bodyKitPartItem.id, CarWheelAxles.Rear))
					{
						rearWheelPart = bodyKitPartItem;
					}
				}
			}
			if (frontWheelPart != null)
			{
				return rearWheelPart != null;
			}
			return false;
		}

		public static int GetNextSort(List<BodyKitPartItem> bodyKitParts)
		{
			int maxSort = -1;
			bodyKitParts.ForEach(delegate(BodyKitPartItem part)
			{
				maxSort = Mathf.Max(maxSort, part.sort);
			});
			return maxSort + 1;
		}
	}
}
