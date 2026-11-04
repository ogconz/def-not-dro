using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsNoPermissionErrorDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsNoPermissionErrorDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("Ch1DaHNOb1Blcm1pc3Npb25FcnJvckRUTy5wcm90bxocQ2hzVXNlclBlcm1p" + "c3Npb25zQ2hhdC5wcm90byJvChdDaHNOb1Blcm1pc3Npb25FcnJvckRUTxIS" + "Cgppc1RlbXBvcmFsGAEgASgIEhMKC2RlbmllZFVudGlsGAIgASgDEisKCnBl" + "cm1pc3Npb24YAyABKA4yFy5DaHNVc2VyUGVybWlzc2lvbnNDaGF0QjUKK2Nh" + "cngucnRhLmNoYXRzZXJ2ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVj" + "aGF0c2IGcHJvdG8z"), new FileDescriptor[1] { ChsUserPermissionsChatReflection.Descriptor }, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsNoPermissionErrorDTO), ChsNoPermissionErrorDTO.Parser, new string[3] { "IsTemporal", "DeniedUntil", "Permission" }, null, null, null)
			}));
		}
	}
}
