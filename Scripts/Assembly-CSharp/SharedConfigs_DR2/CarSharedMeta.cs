using System.Collections.Generic;
using CarModelSystem;
using UnityEngine;

namespace SharedConfigs_DR2
{
	public class CarSharedMeta : BaseBlockableItemSharedMeta
	{
		public bool enabled;

		public bool defaultLocked;

		public string name;

		public string icon;

		public string prefab;

		public string identifier;

		public int tier;

		public int sort;

		public int emptyBonnetId;

		public List<BodyKitItem> bodyKits;

		public List<BodyPartItem> bodyParts;

		public List<WheelPartItem> wheelParts;

		public Dictionary<TypeID, float> paintCategoryCostCoefficients;

		public Dictionary<CarPaintType, float> paintMaterialCostCoefficients;

		public CustomPaintItem customPaint;

		public List<PaintItem> bodyPaints;

		public List<PaintItem> wheelPaints;

		public List<PaintItem> glassPaints;

		public List<PaintItem> smokePaints;

		public List<PaintItem> suspensionPaints;

		private Dictionary<int, BodyPartItem> m_bodyPartsByIds;

		private Dictionary<int, Dictionary<TypeID, List<BodyPartItem>>> m_bodyPartsForBodyKits;

		private Dictionary<int, Dictionary<TypeID, BodyPartItem>> m_defaultBodyPartsForBodyKits;

		private Dictionary<int, WheelPartItem> m_wheelPartsByIds;

		private Dictionary<int, Dictionary<BodyKitWheelCategory, List<WheelPartItem>>> m_wheelPartsForBodyKits;

		private Dictionary<int, WheelPartItem> m_defaultWheelPartsForBodyKits;

		public CarSharedMeta()
		{
			paintCategoryCostCoefficients = new Dictionary<TypeID, float>();
			paintMaterialCostCoefficients = new Dictionary<CarPaintType, float>();
			bodyKits = new List<BodyKitItem>();
			bodyParts = new List<BodyPartItem>();
			wheelParts = new List<WheelPartItem>();
			bodyPaints = new List<PaintItem>();
			glassPaints = new List<PaintItem>();
			smokePaints = new List<PaintItem>();
			wheelPaints = new List<PaintItem>();
			customPaint = new CustomPaintItem();
			suspensionPaints = new List<PaintItem>();
			base.currencyType = CurrencyType.Coins;
		}

		public int CalculateCost(float discount)
		{
			return base.cost - Mathf.RoundToInt((float)base.cost * discount);
		}

		public BodyKitItem GetBodyKit(int id)
		{
			return bodyKits.Find((BodyKitItem meta) => meta.id == id);
		}

		public PaintItem GetBodyPaint(int id)
		{
			return bodyPaints.Find((PaintItem meta) => meta.id == id);
		}

		public PaintItem GetWheelPaint(int id)
		{
			return wheelPaints.Find((PaintItem meta) => meta.id == id);
		}

		public PaintItem GetGlassPaint(int id)
		{
			return glassPaints.Find((PaintItem meta) => meta.id == id);
		}

		public PaintItem GetSmokePaint(int id)
		{
			return smokePaints.Find((PaintItem meta) => meta.id == id);
		}

		public PaintItem GetSuspensionPaint(int id)
		{
			return suspensionPaints.Find((PaintItem meta) => meta.id == id);
		}

		public float GetPaintCategoryCostCoefficient(TypeID partType)
		{
			float value = 0f;
			if (paintCategoryCostCoefficients.TryGetValue(partType, out value))
			{
				return value;
			}
			return 1f;
		}

		public float GetPaintMaterialCostCoefficient(CarPaintType paintType)
		{
			float value = 0f;
			if (paintMaterialCostCoefficients.TryGetValue(paintType, out value))
			{
				return value;
			}
			return 1f;
		}

		public BodyPartItem GetBodyPart(int id)
		{
			m_bodyPartsByIds.TryGetValue(id, out var value);
			return value;
		}

		public List<BodyPartItem> GetBodyParts(int bodyKitId, TypeID partType)
		{
			Dictionary<TypeID, List<BodyPartItem>> bodyPartsByCategories = GetBodyPartsByCategories(bodyKitId);
			if (bodyPartsByCategories != null && bodyPartsByCategories.TryGetValue(partType, out var value))
			{
				return value;
			}
			return null;
		}

		public BodyPartItem GetDefaultBodyPart(int bodyKitId, TypeID partType)
		{
			if (m_defaultBodyPartsForBodyKits.TryGetValue(bodyKitId, out var value) && value.TryGetValue(partType, out var value2))
			{
				return value2;
			}
			return null;
		}

		public Dictionary<TypeID, List<BodyPartItem>> GetBodyPartsByCategories(int bodyKitId)
		{
			m_bodyPartsForBodyKits.TryGetValue(bodyKitId, out var value);
			return value;
		}

