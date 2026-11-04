using UnityEngine;

namespace Utils.InterpolatedSurface.AssetHolder
{
	[CreateAssetMenu(fileName = "BilinearSurfaceHolder", menuName = "Other/BilinearSurfaceHolder")]
	public class BilinearSurfaceHolder : BaseNormalizedSurfaceHolder
	{
		[SerializeField]
		[HideInInspector]
		private BilinearSurface m_storedSurface;

		public override INormalizedSurface StoredSurface => m_storedSurface;
	}
}
