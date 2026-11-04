using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GpuCulling
{
	public class GpuCullingPass : CustomPass
	{
		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			GpuCullingService.instance.Setup();
		}

		protected override void Cleanup()
		{
			GpuCullingService.instance.Release();
		}

		protected override void Execute(CustomPassContext ctx)
		{
			GpuCullingService.instance.Process(ref ctx);
		}
	}
}
