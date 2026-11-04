using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarX.Gameplay.ClippingZone;
using CarX.Points;
using DI;
using PrefsModels;
using SyncMultiplayer;
using UnityEngine;

public class UIXDSInfoContext : BehaviourBindingTarget
{
	public enum DriftTextEvent
	{
		Start = 0,
		SideChange = 1,
		SectorChange = 2
	}

	private class ClipZoneBindingsItem : BaseListElementData
	{
		public IntProperty state = new IntProperty();

		public ColorProperty color = new ColorProperty();

		public bool failedInCurrentFrame;

		private BaseClipping m_host;

		private BaseClipping.State m_oldState;

		public ClipZoneBindingsItem(bool state)
		{
			ApplyState(state);
		}

		public ClipZoneBindingsItem(BaseClipping host)
		{
			m_host = host;
			m_oldState = host.state + 1;
			Update();
		}

		public void Update()
		{
			failedInCurrentFrame = false;
			if (m_oldState != m_host.state)
			{
				m_oldState = m_host.state;
				if (m_host.state == BaseClipping.State.Idle)
				{
					state.value = 0;
					color.value = Color.white;
				}
				else
				{
					state.value = ((m_host.state == BaseClipping.State.Activated) ? 1 : (-1));
					ApplyState(state.value == 1);
				}
				if (m_host.state == BaseClipping.State.Failed)
				{
					failedInCurrentFrame = true;
				}
			}
		}

		private void ApplyState(bool state)
		{
			color.value = (state ? Color.green : Color.red);
		}
	}

	private class SectorBindingsItem : BaseListElementData
	{
		public readonly BoolProperty active = new BoolProperty();

		public readonly StringProperty sector = new StringProperty();

		public readonly StringProperty points = new StringProperty("0");

		public readonly ColorProperty color = new ColorProperty();

		public readonly ColorProperty pointsColor = new ColorProperty();

		public readonly ListProperty clipzonesInfo = new ListProperty();

		public SectorBindingsItem(string name)
		{
			color.value = Color.white;
			pointsColor.value = Color.white;
			sector.value = name;
		}

		public void SetClipzonesInfo(ListProperty source)
		{
			clipzonesInfo.Clear();
			foreach (BaseListElementData item in source.value)
			{
				clipzonesInfo.Add(item);
			}
		}
	}

	public StringProperty syncSCValue = new StringProperty();

	public ColorProperty syncSCColor = new ColorProperty();

	private float m_eventSyncSCHideTime;

	public StringProperty startText = new StringProperty();

	private float m_eventStartHideTime;

	public StringProperty changeText = new StringProperty();

	private float m_eventChangeHideTime;

	public StringProperty sectorText = new StringProperty();

	private float m_sectorHideTime;

	public StringProperty badEventText = new StringProperty();

	public ColorProperty badEventColor = new ColorProperty();

	private float m_badEventHideTime;

	private BoolProperty isXDSActive = new BoolProperty();

	public BoolProperty isTandemMode = new BoolProperty();

	public BoolProperty isResultMode = new BoolProperty();

	public BoolProperty isSectorsVisible = new BoolProperty();

	public StringProperty driftPoints = new StringProperty();

	public ColorProperty driftPointsColor = new ColorProperty();

	public FloatProperty driftPointsNormalized = new FloatProperty();

	public FloatProperty posAngle = new FloatProperty();

	public FloatProperty negAngle = new FloatProperty();

	public FloatProperty tarAngle = new FloatProperty();

	public BoolProperty isTarAngleVisible = new BoolProperty(false);

	public FloatProperty danAngle = new FloatProperty();

	public BoolProperty isDanAngleVisible = new BoolProperty(false);

	public FloatProperty tandemFactor = new FloatProperty();

	public ColorProperty tandemExtraColor = new ColorProperty();

	public ColorProperty tandemBarColor = new ColorProperty();

	private sRaceXDS_Output m_prevData;

	private DriftControllerXDS.TargetSide lastTurn;

	public ListProperty sectorsInfo = new ListProperty();

	public ListProperty clipzonesInfo = new ListProperty();

	private GlobalModel m_globals;

	private HashSet<int> m_xdsLeftStates = new HashSet<int> { 8, 1, 5 };

