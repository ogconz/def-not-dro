using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsContentProperties
	{
		[OriginalName("UNKNOWN")]
		Unknown = 0,
		[OriginalName("HISTORY_SIZE")]
		HistorySize = 1
	}
}
