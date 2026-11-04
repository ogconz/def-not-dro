using System;
using System.Collections.Generic;
using System.Text;
using GameOverlay;

namespace SyncMultiplayer
{
	[Serializable]
	public class LobbyFilterState
	{
		public enum Flag
		{
			Ignore = 0,
			On = 1,
			Off = 2
		}

		public Dictionary<FilterButtonType, Flag> filterDict = new Dictionary<FilterButtonType, Flag>();

		public Dictionary<FilterButtonType, bool> ignoredFilterDict = new Dictionary<FilterButtonType, bool>();

		private Flag driftMode;

		private Flag timeAttack;

		private Flag clippingPoints;

		private Flag collisionType;

		private Flag vote;

		private Flag nightMode;

		private Flag password;

		private Flag fullRoom;

		private Flag friends;

		private Flag externalTrack = (Overlay.instance.IsUserGeneratedContentRestricted ? Flag.Off : Flag.Ignore);

		private Flag deviceAssist;

		private Flag tcMode;

		private Flag wdLimit;

		private StringBuilder m_sb = new StringBuilder();

		public LobbyFilterState()
		{
			filterDict[FilterButtonType.DriftMode] = driftMode;
			filterDict[FilterButtonType.TimeAttack] = timeAttack;
			filterDict[FilterButtonType.ClippingPoints] = clippingPoints;
			filterDict[FilterButtonType.CollisionType] = collisionType;
			filterDict[FilterButtonType.Vote] = vote;
			filterDict[FilterButtonType.NightMode] = nightMode;
			filterDict[FilterButtonType.Password] = password;
			filterDict[FilterButtonType.FullRoom] = fullRoom;
			filterDict[FilterButtonType.Friends] = friends;
			filterDict[FilterButtonType.ExternalTrack] = externalTrack;
			filterDict[FilterButtonType.AssistVersion] = deviceAssist;
			filterDict[FilterButtonType.tcMode] = tcMode;
			filterDict[FilterButtonType.wdLimit] = wdLimit;
		}

		public Flag GetFlagValue(FilterButtonType buttonType)
		{
			if (filterDict.TryGetValue(buttonType, out var value))
			{
				return value;
			}
			filterDict[buttonType] = Flag.Ignore;
			return Flag.Ignore;
		}

		public bool IsFlagEnabled(FilterButtonType buttonType)
		{
			if (filterDict.TryGetValue(buttonType, out var value))
			{
				return value == Flag.On;
			}
			filterDict[buttonType] = Flag.Ignore;
			return false;
		}

		public bool IsDefaultState()
		{
			bool result = true;
			foreach (KeyValuePair<FilterButtonType, Flag> item in filterDict)
			{
				if (item.Value != Flag.Ignore)
				{
					result = false;
					break;
				}
			}
			return result;
		}

		public void SetNextFlag(FilterButtonType buttonType)
		{
			Flag value;
			Flag flag = (filterDict.TryGetValue(buttonType, out value) ? value : Flag.Ignore);
			Flag value2 = Flag.Ignore;
			switch (flag)
			{
			case Flag.Ignore:
				value2 = ((buttonType != FilterButtonType.FullRoom) ? Flag.On : Flag.Off);
				break;
			case Flag.On:
				value2 = ((buttonType != FilterButtonType.Friends) ? Flag.Off : Flag.Ignore);
				break;
			case Flag.Off:
				value2 = Flag.Ignore;
				break;
			}
			filterDict[buttonType] = value2;
		}

		public void Reset()
		{
			foreach (object value in Enum.GetValues(typeof(FilterButtonType)))
			{
				if ((FilterButtonType)value == FilterButtonType.ExternalTrack)
				{
					filterDict[(FilterButtonType)value] = (Overlay.instance.IsUserGeneratedContentRestricted ? Flag.Off : Flag.Ignore);
				}
				else
				{
					filterDict[(FilterButtonType)value] = Flag.Ignore;
				}
			}
		}

		public string GetIgnoredFilters()
		{
			m_sb.Clear();
			foreach (KeyValuePair<FilterButtonType, bool> item in ignoredFilterDict)
			{
				if (item.Value)
				{
					m_sb.Append(item.Key);
					m_sb.Append(",");
				}
			}
			return m_sb.ToString();
		}

		public void SaveFlag(FilterButtonType filterType)
		{
			ignoredFilterDict[filterType] = filterDict[filterType] == Flag.Ignore;
			NetworkController.InstanceGame.lobbyFilterState = this;
		}
	}
}