		public Dictionary<TypeID, BodyPartItem> GetDefaultBodyPartsByCategories(int bodyKitId)
		{
			m_defaultBodyPartsForBodyKits.TryGetValue(bodyKitId, out var value);
			return value;
		}

		public WheelPartItem GetWheelPart(int id)
		{
			m_wheelPartsByIds.TryGetValue(id, out var value);
			return value;
		}

		public WheelPartItem GetDefaultWheelPart(int bodyKitId)
		{
			m_defaultWheelPartsForBodyKits.TryGetValue(bodyKitId, out var value);
			return value;
		}

		public Dictionary<BodyKitWheelCategory, List<WheelPartItem>> GetWheelPartsByCategories(int bodyKitId)
		{
			m_wheelPartsForBodyKits.TryGetValue(bodyKitId, out var value);
			return value;
		}

		private void FillBodyPartsForBodyKits()
		{
			m_bodyPartsByIds = new Dictionary<int, BodyPartItem>(bodyParts.Count);
			m_bodyPartsForBodyKits = new Dictionary<int, Dictionary<TypeID, List<BodyPartItem>>>(bodyKits.Count);
			m_defaultBodyPartsForBodyKits = new Dictionary<int, Dictionary<TypeID, BodyPartItem>>(bodyKits.Count);
			bodyParts.ForEach(delegate(BodyPartItem meta)
			{
				m_bodyPartsByIds[meta.id] = meta;
			});
			for (int num = 0; num < bodyKits.Count; num++)
			{
				BodyKitItem bodyKitItem = bodyKits[num];
				Dictionary<TypeID, List<BodyPartItem>> dictionary = new Dictionary<TypeID, List<BodyPartItem>>();
				Dictionary<TypeID, BodyPartItem> dictionary2 = new Dictionary<TypeID, BodyPartItem>();
				for (int num2 = 0; num2 < bodyKitItem.bodyParts.Count; num2++)
				{
					BodyKitPartItem bodyKitPartItem = bodyKitItem.bodyParts[num2];
					BodyPartItem bodyPart = GetBodyPart(bodyKitPartItem.id);
					if (bodyPart != null)
					{
						TypeID partType = bodyPart.partType;
						if (bodyKitItem.IsDefaultBodyPart(bodyKitPartItem.id))
						{
							dictionary2[partType] = bodyPart;
						}
						if (!dictionary.TryGetValue(partType, out var value))
						{
							value = new List<BodyPartItem>();
							dictionary.Add(partType, value);
						}
						value.Add(bodyPart);
					}
				}
				m_bodyPartsForBodyKits[bodyKitItem.id] = dictionary;
				m_defaultBodyPartsForBodyKits[bodyKitItem.id] = dictionary2;
			}
		}

		private void FillWheelPartsForBodyKits()
		{
			m_wheelPartsByIds = new Dictionary<int, WheelPartItem>(wheelPaints.Count);
			m_wheelPartsForBodyKits = new Dictionary<int, Dictionary<BodyKitWheelCategory, List<WheelPartItem>>>(bodyKits.Count);
			m_defaultWheelPartsForBodyKits = new Dictionary<int, WheelPartItem>();
			wheelParts.ForEach(delegate(WheelPartItem meta)
			{
				m_wheelPartsByIds[meta.id] = meta;
			});
			for (int num = 0; num < bodyKits.Count; num++)
			{
				BodyKitItem bodyKitItem = bodyKits[num];
				Dictionary<BodyKitWheelCategory, List<WheelPartItem>> dictionary = new Dictionary<BodyKitWheelCategory, List<WheelPartItem>>();
				for (int num2 = 0; num2 < bodyKitItem.wheelCategories.Count; num2++)
				{
					BodyKitWheelCategory bodyKitWheelCategory = bodyKitItem.wheelCategories[num2];
					for (int num3 = 0; num3 < bodyKitWheelCategory.wheelParts.Count; num3++)
					{
						BodyKitPartItem bodyKitPartItem = bodyKitWheelCategory.wheelParts[num3];
						WheelPartItem wheelPart = GetWheelPart(bodyKitPartItem.id);
						if (wheelPart != null)
						{
							if (bodyKitItem.IsDefaultWheelPart(bodyKitPartItem.id, out var _))
							{
								m_defaultWheelPartsForBodyKits[bodyKitItem.id] = wheelPart;
							}
							if (!dictionary.TryGetValue(bodyKitWheelCategory, out var value))
							{
								value = new List<WheelPartItem>();
								dictionary.Add(bodyKitWheelCategory, value);
							}
							value.Add(wheelPart);
						}
					}
				}
				m_wheelPartsForBodyKits[bodyKitItem.id] = dictionary;
			}
		}

		public void PostDeserialize()
		{
			FillBodyPartsForBodyKits();
			FillWheelPartsForBodyKits();
		}
	}
}
