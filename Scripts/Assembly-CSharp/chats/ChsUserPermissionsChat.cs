using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsUserPermissionsChat
	{
		[OriginalName("UNKNOWN_PERMISSION")]
		UnknownPermission = 0,
		[OriginalName("CREATE_ROOM")]
		CreateRoom = 1,
		[OriginalName("DELETE_ROOM")]
		DeleteRoom = 2,
		[OriginalName("SEND_MESSAGE")]
		SendMessage = 3,
		[OriginalName("SEND_PRIVATE_MESSAGE")]
		SendPrivateMessage = 4
	}
}
