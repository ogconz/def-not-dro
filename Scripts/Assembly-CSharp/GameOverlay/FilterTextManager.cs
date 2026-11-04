using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SyncMultiplayer;
using UnityEngine;
using Utils.Filter;

namespace GameOverlay
{
	public class FilterTextManager : IFilterTextManager
	{
		private readonly int m_maxFilterRequests = 20;

		private readonly float m_filterRequestTimeDelta = 0.01f;

		private Dictionary<string, string> m_filteredNicknamesNamesCache = new Dictionary<string, string>();

		private Dictionary<string, string> m_filteredRoomsNamesCache = new Dictionary<string, string>();

		private Dictionary<string, string> m_filteredMessageCache = new Dictionary<string, string>();

		private float m_nextTimeToFilterRequest;

		private int m_currentFilterRequestsCount;

		private HashSet<string> m_currentFilterRequest = new HashSet<string>();

		private float m_tempLastTimeToFilterRequest;

		public FilterTextManager(int maxFilterRequests, float filterRequestTimeDelta)
		{
			m_maxFilterRequests = maxFilterRequests;
			m_filterRequestTimeDelta = filterRequestTimeDelta;
		}

		public FilterTextManager()
		{
		}

		public bool TryGetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, out string filteredText)
		{
			nonFiltered = UIUtils.FilterInvalidTags(nonFiltered);
			if (Application.isEditor || string.IsNullOrEmpty(nonFiltered))
			{
				filteredText = nonFiltered;
				return true;
			}
			filteredText = GetFilteredPattern(textFilterType, nonFiltered);
			if (textFilterType == TextFilterType.Unknown)
			{
				Debug.Log("FilterText :: textToFilter == null or wrong textFilterType");
				return true;
			}
			if (GetCacheByFilterType(textFilterType).TryGetValue(nonFiltered, out var value))
			{
				filteredText = value;
				return true;
			}
			if (textFilterType == TextFilterType.Nickname && platformId == Overlay.GetPlatform())
			{
				m_filteredNicknamesNamesCache[nonFiltered] = nonFiltered;
				filteredText = nonFiltered;
				return true;
			}
			return false;
		}

		public string GetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, Action<string> filteredCallback)
		{
			if (TryGetFilteredCachedText(textFilterType, platformId, nonFiltered, out var filteredText))
			{
				filteredCallback.SafeCall(filteredText);
				return filteredText;
			}
			TimeController.StartNow(TryToFilterText(textFilterType, nonFiltered, filteredCallback));
			return filteredText;
		}

		private bool IsCanRequestFilterNow()
		{
			if (m_currentFilterRequestsCount < m_maxFilterRequests)
			{
				return !(Time.unscaledTime < m_nextTimeToFilterRequest);
			}
			return false;
		}

		private IEnumerator TryToFilterText(TextFilterType textFilterType, string nonFiltered, Action<string> filteredCallback)
		{
			nonFiltered = UIUtils.FilterInvalidTags(nonFiltered);
			Dictionary<string, string> filterCacheDict = GetCacheByFilterType(textFilterType);
			if (m_currentFilterRequest.Contains(nonFiltered))
			{
				while (m_currentFilterRequest.Contains(nonFiltered))
				{
					yield return new WaitForSecondsRealtime(0.01f);
				}
				if (filterCacheDict.TryGetValue(nonFiltered, out var value))
				{
					string param = ProcessFiltrationResult(textFilterType, nonFiltered, value, filterCacheDict);
					filteredCallback.SafeCall(param);
				}
				else
				{
					filteredCallback.SafeCall(GetFilteredPattern(textFilterType, nonFiltered));
				}
				yield break;
			}
			m_currentFilterRequest.Add(nonFiltered);
			if (!IsCanRequestFilterNow())
			{
				yield return new WaitUntil(IsCanRequestFilterNow);
			}
			if (filterCacheDict.TryGetValue(nonFiltered, out var value2))
			{
				string param2 = ProcessFiltrationResult(textFilterType, nonFiltered, value2, filterCacheDict);
				filteredCallback.SafeCall(param2);
				m_currentFilterRequest.Remove(nonFiltered);
				yield break;
			}
			m_nextTimeToFilterRequest = Time.unscaledTime + m_filterRequestTimeDelta;
			m_currentFilterRequestsCount++;
			_ = Time.unscaledTime;
			string filteredText = string.Empty;
			yield return ExtendedTextFilter.ApplyFilterAsync(nonFiltered, OverlayFilterTextAsync).ToCoroutine(delegate(string result)
			{
				filteredText = result;
			});
			if (!string.IsNullOrEmpty(filteredText))
			{
				string param3 = ProcessFiltrationResult(textFilterType, nonFiltered, filteredText, filterCacheDict);
				filteredCallback.SafeCall(param3);
			}
			else
			{
				filteredCallback.SafeCall(GetFilteredPattern(textFilterType, nonFiltered));
			}
			m_currentFilterRequestsCount--;
			m_currentFilterRequest.Remove(nonFiltered);
		}

		private async UniTask<string> OverlayFilterTextAsync(string nonFiltered)
		{
			string filteredText = string.Empty;
			await Overlay.instance.TryFilterText(nonFiltered, delegate(string result)
			{
				filteredText = result;
			}).ToUniTask(KeepAliveManager.instance);
			return filteredText;
		}

		private string ProcessFiltrationResult(TextFilterType textFilterType, string nonFiltered, string filtered, Dictionary<string, string> filterCache)
		{
			string text = filtered;
			if ((!nonFiltered.Equals(filtered) && textFilterType == TextFilterType.Nickname) || (Overlay.instance.IsUserGeneratedContentRestricted && textFilterType == TextFilterType.Roomname))
			{
				filtered = GetFilteredPattern(textFilterType, nonFiltered);
			}
			if (nonFiltered.Equals(NetworkGame.USER_CREATED_ROOM_NAME))
			{
				filtered = text;
				NetworkGame.USER_CREATED_ROOM_NAME = text;
			}
			filterCache[nonFiltered] = filtered;
			return filtered;
		}

		private string GetFilteredPattern(TextFilterType textFilterType, string nonFiltered)
		{
			if (textFilterType == TextFilterType.Roomname || textFilterType == TextFilterType.Message)
			{
				return "****";
			}
			int count = Mathf.Clamp(nonFiltered.Length, 4, 10);
			return new string('*', count);
		}

		private Dictionary<string, string> GetCacheByFilterType(TextFilterType textFilterType)
		{
			return textFilterType switch
			{
				TextFilterType.Message => m_filteredMessageCache, 
				TextFilterType.Nickname => m_filteredNicknamesNamesCache, 
				_ => m_filteredRoomsNamesCache, 
			};
		}
	}
}
