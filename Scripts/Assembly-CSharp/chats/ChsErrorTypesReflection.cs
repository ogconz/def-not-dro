using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsErrorTypesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsErrorTypesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChNDaHNFcnJvclR5cGVzLnByb3RvKnUKDUNoc0Vycm9yVHlwZXMSCwoHVU5L" + "Tk9XThAAEhIKDklOVEVSTkFMX0VSUk9SEAESFgoSRklMVEVSX1JFU1RSSUNU" + "SU9OEAISEQoNTk9fUEVSTUlTU0lPThADEhgKFE5PX01FU1NBR0VfUkVDRUlW" + "RVJTEARCNQorY2FyeC5ydGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlv" + "bi5wcm90b6oCBWNoYXRzYgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsErrorTypes) }, null));
		}
	}
}
