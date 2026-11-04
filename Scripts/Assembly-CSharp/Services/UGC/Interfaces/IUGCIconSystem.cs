using System;
using System.Threading;
using UnityEngine;

namespace Services.UGC.Interfaces
{
	public interface IUGCIconSystem
	{
		public enum DownloadResult
		{
			Success = 0,
			NotValid = 1,
			Cancelled = 2
		}

		Texture2D iconTexture { get; }

		string externalIconPath { get; }

		int iconWidth { get; }

		int iconHeight { get; }

		void DisposeIcon();

		void DownloadIcon(Action<Texture2D, DownloadResult> onFinished, Action<float> progress = null, CancellationTokenSource ctsSource = null);
	}
}
