using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsOutcomingMessageTypesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsOutcomingMessageTypesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("Ch5DaHNPdXRjb21pbmdNZXNzYWdlVHlwZXMucHJvdG8qrQEKGENoc091dGNv" + "bWluZ01lc3NhZ2VUeXBlcxIaChZVTktOT1dOX09VVENPTUlOR19UWVBFEAAS" + "EAoMVVNFUl9NRVNTQUdFEAESFAoQVVNFUl9FTlRFUlNfUk9PTRACEhQKEFVT" + "RVJfTEVBVkVTX1JPT00QAxISCg5TWVNURU1fTUVTU0FHRRAEEgcKA0FDSxAF" + "EgkKBUVSUk9SEAYSDwoLUk9PTV9DTE9TRUQQB0I1CitjYXJ4LnJ0YS5jaGF0" + "c2VydmVyLm1vZGVsLmRlc2NyaXB0aW9uLnByb3RvqgIFY2hhdHNiBnByb3Rv" + "Mw=="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsOutcomingMessageTypes) }, null));
		}
	}
}
