using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;

namespace Services.UGC.Steamworks
{
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamUGCDetails_t
	{
		internal PublishedFileId PublishedFileId;

		internal Result Result;

		internal WorkshopFileType FileType;

		internal AppId CreatorAppID;

		internal AppId ConsumerAppID;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
		internal byte[] Title;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8000)]
		internal byte[] Description;

		internal ulong SteamIDOwner;

		internal uint TimeCreated;

		internal uint TimeUpdated;

		internal uint TimeAddedToUserList;

		internal RemoteStoragePublishedFileVisibility Visibility;

		[MarshalAs(UnmanagedType.I1)]
		internal bool Banned;

		[MarshalAs(UnmanagedType.I1)]
		internal bool AcceptedForUse;

		[MarshalAs(UnmanagedType.I1)]
		internal bool TagsTruncated;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1025)]
		internal byte[] Tags;

		internal ulong File;

		internal ulong PreviewFile;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
		internal byte[] PchFileName;

		internal int FileSize;

		internal int PreviewFileSize;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		internal byte[] URL;

		internal uint VotesUp;

		internal uint VotesDown;

		internal float Score;

		internal uint NumChildren;
	}
}
