using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsUserDataDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsUserDataDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNVc2VyRGF0YURUTy5wcm90byItCg5DaHNVc2VyRGF0YURUTxILCgNr" + "ZXkYASABKAkSDgoGdmFsdWVzGAIgAygJQjUKK2NhcngucnRhLmNoYXRzZXJ2" + "ZXIubW9kZWwuZGVzY3JpcHRpb24ucHJvdG+qAgVjaGF0c2IGcHJvdG8z"), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsUserDataDTO), ChsUserDataDTO.Parser, new string[2] { "Key", "Values" }, null, null, null)
			}));
		}
	}
}
