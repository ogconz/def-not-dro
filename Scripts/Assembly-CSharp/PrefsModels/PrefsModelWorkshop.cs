using System.Collections.Generic;
using GameOverlay;
using Services.UGC.Utils;
using SimpleJSON;

namespace PrefsModels
{
	public class PrefsModelWorkshop : BasePrefsModel
	{
		private Dictionary<ulong, int> m_localIDsMap = new Dictionary<ulong, int>();

		[JsonSerialize]
		private List<ulong> m_localIDs = new List<ulong>();

		private HashSet<ulong> m_reserved = new HashSet<ulong>();

		public override void PostLoad()
		{
			GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
			{
				_ = Overlay.instance;
				int num = -1;
				ulong[] array = m_localIDs.ToArray();
				foreach (ulong num2 in array)
				{
					num++;
					if (num2 != 0L)
					{
						if (!m_reserved.Contains(num2) && !UGCServiceProvider.instance.IsContentInstalled(num2))
						{
							m_localIDs[num] = 0uL;
							m_localIDsMap.Remove(num2);
						}
						else
						{
							m_localIDsMap[num2] = num;
						}
					}
				}
			});
		}

		public int ReserveForLocalFiles(ulong globalID)
		{
			m_reserved.Add(globalID);
			return GetLocalID(globalID);
		}

		public void MoveLocalID(ulong oldGlobalID, ulong newGlobalID)
		{
			if (m_localIDsMap.TryGetValue(oldGlobalID, out var value))
			{
				m_localIDsMap.Remove(oldGlobalID);
				m_localIDsMap.Add(newGlobalID, value);
			}
		}

		public int GetLocalID(ulong globalID)
		{
			if (m_localIDsMap.TryGetValue(globalID, out var value))
			{
				return value;
			}
			for (int i = 0; i < m_localIDs.Count; i++)
			{
				if (m_localIDs[i] == 0L)
				{
					m_localIDs[i] = globalID;
					m_localIDsMap[globalID] = i;
					return i;
				}
			}
			m_localIDs.Add(globalID);
			m_localIDsMap[globalID] = m_localIDs.Count - 1;
			return m_localIDs.Count - 1;
		}

		public void Unregister(ulong globalID)
		{
			int value = 0;
			if (m_localIDsMap.TryGetValue(globalID, out value))
			{
				m_localIDs[value] = 0uL;
				m_localIDsMap.Remove(globalID);
			}
		}
	}
}
