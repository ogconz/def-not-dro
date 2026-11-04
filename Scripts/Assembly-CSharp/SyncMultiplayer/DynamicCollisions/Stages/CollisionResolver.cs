using SyncMultiplayer.DynamicCollisions.Structs;
using SyncMultiplayer.DynamicCollisions.Tools;
using SyncMultiplayer.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class CollisionResolver
	{
		[BurstCompile]
		private struct CopyRealCollideStatesJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempDesiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempLockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempSubmitCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realDesiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realLockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realSubmitCollideStates;

			public void Execute(int i)
			{
				int first = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int second = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(first, second, maxSlots);
					tempDesiredCollideStates[pairIndex] = realDesiredCollideStates[pairIndex];
					tempLockCollideStates[pairIndex] = realLockCollideStates[pairIndex];
					tempSubmitCollideStates[pairIndex] = realSubmitCollideStates[pairIndex];
				}
			}
		}

		[BurstCompile]
		private struct ResolveCollisionsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<ResolveFlag> currentResolveMap;

			[ReadOnly]
			public NativeArray<ResolveFlag> previousResolveMap;

			[ReadOnly]
			public NativeArray<int> currentInitiators;

			[ReadOnly]
			public NativeArray<int> previousInitiators;

			[ReadOnly]
			public NativeArray<bool> currentCollideRestrictions;

			[ReadOnly]
			public NativeArray<bool> previousCollideRestrictions;

			[ReadOnly]
			public NativeArray<CarMeta> carsMeta;

			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> desiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> commitedCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> lockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> submitCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> applyCollisionDelay;

			public void Execute(int i)
			{
				int num = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int num2 = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(num, num2, maxSlots);
					ResolveFlag resolveFlag = currentResolveMap[pairIndex];
					ResolveFlag resolveFlag2 = previousResolveMap[pairIndex];
					ResolveFlag flag = resolveFlag ^ resolveFlag2;
					FilterDifference(ref flag);
					if (resolveFlag.Has(ResolveFlag.HasCollideGroup))
					{
						if (flag.Has(ResolveFlag.HasCollideGroup))
						{
							if (!resolveFlag.Has(ResolveFlag.EqualCollideGroup))
							{
								SetCollisionState(pairIndex, num2, requireCollision: false);
							}
							SetSubmitColliding(pairIndex, value: true);
							RemoveCollideLock(pairIndex);
						}
						continue;
					}
					SetSubmitColliding(pairIndex, value: false);
					if (!resolveFlag.Has(ResolveFlag.HasPhysics))
					{
						SetCollisionState(pairIndex, num2, requireCollision: false);
						continue;
					}
					if (flag.Has(ResolveFlag.EqualCollideGroup) && !resolveFlag.Has(ResolveFlag.EqualCollideGroup))
					{
						SetCollisionState(pairIndex, num2, requireCollision: false);
						RemoveCollideLock(pairIndex);
						continue;
					}
					if (resolveFlag.Has(ResolveFlag.UnstableMovement))
					{
						SetCollisionState(pairIndex, num2, requireCollision: false);
						continue;
					}
					if (flag.Has(ResolveFlag.UnstableMovement))
					{
						AddCollisionDelay(pairIndex);
					}
					if (currentCollideRestrictions[num2] || currentCollideRestrictions[num])
					{
						SetCollisionState(pairIndex, num2, requireCollision: false);
						continue;
					}
					if (previousCollideRestrictions[num2] || previousCollideRestrictions[num])
					{
						RemoveCollideLock(pairIndex);
						AddCollisionDelay(pairIndex);
					}
					if (TryGetCurrentUser(num, num2, out var currentUserIndex, out var oppositeUserIndex) && HasAnyDisabledCollision(oppositeUserIndex, currentUserIndex))
					{
						SetCollisionState(pairIndex, oppositeUserIndex, requireCollision: false);
						continue;
					}
					if (resolveFlag.Has(ResolveFlag.LargeDistance))
					{
						SetCollisionState(pairIndex, num2, requireCollision: true);
						RemoveCollideLock(pairIndex);
						continue;
					}
					int num3 = (resolveFlag.Has(ResolveFlag.HasIntersection) ? currentInitiators[pairIndex] : previousInitiators[pairIndex]);
					int num4 = ((num3 == num) ? num2 : num);
					if (num3 != -1)
					{
						if (resolveFlag.Has(ResolveFlag.HighVelocityDiff) && resolveFlag.Has(ResolveFlag.HasPredictedIntersection))
						{
							SetCollisionState(pairIndex, num4, requireCollision: false);
							continue;
						}
						if (resolveFlag.Has(ResolveFlag.HasIntersection) && !resolveFlag.Has(ResolveFlag.HasCollision))
						{
							if (!currentCollideRestrictions[num4])
							{
								SetCollisionState(pairIndex, num4, requireCollision: false);
							}
							continue;
						}
					}
					bool flag2 = flag.Has(ResolveFlag.FirstInitialize);
					if (!flag2)
					{
						flag2 = flag.Has(ResolveFlag.HasIntersection) && !resolveFlag.Has(ResolveFlag.HasCollision) && !resolveFlag.Has(ResolveFlag.HighVelocityDiff);
					}
					if (!flag2)
					{
						flag2 = flag.Has(ResolveFlag.HighVelocityDiff) && resolveFlag.Has(ResolveFlag.HasPredictedIntersection) && !resolveFlag.Has(ResolveFlag.HasHighSpeed);
					}
					if (flag2)
					{
						AddCollisionDelay(pairIndex);
					}
					SetCollisionState(pairIndex, num2, requireCollision: true);
					RemoveCollideLock(pairIndex);
					bool value = resolveFlag.Has(ResolveFlag.HasIntersection);
					SetSubmitColliding(pairIndex, value);
				}
			}

			private void FilterDifference(ref ResolveFlag flag)
			{
				flag &= ~ResolveFlag.IsRapprochement;
				flag &= ~ResolveFlag.IsMoving;
			}

			private void SetCollisionState(int pairIndex, int slotForCheckRestrictions, bool requireCollision)
			{
				if (!requireCollision || !currentCollideRestrictions[slotForCheckRestrictions])
				{
					desiredCollideStates[pairIndex] = requireCollision;
					lockCollideStates[pairIndex] = true;
				}
			}

			private void AddCollisionDelay(int pairIndex)
			{
				applyCollisionDelay[pairIndex] = true;
			}

			private void RemoveCollideLock(int pairIndex)
			{
				lockCollideStates[pairIndex] = false;
			}

			private void SetSubmitColliding(int pairIndex, bool value)
			{
				submitCollideStates[pairIndex] = value;
			}

			private bool TryGetCurrentUser(int localSlotId, int otherSlotId, out int currentUserIndex, out int oppositeUserIndex)
			{
				currentUserIndex = -1;
				oppositeUserIndex = -1;
				bool isCurrentUser = carsMeta[localSlotId].isCurrentUser;
				bool isCurrentUser2 = carsMeta[otherSlotId].isCurrentUser;
				bool result = isCurrentUser || isCurrentUser2;
				if (isCurrentUser)
				{
					currentUserIndex = localSlotId;
					oppositeUserIndex = otherSlotId;
				}
				if (isCurrentUser2)
				{
					currentUserIndex = otherSlotId;
					oppositeUserIndex = localSlotId;
				}
				return result;
			}

			private bool HasAnyDisabledCollision(int localSlotId, int ignoredSlotId)
			{
				for (int i = 0; i < activeSlotsCount; i++)
				{
					int num = activeSlots[i];
					if (localSlotId == num || num == ignoredSlotId)
					{
						continue;
					}
					int pairIndex = PairMath.GetPairIndex(localSlotId, num, maxSlots);
					ResolveFlag flag = currentResolveMap[pairIndex];
					if (flag.Has(ResolveFlag.HasCollideGroup) || !flag.Has(ResolveFlag.EqualCollideGroup) || flag.Has(ResolveFlag.LargeDistance))
					{
						continue;
					}
					if (!flag.Has(ResolveFlag.EqualMovingGroup))
					{
						bool flag2 = HasAnyMovingGroup(localSlotId, num);
						bool flag3 = HasAnyMovingGroup(num, localSlotId);
						if (flag2 != flag3)
						{
							if (flag2)
							{
								continue;
							}
							if (flag3)
							{
								return true;
							}
						}
					}
					if (currentInitiators[pairIndex] == localSlotId && !commitedCollideStates[pairIndex] && lockCollideStates[pairIndex])
					{
						return true;
					}
				}
				return false;
			}

			private bool HasAnyMovingGroup(int localSlotId, int ignoredSlotId)
			{
				for (int i = 0; i < activeSlotsCount; i++)
				{
					int num = activeSlots[i];
					if (localSlotId != num && num != ignoredSlotId)
					{
						int pairIndex = PairMath.GetPairIndex(localSlotId, num, maxSlots);
						if (currentResolveMap[pairIndex].Has(ResolveFlag.EqualMovingGroup))
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		[BurstCompile]
		private struct ResolvePairedCollisionsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<ResolveFlag> currentResolveMap;

			[ReadOnly]
			public NativeArray<ResolveFlag> previousResolveMap;

			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> desiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> commitedCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> lockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> submitCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> applyCollisionDelay;

			public void Execute(int i)
			{
				int first = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int second = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(first, second, maxSlots);
					ResolveFlag resolveFlag = currentResolveMap[pairIndex];
					ResolveFlag resolveFlag2 = previousResolveMap[pairIndex];
					ResolveFlag flag = resolveFlag ^ resolveFlag2;
					if (!resolveFlag.Has(ResolveFlag.HasCollideGroup) && resolveFlag.Has(ResolveFlag.EqualCollideGroup) && !resolveFlag.Has(ResolveFlag.UnstableMovement) && !resolveFlag2.Has(ResolveFlag.UnstableMovement) && resolveFlag.Has(ResolveFlag.EqualMovingGroup) && (!resolveFlag.Has(ResolveFlag.HighVelocityDiff) || resolveFlag.Has(ResolveFlag.LargeDistance) || !resolveFlag.Has(ResolveFlag.HasPredictedIntersection)))
					{
						bool flag2 = resolveFlag.Has(ResolveFlag.HasCollision);
						bool flag3 = resolveFlag.Has(ResolveFlag.HasIntersection);
						bool flag4 = flag.Has(ResolveFlag.HasIntersection);
						if (!flag3 || commitedCollideStates[pairIndex])
						{
							applyCollisionDelay[pairIndex] = flag4 && !flag2 && desiredCollideStates[pairIndex];
							desiredCollideStates[pairIndex] = true;
							lockCollideStates[pairIndex] = false;
							submitCollideStates[pairIndex] = flag2 && flag3;
						}
					}
				}
			}
		}

		[BurstCompile]
		private struct DefineDifferedCollisionsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeList<int> activeSlots;

			[ReadOnly]
			public int activeSlotsCount;

			[ReadOnly]
			public int maxSlots;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempDesiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempLockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> tempSubmitCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realDesiredCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realLockCollideStates;

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> realSubmitCollideStates;

			[WriteOnly]
			public NativeParallelHashSet<int>.ParallelWriter collisionStateDifferenceQueue;

			[WriteOnly]
			public NativeParallelHashSet<int>.ParallelWriter collisionLockDifferenceQueue;

			[WriteOnly]
			public NativeParallelHashSet<int>.ParallelWriter collisionSubmitDifferenceQueue;

			public void Execute(int i)
			{
				int first = activeSlots[i];
				for (int j = i + 1; j < activeSlotsCount; j++)
				{
					int second = activeSlots[j];
					int pairIndex = PairMath.GetPairIndex(first, second, maxSlots);
					if (tempSubmitCollideStates[pairIndex] != realSubmitCollideStates[pairIndex])
					{
						realSubmitCollideStates[pairIndex] = tempSubmitCollideStates[pairIndex];
						collisionSubmitDifferenceQueue.Add(pairIndex);
						collisionStateDifferenceQueue.Add(pairIndex);
					}
					if (tempDesiredCollideStates[pairIndex] != realDesiredCollideStates[pairIndex])
					{
						realDesiredCollideStates[pairIndex] = tempDesiredCollideStates[pairIndex];
						collisionStateDifferenceQueue.Add(pairIndex);
					}
					if (tempLockCollideStates[pairIndex] != realLockCollideStates[pairIndex])
					{
						realLockCollideStates[pairIndex] = tempLockCollideStates[pairIndex];
						collisionLockDifferenceQueue.Add(pairIndex);
					}
				}
			}
		}

		private DynamicCollisionsConfig m_config;

		private DynamicCollisionMapHolder m_collisionMapHolder;

		private NativeArray<bool> m_tempDesiredCollideStates;

		private NativeArray<bool> m_tempLockCollideStates;

		private NativeArray<bool> m_tempSubmitCollideStates;

		private NativeArray<bool> m_tempApplyCollisionDelay;

		private NativeParallelHashSet<int> m_collisionStateDifferenceQueue;

		private NativeParallelHashSet<int> m_collisionLockDifferenceQueue;

		private NativeParallelHashSet<int> m_collisionSubmitDifferenceQueue;

		public CollisionResolver(DynamicCollisionsConfig config, DynamicCollisionMapHolder collisionMapHolder)
		{
			m_config = config;
			m_collisionMapHolder = collisionMapHolder;
			int pairsCount = PairMath.GetPairsCount(config.maxCars);
			m_tempDesiredCollideStates = new NativeArray<bool>(pairsCount, Allocator.Persistent);
			m_tempLockCollideStates = new NativeArray<bool>(pairsCount, Allocator.Persistent);
			m_tempSubmitCollideStates = new NativeArray<bool>(pairsCount, Allocator.Persistent);
			m_tempApplyCollisionDelay = new NativeArray<bool>(pairsCount, Allocator.Persistent);
			m_collisionStateDifferenceQueue = new NativeParallelHashSet<int>(pairsCount, Allocator.Persistent);
			m_collisionLockDifferenceQueue = new NativeParallelHashSet<int>(pairsCount, Allocator.Persistent);
			m_collisionSubmitDifferenceQueue = new NativeParallelHashSet<int>(pairsCount, Allocator.Persistent);
		}

		~CollisionResolver()
		{
			m_tempDesiredCollideStates.Dispose();
			m_tempLockCollideStates.Dispose();
			m_tempSubmitCollideStates.Dispose();
			m_tempApplyCollisionDelay.Dispose();
			m_collisionStateDifferenceQueue.Dispose();
			m_collisionLockDifferenceQueue.Dispose();
			m_collisionSubmitDifferenceQueue.Dispose();
		}

		public void ApplyCollisions(in DataState state)
		{
			if (m_collisionStateDifferenceQueue.Count() == 0 && m_collisionLockDifferenceQueue.Count() == 0 && m_collisionSubmitDifferenceQueue.Count() == 0)
			{
				return;
			}
			foreach (int item in m_collisionSubmitDifferenceQueue)
			{
				m_collisionMapHolder.rawSubmitCollideStates[item] = state.submitCollideStates[item];
			}
			foreach (int item2 in m_collisionStateDifferenceQueue)
			{
				float suspendAfterChangeTime = (m_tempApplyCollisionDelay[item2] ? m_config.collisionDisableTime : 0f);
				m_collisionMapHolder.SetCollisionStateByPairIndex(item2, state.desiredCollideStates[item2], suspendAfterChangeTime, resetSuspendTime: false);
				m_tempApplyCollisionDelay[item2] = false;
			}
			foreach (int item3 in m_collisionLockDifferenceQueue)
			{
				m_collisionMapHolder.rawLockCollideStates[item3] = state.lockCollideStates[item3];
			}
			m_collisionStateDifferenceQueue.Clear();
			m_collisionLockDifferenceQueue.Clear();
			m_collisionSubmitDifferenceQueue.Clear();
		}

		public void CollectCollisions(ref DataState state)
		{
			state.desiredCollideStates.CopyFrom(m_collisionMapHolder.rawDesiredCollideStates);
			state.commitedCollideStates.CopyFrom(m_collisionMapHolder.rawCommitedCollideStates);
			state.lockCollideStates.CopyFrom(m_collisionMapHolder.rawLockCollideStates);
			state.submitCollideStates.CopyFrom(m_collisionMapHolder.rawSubmitCollideStates);
		}

		public JobHandle Process(JobHandle dependency, in DataState currentState, in DataState previousState)
		{
			dependency = IJobParallelForExtensions.Schedule(new CopyRealCollideStatesJob
			{
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				tempDesiredCollideStates = m_tempDesiredCollideStates,
				tempLockCollideStates = m_tempLockCollideStates,
				tempSubmitCollideStates = m_tempSubmitCollideStates,
				realDesiredCollideStates = currentState.desiredCollideStates,
				realLockCollideStates = currentState.lockCollideStates,
				realSubmitCollideStates = currentState.submitCollideStates
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ResolveCollisionsJob
			{
				currentResolveMap = currentState.resolveMap,
				previousResolveMap = previousState.resolveMap,
				currentInitiators = currentState.initiators,
				previousInitiators = previousState.initiators,
				currentCollideRestrictions = currentState.collideRestrictions,
				previousCollideRestrictions = previousState.collideRestrictions,
				carsMeta = currentState.carsMeta,
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				applyCollisionDelay = m_tempApplyCollisionDelay,
				desiredCollideStates = m_tempDesiredCollideStates,
				commitedCollideStates = currentState.commitedCollideStates,
				lockCollideStates = m_tempLockCollideStates,
				submitCollideStates = m_tempSubmitCollideStates
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new ResolvePairedCollisionsJob
			{
				currentResolveMap = currentState.resolveMap,
				previousResolveMap = previousState.resolveMap,
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				desiredCollideStates = m_tempDesiredCollideStates,
				commitedCollideStates = currentState.commitedCollideStates,
				lockCollideStates = m_tempLockCollideStates,
				submitCollideStates = m_tempSubmitCollideStates,
				applyCollisionDelay = m_tempApplyCollisionDelay
			}, currentState.activeCarsCount, 1, dependency);
			dependency = IJobParallelForExtensions.Schedule(new DefineDifferedCollisionsJob
			{
				activeSlots = currentState.activeSlots,
				activeSlotsCount = currentState.activeCarsCount,
				maxSlots = currentState.maxCarsCount,
				tempDesiredCollideStates = m_tempDesiredCollideStates,
				tempLockCollideStates = m_tempLockCollideStates,
				tempSubmitCollideStates = m_tempSubmitCollideStates,
				realDesiredCollideStates = currentState.desiredCollideStates,
				realLockCollideStates = currentState.lockCollideStates,
				realSubmitCollideStates = currentState.submitCollideStates,
				collisionStateDifferenceQueue = m_collisionStateDifferenceQueue.AsParallelWriter(),
				collisionLockDifferenceQueue = m_collisionLockDifferenceQueue.AsParallelWriter(),
				collisionSubmitDifferenceQueue = m_collisionSubmitDifferenceQueue.AsParallelWriter()
			}, currentState.activeCarsCount, 1, dependency);
			return dependency;
		}
	}
}
