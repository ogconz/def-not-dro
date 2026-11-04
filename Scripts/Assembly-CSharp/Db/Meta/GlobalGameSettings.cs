using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using DB.XML;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class GlobalGameSettings
	{
		[Serializable]
		public class ExpSettings
		{
			public int raceStart;

			public int raceEnd;

			public int bronze;

			public int silver;

			public int gold;

			public float estimatePerSecond;

			public int mp_raceStart;

			public int mp_raceEnd;

			public float elapsedPerSecond;

			[SerializeField]
			private List<int> m_mp_challengeList = new List<int>();

			[XmlElement("mp_challenge")]
			public string xmlMp_challenge
			{
				get
				{
					return XmlTools.ListIntToString(m_mp_challengeList);
				}
				set
				{
					m_mp_challengeList = XmlTools.StringToListInt(value);
				}
			}

			[XmlIgnore]
			public ReadOnlyCollection<int> mp_challengeList => m_mp_challengeList.AsReadOnly();
		}

		public string modName = "";

		[SerializeField]
		private ObscuredInt m_startCoins;

		[SerializeField]
		private ObscuredInt m_startRatingPoints;

		[SerializeField]
		private ObscuredInt m_tutorial1pack7coins;

		[SerializeField]
		private List<int> m_expTableList = new List<int>();

		public ExpSettings expSettings = new ExpSettings();

		[SerializeField]
		private int m_goldMoney;

		[SerializeField]
		private float m_goldMFactor;

		[SerializeField]
		private float m_goldEFactor;

		[SerializeField]
		private int m_pageLikeCoinsCost;

		[SerializeField]
		private int m_subscribeCoinsCost;

		[SerializeField]
		private int m_startFocusedCar;

		public int goldMoney
		{
			get
			{
				return m_goldMoney;
			}
			set
			{
				m_goldMoney = value;
			}
		}

		public float goldMFactor
		{
			get
			{
				return m_goldMFactor;
			}
			set
			{
				m_goldMFactor = value;
			}
		}

		public float goldEFactor
		{
			get
			{
				return m_goldEFactor;
			}
			set
			{
				m_goldEFactor = value;
			}
		}

		[XmlElement("expTable")]
		public string xmlExpTable
		{
			get
			{
				return XmlTools.ListIntToString(m_expTableList);
			}
			set
			{
				m_expTableList = XmlTools.StringToListInt(value);
			}
		}

		public int pageLikeCoinsCost
		{
			get
			{
				return m_pageLikeCoinsCost;
			}
			set
			{
				m_pageLikeCoinsCost = value;
			}
		}

		public int subscribeCoinsCost
		{
			get
			{
				return m_subscribeCoinsCost;
			}
			set
			{
				m_subscribeCoinsCost = value;
			}
		}

		public int startCoins
		{
			get
			{
				return m_startCoins;
			}
			set
			{
				m_startCoins = value;
			}
		}

		public int startRatingPoints
		{
			get
			{
				return m_startRatingPoints;
			}
			set
			{
				m_startRatingPoints = value;
			}
		}

		public int startFocusedCar
		{
			get
			{
				return m_startFocusedCar;
			}
			set
			{
				m_startFocusedCar = value;
			}
		}

		public int tutorial1pack7coins
		{
			get
			{
				return m_tutorial1pack7coins;
			}
			set
			{
				m_tutorial1pack7coins = value;
			}
		}

		[XmlIgnore]
		public ReadOnlyCollection<int> expTableList => m_expTableList.AsReadOnly();
	}
}
