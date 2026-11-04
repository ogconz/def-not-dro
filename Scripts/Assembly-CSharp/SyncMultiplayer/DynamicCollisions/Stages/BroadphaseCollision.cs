using SyncMultiplayer.DynamicCollisions.Structs;
using SyncMultiplayer.DynamicCollisions.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class BroadphaseCollision
	{
		[BurstCompile]
		private struct ClearResolveMapJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			[WriteOnly]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				int first = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int second = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(first, second, maxSlots);
					resolveMap[pairIndex] = ResolveFlag.None;
				}
			}
		}

		[BurstCompile]
		private struct DefineFirstInitializePairsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int currentActiveSlotsCount;

			[ReadOnly]
			public int previousActiveSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				i += previousActiveSlotsCount;
				int num = activeSlots[i];
				for (int j = 0; j < currentActiveSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					if (num != num2)
					{
						int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
						resolveMap[pairIndex] = resolveMap[pairIndex].Add(ResolveFlag.FirstInitialize);
					}
				}
			}
		}

		[BurstCompile]
		private struct ComputeQuickResolveMapJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<bool> commitedCollideStates;

			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<CarMeta> carsMeta;

			[ReadOnly]
			public NativeArray<Obb> currentCarsObb;

			[ReadOnly]
			public NativeArray<Obb> previousCarsObb;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[ReadOnly]
			public NativeCurve distanceLimitBySpeedCurve;

			[ReadOnly]
			public float maxDeltaVelocitySqr;

			[ReadOnly]
			public float velocityDiffThresholdSqr;

			[ReadOnly]
			public float cosAngleOfApproach;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				CarMeta carMeta = carsMeta[num];
				Obb obb = currentCarsObb[num];
				float x = distanceLimitBySpeedCurve.Evaluate(math.length(carMeta.velocity));
				bool flag = IsObbChangedPosition(num);
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					CarMeta carMeta2 = carsMeta[num2];
					Obb obb2 = currentCarsObb[num2];
					float y = distanceLimitBySpeedCurve.Evaluate(math.length(carMeta2.velocity));
					float num3 = math.max(x, y);
					bool flag2 = IsObbChangedPosition(num2);
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					float3 float5 = obb2.center - obb.center;
					if (math.lengthsq(float5) > num3 * num3)
					{
						AddFlag(in pairIndex, ResolveFlag.LargeDistance);
					}
					if (carMeta.collideGroupHash == carMeta2.collideGroupHash)
					{
						AddFlag(in pairIndex, ResolveFlag.EqualCollideGroup);
					}
					if (carMeta.collideGroupHash != 0 || carMeta2.collideGroupHash != 0)
					{
						AddFlag(in pairIndex, ResolveFlag.HasCollideGroup);
					}
					if (commitedCollideStates[pairIndex])
					{
						AddFlag(in pairIndex, ResolveFlag.HasCollision);
					}
					if (carMeta.isCarActive && carMeta2.isCarActive)
					{
						AddFlag(in pairIndex, ResolveFlag.HasPhysics);
					}
					if (math.lengthsq(carMeta.velocity) > maxDeltaVelocitySqr && math.lengthsq(carMeta2.velocity) > maxDeltaVelocitySqr)
					{
						AddFlag(in pairIndex, ResolveFlag.HasHighSpeed);
					}
					if (flag || flag2)
					{
						AddFlag(in pairIndex, ResolveFlag.IsMoving);
					}
					float3 x2 = carMeta2.velocity - carMeta.velocity;
					float num4 = math.lengthsq(x2);
					if (!(num4 < velocityDiffThresholdSqr))
					{
						AddFlag(in pairIndex, ResolveFlag.IsMoving);
						if (num4 > maxDeltaVelocitySqr)
						{
							AddFlag(in pairIndex, ResolveFlag.HighVelocityDiff);
						}
						float num5 = math.dot(x2, float5);
						float num6 = math.length(x2) * math.length(float5);
						if (num5 < num6 * (0f - cosAngleOfApproach))
						{
							AddFlag(in pairIndex, ResolveFlag.IsRapprochement);
						}
					}
				}
			}

			private bool IsObbChangedPosition(int slotId)
			{
				return math.lengthsq(currentCarsObb[slotId].center - previousCarsObb[slotId].center) > velocityDiffThresholdSqr;
			}

			private void AddFlag(in int pairIndex, in ResolveFlag flag)
			{
				resolveMap[pairIndex] = resolveMap[pairIndex].Add(in flag);
			}
		}

		[BurstCompile]
		private struct DefineUnstableMovementJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public NativeArray<CarMeta> carsMeta;

			[ReadOnly]
			public NativeArray<Obb> currentCarsObb;

			[ReadOnly]
			public NativeArray<Obb> previousCarsObb;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[ReadOnly]
			public float deltaTimeSqr;

			[ReadOnly]
			public float velocityDiffThresholdSqr;

			[ReadOnly]
			public float unstableMovementDiffThresholdSqr;

			[NativeDisableParallelForRestriction]
			public NativeArray<ResolveFlag> resolveMap;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				bool flag = IsMovementStable(num);
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					ResolveFlag flag2 = resolveMap[pairIndex];
					if (flag2.Has(ResolveFlag.IsMoving) && (!flag || !IsMovementStable(num2)))
					{
						resolveMap[pairIndex] = flag2.Add(ResolveFlag.UnstableMovement);
					}
				}
			}

			private bool IsMovementStable(int slotId)
			{
				float num = math.lengthsq(currentCarsObb[slotId].center - previousCarsObb[slotId].center);
				float num2 = math.lengthsq(carsMeta[slotId].velocity);
				if (num < velocityDiffThresholdSqr || num2 < velocityDiffThresholdSqr)
				{
					return true;
				}
				float num3 = num2 * deltaTimeSqr * unstableMovementDiffThresholdSqr;
				return num <= num3;
			}
		}

		private DynamicCollisionsConfig m_config;

		public BroadphaseCollision(DynamicCollisionsConfig config)
		{
			m_config = config;
		}

		public JobHandle Process(JobHandle dependency, in DataState currentState, in DataState previousState)
		{
			dependency = IJobParallelForExtensions.Schedule(new ClearResolveMapJob
			{
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				resolveMap = currentState.resolveMap
			}, currentState.activeCarsCount, currentState.maxCarsCount, dependency);
			int num = math.clamp(currentState.activeCarsCount - previousState.activeCarsCount, 0, currentState.maxCarsCount);
			if (num > 0 && currentState.activeCarsCount > 1)
			{
				dependency = IJobParallelForExtensions.Schedule(new DefineFirstInitializePairsJob
				{
					activeSlots = currentState.activeSlots,
					currentActiveSlotsCount = currentState.activeCarsCount,
					previousActiveSlotsCount = previousState.activeCarsCount,
					maxSlots = currentState.maxCarsCount,
					resolveMap = currentState.resolveMap
				}, num, currentState.maxCarsCount, dependency);
			}
			dependency = IJobParallelForExtensions.Schedule(new ComputeQuickResolveMapJob
			{
				commitedCollideStates = currentState.commitedCollideStates,
				activeSlots = currentState.activeSlots,
				carsMeta = currentState.carsMeta,
				currentCarsObb = currentState.carsRawTransforms,
				previousCarsObb = previousState.carsRawTransforms,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				distanceLimitBySpeedCurve = m_config.GetNativeDistanceLimitBySpeedCurve(),
				maxDeltaVelocitySqr = m_config.GetConvertedMaxDeltaVelocity() * m_config.GetConvertedMaxDeltaVelocity(),
				velocityDiffThresholdSqr = m_config.velocityDiffThreshold * m_config.velocityDiffThreshold,
				cosAngleOfApproach = math.cos(math.radians(m_config.angleOfApproach)),
				resolveMap = currentState.resolveMap
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new DefineUnstableMovementJob
			{
				activeSlots = currentState.activeSlots,
				carsMeta = currentState.carsMeta,
				currentCarsObb = currentState.carsRawTransforms,
				previousCarsObb = previousState.carsRawTransforms,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				deltaTimeSqr = Time.deltaTime * Time.deltaTime,
				velocityDiffThresholdSqr = m_config.velocityDiffThreshold * m_config.velocityDiffThreshold,
				unstableMovementDiffThresholdSqr = m_config.unstableMovementDiffThreshold * m_config.unstableMovementDiffThreshold,
				resolveMap = currentState.resolveMap
			}, currentState.activeCarsCount, 1, dependency);
			return dependency;
		}
	}
}
