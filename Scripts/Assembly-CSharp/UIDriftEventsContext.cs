using System.Collections.Generic;
using Binding.Base;
using CodeStage.AntiCheat.ObscuredTypes;
using HUD;
using UnityEngine;
using UnityEngine.U2D;

public class UIDriftEventsContext : BehaviourBindingTarget
{
	private static readonly LocalizationKeys.Key[][] namesTypes = new LocalizationKeys.Key[4][]
	{
		new LocalizationKeys.Key[13]
		{
			LocalizationKeys.driftevent_failx1,
			LocalizationKeys.driftevent_speed_x1,
			LocalizationKeys.driftevent_sidechange_x1,
			LocalizationKeys.driftevent_angle_x1,
			LocalizationKeys.driftevent_accuracy_x1,
			LocalizationKeys.driftevent_quality_x1,
			LocalizationKeys.driftevent_jump_x1,
			LocalizationKeys.driftevent_backward_x1,
			LocalizationKeys.driftevent_clippingzone_x1,
			LocalizationKeys.driftevent_clippingpoint_x1,
			LocalizationKeys.driftevent_customClippingEvent_x1,
			LocalizationKeys.driftevent_customClippingEvent_x1,
			LocalizationKeys.driftevent_kissTheWall_x1
		},
		new LocalizationKeys.Key[13]
		{
			LocalizationKeys.driftevent_failx1,
			LocalizationKeys.driftevent_speed_x2,
			LocalizationKeys.driftevent_sidechange_x2,
			LocalizationKeys.driftevent_angle_x2,
			LocalizationKeys.driftevent_accuracy_x2,
			LocalizationKeys.driftevent_quality_x2,
			LocalizationKeys.driftevent_jump_x2,
			LocalizationKeys.driftevent_backward_x2,
			LocalizationKeys.driftevent_clippingzone_x2,
			LocalizationKeys.driftevent_clippingpoint_x2,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_kissTheWall_x1
		},
		new LocalizationKeys.Key[13]
		{
			LocalizationKeys.driftevent_failx1,
			LocalizationKeys.driftevent_speed_x2,
			LocalizationKeys.driftevent_sidechange_x2,
			LocalizationKeys.driftevent_angle_x2,
			LocalizationKeys.driftevent_accuracy_x2,
			LocalizationKeys.driftevent_quality_x2,
			LocalizationKeys.driftevent_jump_x2,
			LocalizationKeys.driftevent_backward_x2,
			LocalizationKeys.driftevent_clippingzone_x2,
			LocalizationKeys.driftevent_clippingpoint_x3,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_kissTheWall_x1
		},
		new LocalizationKeys.Key[13]
		{
			LocalizationKeys.driftevent_failx1,
			LocalizationKeys.driftevent_speed_x2,
			LocalizationKeys.driftevent_sidechange_x2,
			LocalizationKeys.driftevent_angle_x2,
			LocalizationKeys.driftevent_accuracy_x2,
			LocalizationKeys.driftevent_quality_x2,
			LocalizationKeys.driftevent_jump_x2,
			LocalizationKeys.driftevent_backward_x2,
			LocalizationKeys.driftevent_clippingzone_x2,
			LocalizationKeys.driftevent_clippingpoint_x4,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_customClippingEvent_x2,
			LocalizationKeys.driftevent_kissTheWall_x1
		}
	};

	public static readonly ObscuredInt[][] countersEvents = new ObscuredInt[6][]
	{
		new ObscuredInt[NamesTypes[0].Length],
		new ObscuredInt[NamesTypes[0].Length],
		new ObscuredInt[NamesTypes[0].Length],
		new ObscuredInt[NamesTypes[0].Length],
		new ObscuredInt[NamesTypes[0].Length],
		new ObscuredInt[NamesTypes[0].Length]
	};

	private static UIDriftEventsContext m_current = null;

	public static bool silenceByPointsOptions = false;

	public UIDriftEvent DriftEventPrefab;

	[SerializeField]
	private SpriteAtlas m_icons;

	private UIDriftEvent m_showingEvent;

	private Queue<UIDriftEvent> m_queueEvents = new Queue<UIDriftEvent>();

