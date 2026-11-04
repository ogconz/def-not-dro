using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsErrorInfoDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsErrorInfoDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChVDaHNFcnJvckluZm9EVE8ucHJvdG8aE0Noc0Vycm9yVHlwZXMucHJvdG8i" + "UwoPQ2hzRXJyb3JJbmZvRFRPEiEKCWVycm9yVHlwZRgBIAEoDjIOLkNoc0Vy" + "cm9yVHlwZXMSDwoHcGF5bG9hZBgCIAEoDBIMCgRjb2RlGAMgASgFQjUKK2Nh" + "cngucnRhLmNoYXRzZXJ2ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVj" + "aGF0c2IGcHJvdG8z"), new FileDescriptor[1] { ChsErrorTypesReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsErrorInfoDTO), ChsErrorInfoDTO.Parser, new string[3] { "ErrorType", "Payload", "Code" }, null, null, null)
			}));
		}
	}
}
