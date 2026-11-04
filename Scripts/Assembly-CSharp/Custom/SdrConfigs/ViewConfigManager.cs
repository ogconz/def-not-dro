using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Custom.SdrConfigs
{
	public class ViewConfigManager
	{
		private const string SdrViewConfigEndpoint = "view/viewConfig";

		private const string TokenKey = "token";

		private static Error.EType? s_currStatus;

		public static SdrRestViewConfig sdrRestViewConfig { get; private set; }

		public static bool isReceived => s_currStatus.HasValue;

		public static bool isInited
		{
			get
			{
				if (s_currStatus == Error.EType.None)
				{
					return sdrRestViewConfig != null;
				}
				return false;
			}
		}

		public static async UniTask InitConfig()
		{
			if (isInited && isInited)
			{
				return;
			}
			Dictionary<string, string> formParams = new Dictionary<string, string> { 
			{
				"token",
				GameSync.token
			} };
			sdrRestViewConfig = null;
			s_currStatus = null;
			AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
			GameSync.apiClient.CallPost("view/viewConfig", delegate(SdrRestViewConfig config, Error error)
			{
				s_currStatus = error.type;
				if (error.type == Error.EType.None)
				{
					sdrRestViewConfig = config;
				}
				tcs.TrySetResult();
			}, null, formParams);
			await tcs.Task;
		}
	}
}
