using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsOutcomingMessageDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsOutcomingMessageDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChxDaHNPdXRjb21pbmdNZXNzYWdlRFRPLnByb3RvGh5DaHNPdXRjb21pbmdN" + "ZXNzYWdlVHlwZXMucHJvdG8igwEKFkNoc091dGNvbWluZ01lc3NhZ2VEVE8S" + "CgoCaWQYASABKAMSDwoHY29udGVudBgCIAEoDBInCgR0eXBlGAMgASgOMhku" + "Q2hzT3V0Y29taW5nTWVzc2FnZVR5cGVzEhQKDG1lc3NhZ2VUb2tlbhgEIAEo" + "DBINCgV0b2tlbhgFIAEoDEI1CitjYXJ4LnJ0YS5jaGF0c2VydmVyLm1vZGVs" + "LmRlc2NyaXB0aW9uLnByb3RvqgIFY2hhdHNiBnByb3RvMw=="), new FileDescriptor[1] { ChsOutcomingMessageTypesReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsOutcomingMessageDTO), ChsOutcomingMessageDTO.Parser, new string[5] { "Id", "Content", "Type", "MessageToken", "Token" }, null, null, null)
			}));
		}
	}
}
