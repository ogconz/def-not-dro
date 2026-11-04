using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LoadingArt
{
	[CreateAssetMenu(fileName = "LoadingArtAsset", menuName = "ScriptableObjects/LoadingArtAsset")]
	public class LoadingArtConfigSO : ScriptableObject
	{
		public float timeoutChangePC;

		public float timeoutChangeConsole;

		public float timeToFade;

		public List<AssetReferenceSprite> loadingArtSpritesReferences;
	}
}
