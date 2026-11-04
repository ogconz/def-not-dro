using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsUserInfoDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsUserInfoDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNVc2VySW5mb0RUTy5wcm90byJCCg5DaHNVc2VySW5mb0RUTxIKCgJp" + "ZBgBIAEoDBIMCgRuYW1lGAIgASgJEhYKDmFkZGl0aW9uYWxEYXRhGAMgASgJ" + "QjUKK2NhcngucnRhLmNoYXRzZXJ2ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJv" + "dG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsUserInfoDTO), ChsUserInfoDTO.Parser, new string[3] { "Id", "Name", "AdditionalData" }, null, null, null)
			}));
		}
	}
}
