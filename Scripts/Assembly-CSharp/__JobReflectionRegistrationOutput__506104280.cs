using System;
using SyncMultiplayer.DynamicCollisions.Stages;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

[Unity.Jobs.DOTSCompilerGenerated]
internal class __JobReflectionRegistrationOutput__506104280
{
	public static void CreateJobReflectionData()
	{
		try
		{
			IJobParallelForDeferExtensions.EarlyJobInit<FlareSystem.UpdateVertexBufferJob>();
			IJobExtensions.EarlyJobInit<FlareSystem.LightsPreparingJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<LightControlSystem.RefreshJob>();
			IJobParallelForExtensions.EarlyJobInit<BroadphaseCollision.ClearResolveMapJob>();
			IJobParallelForExtensions.EarlyJobInit<BroadphaseCollision.DefineFirstInitializePairsJob>();
			IJobParallelForExtensions.EarlyJobInit<BroadphaseCollision.ComputeQuickResolveMapJob>();
			IJobParallelForExtensions.EarlyJobInit<BroadphaseCollision.DefineUnstableMovementJob>();
			IJobParallelForExtensions.EarlyJobInit<CarColliderCollector.BuildBoundsJob>();
			IJobParallelForExtensions.EarlyJobInit<CarColliderCollector.TransferBoundsToObbJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<CarObbBinder.UpdateObbTransformJob>();
			IJobParallelForExtensions.EarlyJobInit<CarObbBinder.ComputeObbPredictionJob>();
			IJobParallelForExtensions.EarlyJobInit<CollisionResolver.CopyRealCollideStatesJob>();
			IJobParallelForExtensions.EarlyJobInit<CollisionResolver.ResolveCollisionsJob>();
			IJobParallelForExtensions.EarlyJobInit<CollisionResolver.ResolvePairedCollisionsJob>();
			IJobParallelForExtensions.EarlyJobInit<CollisionResolver.DefineDifferedCollisionsJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ClearInitiatorsJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ComputeInitiatorsJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ComputeTwoWayInitiatorsJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ComputeDirectIntersectionJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.BuildVelocitySweptObbJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ComputeSweptVolumeIntersectionJob>();
			IJobParallelForExtensions.EarlyJobInit<NarrowPhaseCollision.ComputeTwoWayCollideRestrictionsJob>();
			IJobExtensions.EarlyJobInit<NarrowPhaseCollision.BuildMovingGroupsJob>();
		}
		catch (Exception ex)
		{
			EarlyInitHelpers.JobReflectionDataCreationFailed(ex);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static void EarlyInit()
	{
		CreateJobReflectionData();
	}
}
