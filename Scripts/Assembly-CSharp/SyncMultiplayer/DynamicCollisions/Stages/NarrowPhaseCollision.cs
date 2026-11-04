using SyncMultiplayer.DynamicCollisions.Structs;
using SyncMultiplayer.DynamicCollisions.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class NarrowPhaseCollision
	{
		[BurstCompile]
		private struct ClearInitiatorsJob : IJobParallelFor
		{
			[WriteOnly]
			public NativeArray<int> array;

			public void Execute(int index)
			{
				array[index] = -1;
			}
		}

		[BurstCompile]
		private struct ComputeInitiatorsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<CarMeta> currentCarsMeta;

			[ReadOnly]
			public NativeArray<Obb> currentCarsObb;

			[ReadOnly]
			public NativeArray<Obb> previousCarsObb;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> currentResolveMap;

			[ReadOnly]
			public NativeArray<ResolveFlag> previousResolveMap;

			[NativeDisableParallelForRestriction]
			public NativeArray<int> currentInitiators;

			[ReadOnly]
			public NativeArray<int> previousInitiators;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				Obb obb = currentCarsObb[num];
				float3 velocity = currentCarsMeta[num].velocity;
				bool flag = currentCarsMeta[num].collideGroupHash != 0;
				float positionSqrDelta = GetPositionSqrDelta(num);
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					Obb obb2 = currentCarsObb[num2];
					float3 velocity2 = currentCarsMeta[num2].velocity;
					bool flag2 = currentCarsMeta[num2].collideGroupHash != 0;
					float positionSqrDelta2 = GetPositionSqrDelta(num2);
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					ResolveFlag flag3 = currentResolveMap[pairIndex];
					ResolveFlag flag4 = previousResolveMap[pairIndex];
					if (flag3.Has(ResolveFlag.HasIntersection) && flag3.Has(ResolveFlag.FirstInitialize))
					{
						currentInitiators[pairIndex] = -1;
					}
					else if (flag != flag2)
					{
						currentInitiators[pairIndex] = (flag ? num : num2);
					}
					else if (flag3.Has(ResolveFlag.HasIntersection) && flag4.Has(ResolveFlag.HasIntersection))
					{
						currentInitiators[pairIndex] = previousInitiators[pairIndex];
					}
					else if (flag3.Has(ResolveFlag.IsMoving))
					{
						if (!flag3.Has(ResolveFlag.IsRapprochement))
						{
							currentInitiators[pairIndex] = ((positionSqrDelta > positionSqrDelta2) ? num : num2);
							continue;
						}
						float3 y = obb2.center - obb.center;
						float b = math.dot(velocity, y);
						float num3 = math.dot(velocity2, y);
						float num4 = Mathf.Max(0f, b);
						float num5 = Mathf.Max(0f, 0f - num3);
						currentInitiators[pairIndex] = ((num4 > num5) ? num : num2);
					}
				}
			}

			private float GetPositionSqrDelta(int slotId)
			{
				return math.lengthsq(currentCarsObb[slotId].center - previousCarsObb[slotId].center);
			}
		}

		[BurstCompile]
		private struct ComputeTwoWayInitiatorsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> currentResolveMap;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> previousResolveMap;

			[NativeDisableParallelForRestriction]
			public NativeArray<int> currentInitiators;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					bool flag = false;
					if (HasAnyCollideRestriction(in currentInitiators, in currentResolveMap, num))
					{
						currentInitiators[pairIndex] = num;
						flag = true;
					}
					if (HasAnyCollideRestriction(in currentInitiators, in currentResolveMap, num2))
					{
						currentInitiators[pairIndex] = num2;
						flag = true;
					}
					if (flag && previousResolveMap[pairIndex].Has(ResolveFlag.FirstInitialize))
					{
						currentResolveMap[pairIndex] = currentResolveMap[pairIndex].Add(ResolveFlag.FirstInitialize);
						currentInitiators[pairIndex] = -1;
					}
				}
			}

			private bool HasAnyCollideRestriction(in NativeArray<int> initiators, in NativeArray<ResolveFlag> resolveMap, int localSlotId)
			{
				for (int i = 0; i < activeSlotsCount; i++)
				{
					int num = activeSlots[i];
					if (localSlotId != num)
					{
						int pairIndex = PairMath.GetPairIndex(localSlotId, num, maxSlots);
						if (initiators[pairIndex] == localSlotId && resolveMap[pairIndex].Has(ResolveFlag.HasIntersection) && !resolveMap[pairIndex].Has(ResolveFlag.HasCollision))
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		[BurstCompile]
		private struct ComputeDirectIntersectionJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<Obb> carsObb;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					ResolveFlag flag = resolveMap[pairIndex];
					if (!flag.Has(ResolveFlag.LargeDistance) && ObbIntersectionTool.IsIntersects(carsObb[num], carsObb[num2]))
					{
						resolveMap[pairIndex] = flag.Add(ResolveFlag.HasIntersection);
					}
				}
			}
		}

		[BurstCompile]
		private struct BuildVelocitySweptObbJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<CarMeta> currentCarsMeta;

			[ReadOnly]
			public NativeArray<CarMeta> previousCarsMeta;

			[ReadOnly]
			public NativeArray<Obb> currentCarsObb;

			[ReadOnly]
			public int segmentCount;

			[ReadOnly]
			public float velocityDiffThresholdSqr;

			[ReadOnly]
			public NativeCurve maxDistanceBySpeedCurve;

			[NativeDisableParallelForRestriction]
			public NativeArray<Obb> velocitySweptObb;

			public void Execute(int index)
			{
				int num = activeSlots[index];
				CarMeta carMeta = currentCarsMeta[num];
				CarMeta carMeta2 = previousCarsMeta[num];
				Obb value = currentCarsObb[num];
				float3 float5 = (carMeta2.velocity + carMeta.velocity) * 0.5f;
				float3 float6 = (carMeta2.angularVelocity + carMeta.angularVelocity) * 0.5f;
				if (math.lengthsq(float5) < velocityDiffThresholdSqr)
				{
					for (int i = 0; i < segmentCount; i++)
					{
						velocitySweptObb[num * segmentCount + i] = value;
					}
					return;
				}
				float num2 = math.length(float5);
				float num3 = math.length(float6);
				float3 axis = ((num3 > 1.1920929E-07f) ? (float6 / num3) : math.up());
				float3 center = value.center;
				quaternion rotation = value.rotation;
				float3 v = float5 / num2;
				float num4 = maxDistanceBySpeedCurve.Evaluate(num2) / num2 / (float)segmentCount;
				for (int j = 0; j < segmentCount; j++)
				{
					float num5 = (float)(j + 1) * num4;
					float angle = num3 * num5;
					quaternion quaternion2 = quaternion.AxisAngle(axis, angle);
					float3 float7 = math.mul(quaternion2, v);
					center += float7 * num2 * num4;
					int index2 = num * segmentCount + j;
					Obb value2 = velocitySweptObb[index2];
					value2.center = center;
					value2.extents = value.extents;
					value2.rotation = math.mul(quaternion2, rotation);
					value2.offset = value.offset;
					velocitySweptObb[index2] = value2;
				}
			}
		}

		[BurstCompile]
		private struct ComputeSweptVolumeIntersectionJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<Obb> carsObb;

			[ReadOnly]
			public NativeArray<Obb> sweptVolumeObb;

			[ReadOnly]
			public int segmentCount;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					ResolveFlag flag = resolveMap[pairIndex];
					if (flag.Has(ResolveFlag.IsRapprochement) && !flag.Has(ResolveFlag.HasPredictedIntersection) && (flag.Has(ResolveFlag.HasIntersection) || IsCarIntersectAnySweptVolume(num, num2) || IsCarIntersectAnySweptVolume(num2, num)))
					{
						resolveMap[pairIndex] = flag.Add(ResolveFlag.HasPredictedIntersection);
					}
				}
			}

			private bool IsCarIntersectAnySweptVolume(int localSlotId, int otherSlotId)
			{
				Obb b = carsObb[otherSlotId];
				for (int num = segmentCount - 1; num >= 0; num--)
				{
					int index = localSlotId * segmentCount + num;
					if (ObbIntersectionTool.IsIntersects(sweptVolumeObb[index], in b))
					{
						return true;
					}
				}
				return false;
			}
		}

		[BurstCompile]
		private struct ComputeTwoWayCollideRestrictionsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<ResolveFlag> currentResolveMap;

			[ReadOnly]
			public NativeArray<int> currentInitiators;

			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			[WriteOnly]
			public NativeArray<bool> currentCollideRestrictions;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				currentCollideRestrictions[num] = false;
				for (int j = 0; j < activeSlotsCount; j++)
				{
					int otherSlotId = activeSlots[j];
					if (HasAnyCollideRestriction(num, otherSlotId))
					{
						currentCollideRestrictions[num] = true;
					}
				}
			}

			private bool HasAnyCollideRestriction(int localSlotId, int otherSlotId)
			{
				if (localSlotId == otherSlotId)
				{
					return false;
				}
				int pairIndex = PairMath.GetPairIndex(localSlotId, otherSlotId, maxSlots);
				ResolveFlag flag = currentResolveMap[pairIndex];
				if (flag.Has(ResolveFlag.HasCollideGroup) || flag.Has(ResolveFlag.HighVelocityDiff) || !flag.Has(ResolveFlag.HasIntersection))
				{
					return false;
				}
				if (currentInitiators[pairIndex] == localSlotId && !flag.Has(ResolveFlag.HasCollision))
				{
					return true;
				}
				if (currentInitiators[pairIndex] == -1 || flag.Has(ResolveFlag.FirstInitialize))
				{
					return true;
				}
				return false;
			}
		}

		[BurstCompile]
		private struct BuildMovingGroupsJob : IJob
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			[NativeDisableParallelForRestriction]
			public NativeArray<ushort> adjacency;

			public void Execute()
			{
				for (int i = 0; i < maxSlots; i++)
				{
					adjacency[i] = 0;
				}
				for (int j = 0; j < activeSlotsCount; j++)
				{
					int num = activeSlots[j];
					for (int k = j + 1; k < activeSlotsCount; k++)
					{
						int num2 = activeSlots[k];
						if (IsEqualMoving(num, num2))
						{
							adjacency[num] |= (ushort)(1 << num2);
							adjacency[num2] |= (ushort)(1 << num);
						}
					}
				}
				for (int l = 0; l < activeSlotsCount; l++)
				{
					int num3 = activeSlots[l];
					for (int m = 0; m < activeSlotsCount; m++)
					{
						int num4 = activeSlots[m];
						if (IsAdjacency(num3, num4))
						{
							adjacency[num4] |= adjacency[num3];
						}
					}
				}
				for (int n = 0; n < activeSlotsCount; n++)
				{
					int num5 = activeSlots[n];
					for (int num6 = n + 1; num6 < activeSlotsCount; num6++)
					{
						int num7 = activeSlots[num6];
						int pairIndex = PairMath.GetPairIndex(num5, num7, maxSlots);
						if (IsAdjacency(num5, num7))
						{
							resolveMap[pairIndex] = resolveMap[pairIndex].Add(ResolveFlag.EqualMovingGroup);
						}
					}
				}
			}

			private bool IsEqualMoving(int localSlotId, int otherSlotId)
			{
				int pairIndex = PairMath.GetPairIndex(localSlotId, otherSlotId, maxSlots);
				ResolveFlag flag = resolveMap[pairIndex];
				if (flag.Has(ResolveFlag.HasHighSpeed) && !flag.Has(ResolveFlag.HighVelocityDiff))
				{
					return !flag.Has(ResolveFlag.LargeDistance);
				}
				return false;
			}

			private bool IsAdjacency(int localSlotId, int otherSlotId)
			{
				return (adjacency[otherSlotId] & (1 << localSlotId)) != 0;
			}
		}

		private DynamicCollisionsConfig m_config;

		public NarrowPhaseCollision(DynamicCollisionsConfig config)
		{
			m_config = config;
		}

		public JobHandle Process(JobHandle dependency, in DataState currentState, in DataState previousState)
		{
			dependency = IJobParallelForExtensions.Schedule(new ComputeDirectIntersectionJob
			{
				activeSlots = currentState.activeSlots,
				carsObb = currentState.carsObb,
				resolveMap = currentState.resolveMap,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new BuildVelocitySweptObbJob
			{
				activeSlots = currentState.activeSlots,
				currentCarsMeta = currentState.carsMeta,
				previousCarsMeta = previousState.carsMeta,
				currentCarsObb = currentState.carsObb,
				segmentCount = m_config.velocitySweptSegmentCount,
				velocityDiffThresholdSqr = m_config.velocityDiffThreshold * m_config.velocityDiffThreshold,
				maxDistanceBySpeedCurve = m_config.GetNativeDistanceLimitBySpeedCurve(),
				velocitySweptObb = currentState.velocitySweptObb
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ComputeSweptVolumeIntersectionJob
			{
				activeSlots = currentState.activeSlots,
				carsObb = currentState.carsObb,
				sweptVolumeObb = currentState.velocitySweptObb,
				resolveMap = currentState.resolveMap,
				segmentCount = m_config.velocitySweptSegmentCount,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ClearInitiatorsJob
			{
				array = currentState.initiators
			}, currentState.initiators.Length, 64, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ComputeInitiatorsJob
			{
				currentInitiators = currentState.initiators,
				previousInitiators = previousState.initiators,
				activeSlots = currentState.activeSlots,
				currentCarsMeta = currentState.carsMeta,
				currentCarsObb = currentState.carsObb,
				previousCarsObb = previousState.carsObb,
				currentResolveMap = currentState.resolveMap,
				previousResolveMap = previousState.resolveMap,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ComputeTwoWayInitiatorsJob
			{
				currentInitiators = currentState.initiators,
				activeSlots = currentState.activeSlots,
				currentResolveMap = currentState.resolveMap,
				previousResolveMap = previousState.resolveMap,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ComputeTwoWayCollideRestrictionsJob
			{
				currentInitiators = currentState.initiators,
				activeSlots = currentState.activeSlots,
				currentResolveMap = currentState.resolveMap,
				currentCollideRestrictions = currentState.collideRestrictions,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount
			}, currentState.activeCarsCount, 1, dependency);
			dependency = new BuildMovingGroupsJob
			{
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				resolveMap = currentState.resolveMap,
				adjacency = currentState.adjacency
			}.Schedule(dependency);
			return dependency;
		}
	}
}
