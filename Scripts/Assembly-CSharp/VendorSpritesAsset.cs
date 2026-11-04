using System;
using System.Collections.Generic;
using UnityEngine;

public class VendorSpritesAsset : ScriptableObject
{
	[Serializable]
	public class VendorSpriteDesc
	{
		public string name;

		public int sortId;

		public Sprite sprite;
	}

	public List<VendorSpriteDesc> vendorsDB = new List<VendorSpriteDesc>();

	private Dictionary<string, VendorSpriteDesc> m_vendorDic = new Dictionary<string, VendorSpriteDesc>();

	public static VendorSpritesAsset LoadDB()
	{
		VendorSpritesAsset vendorSpritesAsset = ResourcesWrapper.Load<VendorSpritesAsset>("DB/VendorSpritesAsset");
		if (vendorSpritesAsset != null)
		{
			foreach (VendorSpriteDesc item in vendorSpritesAsset.vendorsDB)
			{
				vendorSpritesAsset.m_vendorDic[item.name] = item;
			}
		}
		return vendorSpritesAsset;
	}

	public VendorSpriteDesc Find(string vendorName)
	{
		if (!m_vendorDic.TryGetValue(vendorName, out var value))
		{
			value = vendorsDB.Find((VendorSpriteDesc x) => x.name == vendorName);
			m_vendorDic.Add(vendorName, value);
		}
		return value;
	}
}
