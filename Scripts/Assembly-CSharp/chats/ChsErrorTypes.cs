using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsErrorTypes
	{
		[OriginalName("UNKNOWN")]
		Unknown = 0,
		[OriginalName("INTERNAL_ERROR")]
		InternalError = 1,
		[OriginalName("FILTER_RESTRICTION")]
		FilterRestriction = 2,
		[OriginalName("NO_PERMISSION")]
		NoPermission = 3,
		[OriginalName("NO_MESSAGE_RECEIVERS")]
		NoMessageReceivers = 4
	}
}
