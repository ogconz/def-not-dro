using Google.Protobuf.Reflection;

namespace chats
{
	public enum ChsPremiumType
	{
		[OriginalName("UNKNOWN_PREMIUM_TYPE")]
		UnknownPremiumType = 0,
		[OriginalName("SILVER")]
		Silver = 1,
		[OriginalName("GOLD")]
		Gold = 2
	}
}
