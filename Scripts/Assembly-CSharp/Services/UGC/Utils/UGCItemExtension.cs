using System;
using System.Collections.Generic;
using GameOverlay;
using Services.UGC.Interfaces;

namespace Services.UGC.Utils
{
	public static class UGCItemExtension
	{
		private const string NICKNAME_END_POINT = "/getPlayerNameByPlatformId";

		private const string TOKEN_ARG = "token";

		private const string USER_ARG = "platformUserId";

		private static Dictionary<string, string> m_requestArgs = new Dictionary<string, string>(4);

		private static ApiClient m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
		{
			GameLog.Print(AlertLevel.Error, "ServerSharingService", "Server sharing: " + error.debugMessage);
		});

		public static void RequestNameThrowOwnApi(this IUGCOwnerSystem item, Action<string> onFinished)
		{
			m_requestArgs.Clear();
			m_requestArgs.Add("token", GameSync.token);
			m_requestArgs.Add("platformUserId", $"{item.owner.userId.accountId}|{(int)Overlay.instance.GetPlatform()}");
			m_apiClient.CallPost("/getPlayerNameByPlatformId", delegate(SdrNickname result, Error error)
			{
				if (error.type != Error.EType.None || result == null)
				{
					onFinished.SafeCall(null);
				}
				else
				{
					onFinished.SafeCall(result.name);
				}
			}, null, m_requestArgs);
		}
	}
}
