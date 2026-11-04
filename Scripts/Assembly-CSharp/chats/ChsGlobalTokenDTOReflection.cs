using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsGlobalTokenDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsGlobalTokenDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChdDaHNHbG9iYWxUb2tlbkRUTy5wcm90byI1ChFDaHNHbG9iYWxUb2tlbkRU" + "TxINCgV0b2tlbhgBIAEoCRIRCglhdXRob3JpdHkYAiABKAlCNQorY2FyeC5y" + "dGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRz" + "YgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsGlobalTokenDTO), ChsGlobalTokenDTO.Parser, new string[2] { "Token", "Authority" }, null, null, null)
			}));
		}
	}
}
