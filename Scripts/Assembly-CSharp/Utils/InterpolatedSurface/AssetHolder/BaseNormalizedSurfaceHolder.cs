using UnityEngine;

namespace Utils.InterpolatedSurface.AssetHolder
{
	public abstract class BaseNormalizedSurfaceHolder : ScriptableObject
	{
		[SerializeField]
		[HideInInspector]
		private Vector2Int m_stepsCount;

		public Vector2Int stepsCount => m_stepsCount;

		public abstract INormalizedSurface StoredSurface { get; }
	}
}
