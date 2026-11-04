using System;
using System.Collections.Generic;
using GameOverlay;

namespace SharingLiveries
{
	public class LiveriesSharingService : BaseServerSharingService
	{
		private const string MAIN_ROUTE = "livery";

		private Dictionary<string, string> m_argsDict = new Dictionary<string, string>();

		private Dictionary<string, byte[]> m_fileParamsDict = new Dictionary<string, byte[]>();

		private string m_fetchCreatorEndpoint => "livery/liveryCreaterList";

		protected override string m_publishEndpoint => "livery/liveryPublic";

		protected override string m_fetchShortEndpoint => "livery/liveryLobby";

		protected override string m_fetchFullEndpoint => "livery/liveryList";

		protected override string m_fetchSingleEndpoint => "livery/liverySingle";

		protected override string m_markDownloadEndpoint => "livery/liveryMarkDownload";

		protected override string m_makeRatingEndpoint => "livery/liveryRating";

		protected override Dictionary<string, byte[]> PrepareFileParams()
		{
			return m_fileParamsDict;
		}

		protected override bool IsServerImplemented()
		{
			return true;
		}

		protected override bool PrepareUploadRequestParams<T>(out Dictionary<string, string> args, out string endPoint, object data)
		{
			m_argsDict.Clear();
			m_fileParamsDict.Clear();
			args = m_argsDict;
			endPoint = string.Empty;
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(SdrRestLiveryPublic))
			{
				endPoint = m_publishEndpoint;
				return PrepareUploadRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiveryLobby))
			{
				endPoint = m_fetchShortEndpoint;
				return PrepareShortRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiveryList))
			{
				endPoint = m_fetchFullEndpoint;
				return PrepareFullRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiverySingle))
			{
				endPoint = m_fetchSingleEndpoint;
				return PrepareSingleRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiveryMarkDownload))
			{
				endPoint = m_markDownloadEndpoint;
				return PrepareSingleRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiveryRating))
			{
				endPoint = m_makeRatingEndpoint;
				return PrepareRatingRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrRestLiveryCreaterList))
			{
				endPoint = m_fetchCreatorEndpoint;
				return PrepareCreatorRequestParams(data);
			}
			if (typeFromHandle == typeof(SdrNickname))
			{
				endPoint = base.m_fetchNicknameEndPoint;
				return PrepareNicknameRequestParams(data);
			}
			return false;
		}

		private bool PrepareUploadRequestParams(object data)
		{
			(string, int, int, int, string, bool?, byte[], byte[]) obj = ((string, int, int, int, string, bool?, byte[], byte[]))data;
			string item = obj.Item1;
			int item2 = obj.Item2;
			int item3 = obj.Item3;
			int item4 = obj.Item4;
			string item5 = obj.Item5;
			bool? item6 = obj.Item6;
			byte[] item7 = obj.Item7;
			byte[] item8 = obj.Rest.Item1;
			if (item2 <= 0 || item3 <= 0 || string.IsNullOrEmpty(item) || string.IsNullOrEmpty(item5) || item7 == null || item8 == null)
			{
				return false;
			}
			m_argsDict.Add("name", item);
			m_argsDict.Add("carId", item2.ToString());
			m_argsDict.Add("crossMask", ((int)Overlay.GetPlatform()).ToString());
			m_argsDict.Add("categoryMask", item3.ToString());
			m_argsDict.Add("dlcMask", item4.ToString());
			m_argsDict.Add("customLiveryId", item5);
			m_argsDict.Add("hidden", item6.ToString().ToLower());
			m_fileParamsDict.Add("dataFull", item7);
			m_fileParamsDict.Add("dataPreview", item8);
			return true;
		}

		private bool PrepareShortRequestParams(object data)
		{
			SharingLiveriesFilter liveryFilter = data as SharingLiveriesFilter;
			if (liveryFilter == null)
			{
				throw new NullReferenceException("NRE at LiveriesSharingService > PrepareShortRequestParams >>> need define liveryFilter");
			}
			m_argsDict.Add("carId", liveryFilter.carId.ToString());
			m_argsDict.Add("rating", liveryFilter.rating.ToString());
			m_argsDict.Add("download", liveryFilter.downloadCount.ToString());
			m_argsDict.Add("crossMask", liveryFilter.crossmask.ToString());
			m_argsDict.Add("categoryMask", GetCategoryMask().ToString());
			m_argsDict.Add("dlcMask", liveryFilter.dlcMask.ToString());
			m_argsDict.Add("minLayers", liveryFilter.layers.x.ToString());
			m_argsDict.Add("maxLayers", liveryFilter.layers.y.ToString());
			return true;
			int GetCategoryMask()
			{
				if (Enum.TryParse<SharingLiveryCategoryType>(liveryFilter.category, ignoreCase: true, out var result))
				{
					return (int)result;
				}
				return 0;
			}
		}

		private bool PrepareFullRequestParams(object data)
		{
			if (data is int num)
			{
				m_argsDict.Add("carId", num.ToString());
			}
			return true;
		}

		private bool PrepareSingleRequestParams(object data)
		{
			string value = data as string;
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			m_argsDict.Add("code", value);
			return true;
		}

		private bool PrepareCreatorRequestParams(object data)
		{
			var (value, num) = ((string, int))data;
			if (string.IsNullOrEmpty(value) || num <= 0)
			{
				return false;
			}
			m_argsDict.Add("code", value);
			m_argsDict.Add("carId", num.ToString());
			return true;
		}

		private bool PrepareRatingRequestParams(object data)
		{
			var (value, num) = ((string, int))data;
			if (string.IsNullOrEmpty(value) || num < 0)
			{
				return false;
			}
			m_argsDict.Add("code", value);
			m_argsDict.Add("rating", num.ToString());
			return true;
		}

		private bool PrepareNicknameRequestParams(object data)
		{
			if (data is string value)
			{
				m_argsDict.Add("platformUserId", value);
				return true;
			}
			return false;
		}
	}
}
