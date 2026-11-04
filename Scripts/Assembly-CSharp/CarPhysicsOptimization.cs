using System.Collections.Generic;
using GpuCulling;

public class CarPhysicsOptimization
{
	private CullingHandle m_scriptCullingHandle;

	private RaceCar m_car;

	private bool m_targetPhysicsState;

	private bool m_hasScriptDependency;

	private float m_maxCullingDistance;

	private List<FMODBaseScript> m_fmodDependencies = new List<FMODBaseScript>();

	public void Setup(RaceCar car, CullingHandle carCullingHandle)
	{
		m_car = car;
		GpuCullingService instance = GpuCullingService.instance;
		instance.BindCallback(in carCullingHandle, EnableCarXPhysics);
		m_scriptCullingHandle = instance.AddEntity();
		instance.BindDynamicTransform(in m_scriptCullingHandle, m_car.getTransform);
		instance.BindCallback(in m_scriptCullingHandle, EnableScriptDependency);
	}

	public void Release()
	{
		GpuCullingService.instance.RemoveEntity(ref m_scriptCullingHandle);
		ReleaseCarXPhysicsScriptDependencies();
		m_maxCullingDistance = 0f;
	}

	public void BindCarXPhysicsScriptDependency(FMODBaseScript script)
	{
		m_fmodDependencies.Add(script);
		script.CullingDistanceChangeEvent += CullingDistanceChangeHandler;
		CullingDistanceChangeHandler(script.GetCullingDistance());
	}

	private void ReleaseCarXPhysicsScriptDependencies()
	{
		foreach (FMODBaseScript fmodDependency in m_fmodDependencies)
		{
			if (fmodDependency != null)
			{
				fmodDependency.CullingDistanceChangeEvent -= CullingDistanceChangeHandler;
			}
		}
		m_fmodDependencies.Clear();
	}

	private void CullingDistanceChangeHandler(float distance)
	{
		if (distance > m_maxCullingDistance)
		{
			m_maxCullingDistance = distance;
			GpuCullingService.instance.BindSound(in m_scriptCullingHandle, m_car.getTransform.position, m_maxCullingDistance);
		}
	}

	private void EnableCarXPhysics(bool enable)
	{
		m_targetPhysicsState = enable;
		UpdatePhysicsState();
	}

	private void EnableScriptDependency(bool enable)
	{
		m_hasScriptDependency = enable;
		UpdatePhysicsState();
	}

	private void UpdatePhysicsState()
	{
		m_car.EnableCarXPhysicsForNetworkCar(m_targetPhysicsState || m_hasScriptDependency);
	}
}