	private float m_timeLastAdded;

	private const float m_timePauseStart = 0.7f;

	public static LocalizationKeys.Key[][] NamesTypes => namesTypes;

	private void Awake()
	{
		m_current = this;
	}

	private void OnDestroy()
	{
		m_current = null;
	}

	private void OnEnable()
	{
		m_queueEvents.Clear();
		silenceByPointsOptions = !GameConsole.Bool["r_points"];
	}

	private void OnDisable()
	{
		Release();
	}

	public void Release()
	{
		if ((bool)m_showingEvent)
		{
			m_showingEvent.Release();
			m_showingEvent = null;
		}
		foreach (UIDriftEvent queueEvent in m_queueEvents)
		{
			queueEvent.Release();
		}
		m_queueEvents.Clear();
	}

	private void AddEvent(BaseDriftAchievement.AchievementType driftEvent, LocalizationKeys.Key key, int type, int points)
	{
		if (!(DriftEventPrefab == null) && !KeepAliveManager.isUIHidden)
		{
			bool flag = driftEvent >= BaseDriftAchievement.AchievementType.ClippingZone && driftEvent != BaseDriftAchievement.AchievementType.KissTheWall;
			Sprite icon = null;
			if ((bool)m_icons && (type > 0 || driftEvent == BaseDriftAchievement.AchievementType.None))
			{
				icon = m_icons.GetSprite(driftEvent.ToString().ToLower());
			}
			UIDriftEvent uIDriftEvent = Object.Instantiate(DriftEventPrefab, base.transform);
			string inputText = key.LocalizedFormat("  " + UIUtils.DigitsWithSeparate(points));
			uIDriftEvent.Init(driftEvent, UIUtils.ConvertNGUIColorToRich(inputText), type > 0 || flag, icon, flag);
			uIDriftEvent.gameObject.SetActive(value: false);
			m_queueEvents.Enqueue(uIDriftEvent);
		}
	}

	private void StartEvent(UIDriftEvent ev)
	{
		if ((bool)m_showingEvent)
		{
			m_showingEvent.Skip();
		}
		ev.Show();
		m_showingEvent = ev;
		m_timeLastAdded = Time.time;
	}

	private void Update()
	{
		if (Time.time >= m_timeLastAdded + 0.7f && m_queueEvents.Count > 0)
		{
			StartEvent(m_queueEvents.Dequeue());
		}
	}

	private static bool IsNotValid()
	{
		if ((bool)m_current)
		{
			return silenceByPointsOptions;
		}
		return true;
	}

	public static bool HaveShowing()
	{
		if ((bool)m_current)
		{
			return m_current.m_showingEvent;
		}
		return false;
	}

	public static void ShowAddition(BaseDriftAchievement.AchievementType driftEvent, LocalizationKeys.Key key, int type, int points)
	{
		ref ObscuredInt reference = ref countersEvents[type][(byte)driftEvent];
		reference = (int)reference + points;
		if (!IsNotValid())
		{
			m_current.AddEvent(driftEvent, key, type, points);
		}
	}

	public static void Show(BaseDriftAchievement.AchievementType driftEvent, int type, int points)
	{
		ref ObscuredInt reference = ref countersEvents[type][(byte)driftEvent];
		reference = (int)reference + points;
		if (!IsNotValid())
		{
			LocalizationKeys.Key key = NamesTypes[type][(byte)driftEvent];
			m_current.AddEvent(driftEvent, key, type, points);
		}
	}

	public static void ResetStatsEvents()
	{
		ObscuredInt[][] array = countersEvents;
		foreach (ObscuredInt[] array2 in array)
		{
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = 0;
			}
		}
	}

	public static void ResetDriftFail()
	{
		if (!(m_current == null) && (bool)m_current.m_showingEvent && m_current.m_showingEvent.typeAchiev == BaseDriftAchievement.AchievementType.None)
		{
			m_current.m_showingEvent.Skip();
			m_current.m_showingEvent = null;
		}
	}

	public static void Clear()
	{
		if (!(m_current == null))
		{
			m_current.Release();
		}
	}
}
