using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsRoomSearchResultDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsRoomSearchResultDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChxDaHNSb29tU2VhcmNoUmVzdWx0RFRPLnByb3RvGhRDaHNSb29tSW5mb0RU" + "Ty5wcm90byI4ChZDaHNSb29tU2VhcmNoUmVzdWx0RFRPEh4KBXJvb21zGAEg" + "AygLMg8uQ2hzUm9vbUluZm9EVE9CNQorY2FyeC5ydGEuY2hhdHNlcnZlci5t" + "b2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[1] { ChsRoomInfoDTOReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsRoomSearchResultDTO), ChsRoomSearchResultDTO.Parser, new string[1] { "Rooms" }, null, null, null)
			}));
		}
	}
}
