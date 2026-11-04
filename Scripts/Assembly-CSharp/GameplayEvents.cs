using System;
using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using TrackLayout.Data;

public class GameplayEvents : Singleton<GameplayEvents>
{
	[Dependency]
	private BaseModel m_model;

	public bool available;

	private TimeSpan m_cachedTimeForEnd = TimeSpan.Zero;

	private string m_cachedOutputStrTimeForEnd = string.Empty;

	private static EventSettings m_eventSettings;

	public EventSettings eventSettings
	{
		get
		{
			if (m_eventSettings == null)
			{
				m_eventSettings = ResourcesWrapper.Load<EventSettings>("HalloweenEventSettings");
			}
			return m_eventSettings;
		}
	}

	protected override void Initialization()
	{
		base.Initialization();
		DependencyInjector.InjectObjectsInto(this);
	}

	public bool IsContentAvailable(GameEventCode code)
	{
		if (code == GameEventCode.Halloween)
		{
			StatsManager.GetStat(StatsManager.EStats.HalloweenWin, out int value);
			return value > 0;
		}
		return true;
	}

	protected List<DB.Meta.Track> GetItemList()
	{
		List<DB.Meta.Track> list = new List<DB.Meta.Track>();
		foreach (DB.Meta.Track item in m_model.QueryAll<DB.Meta.Track>())
		{
			if (!(item.configuration != "Default") && item.enabled && !item.hidden && TrackMetaDB.HaveConfig(item.scene, RaceMode.HalloweenEvent))
			{
				list.Add(item);
			}
		}
		list.Sort(DB.Meta.Track.Sort);
		return list;
	}

	public string GetEventName(GameEventCode code)
	{
		try
		{
			return code.ToString();
		}
		catch
		{
			return string.Empty;
		}
	}

	public DateTime GetDateOfEndEvent(GameEventCode eventCode)
	{
		return eventCode switch
		{
			GameEventCode.Halloween => new DateTime(2019, 11, 4), 
			GameEventCode.Unknown => DateTime.MinValue, 
			_ => throw new ArgumentOutOfRangeException("eventCode", eventCode, null), 
		};
	}

	public bool GetTimeForEndEvent(GameEventCode eventCode, out TimeSpan result, out string resultStr, out bool locked)
	{
		TimeSpan timeSpan = TimeSpan.FromTicks(GetDateOfEndEvent(eventCode).Ticks - DateTime.Now.Ticks);
		locked = (float)timeSpan.Ticks < 0f;
		_ = string.Empty;
		if (timeSpan.Days > 0)
		{
			if (m_cachedTimeForEnd.Days != timeSpan.Days || m_cachedTimeForEnd.Hours != timeSpan.Hours)
			{
				m_cachedTimeForEnd = timeSpan;
				m_cachedOutputStrTimeForEnd = $"{timeSpan.Days:D2}d:{timeSpan.Hours:D2}h";
				result = timeSpan;
				resultStr = m_cachedOutputStrTimeForEnd;
				return true;
			}
			result = m_cachedTimeForEnd;
			resultStr = m_cachedOutputStrTimeForEnd;
			return false;
		}
		if (timeSpan.Hours > 0)
		{
			if (m_cachedTimeForEnd.Hours != timeSpan.Hours || m_cachedTimeForEnd.Minutes != timeSpan.Minutes || m_cachedTimeForEnd.Seconds != timeSpan.Seconds)
			{
				m_cachedTimeForEnd = timeSpan;
				m_cachedOutputStrTimeForEnd = $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
				result = m_cachedTimeForEnd;
				resultStr = m_cachedOutputStrTimeForEnd;
				return true;
			}
			result = m_cachedTimeForEnd;
			resultStr = m_cachedOutputStrTimeForEnd;
			return false;
		}
		if (m_cachedTimeForEnd.Minutes != timeSpan.Minutes || m_cachedTimeForEnd.Seconds != timeSpan.Seconds)
		{
			m_cachedTimeForEnd = timeSpan;
			m_cachedOutputStrTimeForEnd = $"{timeSpan.Minutes: D2}m:{timeSpan.Seconds:D2}s";
			result = m_cachedTimeForEnd;
			resultStr = m_cachedOutputStrTimeForEnd;
			return true;
		}
		result = m_cachedTimeForEnd;
		resultStr = m_cachedOutputStrTimeForEnd;
		return false;
	}

	public void ShowEventDialog(ContextOwner owner)
	{
		UIMessageBox.ShowHint(LocalizationKeys.content_available_before_complete_event.LocalizedFormat(), "", null, owner);
	}
}
