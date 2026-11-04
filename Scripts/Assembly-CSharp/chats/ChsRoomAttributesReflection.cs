using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsRoomAttributesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsRoomAttributesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChdDaHNSb29tQXR0cmlidXRlcy5wcm90byoqChFDaHNSb29tQXR0cmlidXRl" + "cxILCgdVTktOT1dOEAASCAoEbmFtZRABQjUKK2NhcngucnRhLmNoYXRzZXJ2" + "ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsRoomAttributes) }, null));
		}
	}
}
