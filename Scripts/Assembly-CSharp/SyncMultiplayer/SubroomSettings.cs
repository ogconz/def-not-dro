namespace SyncMultiplayer
{
	public class SubroomSettings
	{
		public SubroomMode subroomMode;

		public int playersCount;

		public string pointName;

		public SubroomFlags xdsFlags;

		public void SetDefaults()
		{
			subroomMode = SubroomMode.Challenge;
			playersCount = 2;
			pointName = string.Empty;
			xdsFlags = SubroomFlags.None;
		}
	}
}
