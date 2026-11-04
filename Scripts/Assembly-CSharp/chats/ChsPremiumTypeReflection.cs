using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsPremiumTypeReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsPremiumTypeReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNQcmVtaXVtVHlwZS5wcm90bypACg5DaHNQcmVtaXVtVHlwZRIYChRV" + "TktOT1dOX1BSRU1JVU1fVFlQRRAAEgoKBlNJTFZFUhABEggKBEdPTEQQAkI1" + "CitjYXJ4LnJ0YS5jaGF0c2VydmVyLm1vZGVsLmRlc2NyaXB0aW9uLnByb3Rv" + "qgIFY2hhdHNiBnByb3RvMw=="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsPremiumType) }, null));
		}
	}
}
