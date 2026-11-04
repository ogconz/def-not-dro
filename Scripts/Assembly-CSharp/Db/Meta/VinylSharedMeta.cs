using System.Xml.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using SimpleJSON;
using UnityEngine;

namespace DB.Meta
{
	public class VinylSharedMeta : IDBMeta, IDBCanCost
	{
		[SerializeField]
		private int m_id;

		[SerializeField]
		private ObscuredInt m_cost;

		[JsonIgnore]
		public const int VINYL_PRESETS_CATEGORY_ID = -1;

		public string texturePath;

		public string flippedTexturePath;

		public float minSize;

		public float maxSize;

		public Color[] defaultColors;

		public int[] defaultMaterialProps;

		public int categoryId;

		public int initAngle;

		public int sort;

		public bool defaultLocked;

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

		public VinylSharedMeta()
		{
		}

		public VinylSharedMeta(int id, int cost, bool defaultLocked, int sort, string texturePath, string flippedTexturePath, float minSize, float maxSize, Color[] defaultColors, int[] defaultMaterialProps, int categoryId)
		{
			this.id = id;
			this.cost = cost;
			this.defaultLocked = defaultLocked;
			this.sort = sort;
			this.texturePath = texturePath;
			this.flippedTexturePath = flippedTexturePath;
			this.minSize = minSize;
			this.maxSize = maxSize;
			this.defaultColors = defaultColors;
			this.defaultMaterialProps = defaultMaterialProps;
			this.categoryId = categoryId;
		}
	}
}
