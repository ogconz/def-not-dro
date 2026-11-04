using System;
using System.Collections.Generic;
using UnityEngine;

namespace DB.Meta
{
	public class VinylsSystemMeta : ScriptableObject
	{
		public enum VinylMetaPlatform
		{
			Steam = 0,
			Consoles = 1
		}

		public const string ASSET_PATH = "Assets/Settings/Vinyls/Meta/VinylsSystemMeta.asset";

		public const string ASSET_CONSOLE_PATH = "Assets/Settings/Vinyls/Meta/VinylsSystemMetaConsoles.asset";

		public List<VinylCategorysMeta> categorys = new List<VinylCategorysMeta>();

		public List<VinylMeta> vinyls = new List<VinylMeta>();

		public VinylMetaPlatform vinylMetaPlatformType;

		private Dictionary<int, List<VinylMeta>> m_cacheCategory = new Dictionary<int, List<VinylMeta>>();

		private Dictionary<int, VinylMeta> m_cacheVinyls = new Dictionary<int, VinylMeta>();

		public VinylCategorysMeta GetCategoryById(int categoryId)
		{
			VinylCategorysMeta vinylCategorysMeta = categorys.Find((VinylCategorysMeta x) => x.id == categoryId);
			if (vinylCategorysMeta == null)
			{
				GameLog.Print(AlertLevel.Error, "Vinyls", "VinylCategorysMeta not found categoryId = " + categoryId);
			}
			return vinylCategorysMeta;
		}

		public VinylCategorysMeta GetCategoryByVinylId(int vinylId)
		{
			VinylMeta vinylById = GetVinylById(vinylId);
			if (vinylById == null)
			{
				return null;
			}
			return GetCategoryById(vinylById.categoryId);
		}

		public List<VinylMeta> GetVinylByName(string name)
		{
			List<VinylMeta> list = vinyls.FindAll((VinylMeta vinyl) => GetNameFromTexturePath(vinyl).IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);
			List<VinylMeta> list2 = list.FindAll((VinylMeta vinyl) => GetNameFromTexturePath(vinyl).Equals(name, StringComparison.OrdinalIgnoreCase));
			if (list2.Count == 0)
			{
				return list;
			}
			return list2;
		}

		public string GetNameFromTexturePath(VinylMeta vinylMeta)
		{
			return vinylMeta.texturePath.Substring(vinylMeta.texturePath.IndexOf('/') + 1);
		}

		public string GetDirFromTexturePath(VinylMeta vinylMeta)
		{
			return vinylMeta.texturePath.Substring(0, vinylMeta.texturePath.IndexOf('/'));
		}

		public VinylMeta GetVinylById(int vinylId)
		{
			if (m_cacheVinyls.Count == 0)
			{
				CacheVinyls();
			}
			if (!m_cacheVinyls.TryGetValue(vinylId, out var value))
			{
				GameLog.Print(AlertLevel.Warning, "Vinyls", "VinylMeta not found vinylId = " + vinylId);
			}
			return value;
		}

		public List<VinylMeta> GetVinylsWithCategory(int categoryId)
		{
			if (!m_cacheCategory.TryGetValue(categoryId, out var value))
			{
				value = vinyls.FindAll((VinylMeta x) => x.categoryId == categoryId);
				if (value != null && value.Count > 0)
				{
					m_cacheCategory.Add(categoryId, value);
				}
				else
				{
					GameLog.Print(AlertLevel.Warning, "Vinyls", "VinylMeta not found categoryId = " + categoryId);
					value = new List<VinylMeta>();
				}
			}
			return value;
		}

		public List<VinylMeta> GetVinylsWithCategory(string categoryName)
		{
			int categoryId = -1;
			VinylCategorysMeta vinylCategorysMeta = categorys.Find((VinylCategorysMeta x) => x.icon == categoryName);
			if (vinylCategorysMeta != null)
			{
				categoryId = vinylCategorysMeta.id;
			}
			return GetVinylsWithCategory(categoryId);
		}

		private void CacheVinyls()
		{
			m_cacheVinyls.Clear();
			foreach (VinylMeta vinyl in vinyls)
			{
				m_cacheVinyls.Add(vinyl.id, vinyl);
			}
		}
	}
}