	private HashSet<int> m_xdsRightStates = new HashSet<int> { 7, 0, 6 };

	public static UIXDSInfoContext instance { get; private set; }

	public DriftControllerXDS controller { get; private set; }

	public bool inRace => controller.inRace;

	public bool awaitingStart => controller.awaitingStart;

	public float scores => controller.score;

	public RaceCar tandemLeader => controller.tandemLeader;

	public bool activeXDS
	{
		get
		{
			if (isXDSActive.value)
			{
				return base.isActiveAndEnabled;
			}
			return false;
		}
	}

	public void Init(DriftControllerXDS xds)
	{
		controller = xds;
		controller.Init();
		DriftControllerXDS driftControllerXDS = controller;
		driftControllerXDS.onSectorDiscard = (Action<DisqualifyReason>)Delegate.Combine(driftControllerXDS.onSectorDiscard, new Action<DisqualifyReason>(OnSectorDiscard));
		DriftControllerXDS driftControllerXDS2 = controller;
		driftControllerXDS2.onRaceDisqualify = (Action<DisqualifyReason>)Delegate.Combine(driftControllerXDS2.onRaceDisqualify, new Action<DisqualifyReason>(OnRaceDisqualify));
		GameEvents.OnCarTCChanged.Subscribe(GameEventFlag.Race, delegate(GameEvents.CarTCInfo e)
		{
			ShowEvent(DriftTextEvent.Start, e.currentState ? LocalizationKeys.xds_esp_on.Localized() : LocalizationKeys.xds_esp_off.Localized());
		});
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		m_globals = gamePrefs.globals;
	}

	private void OnDestroy()
	{
		if (controller != null)
		{
			DriftControllerXDS driftControllerXDS = controller;
			driftControllerXDS.onSectorDiscard = (Action<DisqualifyReason>)Delegate.Remove(driftControllerXDS.onSectorDiscard, new Action<DisqualifyReason>(OnSectorDiscard));
			DriftControllerXDS driftControllerXDS2 = controller;
			driftControllerXDS2.onRaceDisqualify = (Action<DisqualifyReason>)Delegate.Remove(driftControllerXDS2.onRaceDisqualify, new Action<DisqualifyReason>(OnRaceDisqualify));
		}
		DiscardRace();
	}

	protected void Update()
	{
		if (controller == null)
		{
			return;
		}
		HideDepletedEvents();
		isResultMode.value = controller.resultMode;
		isSectorsVisible.value = sectorsInfo.count > 0 && controller.output.drift_cur_sector > 0;
		if (!controller.inRace && !controller.resultMode)
		{
			return;
		}
		if (controller.sectors.Length != sectorsInfo.count)
		{
			ResetUI();
		}
		if (sectorsInfo.count == 0)
		{
			return;
		}
		sRaceXDS_Output output = controller.output;
		float driftAngle = controller.driftAngle;
		float num = Mathf.Clamp(driftAngle / 80f, -1f, 1f);
		posAngle.value = ((driftAngle > 3f) ? num : 0f);
		negAngle.value = ((driftAngle < -3f) ? (0f - num) : 0f);
		UpdatePointsAndTargetAngleUI(ref output, num);
		if (controller.accumulatedBrake >= controller.maxAccumulatedBrake && controller.maxAccumulatedBrake > 0f && !isTandemMode.value && !(controller.tandemLeader == PlayerCarControl.instance.car))
		{
			ShowBadEvent(Color.red, LocalizationKeys.xds_slowdown.Localized(), canOverrideMessage: true, 0.5f);
		}
		if (controller.tandemFactor == 0f)
		{
			ShowBadEvent(Color.red, LocalizationKeys.xds_warn_overtake.Localized(), canOverrideMessage: false, 0.5f);
		}
		for (int i = 0; i < clipzonesInfo.count; i++)
		{
			ClipZoneBindingsItem obj = clipzonesInfo.GetByIndex(i) as ClipZoneBindingsItem;
			obj.Update();
			if (obj.failedInCurrentFrame)
			{
				ShowBadEvent(Color.red, LocalizationKeys.xds_rsn_clip.Localized(), canOverrideMessage: true);
			}
		}
		float num2 = Mathf.Clamp01((controller.tandemFactor - 0.8f) / 0.19999999f);
		Color b;
		if (controller.tandemFactor == 0f)
		{
			b = Color.red;
			tandemFactor.value = 1f;
			tandemExtraColor.value = Color.Lerp(tandemExtraColor.value, Color.clear, Time.deltaTime * 10f);
		}
		else
		{
			tandemFactor.value = num2;
			b = ((num2 < 0.25f) ? Color.Lerp(Color.red, Color.yellow, num2 * 4f) : ((num2 < 0.5f) ? Color.Lerp(Color.yellow, Color.cyan, (num2 - 0.25f) * 4f) : ((!(num2 < 0.75f)) ? Color.Lerp(new Color(0f, 0.75f, 0f), new Color(0f, 1f, 0f), (num2 - 0.75f) * 4f) : Color.Lerp(Color.cyan, new Color(0f, 0.75f, 0f), (num2 - 0.5f) * 4f))));
			tandemExtraColor.value = Color.Lerp(tandemExtraColor.value, Color.clear, Time.deltaTime * 10f);
		}
		tandemBarColor.value = Color.Lerp(tandemBarColor.value, b, Time.deltaTime * 10f);
		GenerateEvents(ref output);
		bool updateGrid = output.drift_cur_sector != m_prevData.drift_cur_sector;
		m_prevData = output;
		FillSectorsInfo(updateGrid);
	}

