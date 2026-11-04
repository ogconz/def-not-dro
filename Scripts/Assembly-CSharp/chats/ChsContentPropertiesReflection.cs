using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsContentPropertiesReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsContentPropertiesReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChpDaHNDb250ZW50UHJvcGVydGllcy5wcm90byo1ChRDaHNDb250ZW50UHJv" + "cGVydGllcxILCgdVTktOT1dOEAASEAoMSElTVE9SWV9TSVpFEAFCNQorY2Fy" + "eC5ydGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNo" + "YXRzYgZwcm90bzM="), new FileDescriptor[0], new GeneratedClrTypeInfo(new Type[1] { typeof(ChsContentProperties) }, null));
		}
	}
}
