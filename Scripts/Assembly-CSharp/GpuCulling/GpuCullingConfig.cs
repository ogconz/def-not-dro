using UnityEngine;

namespace GpuCulling
{
	[CreateAssetMenu(fileName = "GpuCullingConfig", menuName = "Gpu Culling Config")]
	public class GpuCullingConfig : ScriptableObject
	{
		public ComputeShader cullingProcessShader;

		public ComputeShader copyDepthTextureShader;

		public ComputeShader mipGeneratorShader;

		public int capacity;

		public float minDistance;

		public float maxDistance;
	}
}
