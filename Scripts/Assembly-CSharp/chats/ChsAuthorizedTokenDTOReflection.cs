using System;
using Google.Protobuf.Reflection;

namespace chats
{
	public static class ChsAuthorizedTokenDTOReflection
	{
		private static FileDescriptor descriptor;

		public static FileDescriptor Descriptor => descriptor;

		static ChsAuthorizedTokenDTOReflection()
		{
			descriptor = FileDescriptor.FromGeneratedCode(Convert.FromBase64String("ChtDaHNBdXRob3JpemVkVG9rZW5EVE8ucHJvdG8aHENoc1VzZXJQZXJtaXNz" + "aW9uc0NoYXQucHJvdG8aFENoc1VzZXJEYXRhRFRPLnByb3RvIusBChVDaHNB" + "dXRob3JpemVkVG9rZW5EVE8SDQoFdG9rZW4YBCABKAwSMwoSZ3JhbnRlZFBl" + "cm1pc3Npb25zGAUgAygOMhcuQ2hzVXNlclBlcm1pc3Npb25zQ2hhdBIyChFk" + "ZW5pZWRQZXJtaXNzaW9ucxgGIAMoDjIXLkNoc1VzZXJQZXJtaXNzaW9uc0No" + "YXQSFgoOYWRkaXRpb25hbERhdGEYByABKAkSMgoZYWRkaXRpb25hbERhdGFD" + "b2xsZWN0aW9ucxgIIAMoCzIPLkNoc1VzZXJEYXRhRFRPEg4KBnVzZXJJZBgJ" + "IAEoDEI1CitjYXJ4LnJ0YS5jaGF0c2VydmVyLm1vZGVsLmRlc2NyaXB0aW9u" + "LnByb3RvqgIFY2hhdHNiBnByb3RvMw=="), new FileDescriptor[2]
			{
				ChsUserPermissionsChatReflection.Descriptor,
				ChsUserDataDTOReflection.Descriptor
			}, new GeneratedClrTypeInfo(null, new GeneratedClrTypeInfo[1]
			{
				new GeneratedClrTypeInfo(typeof(ChsAuthorizedTokenDTO), ChsAuthorizedTokenDTO.Parser, new string[6] { "Token", "GrantedPermissions", "DeniedPermissions", "AdditionalData", "AdditionalDataCollections", "UserId" }, null, null, null)
			}));
		}
	}
}
