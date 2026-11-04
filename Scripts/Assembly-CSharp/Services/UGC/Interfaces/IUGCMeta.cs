using System;

namespace Services.UGC.Interfaces
{
	public interface IUGCMeta
	{
		string description { get; }

		ulong sizeBytes { get; }

		ulong archievedSizeBytes { get; }

		string[] tags { get; }

		string externalPath { get; }

		string localPath { get; }

		bool needUpdate { get; }

		DateTime timeCreated { get; }

		string targetClientVersion { get; }

		void ShowInOverlay();
	}
}
