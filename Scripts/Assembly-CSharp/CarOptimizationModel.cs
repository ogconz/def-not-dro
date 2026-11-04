using GpuCulling;
using UnityEngine;

public class CarOptimizationModel
{
	private CullingHandle m_cullingHandle;

	private CarPhysicsOptimization m_physicsCulling = new CarPhysicsOptimization();

	public CarOptimizationModel(RaceCar car)
	{
		GpuCullingService instance = GpuCullingService.instance;
		m_cullingHandle = instance.AddEntity();
		instance.BindObject(in m_cullingHandle, car.carX.bounds);
		instance.BindDynamicTransform(in m_cullingHandle, car.getTransform);
		instance.BindDynamicOffset(in m_cullingHandle, new Vector3(0f, car.carX.bounds.extents.y, 0f));
		m_physicsCulling.Setup(car, m_cullingHandle);
		instance.BindCallback(in m_cullingHandle, car.SetLogicUpdate);
		instance.BindCallback(in m_cullingHandle, car.EnableCarModel);
		instance.BindCallback(in m_cullingHandle, car.EnableDriverModel);
		BindScriptsCulling(car, instance);
		((GpuCullingServiceDebug)instance).BindObjectForDebug(m_cullingHandle, car);
	}

	public void Release()
	{
		m_physicsCulling.Release();
		GpuCullingService.instance.RemoveEntity(ref m_cullingHandle);
	}

	private void BindScriptsCulling(RaceCar car, GpuCullingService service)
	{
		CARXBaseScript[] componentsInChildren = car.gameObject.GetComponentsInChildren<CARXBaseScript>();
		foreach (CARXBaseScript cARXBaseScript in componentsInChildren)
		{
			if (cARXBaseScript is FMODBaseScript script)
			{
				m_physicsCulling.BindCarXPhysicsScriptDependency(script);
			}
			else
			{
				service.BindCallback(in m_cullingHandle, cARXBaseScript.SetActive);
			}
		}
	}
}
