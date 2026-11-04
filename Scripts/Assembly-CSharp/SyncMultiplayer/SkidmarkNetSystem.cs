namespace SyncMultiplayer
{
	public class SkidmarkNetSystem : BaseNetGameSystem
	{
		public override void DoPlayerEnter(NetworkPlayer player)
		{
			base.DoPlayerEnter(player);
			UpdateAllPlayerSkidmark();
		}

		public override void PostPlayerLeave(NetworkPlayer player)
		{
			base.PostPlayerLeave(player);
			UpdateAllPlayerSkidmark();
		}

		private void UpdateAllPlayerSkidmark()
		{
			foreach (NetworkPlayer player in base.owner.host.Players)
			{
				if (player != null && !BaseCar.IsNull(player.userCar))
				{
					player.userCar.OnSkidmarkChangeHandler();
				}
			}
		}
	}
}
