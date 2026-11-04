using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsCreateRoomInfoDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsCreateRoomInfoDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChpDaHNDcmVhdGVSb29tSW5mb0RUTy5wcm90bxolUnR1QXV0aGVudGljYXRp" + "b25DcmVkZW50aWFsc0RUTy5wcm90bxoUQ2hzS2V5VmFsdWVEVE8ucHJvdG8i" + "3gEKFENoc0NyZWF0ZVJvb21JbmZvRFRPEjUKC2NyZWRlbnRpYWxzGAEgASgL" + "MiAuUnR1QXV0aGVudGljYXRpb25DcmVkZW50aWFsc0RUTxIMCgRuYW1lGAIg" + "ASgJEikKEHB1YmxpY0F0dHJpYnV0ZXMYAyADKAsyDy5DaHNLZXlWYWx1ZURU" + "TxIqChFwcml2YXRlQXR0cmlidXRlcxgEIAMoCzIPLkNoc0tleVZhbHVlRFRP" + "EioKEWNvbnRlbnRQcm9wZXJ0aWVzGAUgAygLMg8uQ2hzS2V5VmFsdWVEVE9C" + "NQorY2FyeC5ydGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90" + "b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[2]
			{
				RtuAuthenticationCredentialsDTOReflection.Descriptor,
				ChsKeyValueDTOReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsCreateRoomInfoDTO), ChsCreateRoomInfoDTO.Parser, new string[5] { "Credentials", "Name", "PublicAttributes", "PrivateAttributes", "ContentProperties" }, null, null, null)
			}));
		}
	}
}
