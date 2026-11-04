using System;
using System.Collections.ObjectModel;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using UnityEngine;

namespace PrefsModels
{
	public class PlayerProfileModel : BasePrefsModel
	{
		private GamePrefs m_prefs;

		private BaseModel m_base;

		public Action onPlayerExpChanged;

		private ReadOnlyCollection<int> m_expTableList;

		private int m_nextLevelExp;

		private int m_prevLevelExp;

		private int m_prevLevel;

		private int m_level;

		public string name => Overlay.instance.player.nickName;

		public int exp
		{
			get
			{
				return m_prefs.globals.playerExp;
			}
			set
			{
				m_prefs.globals.playerExp = value;
			}
		}

		public PlayerId id => Overlay.playerId;

		public int nextLevelExp => m_nextLevelExp;

		public int prevLevelExp => m_prevLevelExp;

		public int level => m_level;

		public override void Start()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_base = DependencyInjector.Resolve<BaseModel>();
			m_expTableList = m_base.GetSingle<GlobalGameSettings>().expTableList;
			GlobalModel globals = m_prefs.globals;
			globals.PlayerExpChanged = (Action)Delegate.Combine(globals.PlayerExpChanged, (Action)delegate
			{
				UpdatePlayerLevel();
				onPlayerExpChanged.Call();
			});
			UpdatePlayerLevel();
		}

		public override void Load()
		{
		}

		public override void Save()
		{
		}

		public override void Restore(string date)
		{
		}

		public override void MakeBackup(string date)
		{
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is PlayerProfileModel playerProfileModel))
			{
				return true;
			}
			if (level >= playerProfileModel.level)
			{
				return exp == playerProfileModel.exp;
			}
			return false;
		}

		public void UnlockNewLevelFeatures()
		{
			int num = level;
			int num2 = Mathf.Min(m_prefs.globals.lastLevelNotify, level);
			if (num != num2 && !Overlay.instance.IsDLCInstalled(DLCCode.FreeContent) && !(States.current as BaseGameState == null))
			{
				m_prefs.globals.lastLevelNotify = num;
				m_prefs.globals.Save();
			}
		}

		private void UpdatePlayerLevel()
		{
			m_level = GetLevel();
			if (m_level != m_prevLevel)
			{
				StatsManager.TrySetMaxStat(StatsManager.EStats.ProfileLevel, m_level);
				ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.LevelUnlocked, m_level);
				m_prevLevel = m_level;
				m_nextLevelExp = ((m_level == m_expTableList.Count) ? m_expTableList[m_expTableList.Count - 1] : m_expTableList[m_level]);
				m_prevLevelExp = ((m_level == m_expTableList.Count) ? m_expTableList[m_expTableList.Count - 1] : m_expTableList[m_level - 1]);
			}
		}

		private int GetLevel()
		{
			if (m_expTableList == null)
			{
				return 1;
			}
			int num = exp;
			for (int i = 0; i < m_expTableList.Count; i++)
			{
				if (m_expTableList[i] > num)
				{
					return i;
				}
			}
			return m_expTableList.Count;
		}
	}
}
