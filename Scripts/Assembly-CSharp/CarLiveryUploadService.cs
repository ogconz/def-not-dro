using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameOverlay;
using SyncMultiplayer;

public class CarLiveryUploadService
{
	private class SdrRestLiveryRtmp
	{
		public SdrLiveryRtmpData livery;
	}

	private class SdrLiveryRtmpData
	{
		public string playerId;

		public string pushHash;

		public DateTime pushDate;
	}

	private const string LIVERY_EXISTS_ENDPOINT = "/livery_rtmp/liveryRtmpExists";

	private const string LIVERY_PUSH_ENDPOINT = "/livery_rtmp/liveryRtmpPush";

	private const string TOKEN_KEY = "token";

	private const string HASH_KEY = "hash";

	private const string DATA_KEY = "data";

	private const int MAX_DAYS = 30;

	private const string DOWNLOAD_LIVERY_URL = "https://s3.eu-west-1.amazonaws.com/carx-dr1-steam/liveries_rtmp/{0}/{1}";

	private const string CONFIG_RC = "-rc.carx";

	private const string DEV_PREFIX = "dev";

	private const string RC_PREFIX = "rc";

	private const string PROD_PREFIX = "prod";

	private ApiClient m_apiClient;

	private readonly ConcurrentDictionary<string, string> m_requestArgs = new ConcurrentDictionary<string, string>();

	private readonly ConcurrentDictionary<string, byte[]> m_requestFileArgs = new ConcurrentDictionary<string, byte[]>();

	public async UniTask UploadCarLiveryIfNeed(string hash, byte[] data)
	{
		m_requestArgs.Clear();
		m_requestArgs["token"] = GameSync.token;
		bool liveryExistsRequestDone = false;
		bool liveryExists = false;
		bool gotErrorResponse = true;
		MasterClient().CallPost("/livery_rtmp/liveryRtmpExists", delegate(SdrRestLiveryRtmp existResp, Error error)
		{
			if (error.type != Error.EType.None)
			{
				GameLog.Print(AlertLevel.Error, "LiveryTransferSystem", $"Unable to check if livery Exists >> {error.type}");
			}
			else
			{
				liveryExists = hash == existResp.livery?.pushHash;
				if ((DateTime.UtcNow - existResp.livery.pushDate).Days > 30)
				{
					liveryExists = false;
				}
				gotErrorResponse = false;
			}
			liveryExistsRequestDone = true;
		}, null, m_requestArgs.ToDictionary((KeyValuePair<string, string> pair) => pair.Key, (KeyValuePair<string, string> pair) => pair.Value));
		await UniTask.WaitUntil(() => liveryExistsRequestDone);
		if (liveryExists || gotErrorResponse)
		{
			if (liveryExists)
			{
				GameLog.Print(AlertLevel.Notify, "LiveryTransferSystem", "Skip uploading livery - data already exists on server");
				NetworkController.InstanceGame.systems.Get<NetGameCarSwapSystem>().SEND_LiveryReady();
			}
			return;
		}
		m_requestArgs["hash"] = hash;
		m_requestFileArgs.Clear();
		m_requestFileArgs["data"] = data;
		bool requestDone = false;
		m_apiClient.CallPost("/livery_rtmp/liveryRtmpPush", delegate(SdrRestLiveryRtmp existResp, Error error)
		{
			if (error.type != Error.EType.None)
			{
				GameLog.Print(AlertLevel.Error, "LiveryTransferSystem", $"Unable to upload livery >> {error.type}");
			}
			else
			{
				GameLog.Print(AlertLevel.Notify, "LiveryTransferSystem", "Livery successfully uploaded");
			}
			requestDone = true;
		}, null, m_requestArgs.ToDictionary((KeyValuePair<string, string> pair) => pair.Key, (KeyValuePair<string, string> pair) => pair.Value), m_requestFileArgs.ToDictionary((KeyValuePair<string, byte[]> pair) => pair.Key, (KeyValuePair<string, byte[]> pair) => pair.Value));
		await UniTask.WaitUntil(() => requestDone);
		NetworkController.InstanceGame.systems.Get<NetGameCarSwapSystem>().SEND_LiveryReady();
	}

	public async UniTask<byte[]> DownloadLiveryData(PlayerId playerId)
	{
		string uri = $"https://s3.eu-west-1.amazonaws.com/carx-dr1-steam/liveries_rtmp/{GetLandscapePrefix()}/{playerId}";
		bool done = false;
		byte[] res = null;
		MasterClient().SendGetRequestForLiveryData(uri, delegate(byte[] result)
		{
			res = result;
			done = true;
		});
		while (!done)
		{
			await UniTask.Yield();
		}
		return res;
	}

	private ApiClient MasterClient()
	{
		if (m_apiClient == null)
		{
			m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "CarLiveryUploadService: " + error.debugMessage);
			});
		}
		return m_apiClient;
	}

	private string GetLandscapePrefix()
	{
		string empty = string.Empty;
		empty = "prod";
		ChangePrefixInCaseOfRedirect(ref empty);
		return empty;
	}

	private void ChangePrefixInCaseOfRedirect(ref string landscape)
	{
	}
}
