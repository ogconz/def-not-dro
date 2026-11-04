using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using GameInput;
using TrackLayout.Behaviours;
using TrackLayout.Data;
using TrackLayout.Style;
using UnityEngine;

[AudioFXBlocker(true)]
public class UISelectorTrackMetaConfigContext : BaseBindingBehaviourTarget
{
	public class RewardItemNode : BaseBindingBehaviourTarget
	{
		public StringProperty points = new StringProperty();

		public StringProperty exp = new StringProperty();

		public StringProperty coins = new StringProperty();

		public void FillFrom(RewardNode reward, GamePrefs prefs)
		{
			points.value = DriftControllerXDS.ConvertScoresToResultString(reward.value);
			coins.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt((float)reward.coinsReward * prefs.coinsFactor));
			exp.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt((float)reward.expReward * prefs.expFactor));
		}
	}

	public class BestItemNode : BaseBindingBehaviourTarget
	{
		public StringProperty points = new StringProperty();

		public StringProperty pos = new StringProperty();

		public void FillFrom(int posParam, float pointsParam)
		{
			pos.value = posParam.ToString();
			points.value = DriftControllerXDS.ConvertScoresToResultString(pointsParam);
		}
	}

	public readonly Property<string> trackName = new StringProperty();

	public readonly Property<string> configName = new StringProperty();

	public readonly Property<Material> materialSlowZonesInfo = new MaterialProperty();

	public string sceneName = "";

	private List<TrackConfigMetaItem> m_trackList = new List<TrackConfigMetaItem>();

	public TrackMetaConfigSelectorList tracksList;

	public GameObject previewNode;

	public GameObject rewardList;

	public Transform rewardPrefab;

	public GameObject bestFirstList;

	public GameObject bestSecondList;

	public GameObject slowZonesInfo;

	public Transform bestPrefab;

	private GamePrefs m_prefs;

	public RaceMode raceMode { get; private set; } = RaceMode.Freeride;

	public void Setup(string trackName, string sceneName, RaceMode raceMode)
	{
		this.trackName.value = trackName;
		this.sceneName = sceneName;
		this.raceMode = raceMode;
		ReloadTracks();
	}

	protected override void OnActivate()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		base.OnActivate();
		BringForwardByDepth();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.singlemode_xds.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = OnClickClose
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnSelect
			}
		});
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
	}

	protected override void OnDeactivate()
	{
		HideTopPanel();
		HideBottomPanel();
		base.OnDeactivate();
		UISelectorTrackContext uISelectorTrackContext = base.owner.Get<UISelectorTrackContext>();
		if (!(uISelectorTrackContext != null))
		{
			return;
		}
		uISelectorTrackContext.tracksList.ForEach(delegate(TrackSelectorListItem item)
		{
			Array.ForEach(item.subItems, delegate(TrackSubItem subItem)
			{
				subItem?.InitPreview();
			});
		});
	}

	private void ReloadTracks(bool fillVisualList = true)
	{
		m_trackList.Clear();
		m_trackList = GetItemList();
		if (fillVisualList)
		{
			tracksList.Clear();
			tracksList.AddRange(m_trackList);
			tracksList.RecalcList();
		}
		SetTrackInfo(GetSelection());
	}

	protected List<TrackConfigMetaItem> GetItemList()
	{
		List<TrackConfigMetaItem> list = new List<TrackConfigMetaItem>();
		foreach (TrackMetaConfig config in TrackMetaDB.GetConfigList(sceneName, raceMode))
		{
			list.Add(new TrackConfigMetaItem(this, config));
		}
		return list;
	}

	private void SetSelection(int trackId)
	{
		for (int i = 0; i < tracksList.GetCount(); i++)
		{
			_ = tracksList.items[i];
		}
	}

	public TrackConfigMetaItem GetSelection()
	{
		if (tracksList.selectedIndex > -1)
		{
			return tracksList.GetSelectedItem();
		}
		if (tracksList.GetCount() > 0)
		{
			return tracksList.GetItem(0);
		}
		return null;
	}

	public void SetTrackInfo(TrackConfigMetaItem trackInfo)
	{
		UpdateRewardList(trackInfo);
		UpdateBestLists(trackInfo);
		tracksList.selectedIndex = tracksList.IndexOf(trackInfo);
		string value = "";
		if (trackInfo != null)
		{
			value = trackInfo.meta.GetLocalizedName();
			TrackLayoutController instance = Singleton<TrackLayoutController>.instance;
			instance.SetDataRAW(trackInfo.meta.data, destroyActive: true);
			instance.BindPreview(previewNode.transform);
			RadarStyle radarStyle = instance.GetRadarStyle();
			if (radarStyle != null && radarStyle.useSlowZones && trackInfo.meta.data.speedRules.Length != 0 && IfOneSlowZonesDisplayed(trackInfo.meta))
			{
				if (!slowZonesInfo.activeSelf)
				{
					slowZonesInfo.SetActive(value: true);
				}
				PreviewSlowZoneInfo();
			}
			else if (slowZonesInfo.activeSelf)
			{
				slowZonesInfo.SetActive(value: false);
			}
		}
		configName.value = value;
	}

	private bool IfOneSlowZonesDisplayed(TrackMetaConfig config)
	{
		SpeedRuleData[] speedRules = config.data.speedRules;
		for (int i = 0; i < speedRules.Length; i++)
		{
			if (speedRules[i].display)
			{
				return true;
			}
		}
		return false;
	}

	private void PreviewSlowZoneInfo()
	{
		RadarSlowZoneStyle lastSlowZone = RadarSlowZoneStyle.lastSlowZone;
		if (lastSlowZone.currentMaterial != null)
		{
			Material material = new Material(lastSlowZone.currentMaterial);
			material.SetColor("_ColorOutLine", lastSlowZone.styleOutLine.color);
			material.SetFloat("_OutLineSize", lastSlowZone.styleOutLine.width / 20f);
			materialSlowZonesInfo.value = material;
		}
	}

	private void UpdateRewardList(TrackConfigMetaItem trackInfo)
	{
		if ((bool)rewardList)
		{
			rewardList.transform.DestroyChildren();
		}
		if (trackInfo == null || rewardPrefab == null)
		{
			return;
		}
		rewardPrefab.gameObject.SetActive(value: false);
		foreach (RewardNode item in trackInfo.meta.GetRewardListFor(raceMode))
		{
			RewardItemNode orAddComponent = UnityEngine.Object.Instantiate(rewardPrefab, rewardList.transform, worldPositionStays: false).gameObject.GetOrAddComponent<RewardItemNode>();
			orAddComponent.gameObject.SetActive(value: true);
			orAddComponent.FillFrom(item, m_prefs);
		}
	}

	private void UpdateBestLists(TrackConfigMetaItem trackInfo)
	{
		bestFirstList?.transform.DestroyChildren();
		bestSecondList?.transform.DestroyChildren();
		if (trackInfo == null || bestPrefab == null)
		{
			return;
		}
		string key = trackInfo.meta.EncodeName(raceMode, sceneName);
		bestPrefab.gameObject.SetActive(value: false);
		if (!m_prefs.stats.xdsRaceTop.TryGetValue(key, out var value))
		{
			return;
		}
		int num = 0;
		foreach (float item in value.firstRaceTop)
		{
			BestItemNode orAddComponent = UnityEngine.Object.Instantiate(bestPrefab, bestFirstList.transform, worldPositionStays: false).gameObject.GetOrAddComponent<BestItemNode>();
			orAddComponent.gameObject.SetActive(value: true);
			orAddComponent.FillFrom(++num, item);
		}
		num = 0;
		foreach (float item2 in value.secondRaceTop)
		{
			BestItemNode orAddComponent2 = UnityEngine.Object.Instantiate(bestPrefab, bestSecondList.transform, worldPositionStays: false).gameObject.GetOrAddComponent<BestItemNode>();
			orAddComponent2.gameObject.SetActive(value: true);
			orAddComponent2.FillFrom(++num, item2);
		}
	}

	private bool IsLockedTrack(int? id)
	{
		return false;
	}

	public void LoadTrack(TrackConfigMetaItem trackInfo)
	{
		m_prefs.globals.lastMetaConfig = trackInfo.meta;
		States.State<LoadRaceState>().LoadLocalRace(trackInfo.meta.data);
		SoundController.PlayFX(FXSample.RaceStartSound);
	}

	public void OnClickClose()
	{
		Hide();
	}

	public void OnSelect()
	{
		if (tracksList.selectedIndex > -1)
		{
			tracksList.GetSelectedItem()?.OnDoubleClick();
		}
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	public void NavigateLeft()
	{
		if (tracksList.selectedIndex > 0)
		{
			TrackMetaConfigSelectorList trackMetaConfigSelectorList = tracksList;
			int selectedIndex = trackMetaConfigSelectorList.selectedIndex - 1;
			trackMetaConfigSelectorList.selectedIndex = selectedIndex;
			SetTrackInfo(tracksList.GetSelectedItem());
		}
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	public void NavigateRight()
	{
		if (tracksList.selectedIndex < tracksList.GetCount() - 1)
		{
			TrackMetaConfigSelectorList trackMetaConfigSelectorList = tracksList;
			int selectedIndex = trackMetaConfigSelectorList.selectedIndex + 1;
			trackMetaConfigSelectorList.selectedIndex = selectedIndex;
			SetTrackInfo(tracksList.GetSelectedItem());
		}
	}
}
