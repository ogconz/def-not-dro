using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsOutcomingMessageTypes
	{
		[OriginalName("UNKNOWN_OUTCOMING_TYPE")]
		UnknownOutcomingType = 0,
		[OriginalName("USER_MESSAGE")]
		UserMessage = 1,
		[OriginalName("USER_ENTERS_ROOM")]
		UserEntersRoom = 2,
		[OriginalName("USER_LEAVES_ROOM")]
		UserLeavesRoom = 3,
		[OriginalName("SYSTEM_MESSAGE")]
		SystemMessage = 4,
		[OriginalName("ACK")]
		Ack = 5,
		[OriginalName("ERROR")]
		Error = 6,
		[OriginalName("ROOM_CLOSED")]
		RoomClosed = 7
	}
}
