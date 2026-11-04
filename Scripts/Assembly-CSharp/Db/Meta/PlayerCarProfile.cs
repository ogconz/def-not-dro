using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using CarProfile;
using CodeStage.AntiCheat.ObscuredTypes;
using Db.Meta.CarProfile;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class PlayerCarProfile : IDBMeta, IDBCanCost
	{
		[Serializable]
		public class DynoInfo
		{
			[SerializeField]
			private List<CarProfileController> m_controllers;

			public List<CarProfileController> controllers
			{
				get
				{
					return m_controllers;
				}
				set
				{
					m_controllers = value;
				}
			}

			public DynoInfo()
			{
				controllers = new List<CarProfileController>();
			}

			public CarProfileChanger GetChanger(CarProfileControllerType controllerType, CarProfileParameterType parameterType)
			{
				int parameterId = (int)parameterType;
				for (int i = 0; i < m_controllers.Count; i++)
				{
					CarProfileController carProfileController = m_controllers[i];
					if (carProfileController.type == controllerType)
					{
						CarProfileChanger carProfileChanger = carProfileController.changers.Find((CarProfileChanger c) => c.parameterId == parameterId);
						if (carProfileChanger != null)
						{
							return carProfileChanger;
						}
					}
				}
				return null;
			}

			public DynoInfo Copy()
			{
				DynoInfo dynoInfo = new DynoInfo();
				dynoInfo.controllers = new List<CarProfileController>(controllers?.Count ?? 0);
				if (controllers != null)
				{
					foreach (CarProfileController controller in controllers)
					{
						dynoInfo.controllers.Add(controller.Copy());
					}
				}
				return dynoInfo;
			}
		}

		public const string StockName = "Stock";

		public const string TurboName = "Turbo";

		public const string RacingName = "Racing";

		public const string DriftName = "Drift";

		public const string UltimateName = "Ultimate";

		public const string UltimateProName = "UltimatePro";

		[SerializeField]
		private int m_id;

		[SerializeField]
		private int m_carId;

		[SerializeField]
		private string m_name;

		[SerializeField]
		private string m_icon;

		[SerializeField]
		private string m_asset;

		[SerializeField]
		private string m_identifier;

		[SerializeField]
		private ObscuredInt m_cost;

		[SerializeField]
		private bool m_defaultLocked;

		[SerializeField]
		private int m_sort;

		[SerializeField]
		private DynoInfo m_dynoInfo;

		[SerializeField]
		private int m_engineId;

		[XmlAttribute]
		public int id
		{
			get
			{
				return m_id;
			}
			set
			{
				m_id = value;
			}
		}

		[XmlAttribute]
		public int carId
		{
			get
			{
				return m_carId;
			}
			set
			{
				m_carId = value;
			}
		}

		[XmlAttribute]
		public string name
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}

		[XmlAttribute]
		[DefaultValue("")]
		public string icon
		{
			get
			{
				return m_icon;
			}
			set
			{
				m_icon = value;
			}
		}

		[XmlAttribute]
		public string asset
		{
			get
			{
				return m_asset;
			}
			set
			{
				m_asset = value;
			}
		}

		[XmlAttribute]
		public string identifier
		{
			get
			{
				return m_identifier;
			}
			set
			{
				m_identifier = value;
			}
		}

		public DynoInfo dynoInfo
		{
			get
			{
				return m_dynoInfo;
			}
			set
			{
				m_dynoInfo = value;
			}
		}

		[XmlAttribute]
		public int cost
		{
			get
			{
				return m_cost;
			}
			set
			{
				m_cost = value;
			}
		}

		[XmlAttribute]
		public bool defaultLocked
		{
			get
			{
				return m_defaultLocked;
			}
			set
			{
				m_defaultLocked = value;
			}
		}

		[XmlAttribute]
		public int sort
		{
			get
			{
				return m_sort;
			}
			set
			{
				m_sort = value;
			}
		}

		public PlayerCarProfile()
		{
			dynoInfo = new DynoInfo();
		}

		public PlayerCarProfile Copy()
		{
			return new PlayerCarProfile
			{
				m_id = m_id,
				m_carId = m_carId,
				m_name = m_name,
				m_icon = m_icon,
				m_asset = m_asset,
				m_identifier = m_identifier,
				m_cost = m_cost,
				m_defaultLocked = m_defaultLocked,
				m_sort = m_sort,
				m_engineId = m_engineId,
				m_dynoInfo = m_dynoInfo.Copy()
			};
		}
	}
}
