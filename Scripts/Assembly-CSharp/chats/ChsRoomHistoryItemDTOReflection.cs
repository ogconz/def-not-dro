using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsRoomHistoryItemDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsRoomHistoryItemDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChtDaHNSb29tSGlzdG9yeUl0ZW1EVE8ucHJvdG8aF0Noc0NvbnRlbnRJdGVt" + "RFRPLnByb3RvGh5DaHNPdXRjb21pbmdNZXNzYWdlVHlwZXMucHJvdG8icQoV" + "Q2hzUm9vbUhpc3RvcnlJdGVtRFRPEgoKAmlkGAEgASgDEiMKB2NvbnRlbnQY" + "AiABKAsyEi5DaHNDb250ZW50SXRlbURUTxInCgR0eXBlGAMgASgOMhkuQ2hz" + "T3V0Y29taW5nTWVzc2FnZVR5cGVzQjUKK2NhcngucnRhLmNoYXRzZXJ2ZXIu" + "bW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[2]
			{
				ChsContentItemDTOReflection.Descriptor,
				ChsOutcomingMessageTypesReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsRoomHistoryItemDTO), ChsRoomHistoryItemDTO.Parser, new string[3] { "Id", "Content", "Type" }, null, null, null)
			}));
		}
	}
}
