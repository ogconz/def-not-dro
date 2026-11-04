using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsEnterRoomAttributesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsEnterRoomAttributesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChxDaHNFbnRlclJvb21BdHRyaWJ1dGVzLnByb3RvKjgKFkNoc0VudGVyUm9v" + "bUF0dHJpYnV0ZXMSCwoHVU5LTk9XThAAEhEKDVNVQlNDUklQVElPTlMQAUI1" + "CitjYXJ4LnJ0YS5jaGF0c2VydmVyLm1vZGVsLmRlc2NyaXB0aW9uLnByb3Rv" + "qgIFY2hhdHNiBnByb3RvMw=="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsEnterRoomAttributes) }, null));
		}
	}
}
