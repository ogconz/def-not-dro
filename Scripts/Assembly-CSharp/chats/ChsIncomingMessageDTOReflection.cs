using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsIncomingMessageDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsIncomingMessageDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChtDaHNJbmNvbWluZ01lc3NhZ2VEVE8ucHJvdG8aHUNoc0luY29taW5nTWVz" + "c2FnZVR5cGVzLnByb3RvIooBChVDaHNJbmNvbWluZ01lc3NhZ2VEVE8SDQoF" + "dG9rZW4YASABKAwSFAoMbWVzc2FnZVRva2VuGAIgASgMEg8KB3BheWxvYWQY" + "AyABKAwSJgoEdHlwZRgEIAEoDjIYLkNoc0luY29taW5nTWVzc2FnZVR5cGVz" + "EhMKC2ZyYW1lSGVpZ2h0GAUgASgFQjUKK2NhcngucnRhLmNoYXRzZXJ2ZXIu" + "bW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[1] { ChsIncomingMessageTypesReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsIncomingMessageDTO), ChsIncomingMessageDTO.Parser, new string[5] { "Token", "MessageToken", "Payload", "Type", "FrameHeight" }, null, null, null)
			}));
		}
	}
}
