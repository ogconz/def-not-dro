using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsContentItemDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsContentItemDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChdDaHNDb250ZW50SXRlbURUTy5wcm90bxoUQ2hzS2V5VmFsdWVEVE8ucHJv" + "dG8aFENoc1ByZW1pdW1UeXBlLnByb3RvIukBChFDaHNDb250ZW50SXRlbURU" + "TxIOCgZhdXRob3IYAiABKAwSEgoKYXV0aG9yTmFtZRgDIAEoCRISCgpyYXdD" + "b250ZW50GAQgASgMEhEKCXRpbWVzdGFtcBgFIAEoAxIVCg1wcmV2aW91c1Bs" + "YWNlGAYgASgFEiQKC3ByZW1pdW1UeXBlGAcgASgOMg8uQ2hzUHJlbWl1bVR5" + "cGUSEwoLZnJhbWVIZWlnaHQYCCABKAUSFgoOYXV0aG9yR2xvYmFsSWQYCSAB" + "KAkSEAoIY2xhbk5hbWUYCiABKAkSDQoFYWRtaW4YZSABKAhCNQorY2FyeC5y" + "dGEuY2hhdHNlcnZlci5tb2RlbC5kZXNjcmlwdGlvbi5wcm90b6oCBWNoYXRz" + "YgZwcm90bzM="), new FileDescriptor[2]
			{
				ChsKeyValueDTOReflection.Descriptor,
				ChsPremiumTypeReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsContentItemDTO), ChsContentItemDTO.Parser, new string[10] { "Author", "AuthorName", "RawContent", "Timestamp", "PreviousPlace", "PremiumType", "FrameHeight", "AuthorGlobalId", "ClanName", "Admin" }, null, null, null)
			}));
		}
	}
}
