using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsFilterAlertDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsFilterAlertDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChdDaHNGaWx0ZXJBbGVydERUTy5wcm90bxocQ2hzVXNlclBlcm1pc3Npb25z" + "Q2hhdC5wcm90bxoUQ2hzRmlsdGVyTmFtZXMucHJvdG8ikQEKEUNoc0ZpbHRl" + "ckFsZXJ0RFRPEiMKCmZpbHRlck5hbWUYASABKA4yDy5DaHNGaWx0ZXJOYW1l" + "cxIjChtwZW5hbHR5RHVyYXRpb25NaWxsaXNlY29uZHMYAiABKAMSMgoRZGVu" + "aWVkUGVybWlzc2lvbnMYAyADKA4yFy5DaHNVc2VyUGVybWlzc2lvbnNDaGF0" + "QjUKK2NhcngucnRhLmNoYXRzZXJ2ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJv" + "dG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[2]
			{
				ChsUserPermissionsChatReflection.Descriptor,
				ChsFilterNamesReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsFilterAlertDTO), ChsFilterAlertDTO.Parser, new string[3] { "FilterName", "PenaltyDurationMilliseconds", "DeniedPermissions" }, null, null, null)
			}));
		}
	}
}
