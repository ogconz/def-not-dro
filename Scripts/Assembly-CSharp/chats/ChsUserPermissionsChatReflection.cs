using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsUserPermissionsChatReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsUserPermissionsChatReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChxDaHNVc2VyUGVybWlzc2lvbnNDaGF0LnByb3RvKn4KFkNoc1VzZXJQZXJt" + "aXNzaW9uc0NoYXQSFgoSVU5LTk9XTl9QRVJNSVNTSU9OEAASDwoLQ1JFQVRF" + "X1JPT00QARIPCgtERUxFVEVfUk9PTRACEhAKDFNFTkRfTUVTU0FHRRADEhgK" + "FFNFTkRfUFJJVkFURV9NRVNTQUdFEARCNQorY2FyeC5ydGEuY2hhdHNlcnZl" + "ci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsUserPermissionsChat) }, null));
		}
	}
}
