namespace SyncMultiplayer
{
	public struct RoomSettings
	{
		public int trackID;

		public ulong externalId;

		public string password;

		public string roomName;

		public string trackName;

		public byte newFlags;

		public byte oldFlags;

		public bool nightMode;

		public int adhLimit;

		public bool crossPlay;

		public string lightPreset;

		public int spawnPoint;

		public int assistVersion;

		public byte ugcServiceId;

		public int pdLimit;

		public int tcMode;

		public int wdLimit;

		public int collisionType;

		public RoomSettings(int trackID, ulong externalId, string password, string roomName, string trackName, byte newFlags, byte oldFlags, bool nightMode, int adhLimit, bool crossPlay, string lightPreset, int spawnPoint, int assistVersion, byte ugcServiceId, int pdLimit, int tcMode, int wdLimit, int collisionType)
		{
			this.trackID = trackID;
			this.externalId = externalId;
			this.password = password;
			this.roomName = roomName;
			this.trackName = trackName;
			this.newFlags = newFlags;
			this.oldFlags = oldFlags;
			this.nightMode = nightMode;
			this.adhLimit = adhLimit;
			this.crossPlay = crossPlay;
			this.lightPreset = lightPreset;
			this.spawnPoint = spawnPoint;
			this.assistVersion = assistVersion;
			this.ugcServiceId = ugcServiceId;
			this.pdLimit = pdLimit;
			this.tcMode = tcMode;
			this.wdLimit = wdLimit;
			this.collisionType = collisionType;
		}

		public static implicit operator RoomSettings(SmartfoxRoomOptions options)
		{
			return new RoomSettings(options.TrackID, options.ExternalTrackId, options.Name, options.Caption, options.TrackName, (byte)options.NewRoomSettings, (byte)options.OldRoomSettings, options.RoomNightMode, options.RoomAdhLimit, crossPlay: false, options.RoomLightPreset, options.SpawnPointIndex, options.AssistVersion, options.ugcServiceId, options.RoomPowerDensityLimit, options.RoomTractionControlMode, options.RoomWheelDriveType, options.CollisionType);
		}
	}
}