	private void UpdatePointsAndTargetAngleUI(ref sRaceXDS_Output output, float phaseAngle)
	{
		string value = "";
		bool flag = controller.IsSectorDiscarded(output.drift_cur_sector - 1);
		float value2 = 0.5f * (1f - phaseAngle);
		bool value3 = controller.driftAngleAbs > 3f;
		bool flag2 = false;
		if (controller.targetSide == DriftControllerXDS.TargetSide.Left || controller.targetSide == DriftControllerXDS.TargetSide.Right)
		{
			flag2 = controller.targetSide == DriftControllerXDS.TargetSide.Left;
			_ = controller.targetSide;
			lastTurn = (flag2 ? DriftControllerXDS.TargetSide.Left : DriftControllerXDS.TargetSide.Right);
		}
		else if (controller.targetSide == DriftControllerXDS.TargetSide.StrictLeft || controller.targetSide == DriftControllerXDS.TargetSide.StrictRight)
		{
			flag2 = controller.targetSide == DriftControllerXDS.TargetSide.StrictLeft;
			_ = controller.targetSide;
			lastTurn = (flag2 ? DriftControllerXDS.TargetSide.Left : DriftControllerXDS.TargetSide.Right);
		}
		else
		{
			flag2 = false;
		}
		DriftControllerXDS.TargetSide targetSide = DriftControllerXDS.TargetSide.NotActive;
		if (output.drift_cur_sector_blank)
		{
			value = "";
		}
		else if (flag)
		{
			value = "0";
		}
		else if (m_xdsLeftStates.Contains(output.drift_state))
		{
			targetSide = ((lastTurn == DriftControllerXDS.TargetSide.NotActive) ? DriftControllerXDS.TargetSide.Left : lastTurn);
		}
		else if (!m_xdsRightStates.Contains(output.drift_state))
		{
			value = ((output.drift_cur_scores != 0f) ? Mathf.RoundToInt(output.drift_cur_scores).ToString() : "");
		}
		else
		{
			targetSide = ((lastTurn == DriftControllerXDS.TargetSide.NotActive) ? DriftControllerXDS.TargetSide.Right : lastTurn);
		}
		if (targetSide != DriftControllerXDS.TargetSide.NotActive)
		{
			float num = output.driftang_tar;
			if (output.drift_cur_sector > 0 && num < 20f)
			{
				num = 20f;
			}
			value3 = output.driftang_tar > 3f;
			if (targetSide == DriftControllerXDS.TargetSide.Left)
			{
				value = " << ";
				value2 = 0.5f * (1f - Mathf.Abs(num) / 80f);
			}
			else
			{
				value = " >> ";
				value2 = 0.5f * (1f + Mathf.Abs(num) / 80f);
			}
		}
		if (controller.targetSide == DriftControllerXDS.TargetSide.Left || controller.targetSide == DriftControllerXDS.TargetSide.Right)
		{
			value2 = (flag2 ? 0.375f : 0.625f);
			value3 = true;
		}
		driftPoints.value = value;
		driftPointsNormalized.value = Mathf.Lerp(driftPointsNormalized.value, output.drift_cur_scores * 0.01f, Time.deltaTime * 5f);
		Color value4;
		if (flag)
		{
			value4 = Color.red;
		}
		else
		{
			value4 = new Color(0.93f, 0.78f, 0.36f);
			value4 = ((controller.bonusSign > 0f) ? Color.Lerp(value4, Color.green, controller.bonusSign) : Color.Lerp(value4, Color.red, 0f - controller.bonusSign));
		}
		driftPointsColor.value = value4;
		tarAngle.value = value2;
		isTarAngleVisible.value = value3;
		bool value5 = false;
		if (!flag)
		{
			if (controller.targetSide == DriftControllerXDS.TargetSide.Depleted)
			{
				value5 = true;
				danAngle.value = ((lastTurn == DriftControllerXDS.TargetSide.Left) ? 0.625f : 0.375f);
			}
			else if (controller.targetSide == DriftControllerXDS.TargetSide.StrictLeft)
			{
				value5 = true;
				danAngle.value = 0.625f;
			}
			else if (controller.targetSide == DriftControllerXDS.TargetSide.StrictRight)
			{
				value5 = true;
				danAngle.value = 0.375f;
			}
		}
		isDanAngleVisible.value = value5;
	}

