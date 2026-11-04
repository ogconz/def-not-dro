using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsFilterNamesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsFilterNamesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNGaWx0ZXJOYW1lcy5wcm90bypSCg5DaHNGaWx0ZXJOYW1lcxILCgdV" + "TktOT1dOEAASFQoRT0ZGRU5TSVZFX0xBR1VBR0UQARIICgRTUEFNEAISEgoO" + "TUVTU0FHRV9MRU5HVEgQA0I1CitjYXJ4LnJ0YS5jaGF0c2VydmVyLm1vZGVs" + "LmRlc2NyaXB0aW9uLnByb3RvqgIFY2hhdHNiBnByb3RvMw=="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsFilterNames) }, null));
		}
	}
}
