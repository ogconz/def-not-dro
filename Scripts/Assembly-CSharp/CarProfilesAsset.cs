using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CarProfilesAsset : ScriptableObject
{
	[Serializable]
	public class NamedDesc
	{
		public string name;

		public BaseCar.Desc desc;
	}

	public List<NamedDesc> profilesDB = new List<NamedDesc>();

	public static CarProfilesAsset LoadDB()
	{
		return ResourcesWrapper.Load<CarProfilesAsset>("DB/CarProfilesAsset");
	}

	public BaseCar.Desc Find(string descName)
	{
		for (int i = 0; i < profilesDB.Count; i++)
		{
			NamedDesc namedDesc = profilesDB[i];
			if (namedDesc.name == descName)
			{
				return namedDesc.desc;
			}
		}
		return null;
	}
}
