using System.Collections.Generic;
using UnityEngine;

public static class ParticlesManager
{
	private class SharedParticleInstance
	{
		public ParticleSystem particleSystem;

		public int usageCount;
	}

	private static readonly Dictionary<ParticleSystem, SharedParticleInstance> m_sharedParticleInstances = new Dictionary<ParticleSystem, SharedParticleInstance>();

	public static ParticleSystem InstantiateParticleSystem(ParticleSystem reference, bool needUniqueInstance, bool initCollision)
	{
		if (needUniqueInstance)
		{
			return CreateParticleInstance(reference, initCollision);
		}
		SharedParticleInstance sharedParticleInstance;
		if (m_sharedParticleInstances.ContainsKey(reference))
		{
			sharedParticleInstance = m_sharedParticleInstances[reference];
			if (sharedParticleInstance.particleSystem == null)
			{
				sharedParticleInstance.particleSystem = CreateParticleInstance(reference, initCollision);
				sharedParticleInstance.usageCount = 0;
			}
		}
		else
		{
			sharedParticleInstance = new SharedParticleInstance
			{
				particleSystem = CreateParticleInstance(reference, initCollision),
				usageCount = 0
			};
			m_sharedParticleInstances.Add(reference, sharedParticleInstance);
		}
		sharedParticleInstance.usageCount++;
		return sharedParticleInstance.particleSystem;
	}

	private static ParticleSystem CreateParticleInstance(ParticleSystem reference, bool initCollision)
	{
		ParticleSystem particleSystem = Object.Instantiate(reference);
		particleSystem.transform.localPosition = Vector3.zero;
		particleSystem.transform.localRotation = Quaternion.identity;
		particleSystem.transform.localScale = Vector3.one;
		if (initCollision)
		{
			InitCollisionModule(particleSystem);
		}
		return particleSystem;
	}

	private static void InitCollisionModule(ParticleSystem particleSystem)
	{
		ParticleSystem.CollisionModule collision = particleSystem.collision;
		collision.enabled = true;
		collision.enableDynamicColliders = true;
		collision.maxCollisionShapes = 4;
		collision.mode = ParticleSystemCollisionMode.Collision3D;
		collision.quality = ParticleSystemCollisionQuality.Low;
		collision.type = ParticleSystemCollisionType.World;
		collision.collidesWith = 8388608;
	}

	public static void DestroyParticleSystem(ref ParticleSystem particleSystem)
	{
		foreach (KeyValuePair<ParticleSystem, SharedParticleInstance> sharedParticleInstance in m_sharedParticleInstances)
		{
			if (!(sharedParticleInstance.Value.particleSystem != particleSystem))
			{
				sharedParticleInstance.Value.usageCount--;
				if (sharedParticleInstance.Value.usageCount == 0)
				{
					Object.Destroy(sharedParticleInstance.Value.particleSystem.gameObject);
					m_sharedParticleInstances.Remove(sharedParticleInstance.Key);
				}
				particleSystem = null;
				return;
			}
		}
		if (particleSystem != null)
		{
			Object.Destroy(particleSystem.gameObject);
			particleSystem = null;
		}
	}
}
