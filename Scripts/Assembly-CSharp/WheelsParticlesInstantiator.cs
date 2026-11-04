using CarX;
using UnityEngine;

public class WheelsParticlesInstantiator : MonoBehaviour
{
	private void Awake()
	{
		if ((bool)CarResourceProvider.instance)
		{
			RaceCar componentInParent = GetComponentInParent<RaceCar>();
			if ((bool)componentInParent)
			{
				Car carX = componentInParent.carX;
				_ = CarResourceProvider.instance;
				WheelsParticles[] componentsInChildren = carX.GetComponentsInChildren<WheelsParticles>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					Object.Destroy(componentsInChildren[i]);
				}
				WheelBurnout[] componentsInChildren2 = carX.GetComponentsInChildren<WheelBurnout>();
				for (int i = 0; i < componentsInChildren2.Length; i++)
				{
					Object.Destroy(componentsInChildren2[i]);
				}
				base.gameObject.AddComponent<TireSmokeGpuEmitter>().Init(componentInParent);
				int num = 0;
				foreach (CARXSurfaceTemplate sceneTemplate in CARXSurfaceManager.GetSceneTemplates())
				{
					num = CARXSurfaceManager.IndexOf(sceneTemplate);
					if (!(sceneTemplate != null))
					{
						continue;
					}
					CARXWheelsParticlesData particleData = sceneTemplate.wheelVfxData.particleData;
					if (particleData != null && !sceneTemplate.emitGpuSmoke)
					{
						base.gameObject.AddComponent<WheelsParticles>().Init(particleData, num, componentInParent);
					}
					CARXWheelAccessor[] componentsInChildren3 = carX.GetComponentsInChildren<CARXWheelAccessor>();
					foreach (CARXWheelAccessor wheelAccessor in componentsInChildren3)
					{
						CARXWheelsBurnoutData burnoutData = sceneTemplate.wheelVfxData.burnoutData;
						if (burnoutData != null)
						{
							base.gameObject.AddComponent<WheelBurnout>().Init(burnoutData, num, wheelAccessor, componentInParent);
						}
					}
				}
			}
		}
		Object.Destroy(this);
	}
}
