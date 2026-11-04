using System;
using System.Collections;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.Networking;

namespace SharingLiveries
{
	public class LiverySharingListElementData
	{
		private Coroutine m_fetchFullDataCoroutine;

		public string liveryName { get; private set; }

		public string liveryId { get; private set; }

		public string code { get; private set; }

		public string publisherPlayerId { get; private set; }

		public string publisherNickname { get; private set; }

		public DateTime createDate { get; private set; }

		public int layersCount { get; private set; }

		public int downloadCount { get; private set; }

		public float rating { get; private set; }

		public bool isOfficial { get; private set; }

		public bool isPrivate { get; private set; } = true;

		public bool isLocalLivery { get; private set; }

		public string previewDataUrl { get; private set; }

		public CarVilylsInfo.VinylPreset preset { get; private set; }

		public LiverySharingListElementData(SdrLiveryShort item)
		{
			if (item != null)
			{
				liveryName = item.name;
				liveryId = item.customLiveryId;
				code = item.code;
				publisherPlayerId = item.createPlayerId;
				publisherNickname = item.createPlayerName;
				createDate = item.createDate;
				layersCount = item.dataLayers;
				downloadCount = item.download;
				previewDataUrl = item.dataPreview;
				rating = (float)item.rating / 10f;
				isOfficial = item.official;
				isPrivate = item.hidden;
			}
		}

		public LiverySharingListElementData(SdrLiveryFull item)
		{
			if (item != null)
			{
				liveryName = item.name;
				liveryId = item.customLiveryId;
				code = item.code;
				publisherPlayerId = item.createPlayerId;
				publisherNickname = Overlay.instance.player.nickName;
				createDate = item.createDate;
				layersCount = item.dataLayers;
				downloadCount = item.download;
				previewDataUrl = item.dataPreview;
				rating = (float)item.rating / 10f;
				isOfficial = item.official;
				isPrivate = item.hidden;
			}
		}

		public void SetNickname(string name)
		{
			publisherNickname = name;
		}

		public void UpdateLocalCustomLiveryStatus(bool localLivery)
		{
			isLocalLivery = localLivery;
		}

		public void StopFetchCorourine()
		{
			if (m_fetchFullDataCoroutine != null)
			{
				TimeController.StopNow(m_fetchFullDataCoroutine);
				m_fetchFullDataCoroutine = null;
			}
		}

		public void SetElementData(SdrLiveryFull item, Action<CarVilylsInfo.VinylPreset> onDone)
		{
			m_fetchFullDataCoroutine = TimeController.StartNow(FetchLayersDataAsync(onDone));
			IEnumerator FetchLayersDataAsync(Action<CarVilylsInfo.VinylPreset> action)
			{
				bool layersRequestDone = false;
				byte[] layersData = null;
				if (preset != null)
				{
					layersRequestDone = true;
				}
				else
				{
					yield return FetchResourceCouroutine(item.dataFull, delegate(byte[] data)
					{
						layersData = data;
						layersRequestDone = true;
					});
					yield return new WaitUntil(() => layersRequestDone);
				}
				if (preset == null)
				{
					preset = new CarVilylsInfo.VinylPreset
					{
						liveryId = item.customLiveryId,
						time = item.createDate.ToBinary(),
						layers = CustomTypes.DeserializeLiveryLayers(layersData)
					};
				}
				action.SafeCall(preset);
			}
		}

		public IEnumerator FetchResourceCouroutine(string url, Action<byte[]> onFetched)
		{
			if (string.IsNullOrEmpty(url))
			{
				onFetched.SafeCall(null);
				yield break;
			}
			using UnityWebRequest request = new UnityWebRequest(url)
			{
				downloadHandler = new DownloadHandlerBuffer()
			};
			yield return request.SendWebRequest();
			if (request.result != UnityWebRequest.Result.Success)
			{
				onFetched.SafeCall(null);
			}
			else
			{
				onFetched.SafeCall(request.downloadHandler.data);
			}
		}
	}
}
