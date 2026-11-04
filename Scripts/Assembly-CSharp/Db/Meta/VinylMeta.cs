using System;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class VinylMeta
	{
		public string texturePath;

		public int id;

		public string flippedTexturePath;

		public int categoryId;

		public int sort;

		public AvailablePlatform availablePlatform;

		public Color[] defaultColors = new Color[1] { Color.white };

		public int[] defaultMaterialProps = new int[1];

		public VinylMeta Clone()
		{
			VinylMeta vinylMeta = new VinylMeta();
			vinylMeta.id = id;
			vinylMeta.texturePath = texturePath;
			vinylMeta.flippedTexturePath = flippedTexturePath;
			vinylMeta.categoryId = categoryId;
			vinylMeta.sort = sort;
			vinylMeta.availablePlatform = availablePlatform;
			vinylMeta.defaultColors = new Color[defaultColors.Length];
			vinylMeta.defaultMaterialProps = new int[defaultMaterialProps.Length];
			defaultColors.CopyTo(vinylMeta.defaultColors, 0);
			defaultMaterialProps.CopyTo(vinylMeta.defaultMaterialProps, 0);
			return vinylMeta;
		}
	}
}
