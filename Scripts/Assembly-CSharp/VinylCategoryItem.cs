using System;
using System.Collections.Generic;
using DB.Meta;
using GameOverlay;
using UnityEngine;

public class VinylCategoryItem : BaseVinylItemViewModel<Sprite>, IComparable<VinylCategoryItem>
{
	public VinylCategorysMeta meta;

	public float currentLoopTime;

	private List<VinylMeta> m_vinylsMeta;

	private List<VinylLayerItem> m_vinylElements = new List<VinylLayerItem>();

	private bool m_showVinylsCount;

	public DLCCode dlcCode => meta.dlcCode;

	public bool showVinylsCount => m_showVinylsCount;

	public VinylCategoryItem(VinylCategorysMeta meta)
	{
		this.meta = meta;
		m_vinylsMeta = Singleton<VinylMetaProvider>.instance.meta.GetVinylsWithCategory(meta.id);
	}

	public List<VinylLayerItem> GetVinylsElements()
	{
		bool flag = false;
		foreach (VinylMeta vinylsMetum in m_vinylsMeta)
		{
			if (vinylsMetum.availablePlatform != AvailablePlatform.None && !ContainsVinylElement(vinylsMetum))
			{
				m_vinylElements.Add(new VinylLayerItem(vinylsMetum));
				flag = true;
			}
		}
		if (flag)
		{
			m_vinylElements.Sort();
		}
		return m_vinylElements;
	}

	private bool ContainsVinylElement(VinylMeta meta)
	{
		for (int i = 0; i < m_vinylElements.Count; i++)
		{
			if (m_vinylElements[i].meta == meta)
			{
				return true;
			}
		}
		return false;
	}

	public void UnloadResources()
	{
		for (int i = 0; i < m_vinylElements.Count; i++)
		{
			m_vinylElements[i].Unload();
		}
	}

	public void SetVinylsCountVisibility(bool show)
	{
		m_showVinylsCount = show;
	}

	protected override string GetTexturePath()
	{
		if (meta == null)
		{
			return null;
		}
		return "Vinyls/CategoryIcons/" + meta.icon;
	}

	public int CompareTo(VinylCategoryItem other)
	{
		if (this == other || meta == other.meta)
		{
			return 0;
		}
		return meta.sort.CompareTo(other.meta.sort);
	}
}
