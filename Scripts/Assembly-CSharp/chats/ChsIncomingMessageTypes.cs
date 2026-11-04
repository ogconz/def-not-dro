using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsIncomingMessageTypes
	{
		[OriginalName("UNKNOWN")]
		Unknown = 0,
		[OriginalName("ENTER")]
		Enter = 1,
		[OriginalName("LEAVE")]
		Leave = 2,
		[OriginalName("MESSAGE")]
		Message = 3,
		[OriginalName("DEL_ROOM")]
		DelRoom = 4,
		[OriginalName("ACK")]
		Ack = 5,
		[OriginalName("FIND_ROOMS")]
		FindRooms = 6,
		[OriginalName("CREATE_ROOM")]
		CreateRoom = 7,
		[OriginalName("ADD_DATA")]
		AddData = 8,
		[OriginalName("REMOVE_DATA")]
		RemoveData = 9,
		[OriginalName("PRIVATE_MESSAGE")]
		PrivateMessage = 10
	}
}
