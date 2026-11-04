using System;
using System.Collections.Generic;
using SyncMultiplayer;

public abstract class BaseServerSharingService
{
	public class RestServerPlayerStateAndOwnership
	{
		public RestServerPlayerRestrict restrict;
	}

	public class RestServerPlayerRestrict
	{
		public AdminBanReason type;

		public long endDate;
	}

	public Action<bool, Type> onRequestProcessing;

	private const string TOKEN_STR = "token";

	private ApiClient m_apiClient;

	private Dictionary<string, string> m_stateDict = new Dictionary<string, string>();

	protected string m_fetchNicknameEndPoint => "/getPlayerNameByPlatformId";

	protected virtual string m_publishEndpoint => string.Empty;

	protected virtual string m_fetchShortEndpoint => string.Empty;

	protected virtual string m_fetchFullEndpoint => string.Empty;

	protected virtual string m_fetchSingleEndpoint => string.Empty;

	protected virtual string m_markDownloadEndpoint => string.Empty;

	protected virtual string m_makeRatingEndpoint => string.Empty;

	protected abstract Dictionary<string, byte[]> PrepareFileParams();

	protected abstract bool IsServerImplemented();

	protected abstract bool PrepareUploadRequestParams<T>(out Dictionary<string, string> args, out string endPoint, object data);

	public static T Create<T>()
	{
		return Activator.CreateInstance<T>();
	}

	private ApiClient MasterClient()
	{
		if (m_apiClient == null)
		{
			m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
			{
				GameLog.Print(AlertLevel.Error, "ServerSharingService", "Server sharing: " + error.debugMessage);
			});
		}
		return m_apiClient;
	}

	private void AddTokenParam(Dictionary<string, string> args)
	{
		args.Add("token", GameSync.token);
	}

	public void MakeRequestOfType<T>(object data, Action<T> onDone) where T : class
	{
		if (!PrepareUploadRequestParams<T>(out var args, out var endPoint, data))
		{
			GameLog.Print(AlertLevel.Error, "ServerSharingService", "Unable to prepare request data");
			onDone.SafeCall(null);
			return;
		}
		if (!IsServerImplemented())
		{
			onDone.SafeCall(Activator.CreateInstance<T>());
			return;
		}
		if (string.IsNullOrEmpty(endPoint))
		{
			GameLog.Print(AlertLevel.Error, "ServerSharingService", "Cannot proceed request > Endpoint is empty. Please make sure to override it");
			onDone.SafeCall(null);
			return;
		}
		onRequestProcessing.SafeCall(param1: true, typeof(T));
		AddTokenParam(args);
		MasterClient().CallPost(endPoint, delegate(T result, Error error)
		{
			onRequestProcessing.SafeCall(param1: false, typeof(T));
			if (error.type != Error.EType.None)
			{
				GameLog.Print(AlertLevel.Error, "ServerSharingService", "Response error for sharing request: " + error.debugMessage);
				ShowErrorMsg(error);
				onDone.SafeCall(null);
			}
			else
			{
				onDone.SafeCall(result);
			}
		}, null, args, PrepareFileParams());
		static SharingErrorResponseType GetErrorMsgType(string msg)
		{
			if (msg != null)
			{
				if (msg.Contains("already published"))
				{
					return SharingErrorResponseType.AlreadyPublished;
				}
				if (msg.Contains("not found setup"))
				{
					return SharingErrorResponseType.PublicationNotExist;
				}
				if (msg.Contains("invalidName"))
				{
					return SharingErrorResponseType.InvalidName;
				}
				if (msg.Contains("reached max count"))
				{
					return SharingErrorResponseType.ReachedMaxCount;
				}
				if (msg.Contains("publishing restricted"))
				{
					return SharingErrorResponseType.PublishingRestricted;
				}
			}
			return SharingErrorResponseType.None;
		}
		static string GetResponseLocalizedErrorMsg(SharingErrorResponseType result)
		{
			return result switch
			{
				SharingErrorResponseType.AlreadyPublished => LocalizationKeys.already_published.Localized(), 
				SharingErrorResponseType.PublicationNotExist => LocalizationKeys.publication_not_exist.Localized(), 
				SharingErrorResponseType.InvalidName => LocalizationKeys.wrong_name.Localized(), 
				SharingErrorResponseType.ReachedMaxCount => LocalizationKeys.reached_max_publication_count_per_day.Localized(), 
				SharingErrorResponseType.PublishingRestricted => LocalizationKeys.publish_restricted.Localized(), 
				_ => LocalizationKeys.error_server_error.Localized(), 
			};
		}
		void ShowErrorMsg(Error error)
		{
			SharingErrorResponseType sharingErrorResponseType = GetErrorMsgType(error.debugMessage);
			if (sharingErrorResponseType == SharingErrorResponseType.PublishingRestricted)
			{
				UILockInputContext.EnableLock();
				GetRestrictionStatusInfo(delegate(long endDate)
				{
					UILockInputContext.DisableLock();
					ShowAdminRestrictAccessError(endDate);
				});
			}
			else
			{
				GUICommonGodVoice.ShowText(GetResponseLocalizedErrorMsg(sharingErrorResponseType), 3f);
			}
		}
	}

	private void GetRestrictionStatusInfo(Action<long> isRestrictedCallback)
	{
		m_stateDict.Clear();
		AddTokenParam(m_stateDict);
		MasterClient().CallPost("internal/getServerPlayerStateAndOwnership", delegate(RestServerPlayerStateAndOwnership info, Error error)
		{
			if (info == null || info.restrict == null)
			{
				isRestrictedCallback.SafeCall(0L);
			}
			else
			{
				AdminBanReason type = info.restrict.type;
				if (type == AdminBanReason.ACCESS_PUBLIC_SETUP || type == AdminBanReason.ACCESS_PUBLIC_LIVERY)
				{
					isRestrictedCallback.SafeCall(info.restrict.endDate);
				}
				else
				{
					isRestrictedCallback.SafeCall(0L);
				}
			}
		}, null, m_stateDict);
	}

	private void ShowAdminRestrictAccessError(long timeEnd)
	{
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timeEnd).ToLocalTime();
		GUICommonGodVoice.ShowText(((timeEnd > 0) ? LocalizationKeys.mp_ban_date_msg : LocalizationKeys.unknown_error).LocalizedFormat(dateTime), 5f);
	}
}
