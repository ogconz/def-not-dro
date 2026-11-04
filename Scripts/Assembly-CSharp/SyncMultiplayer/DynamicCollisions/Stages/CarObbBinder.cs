using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class CarObbBinder
	{
		[BurstCompile]
		private struct UpdateObbTransformJob : IJobParallelForTransform
		{
			[ReadOnly]
			public NativeList<int> indices;

			[NativeDisableParallelForRestriction]
			public NativeArray<Obb> cars;

			public void Execute(int index, TransformAccess transform)
			{
				int index2 = indices[index];
				Obb value = cars[index2];
				transform.GetPositionAndRotation(out var position, out var rotation);
				value.center = position;
				value.rotation = rotation;
				cars[index2] = value;
			}
		}

		[BurstCompile]
		private struct ComputeObbPredictionJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> indices;

			[ReadOnly]
			public NativeArray<CarMeta> carsMeta;

			[ReadOnly]
			public float deltaTime;

			[NativeDisableParallelForRestriction]
			[ReadOnly]
			public NativeArray<Obb> source;

			[NativeDisableParallelForRestriction]
			[WriteOnly]
			public NativeArray<Obb> destination;

			public void Execute(int index)
			{
				int index2 = indices[index];
				CarMeta carMeta = carsMeta[index2];
				Obb value = source[index2];
				value.center += carMeta.velocity * deltaTime;
				value.rotation = math.mul(value.rotation, GetAngularRotationDelta(in carMeta.angularVelocity));
				value.center += math.mul(value.rotation, value.offset);
				destination[index2] = value;
			}

			private quaternion GetAngularRotationDelta(in float3 angularVelocity)
			{
				float num = math.length(angularVelocity) * deltaTime;
				float3 axis = math.normalizesafe(angularVelocity);
				if (!(num > 1.1920929E-07f))
				{
					return quaternion.identity;
				}
				return quaternion.AxisAngle(axis, num);
			}
		}

		private DynamicCollisionsConfig m_config;

		private TransformAccessArray m_transformAccessArray;

		private NativeList<int> m_indices;

		public CarObbBinder(DynamicCollisionsConfig config)
		{
			m_config = config;
			m_transformAccessArray = new TransformAccessArray(config.maxCars);
			m_indices = new NativeList<int>(config.maxCars, Allocator.Persistent);
		}

		~CarObbBinder()
		{
			m_transformAccessArray.Dispose();
			m_indices.Dispose();
		}

		public void BindPlayerCar(NetworkPlayer player, RaceCar car)
		{
			m_transformAccessArray.Add(car.transform);
			m_indices.AddNoResize(player.SlotID);
		}

		public void ReleasePlayerCar(NetworkPlayer player)
		{
			int index = m_indices.IndexOf((int)player.SlotID);
			m_transformAccessArray.RemoveAtSwapBack(index);
			m_indices.RemoveAtSwapBack(index);
		}

		public JobHandle Process(JobHandle dependency, in DataState state)
		{
			dependency = new UpdateObbTransformJob
			{
				indices = m_indices,
				cars = state.carsRawTransforms
			}.ScheduleReadOnly(m_transformAccessArray, state.maxCarsCount, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ComputeObbPredictionJob
			{
				indices = m_indices,
				carsMeta = state.carsMeta,
				source = state.carsRawTransforms,
				destination = state.carsObb,
				deltaTime = Time.deltaTime * m_config.boundsPredictionMultiplier
			}, state.activeCarsCount, 1, dependency);
			return dependency;
		}
	}
}
