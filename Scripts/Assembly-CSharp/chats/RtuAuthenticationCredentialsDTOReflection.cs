using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class RtuAuthenticationCredentialsDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static RtuAuthenticationCredentialsDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("CiVSdHVBdXRoZW50aWNhdGlvbkNyZWRlbnRpYWxzRFRPLnByb3RvGhRDaHNL" + "ZXlWYWx1ZURUTy5wcm90bxoXQ2hzR2xvYmFsVG9rZW5EVE8ucHJvdG8ijQEK" + "H1J0dUF1dGhlbnRpY2F0aW9uQ3JlZGVudGlhbHNEVE8SJwoLZ2xvYmFsVG9r" + "ZW4YASABKAsyEi5DaHNHbG9iYWxUb2tlbkRUTxIOCgZyb29tSWQYAiABKAkS" + "DAoEbmFtZRgDIAEoCRIjCgphdHRyaWJ1dGVzGAQgAygLMg8uQ2hzS2V5VmFs" + "dWVEVE9CNQorY2FyeC5ydGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlv" + "bi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[2]
			{
				ChsKeyValueDTOReflection.Descriptor,
				ChsGlobalTokenDTOReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(RtuAuthenticationCredentialsDTO), RtuAuthenticationCredentialsDTO.Parser, new string[4] { "GlobalToken", "RoomId", "Name", "Attributes" }, null, null, null)
			}));
		}
	}
}
