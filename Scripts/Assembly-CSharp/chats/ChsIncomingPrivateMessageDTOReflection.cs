using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsIncomingPrivateMessageDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsIncomingPrivateMessageDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("CiJDaHNJbmNvbWluZ1ByaXZhdGVNZXNzYWdlRFRPLnByb3RvIkIKHENoc0lu" + "Y29taW5nUHJpdmF0ZU1lc3NhZ2VEVE8SDwoHY29udGVudBgBIAEoDBIRCgly" + "ZWNlaXZlcnMYAiADKAlCNQorY2FyeC5ydGEuY2hhdHNlcnZlci5tb2RlbC5k" + "ZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsIncomingPrivateMessageDTO), ChsIncomingPrivateMessageDTO.Parser, new string[2] { "Content", "Receivers" }, null, null, null)
			}));
		}
	}
}
