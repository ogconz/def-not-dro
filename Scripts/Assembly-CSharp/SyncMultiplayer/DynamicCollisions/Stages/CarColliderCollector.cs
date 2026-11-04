using System.Collections.Generic;
using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class CarColliderCollector
	{
		[BurstCompile]
		private struct BuildBoundsJob : IJobParallelFor
		{
			private const int CornersCount = 8;

			[ReadOnly]
			public NativeArray<Aabb> source;

			[ReadOnly]
			public NativeArray<float4x4> localToWorldPerCollider;

			[ReadOnly]
			public NativeArray<float4x4> worldToLocalPerCar;

			[ReadOnly]
			public NativeArray<int> offsets;

			[ReadOnly]
			public NativeArray<int> lengths;

			[WriteOnly]
			public NativeArray<Aabb> results;

			public void Execute(int index)
			{
				float4x4 a = worldToLocalPerCar[index];
				int num = offsets[index];
				int num2 = lengths[index];
				float3 float5 = source[num].min;
				float3 float6 = source[num].max;
				for (int i = 1; i < num2; i++)
				{
					int index2 = num + i;
					float4x4 a2 = localToWorldPerCollider[index2];
					for (int j = 0; j < 8; j++)
					{
						float3 corner = GetCorner(source[index2], j);
						float3 b = math.transform(a2, corner);
						float3 y = math.transform(a, b);
						float5 = math.min(float5, y);
						float6 = math.max(float6, y);
					}
				}
				results[index] = new Aabb(float5, float6);
			}

			private float3 GetCorner(Aabb aabb, int i)
			{
				return math.float3(((i & 1) == 0) ? aabb.min.x : aabb.max.x, ((i & 2) == 0) ? aabb.min.y : aabb.max.y, ((i & 4) == 0) ? aabb.min.z : aabb.max.z);
			}
		}

		[BurstCompile]
		private struct TransferBoundsToObbJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<Aabb> source;

			[ReadOnly]
			public NativeArray<int> indices;

			[ReadOnly]
			public float3 scale;

			[NativeDisableParallelForRestriction]
			public NativeArray<Obb> cars;

			public void Execute(int index)
			{
				Aabb bounds = source[index];
				ScaleBounds(ref bounds, in scale);
				int index2 = indices[index];
				Obb value = cars[index2];
				value.extents = (bounds.max - bounds.min) * 0.5f;
				value.offset = bounds.min + value.extents;
				cars[index2] = value;
			}

			private void ScaleBounds(ref Aabb bounds, in float3 scaleFactor)
			{
				float3 obj = bounds.max - bounds.min;
				float3 float5 = obj * 0.5f;
				float3 float6 = obj * scaleFactor * 0.5f - float5;
				bounds.min -= float6;
				bounds.max += float6;
			}
		}

		private List<NetworkPlayer> m_requests;

		private LayerMask m_carsLayerMask;

		private float3 m_boundsScale;

		private int m_requestsCount;

		private NativeArray<Aabb> m_allCollidersFromCars;

		private NativeArray<Aabb> m_totalColliderPerCar;

		private NativeArray<float4x4> m_localToWorldPerCollider;

		private NativeArray<float4x4> m_worldToLocalPerCar;

		private NativeArray<int> m_offsets;

		private NativeArray<int> m_lengths;

		private NativeArray<int> m_indices;

		public CarColliderCollector(DynamicCollisionsConfig config)
		{
			m_requests = new List<NetworkPlayer>(config.maxCars);
			m_carsLayerMask = LayerMask.GetMask("Car", "LocalCar");
			m_boundsScale = math.float3(1) * config.boundsScale;
			m_allCollidersFromCars = new NativeArray<Aabb>(config.maxCars * config.maxCollidersPerCar, Allocator.Persistent);
			m_totalColliderPerCar = new NativeArray<Aabb>(config.maxCars, Allocator.Persistent);
			m_localToWorldPerCollider = new NativeArray<float4x4>(config.maxCars * config.maxCollidersPerCar, Allocator.Persistent);
			m_worldToLocalPerCar = new NativeArray<float4x4>(config.maxCars, Allocator.Persistent);
			m_offsets = new NativeArray<int>(config.maxCars, Allocator.Persistent);
			m_lengths = new NativeArray<int>(config.maxCars, Allocator.Persistent);
			m_indices = new NativeArray<int>(config.maxCars, Allocator.Persistent);
		}

		~CarColliderCollector()
		{
			m_requests = null;
			m_allCollidersFromCars.Dispose();
			m_totalColliderPerCar.Dispose();
			m_localToWorldPerCollider.Dispose();
			m_worldToLocalPerCar.Dispose();
			m_offsets.Dispose();
			m_lengths.Dispose();
			m_indices.Dispose();
		}

		public void CreateRequest(NetworkPlayer player)
		{
			if (!m_requests.Contains(player))
			{
				m_requests.Add(player);
			}
		}

		public void RemoveRequest(NetworkPlayer player)
		{
			if (m_requests.Contains(player))
			{
				m_requests.Remove(player);
			}
		}

		public void CollectRequests()
		{
			if (m_requests.Count != 0)
			{
				int offset = 0;
				m_requestsCount = m_requests.Count;
				for (int i = 0; i < m_requestsCount; i++)
				{
					NetworkPlayer networkPlayer = m_requests[i];
					m_indices[i] = networkPlayer.SlotID;
					m_offsets[i] = offset;
					m_worldToLocalPerCar[i] = networkPlayer.userCar.transform.worldToLocalMatrix;
					AddCollidersFromCar(networkPlayer.userCar, ref offset, out var length);
					m_lengths[i] = length;
				}
				m_requests.Clear();
			}
		}

		public JobHandle Process(JobHandle dependency, in DataState state)
		{
			if (m_requestsCount == 0)
			{
				return dependency;
			}
			dependency = IJobParallelForExtensions.Schedule(new BuildBoundsJob
			{
				source = m_allCollidersFromCars,
				localToWorldPerCollider = m_localToWorldPerCollider,
				worldToLocalPerCar = m_worldToLocalPerCar,
				offsets = m_offsets,
				lengths = m_lengths,
				results = m_totalColliderPerCar
			}, m_requestsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new TransferBoundsToObbJob
			{
				source = m_totalColliderPerCar,
				indices = m_indices,
				cars = state.carsRawTransforms,
				scale = m_boundsScale
			}, m_requestsCount, 1, dependency);
			m_requestsCount = 0;
			return dependency;
		}

		private void AddCollidersFromCar(RaceCar car, ref int offset, out int length)
		{
			length = 0;
			for (int i = 0; i < car.carModel.colliders.Length; i++)
			{
				Collider collider = car.carModel.colliders[i];
				if (collider != null && (m_carsLayerMask.value & (1 << collider.gameObject.layer)) != 0)
				{
					Bounds localBoundsFromCollider = GetLocalBoundsFromCollider(collider);
					m_allCollidersFromCars[offset] = new Aabb(localBoundsFromCollider.min, localBoundsFromCollider.max);
					m_localToWorldPerCollider[offset] = collider.transform.localToWorldMatrix;
					offset++;
					length++;
				}
			}
		}

		private Bounds GetLocalBoundsFromCollider(Collider collider)
		{
			if (!(collider is BoxCollider boxCollider))
			{
				if (!(collider is SphereCollider sphereCollider))
				{
					if (!(collider is CapsuleCollider capsuleCollider))
					{
						if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
						{
							return meshCollider.sharedMesh.bounds;
						}
						return collider.bounds;
					}
					Vector3 zero = Vector3.zero;
					zero[capsuleCollider.direction] = capsuleCollider.height;
					zero[(capsuleCollider.direction + 1) % 3] = capsuleCollider.radius * 2f;
					zero[(capsuleCollider.direction + 2) % 3] = capsuleCollider.radius * 2f;
					return new Bounds(capsuleCollider.center, zero);
				}
				Vector3 size = Vector3.one * sphereCollider.radius * 2f;
				return new Bounds(sphereCollider.center, size);
			}
			return new Bounds(boxCollider.center, boxCollider.size);
		}
	}
}
