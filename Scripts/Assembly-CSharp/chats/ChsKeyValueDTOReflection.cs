using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsKeyValueDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsKeyValueDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChRDaHNLZXlWYWx1ZURUTy5wcm90byIsCg5DaHNLZXlWYWx1ZURUTxILCgNr" + "ZXkYASABKAkSDQoFdmFsdWUYAiABKAlCNQorY2FyeC5ydGEuY2hhdHNlcnZl" + "ci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsKeyValueDTO), ChsKeyValueDTO.Parser, new string[2] { "Key", "Value" }, null, null, null)
			}));
		}
	}
}
