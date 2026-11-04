using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsIncomingMessageTypesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsIncomingMessageTypesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("Ch1DaHNJbmNvbWluZ01lc3NhZ2VUeXBlcy5wcm90byq1AQoXQ2hzSW5jb21p" + "bmdNZXNzYWdlVHlwZXMSCwoHVU5LTk9XThAAEgkKBUVOVEVSEAESCQoFTEVB" + "VkUQAhILCgdNRVNTQUdFEAMSDAoIREVMX1JPT00QBBIHCgNBQ0sQBRIOCgpG" + "SU5EX1JPT01TEAYSDwoLQ1JFQVRFX1JPT00QBxIMCghBRERfREFUQRAIEg8K" + "C1JFTU9WRV9EQVRBEAkSEwoPUFJJVkFURV9NRVNTQUdFEApCNQorY2FyeC5y" + "dGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRz" + "YgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsIncomingMessageTypes) }, null));
		}
	}
}
