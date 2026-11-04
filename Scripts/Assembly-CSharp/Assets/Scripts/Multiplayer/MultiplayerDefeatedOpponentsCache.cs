using System.Collections.Generic;
using DI;

namespace Assets.Scripts.Multiplayer
{
	public class MultiplayerDefeatedOpponentsCache : ITransientDependency
	{
		private class MpDefeatedOpponentsData
		{
			public int seasonNumber;

			public Dictionary<int, double> timesById = new Dictionary<int, double>();
		}

		private double m_defeatedPauseInSeconds;

		private MpDefeatedOpponentsData m_opponentsData = new MpDefeatedOpponentsData();

		public MultiplayerDefeatedOpponentsCache()
		{
			LoadDataWithVersionFix();
		}

		private void Save()
		{
			TryCatchPrefs.SaveObject("MultiplayerDefeatedOpponentsCache", m_opponentsData);
		}

		private void LoadDataWithVersionFix()
		{
			if (!TryCatchPrefs.HasKey("MultiplayerDefeatedOpponentsCache"))
			{
				Save();
				return;
			}
			m_opponentsData = TryCatchPrefs.LoadObject<MpDefeatedOpponentsData>("MultiplayerDefeatedOpponentsCache");
			if (m_opponentsData == null)
			{
				Dictionary<int, double> timesById = TryCatchPrefs.LoadObjectOrCreateDefault<Dictionary<int, double>>("MultiplayerDefeatedOpponentsCache");
				m_opponentsData = new MpDefeatedOpponentsData
				{
					timesById = timesById
				};
				Save();
			}
		}

		private Dictionary<int, double> CleanExpiredOpponents(double timeNow, ICollection<KeyValuePair<int, double>> opponents)
		{
			Dictionary<int, double> dictionary = new Dictionary<int, double>(opponents.Count);
			foreach (KeyValuePair<int, double> opponent in opponents)
			{
				if (!IsTimeExpired(timeNow, opponent.Value))
				{
					dictionary.Add(opponent.Key, opponent.Value);
				}
			}
			return dictionary;
		}

		public void Setup(int defeatedPauseInMinutes, int seasonNumber)
		{
			m_defeatedPauseInSeconds = defeatedPauseInMinutes * 60;
			if (m_opponentsData.seasonNumber != seasonNumber)
			{
				m_opponentsData.seasonNumber = seasonNumber;
				m_opponentsData.timesById.Clear();
				Save();
				return;
			}
			Dictionary<int, double> dictionary = CleanExpiredOpponents(TimerUtils.CurrentTime, m_opponentsData.timesById);
			if (dictionary.Count != m_opponentsData.timesById.Count)
			{
				m_opponentsData.timesById = dictionary;
				Save();
			}
		}

		public void Set(int? opponentId)
		{
			if (opponentId.HasValue)
			{
				if (m_opponentsData.timesById.ContainsKey(opponentId.Value))
				{
					m_opponentsData.timesById[opponentId.Value] = TimerUtils.CurrentTime;
				}
				else
				{
					m_opponentsData.timesById.Add(opponentId.Value, TimerUtils.CurrentTime);
				}
				Save();
			}
		}

		private bool IsTimeExpired(double currentTime, double lastTime)
		{
			return lastTime + m_defeatedPauseInSeconds < currentTime;
		}

		public bool IsBattleAvaliable(int? opponentId)
		{
			if (opponentId.HasValue)
			{
				if (m_opponentsData.timesById.ContainsKey(opponentId.Value))
				{
					double lastTime = m_opponentsData.timesById[opponentId.Value];
					return IsTimeExpired(TimerUtils.CurrentTime, lastTime);
				}
				return true;
			}
			return false;
		}
	}
}