	private string GetLastTurnString()
	{
		if (lastTurn != DriftControllerXDS.TargetSide.Left)
		{
			return " >> ";
		}
		return " << ";
	}

	protected void OnRaceDisqualify(DisqualifyReason reason)
	{
	}

	protected void OnSectorDiscard(DisqualifyReason reason)
	{
		string value = ((reason == DisqualifyReason.Unknown) ? string.Empty : LocalizationKeys.xds_rsn_.AddLocalized(reason.ToString().ToLower()));
		badEventText.value = value;
		badEventColor.value = new Color(1f, 0f, 0f, 1f);
		m_badEventHideTime = Time.time + 2f;
		m_prevData.drift_cur_sector = -1;
	}

	private void FillSectorsInfo(bool updateGrid)
	{
		_ = controller.xds;
		for (int i = 0; i < m_prevData.num_drift_sectors - 1; i++)
		{
			SectorBindingsItem sectorBindingsItem = sectorsInfo.GetByIndex(i) as SectorBindingsItem;
			sectorBindingsItem.active.value = i < m_prevData.drift_cur_sector;
			sRaceXDS_DriftSector info = controller.sectors[i].info;
			float num = controller.sectors[i].TokenZonesPercent();
			float mid_score = info.mid_score;
			sectorBindingsItem.sector.value = "S" + i;
			if (info.isblank)
			{
				sectorBindingsItem.points.value = " - BLANK -";
			}
			else
			{
				sectorBindingsItem.points.value = (controller.IsSectorDiscarded(i) ? 0f : (mid_score * info.max_scores * num)).ToString("0.0") + "/" + (100f * info.max_scores).ToString("0.0");
			}
		}
		SectorBindingsItem sectorBindingsItem2 = sectorsInfo.GetByIndex(m_prevData.num_drift_sectors - 1) as SectorBindingsItem;
		sectorBindingsItem2.active.value = m_prevData.drift_cur_sector == m_prevData.num_drift_sectors;
		sectorBindingsItem2.points.value = DriftControllerXDS.ConvertScoresToResultString(scores);
		if (updateGrid)
		{
			int num2 = m_prevData.drift_cur_sector - 1;
			if (num2 == sectorsInfo.count - 1)
			{
				sectorBindingsItem2.SetClipzonesInfo(clipzonesInfo);
			}
			if (num2 >= 0 && num2 < sectorsInfo.count)
			{
				SectorBindingsItem sectorBindingsItem3 = sectorsInfo.GetByIndex(num2) as SectorBindingsItem;
				sRaceXDS_DriftSector info2 = controller.sectors[num2].info;
				string text = ((num2 == sectorsInfo.count - 1) ? LocalizationKeys.xds_finish.Localized() : sectorBindingsItem3.sector.value);
				ShowEvent(DriftTextEvent.SectorChange, text + ((info2.max_scores == 0f) ? " Blank" : ""));
			}
			for (int j = 0; j < m_prevData.num_drift_sectors - 1; j++)
			{
				SectorBindingsItem obj = sectorsInfo.GetByIndex(j) as SectorBindingsItem;
				float a = ((j == m_prevData.drift_cur_sector - 1) ? 1f : 0.65f);
				Color value = (controller.IsSectorDiscarded(j) ? Color.red : Color.white);
				value.a = a;
				obj.pointsColor.value = value;
				value = Color.white;
				value.a = a;
				obj.color.value = value;
			}
		}
		else
		{
			int num3 = m_prevData.drift_cur_sector - 1;
			if (num3 >= 0 && num3 < sectorsInfo.count - 1)
			{
				SectorBindingsItem obj2 = sectorsInfo.GetByIndex(num3) as SectorBindingsItem;
				Color value2 = (controller.IsSectorDiscarded(num3) ? Color.red : Color.white);
				obj2.pointsColor.value = value2;
			}
		}
	}

