using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsFilterNames
	{
		[OriginalName("UNKNOWN")]
		Unknown = 0,
		[OriginalName("OFFENSIVE_LAGUAGE")]
		OffensiveLaguage = 1,
		[OriginalName("SPAM")]
		Spam = 2,
		[OriginalName("MESSAGE_LENGTH")]
		MessageLength = 3
	}
}
