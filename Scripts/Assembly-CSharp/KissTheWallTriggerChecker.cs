using Unity.Collections;
using UnityEngine;

public class KissTheWallTriggerChecker : MonoBehaviour
{
	private KissTheWallCarConfig m_carConfig;

	private Vector3[] m_raycastDirections;

	private Vector3 m_vector;

	private bool m_right;

	public bool right => m_right;

	public Vector3 vector => m_vector;

	public void Initialize(KissTheWallCarConfig config, bool right)
	{
		m_carConfig = config;
		m_raycastDirections = new Vector3[config.rayCount];
		m_right = right;
		CreateRaycastDirections();
	}

	private void CreateRaycastDirections()
	{
		Vector3 vector = -Vector3.forward;
		float num = ((!m_right) ? 1 : (-1));
		float num2 = ((m_carConfig.rayCount <= 1) ? m_carConfig.checkAngle : (m_carConfig.checkAngle / (float)(m_carConfig.rayCount - 1)));
		for (int i = 0; i < m_raycastDirections.Length; i++)
		{
			m_raycastDirections[i] = Quaternion.Euler(0f, (float)i * num2 * num, 0f) * vector;
		}
	}

	public Transform GetNearestWall()
	{
		return GetNearestWall(m_carConfig.layerMask, m_carConfig.maxDistance, out m_vector);
	}

	private Transform GetNearestWall(LayerMask layerMask, float maxDistance, out Vector3 vector)
	{
		Vector3 position = base.transform.position;
		CastRays(maxDistance, layerMask, position, out var results);
		float num = maxDistance * maxDistance;
		Transform result = null;
		vector = Vector3.zero;
		foreach (RaycastHit item in results)
		{
			if (item.ContainesAcceptableHitInfo(m_carConfig.ignoredMaterials))
			{
				Vector3 vector2 = item.point - position;
				float sqrMagnitude = vector2.sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = item.transform;
					vector = vector2;
				}
			}
		}
		results.Dispose();
		return result;
	}

	private void CastRays(float maxDistance, LayerMask layerMask, Vector3 point, out NativeArray<RaycastHit> results)
	{
		NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(m_raycastDirections.Length, Allocator.TempJob);
		results = new NativeArray<RaycastHit>(commands.Length, Allocator.TempJob);
		for (int i = 0; i < commands.Length; i++)
		{
			Vector3 direction = base.transform.TransformDirection(m_raycastDirections[i]);
			commands[i] = new RaycastCommand(point, direction, maxDistance, layerMask);
		}
		RaycastCommand.ScheduleBatch(commands, results, commands.Length).Complete();
		commands.Dispose();
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		Gizmos.color = Color.red;
		Vector3[] raycastDirections = m_raycastDirections;
		foreach (Vector3 direction in raycastDirections)
		{
			Vector3 direction2 = base.transform.TransformDirection(direction) * m_carConfig.maxDistance;
			Gizmos.DrawRay(position, direction2);
		}
		Gizmos.color = Color.white;
		raycastDirections = m_raycastDirections;
		foreach (Vector3 direction3 in raycastDirections)
		{
			Vector3 direction4 = base.transform.TransformDirection(direction3) * m_carConfig.detectDistance;
			Gizmos.DrawRay(position, direction4);
		}
	}
}