	private void ResetUI()
	{
		sectorsInfo.Clear();
		sRaceXDS_Output output = controller.output;
		m_prevData = output;
		for (int i = 0; i < controller.sectors.Length - 1; i++)
		{
			sectorsInfo.Add(new SectorBindingsItem("S" + (i + 1)));
		}
		sectorsInfo.Add(new SectorBindingsItem(""));
		isXDSActive.value = controller.sectors.Length != 0;
		isTandemMode.value = controller.tandemLeader != null;
		ShowEvent(DriftTextEvent.Start, controller.car.useTC ? LocalizationKeys.xds_esp_on.Localized() : LocalizationKeys.xds_esp_off.Localized());
		UpdateClipzonesUI();
	}

	public void UpdateClipzonesUI()
	{
		clipzonesInfo.Clear();
		for (int i = 0; i < controller.clippingObjects.Count; i++)
		{
			ClipZoneBindingsItem data = new ClipZoneBindingsItem(controller.clippingObjects[i]);
			clipzonesInfo.Add(data);
			int sector = controller.clippingObjects[i].sector;
			if (sector >= 0 && sector < sectorsInfo.count)
			{
				(sectorsInfo.GetByIndex(sector) as SectorBindingsItem).clipzonesInfo.Add(data);
			}
		}
	}

	public void SetupTandemLead(RaceCar leadCar)
	{
		controller.SetupTandemLead(leadCar);
	}

	protected void GenerateEvents(ref sRaceXDS_Output output)
	{
		if (controller.bonusSign < 0f)
		{
			ShowBadEvent(Color.Lerp(Color.red, Color.yellow, 0.5f), LocalizationKeys.xds_rsn_badclipzone.Localized(), canOverrideMessage: true, 0.1f);
			controller.isContactDetected = false;
		}
		if (controller.isContactDetected)
		{
			ShowBadEvent(Color.Lerp(Color.red, Color.yellow, 0.5f), LocalizationKeys.xds_warn_contact.Localized(), canOverrideMessage: true, 1f);
			controller.isContactDetected = false;
		}
		if (!controller.IsSectorDiscarded(output.drift_cur_sector - 1) && !output.drift_cur_sector_blank && output.drift_cur_sector >= 0)
		{
			if (output.correction_factor > 0f)
			{
				ShowBadEvent(new Color(1f, 0f, 0f, output.correction_factor), LocalizationKeys.xds_warn_correction.Localized(), canOverrideMessage: false, 0.001f);
			}
			float currentMinSpeed = controller.GetCurrentMinSpeed();
			if (controller.car.carX.speedKMH < currentMinSpeed + 20f)
			{
				float t = (controller.car.carX.speedKMH - currentMinSpeed) / 20f;
				ShowBadEvent(Color.Lerp(Color.red, Color.yellow, t), LocalizationKeys.xds_warn_lowspeed.Localized(), canOverrideMessage: false, 0.001f);
			}
		}
		if (output.drift_init_time != 0f)
		{
			if (m_prevData.drift_init_time != output.drift_init_time)
			{
				ShowEvent(DriftTextEvent.SideChange, LocalizationKeys.xds_initiation_time.LocalizedFormat(output.drift_init_time.ToString("0.00")));
			}
			if (m_prevData.drift_init_max_spd != output.drift_init_max_spd)
			{
				float num = output.drift_init_max_spd;
				string arg = LocalizationKeys.settings_game_speed_unit_kmh.Localized().ToLower();
				if (!m_globals.speedKmh)
				{
					num /= 1.609344f;
					arg = LocalizationKeys.settings_game_speed_unit_mph.Localized().ToLower();
				}
				ShowEvent(DriftTextEvent.Start, $"{num:F0} {arg}");
				if (!controller.isSelfTandemLeader)
				{
					syncSCValue.value = (output.last_init_syncfactor * 100f).ToString("0");
					syncSCColor.value = Color.Lerp(Color.yellow, Color.green, output.last_init_syncfactor);
					m_eventSyncSCHideTime = Time.time + 2f;
				}
			}
		}
		if (m_prevData.drift_transfer_time_last != output.drift_transfer_time_last)
		{
			ShowEvent(DriftTextEvent.SideChange, LocalizationKeys.xds_initiation_time.LocalizedFormat(output.drift_transfer_time_last.ToString("0.00")));
		}
		if (!controller.isSelfTandemLeader && controller.isSideChanged)
		{
			controller.isSideChanged = false;
			syncSCValue.value = (output.last_transfer_syncfactor * 100f).ToString("0");
			syncSCColor.value = Color.Lerp(Color.yellow, Color.green, output.last_transfer_syncfactor);
			m_eventSyncSCHideTime = Time.time + 2f;
		}
	}

