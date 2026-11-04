using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Collections;

namespace SyncMultiplayer.DynamicCollisions.Tools
{
	public class DataStateHolder
	{
		protected DataState m_previous;

		protected DataState m_current;

		public DataStateHolder(DynamicCollisionsConfig config)
		{
			AllocateState(config, out m_previous);
			AllocateState(config, out m_current);
		}

		~DataStateHolder()
		{
			ReleaseState(ref m_previous);
			ReleaseState(ref m_current);
		}

		public ref DataState GetCurrentState()
		{
			return ref m_current;
		}

		public ref DataState GetPreviousState()
		{
			return ref m_previous;
		}

		public virtual void ShiftStates()
		{
			CopyState(in m_current, ref m_previous);
		}

		protected void CopyState(in DataState source, ref DataState destination)
		{
			destination.resolveMap.CopyFrom(source.resolveMap);
			destination.desiredCollideStates.CopyFrom(source.desiredCollideStates);
			destination.commitedCollideStates.CopyFrom(source.commitedCollideStates);
			destination.lockCollideStates.CopyFrom(source.lockCollideStates);
			destination.submitCollideStates.CopyFrom(source.submitCollideStates);
			destination.initiators.CopyFrom(source.initiators);
			destination.collideRestrictions.CopyFrom(source.collideRestrictions);
			destination.adjacency.CopyFrom(source.adjacency);
			destination.activeSlots.CopyFrom(in source.activeSlots);
			destination.carsMeta.CopyFrom(source.carsMeta);
			destination.carsRawTransforms.CopyFrom(source.carsRawTransforms);
			destination.carsObb.CopyFrom(source.carsObb);
			destination.velocitySweptObb.CopyFrom(source.velocitySweptObb);
			destination.activeCarsCount = source.activeCarsCount;
			destination.maxCarsCount = source.maxCarsCount;
		}

		protected void AllocateState(DynamicCollisionsConfig config, out DataState state)
		{
			state = default(DataState);
			state.resolveMap = new NativeArray<ResolveFlag>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.desiredCollideStates = new NativeArray<bool>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.commitedCollideStates = new NativeArray<bool>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.lockCollideStates = new NativeArray<bool>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.submitCollideStates = new NativeArray<bool>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.initiators = new NativeArray<int>(PairMath.GetPairsCount(config.maxCars), Allocator.Persistent);
			state.collideRestrictions = new NativeArray<bool>(config.maxCars, Allocator.Persistent);
			state.adjacency = new NativeArray<ushort>(config.maxCars, Allocator.Persistent);
			state.activeSlots = new NativeList<int>(config.maxCars, Allocator.Persistent);
			state.carsMeta = new NativeArray<CarMeta>(config.maxCars, Allocator.Persistent);
			state.carsRawTransforms = new NativeArray<Obb>(config.maxCars, Allocator.Persistent);
			state.carsObb = new NativeArray<Obb>(config.maxCars, Allocator.Persistent);
			state.velocitySweptObb = new NativeArray<Obb>(config.maxCars * config.velocitySweptSegmentCount, Allocator.Persistent);
			state.maxCarsCount = config.maxCars;
		}

		protected void ReleaseState(ref DataState state)
		{
			state.resolveMap.Dispose();
			state.desiredCollideStates.Dispose();
			state.commitedCollideStates.Dispose();
			state.lockCollideStates.Dispose();
			state.submitCollideStates.Dispose();
			state.initiators.Dispose();
			state.collideRestrictions.Dispose();
			state.adjacency.Dispose();
			state.activeSlots.Dispose();
			state.carsMeta.Dispose();
			state.carsRawTransforms.Dispose();
			state.carsObb.Dispose();
			state.velocitySweptObb.Dispose();
		}
	}
}
