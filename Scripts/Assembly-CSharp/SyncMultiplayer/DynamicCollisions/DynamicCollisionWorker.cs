using System;
using SyncMultiplayer.DynamicCollisions.Stages;
using SyncMultiplayer.DynamicCollisions.Structs;
using SyncMultiplayer.DynamicCollisions.Tools;
using SyncMultiplayer.Utils;
using Unity.Jobs;

namespace SyncMultiplayer.DynamicCollisions
{
	public class DynamicCollisionWorker
	{
		private DataStateHolder m_stateHolder;

		private UserRegedit m_userRegedit;

		private UserCarHolder m_userCarHolder;

		private CarSlotRegedit m_carSlotRegedit;

		private CarInfoCollector m_carInfoCollector;

		private CarColliderCollector m_colliderCollector;

		private CarObbBinder m_carObbBinder;

		private BroadphaseCollision m_broadphaseCollision;

		private NarrowPhaseCollision m_narrowPhaseCollision;

		private CollisionResolver m_collisionResolver;

		private JobHandle m_jobHandle;

		public UserRegedit userRegedit => m_userRegedit;

		public DynamicCollisionWorker(NetGameSystemsManager netSystems, DynamicCollisionMapHolder collisionMapHolder)
		{
			DynamicCollisionsConfig instance = ScriptableSingleton<DynamicCollisionsConfig>.instance;
			m_stateHolder = new DataStateHolder(instance);
			m_userRegedit = new UserRegedit(instance, netSystems);
			m_userCarHolder = new UserCarHolder(instance, m_userRegedit);
			m_carSlotRegedit = new CarSlotRegedit(instance);
			m_carInfoCollector = new CarInfoCollector(m_userRegedit, m_userCarHolder);
			m_colliderCollector = new CarColliderCollector(instance);
			m_carObbBinder = new CarObbBinder(instance);
			m_broadphaseCollision = new BroadphaseCollision(instance);
			m_narrowPhaseCollision = new NarrowPhaseCollision(instance);
			m_collisionResolver = new CollisionResolver(instance, collisionMapHolder);
			UserCarHolder userCarHolder = m_userCarHolder;
			userCarHolder.onCarCreated = (Action<RaceCar, NetworkPlayer>)Delegate.Combine(userCarHolder.onCarCreated, new Action<RaceCar, NetworkPlayer>(OnCarCreatedHandler));
			UserCarHolder userCarHolder2 = m_userCarHolder;
			userCarHolder2.onCarRemoved = (Action<RaceCar, NetworkPlayer>)Delegate.Combine(userCarHolder2.onCarRemoved, new Action<RaceCar, NetworkPlayer>(OnCarRemovedHandler));
		}

		~DynamicCollisionWorker()
		{
			UserCarHolder userCarHolder = m_userCarHolder;
			userCarHolder.onCarCreated = (Action<RaceCar, NetworkPlayer>)Delegate.Remove(userCarHolder.onCarCreated, new Action<RaceCar, NetworkPlayer>(OnCarCreatedHandler));
			UserCarHolder userCarHolder2 = m_userCarHolder;
			userCarHolder2.onCarRemoved = (Action<RaceCar, NetworkPlayer>)Delegate.Remove(userCarHolder2.onCarRemoved, new Action<RaceCar, NetworkPlayer>(OnCarRemovedHandler));
			m_stateHolder = null;
			m_userRegedit = null;
			m_userCarHolder = null;
			m_carSlotRegedit = null;
			m_carInfoCollector = null;
			m_colliderCollector = null;
			m_carObbBinder = null;
			m_broadphaseCollision = null;
			m_narrowPhaseCollision = null;
			m_collisionResolver = null;
		}

		public void Update()
		{
			ref DataState currentState = ref m_stateHolder.GetCurrentState();
			ref DataState previousState = ref m_stateHolder.GetPreviousState();
			m_jobHandle.Complete();
			m_jobHandle = default(JobHandle);
			m_collisionResolver.ApplyCollisions(in currentState);
			m_stateHolder.ShiftStates();
			m_userCarHolder.Process();
			m_carSlotRegedit.ProcessQueue(ref currentState);
			m_carInfoCollector.Process(ref currentState);
			m_colliderCollector.CollectRequests();
			m_collisionResolver.CollectCollisions(ref currentState);
			m_jobHandle = m_colliderCollector.Process(m_jobHandle, in currentState);
			m_jobHandle = m_carObbBinder.Process(m_jobHandle, in currentState);
			m_jobHandle = m_broadphaseCollision.Process(m_jobHandle, in currentState, in previousState);
			m_jobHandle = m_narrowPhaseCollision.Process(m_jobHandle, in currentState, in previousState);
			m_jobHandle = m_collisionResolver.Process(m_jobHandle, in currentState, in previousState);
		}

		public void AddPlayer(NetworkPlayer player)
		{
			m_userRegedit.BindPlayer(player);
		}

		public void RemovePlayer(NetworkPlayer player)
		{
			m_userRegedit.ReleasePlayer(player);
		}

		private void OnCarCreatedHandler(RaceCar car, NetworkPlayer player)
		{
			m_carSlotRegedit.BindPlayer(player);
			m_colliderCollector.CreateRequest(player);
			m_carObbBinder.BindPlayerCar(player, car);
		}

		private void OnCarRemovedHandler(RaceCar car, NetworkPlayer player)
		{
			m_carSlotRegedit.ReleasePlayer(player);
			m_colliderCollector.RemoveRequest(player);
			m_carObbBinder.ReleasePlayerCar(player);
		}
	}
}