	protected void HideDepletedEvents()
	{
		if (m_eventStartHideTime != 0f && Time.time > m_eventStartHideTime)
		{
			startText.value = "";
			m_eventStartHideTime = 0f;
		}
		if (m_eventSyncSCHideTime != 0f && Time.time > m_eventSyncSCHideTime)
		{
			syncSCValue.value = "";
			m_eventSyncSCHideTime = 0f;
		}
		if (m_eventChangeHideTime != 0f && Time.time > m_eventChangeHideTime)
		{
			changeText.value = "";
			m_eventChangeHideTime = 0f;
		}
		if (m_badEventHideTime != 0f && Time.time > m_badEventHideTime)
		{
			badEventText.value = "";
			m_badEventHideTime = 0f;
		}
		if (m_sectorHideTime != 0f && Time.time > m_sectorHideTime)
		{
			sectorText.value = "";
			m_sectorHideTime = 0f;
		}
	}

	public void ShowBadEvent(Color color, string text, bool canOverrideMessage, float time = 2f)
	{
		if (m_badEventHideTime == 0f || canOverrideMessage)
		{
			if (time != 0f)
			{
				m_badEventHideTime = Time.time + time;
			}
			badEventText.value = text;
			badEventColor.value = color;
		}
	}

	protected void ShowEvent(DriftTextEvent evt, string text, float time = 2f)
	{
		switch (evt)
		{
		case DriftTextEvent.Start:
			startText.value = text;
			m_eventStartHideTime = Time.time + time;
			break;
		case DriftTextEvent.SideChange:
			changeText.value = text;
			m_eventChangeHideTime = Time.time + time;
			break;
		case DriftTextEvent.SectorChange:
			sectorText.value = text;
			m_sectorHideTime = Time.time + time;
			break;
		}
	}

	public void PrepareForStart(float startCountdownTime = 0f)
	{
		sectorsInfo.Clear();
		clipzonesInfo.Clear();
		controller.PrepareForStart(startCountdownTime);
		m_prevData.drift_cur_sector = -1;
		isXDSActive.value = false;
		driftPointsNormalized.value = 0f;
		driftPoints.value = string.Empty;
		isTarAngleVisible.value = false;
		isDanAngleVisible.value = false;
		lastTurn = DriftControllerXDS.TargetSide.NotActive;
	}

	public void HoldResult()
	{
		controller.FinishRace();
		isXDSActive.value = false;
	}

	public void DiscardRace(bool discardScore = true)
	{
		if (controller != null)
		{
			controller.DiscardRace(discardScore);
		}
		isXDSActive.value = false;
	}
}
