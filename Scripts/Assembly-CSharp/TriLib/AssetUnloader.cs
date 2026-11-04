using UnityEngine;

namespace TriLib
{
	public class AssetUnloader : MonoBehaviour
	{
		protected virtual void OnDestroy()
		{
			if (Application.isPlaying)
			{
				Resources.UnloadUnusedAssets();
			}
		}
	}
}
