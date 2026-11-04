using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsRoomInfoDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsRoomInfoDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNSb29tSW5mb0RUTy5wcm90bxoUQ2hzS2V5VmFsdWVEVE8ucHJvdG8i" + "SwoOQ2hzUm9vbUluZm9EVE8SDgoGcm9vbUlkGAEgASgMEikKEHB1YmxpY0F0" + "dHJpYnV0ZXMYAiADKAsyDy5DaHNLZXlWYWx1ZURUT0I1CitjYXJ4LnJ0YS5j" + "aGF0c2VydmVyLm1vZGVsLmRlc2NyaXB0aW9uLnByb3RvqgIFY2hhdHNiBnBy" + "b3RvMw=="), new FileDescriptor[1] { ChsKeyValueDTOReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsRoomInfoDTO), ChsRoomInfoDTO.Parser, new string[2] { "RoomId", "PublicAttributes" }, null, null, null)
			}));
		}
	}
}
