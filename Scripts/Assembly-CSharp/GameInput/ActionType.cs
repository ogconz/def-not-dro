namespace GameInput
{
	public enum ActionType : byte
	{
		ErrorAction = 0,
		[SteeringDeviceDependent]
		Turn = 1,
		[SteeringDeviceDependent]
		Accelerate = 2,
		Brake = 3,
		Clutch = 4,
		HandBrake = 5,
		Flares = 6,
		NextCamera = 7,
		PrevCamera = 8,
		RearViewCamera = 9,
		RestartRace = 10,
		RespawnCar = 11,
		GearUp = 12,
		GearDown = 13,
		Gear1 = 14,
		Gear2 = 15,
		Gear3 = 16,
		Gear4 = 17,
		Gear5 = 18,
		Gear6 = 19,
		GearR = 20,
		VoteMap = 21,
		HideUI = 22,
		HideMouse = 23,
		[SystemAction]
		HideDriver = 24,
		[SystemAction]
		HideWheel = 25,
		[SystemAction]
		DPadUp = 26,
		[SystemAction]
		DPadRight = 27,
		[SystemAction]
		DPadDown = 28,
		[SystemAction]
		DPadLeft = 29,
		[SystemAction]
		InterfaceSelect = 30,
		FreeCamMode = 31,
		FreeCamView = 32,
		[SystemAction]
		MenuLayerSeparator = 33,
		[SystemAction]
		NavNextPage = 34,
		[SystemAction]
		NavPrevPage = 35,
		[SystemAction]
		NavUp = 36,
		[SystemAction]
		NavDown = 37,
		[SystemAction]
		NavLeft = 38,
		[SystemAction]
		NavRight = 39,
		[SystemAction]
		NavCancelBtnB = 40,
		[SystemAction]
		NavSelectBtnA = 41,
		[SystemAction]
		NavMouseSelect = 42,
		[SystemAction]
		NavBack = 43,
		[SystemAction]
		NavStart = 44,
		[SystemAction]
		NavAdditional0BtnX = 45,
		[SystemAction]
		NavAdditional1BtnY = 46,
		[SystemAction]
		NavBtnExpand = 47,
		[SystemAction]
		NavDPadUp = 48,
		[SystemAction]
		NavDPadDown = 49,
		[SystemAction]
		NavDPadRight = 50,
		[SystemAction]
		NavDPadLeft = 51,
		[SystemAction]
		NavAdditionalPrevBtnL2 = 52,
		[SystemAction]
		NavAdditionalNextBtnR2 = 53,
		[SystemAction]
		NavAddUp = 54,
		[SystemAction]
		NavAddDown = 55,
		[SystemAction]
		NavAddRight = 56,
		[SystemAction]
		NavAddLeft = 57,
		RaceStats = 58,
		RaceJoin = 59,
		RaceCreate = 60,
		[SystemAction]
		NavBtnExpandRight = 61,
		VoiceChat = 62,
		[SystemAction]
		FlyMovement = 63,
		[SystemAction]
		FlyStrafe = 64,
		[SystemAction]
		FlyRotationVertical = 65,
		[SystemAction]
		FlyRotationHorizontal = 66,
		[SystemAction]
		FlyHeightUp = 67,
		[SystemAction]
		FlyHeightDown = 68,
		[SystemAction]
		FlyTurboMode = 69,
		[SystemAction]
		FlyHideUI = 70,
		[SystemAction]
		FlyStatistics = 71,
		[SystemAction]
		FlyChangeFOV = 72,
		[SystemAction]
		FlyReset = 73,
		[SystemAction]
		NavReport = 74,
		Headlights = 75,
		Ignition = 76,
		Starter = 77,
		VRCamReset = 78,
		Gear7 = 79,
		ShowInputBar = 80,
		[SystemAction]
		HideSidePanel = 81,
		[SystemAction]
		ZoomIn = 82,
		[SystemAction]
		ZoomOut = 83,
		ShowRadialMenu = 84,
		GoToDynostand = 85,
		TeleportToWaypoint = 86,
		CreateWaypoint = 87,
		OpenTextChat = 88,
		[SystemAction]
		OpenTextChatReserve = 89
	}
}
