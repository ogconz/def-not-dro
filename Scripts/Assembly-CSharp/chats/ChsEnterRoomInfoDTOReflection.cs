using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsEnterRoomInfoDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsEnterRoomInfoDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChlDaHNFbnRlclJvb21JbmZvRFRPLnByb3RvGhtDaHNBdXRob3JpemVkVG9r" + "ZW5EVE8ucHJvdG8aG0Noc1Jvb21IaXN0b3J5SXRlbURUTy5wcm90bxoUQ2hz" + "VXNlckluZm9EVE8ucHJvdG8aFENoc0tleVZhbHVlRFRPLnByb3RvIvwBChND" + "aHNFbnRlclJvb21JbmZvRFRPEiUKBXRva2VuGAEgASgLMhYuQ2hzQXV0aG9y" + "aXplZFRva2VuRFRPEicKB2NvbnRlbnQYAiADKAsyFi5DaHNSb29tSGlzdG9y" + "eUl0ZW1EVE8SJAoLdXNlcnNJblJvb20YAyADKAsyDy5DaHNVc2VySW5mb0RU" + "TxIpChBwdWJsaWNBdHRyaWJ1dGVzGAQgAygLMg8uQ2hzS2V5VmFsdWVEVE8S" + "DgoGcm9vbUlkGAUgASgJEjQKG3RlbXBvcmFsbHlEZW5pZWRQZXJtaXNzaW9u" + "cxgGIAMoCzIPLkNoc0tleVZhbHVlRFRPQjUKK2NhcngucnRhLmNoYXRzZXJ2" + "ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[4]
			{
				ChsAuthorizedTokenDTOReflection.Descriptor,
				ChsRoomHistoryItemDTOReflection.Descriptor,
				ChsUserInfoDTOReflection.Descriptor,
				ChsKeyValueDTOReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsEnterRoomInfoDTO), ChsEnterRoomInfoDTO.Parser, new string[6] { "Token", "Content", "UsersInRoom", "PublicAttributes", "RoomId", "TemporallyDeniedPermissions" }, null, null, null)
			}));
		}
	}
}
