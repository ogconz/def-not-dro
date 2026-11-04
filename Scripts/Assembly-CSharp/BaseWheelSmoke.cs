using System;
using UnityEngine;

public class BaseWheelSmoke : CARXBaseScript
{
	[SerializeField]
	private Color m_smokeColor = Color.white;

	public Color smokeColor => m_smokeColor;

	public void SetSmokeColor(Color color, Action<BaseWheelSmoke> callForPS = null)
	{
		m_smokeColor = color;
		callForPS.Call(this);
	}

	public virtual void ClearSmoke()
	{
	}

	protected ParticleSystem InstantiateParticleSystem(ParticleSystem prefab)
	{
		ParticleSystem particleSystem = UnityEngine.Object.Instantiate(prefab);
		particleSystem.transform.parent = base.transform;
		particleSystem.transform.localPosition = Vector3.zero;
		particleSystem.transform.localRotation = Quaternion.identity;
		particleSystem.transform.localScale = Vector3.one;
		return particleSystem;
	}

	protected void DestroyParticleSystem(ref ParticleSystem particleSystem)
	{
		if ((bool)particleSystem)
		{
			UnityEngine.Object.Destroy(particleSystem.gameObject);
			particleSystem = null;
		}
	}
}
