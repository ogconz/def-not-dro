using System.Collections.Generic;
using UnityEngine;
using Utils.ColorExtensions;
using VinylSystem;

namespace Utils.VinylLayerExtensions
{
	public static class VinylLayerExtensions
	{
		public static bool LayerEquals(this VinylLayer thisLayer, VinylLayer other)
		{
			Color[] colors = thisLayer.colors;
			Color[] colors2 = other.colors;
			int[] materialProps = thisLayer.materialProps;
			int[] materialProps2 = other.materialProps;
			if (colors.Length != colors2.Length || materialProps.Length != materialProps2.Length || thisLayer.rawId != other.rawId)
			{
				return false;
			}
			for (int i = 0; i < colors.Length; i++)
			{
				Color colorLeft = colors[i];
				Color colorRight = colors2[i];
				if (!colorLeft.IsColorHexMatch(colorRight))
				{
					return false;
				}
			}
			for (int j = 0; j < materialProps.Length; j++)
			{
				int num = materialProps[j];
				int num2 = materialProps2[j];
				if (num != num2)
				{
					return false;
				}
			}
			bool num3 = thisLayer.position == other.position && thisLayer.rotation == other.rotation && thisLayer.scale == other.scale && thisLayer.angle.Equals(other.angle);
			bool flag = thisLayer.isHorizontalFlip == other.isHorizontalFlip && thisLayer.isVerticalFlip == other.isVerticalFlip && thisLayer.isSymmetry == other.isSymmetry && thisLayer.groupMask == other.groupMask;
			return num3 && flag;
		}

		public static List<VinylLayer> Clone(this List<VinylLayer> thisList)
		{
			List<VinylLayer> list = new List<VinylLayer>(thisList.Count);
			foreach (VinylLayer @this in thisList)
			{
				list.Add(@this.Copy());
			}
			return list;
		}

		public static List<VinylLayer> Crop(this List<VinylLayer> list)
		{
			int maxLayersCount = VinylSystemConfig.config.maxLayersCount;
			if (list.Count > maxLayersCount)
			{
				list.RemoveRange(maxLayersCount, list.Count - maxLayersCount);
			}
			return list;
		}
	}
}
