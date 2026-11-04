using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using CarModelSystem;
using DB;
using DB.Meta;
using DI;
using SharedConfigs_DR2;
using SimpleJSON;
using UnityEngine;

namespace Customization
{
	public static class CarPartDR2Importer
	{
		[Serializable]
		public class DR1_Output
		{
			[XmlAttribute]
			public int carId;

			[XmlAttribute]
			public int emptyBonnetId;

			[XmlAttribute]
			public string path;

			[XmlElement(ElementName = "type")]
			public List<DR1_OutputPart> parts;
		}

		[Serializable]
		public class DR1_OutputPart
		{
			public BodyKitItem meta;

			public int sort;

			[XmlAttribute]
			public int id;

			[XmlAttribute]
			[DefaultValue("")]
			public string prefab;

			[XmlAttribute]
			[DefaultValue("")]
			public string type;

			[XmlAttribute]
			[DefaultValue("")]
			public string force;

			[XmlAttribute]
			[DefaultValue("")]
			public string model0;

			[XmlAttribute]
			[DefaultValue("")]
			public string enable;

			[XmlAttribute]
			[DefaultValue("")]
			public string disable;

			[XmlAttribute]
			[DefaultValue("")]
			public string take;

			[XmlAttribute]
			[DefaultValue("")]
			public string include;

			[XmlAttribute]
			[DefaultValue("")]
			public string exclude;
		}

		private const int PARTS_ID_SHIFT = 20;

		private static Dictionary<string, string> m_fileNames = new Dictionary<string, string>();

		private static BaseModel m_db;

		private static List<PlayerCar> m_carDB;

		private static HashSet<string> m_fullList = new HashSet<string>
		{
			"Bonnet", "BumperFront", "BumperRear", "Cage", "Doors", "Exhaust", "LightsFront", "LightsRear", "Mirrors", "Roof",
			"Skirts", "Spoiler", "Trunk", "Wing", "SeatLeft", "SeatRight", "SteeringWheel", "Handbrake", "Shifter"
		};

		public static void Init()
		{
			GameConsole.SetCmd("db2_customization_convert", Convert);
			m_db = DependencyInjector.Resolve<BaseModel>();
			m_carDB = m_db.QueryAll<PlayerCar>();
			TextAsset[] array = Resources.LoadAll<TextAsset>("DB/Parts/");
			foreach (TextAsset textAsset in array)
			{
				string[] array2 = textAsset.name.Split(new char[1] { '_' }, 2);
				if (array2.Length > 1)
				{
					m_fileNames[array2[1].ToLower()] = textAsset.name;
				}
			}
		}

		public static void Convert()
		{
			List<CarSharedMeta> list = new List<CarSharedMeta>();
			TextAsset[] array = Resources.LoadAll<TextAsset>("DB/Customization DR2");
			for (int i = 0; i < array.Length; i++)
			{
				CarSharedMeta carSharedMeta = JSON.Parse<CarSharedMeta>(array[i].text, skipErrors: true);
				if (carSharedMeta == null || carSharedMeta.bodyKits.Count <= 0)
				{
					continue;
				}
				foreach (SharedConfigs_DR2.BodyPartItem bodyPart in carSharedMeta.bodyParts)
				{
					if (bodyPart.partType.Equals(default(TypeID)))
					{
						TypeID partType = new TypeID("Chassis");
						bodyPart.partType = partType;
					}
				}
				list.Add(carSharedMeta);
			}
			foreach (CarSharedMeta item in list)
			{
				Convert(item);
			}
		}

		public static void Convert(CarSharedMeta meta)
		{
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Initialize()
		{
			GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
			{
				Init();
			});
		}
	}
}
