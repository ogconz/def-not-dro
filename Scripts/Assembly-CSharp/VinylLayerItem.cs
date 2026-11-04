using System;
using DB.Meta;
using UnityEngine;
using VinylSystem;

public class VinylLayerItem : BaseVinylItemViewModel<Texture2D>, IComparable<VinylLayerItem>
{
	public readonly int id;

	public readonly VinylMeta meta;

	public readonly VinylLayer vinylLayer;

	public VinylLayerItem(VinylLayer vinylLayer)
	{
		if (vinylLayer != null)
		{
			id = vinylLayer.rawId;
			meta = Singleton<VinylMetaProvider>.instance.meta.GetVinylById(id);
			this.vinylLayer = vinylLayer;
		}
	}

	public VinylLayerItem(VinylMeta vinylMeta)
	{
		id = vinylMeta.id;
		meta = vinylMeta;
	}

	protected override string GetTexturePath()
	{
		if (meta == null)
		{
			return null;
		}
		return VinylSystemConfig.config.texturesLocation + meta.texturePath;
	}

	public int CompareTo(VinylLayerItem other)
	{
		if (this == other || meta == other.meta)
		{
			return 0;
		}
		return meta.sort.CompareTo(other.meta.sort);
	}
}
