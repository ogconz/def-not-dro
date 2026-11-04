using SyncMultiplayer.DynamicCollisions.Structs;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class CarInfoCollector
	{
		private UserRegedit m_userRegedit;

		private UserCarHolder m_userCarHolder;

		public CarInfoCollector(UserRegedit userRegedit, UserCarHolder userCarHolder)
		{
			m_userRegedit = userRegedit;
			m_userCarHolder = userCarHolder;
		}

		public void Process(ref DataState state)
		{
			for (int i = 0; i < state.activeCarsCount; i++)
			{
				int slotId = state.activeSlots[i];
				FillMeta(m_userRegedit.GetUserBySlotId(slotId), ref state);
			}
		}

		private void FillMeta(NetworkPlayer player, ref DataState state)
		{
			int slotID = player.SlotID;
			CarMeta value = state.carsMeta[slotID];
			RaceCar car = m_userCarHolder.GetCar(slotID);
			if (!IsCarInvalid(car))
			{
				value.isCurrentUser = slotID == m_userRegedit.GetLocalUser().SlotID;
				value.isCarActive = car.carX.GetCarActive();
				value.velocity = car.carX.velocity;
				value.angularVelocity = car.carX.angularVelocity;
				value.collideGroupHash = ((!string.IsNullOrEmpty(player.collideGroup)) ? player.collideGroup.GetHashCode() : 0);
				state.carsMeta[slotID] = value;
			}
		}

		private bool IsCarInvalid(RaceCar car)
		{
			if (!BaseCar.IsNull(car) && !car.IsPhysicsBroken())
			{
				return !car.carX.isCreated;
			}
			return true;
		}
	}
}
