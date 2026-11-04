using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsRoomByAttributeRequestDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsRoomByAttributeRequestDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("CiJDaHNSb29tQnlBdHRyaWJ1dGVSZXF1ZXN0RFRPLnByb3RvIkkKHENoc1Jv" + "b21CeUF0dHJpYnV0ZVJlcXVlc3REVE8SEQoJaW5kZXhOYW1lGAEgASgJEhYK" + "DmF0dHJpYnV0ZVZhbHVlGAIgASgJQjUKK2NhcngucnRhLmNoYXRzZXJ2ZXIu" + "bW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsRoomByAttributeRequestDTO), ChsRoomByAttributeRequestDTO.Parser, new string[2] { "IndexName", "AttributeValue" }, null, null, null)
			}));
		}
	}
}
