using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Steamworks;
using Services.UGC.Utils;
using SharingSetups;
using TrackLayout.Data;
using UnityEngine;
using UnityEngine.U2D;

[AudioFXBlocker(true)]
public class UISelectorTrackContext : BaseBindingBehaviourTarget
{
	public enum SwitcherMode
	{
		Favorites = 0,
		Tracks = 1,
		WorkshopTracks = 2
	}

	[SerializeField]
	private SpriteAtlas m_setupsAtlas;

	[SerializeField]
	private UIBaseSharingPublication m_baseSharingPublication;

	[SerializeField]
	private CanvasGroup m_trackContainerCanvasGroup;

	private readonly BoolProperty m_showWorkshopSwitcher = new BoolProperty();

	private readonly BoolProperty m_isLoadingTracks = new BoolProperty();

	private readonly BoolProperty m_isLoadingTracksList = new BoolProperty();

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	private bool m_needShowLoadingOnEnter = true;

	private bool m_nonExternalNightModeActive;

	public TrackSelectorList tracksList;

	public readonly Property<bool> visibleDetailPanel = new BoolProperty();

	public readonly Property<bool> isReverse = new BoolProperty();

	public readonly Property<string> trackName = new StringProperty();

	public readonly Property<Sprite> spriteTrack = new SpriteProperty();

	public readonly BoolProperty isExternalTrackInfo = new BoolProperty();

	public readonly Property<string> goldLimit = new StringProperty();

	public readonly Property<string> silverLimit = new StringProperty();

	public readonly Property<string> bronzeLimit = new StringProperty();

	public readonly Property<string> bestResult = new StringProperty();

	public readonly Property<int> cupsLevel = new IntProperty();

	public readonly Property<bool> isTimeattack = new BoolProperty(false);

	public readonly Property<bool> isTraining = new BoolProperty(false);

	public readonly Property<bool> nightMode = new BoolProperty(false);

	public readonly Property<bool> isEventMode = new BoolProperty(false);

	public readonly Property<string> tracksCountText = new StringProperty();

	public readonly Property<string> pumpkinsCountText = new StringProperty();

	public readonly Property<float> tracksCount = new FloatProperty(0f);

	public readonly FloatProperty pumpkinsCount = new FloatProperty(0f);

	public readonly StringProperty carTitle = new StringProperty();

	private readonly IntProperty m_switcherMode = new IntProperty();

	private readonly SpriteProperty m_setupIcon = new SpriteProperty();

	private readonly BoolProperty m_showPublicationBlock = new BoolProperty();

	private readonly BoolProperty m_showEmptyMsg = new BoolProperty();

	private readonly BoolProperty m_showTrackDetails = new BoolProperty();

	private readonly BoolProperty m_showSwitcher = new BoolProperty();

	private readonly BoolProperty m_showExernalTrackMode = new BoolProperty();

	private List<DB.Meta.Track> m_tracks;

	private SwitcherMode m_switcherModeType;

	private int? m_selectedTrackId;

	private List<ActionHintData> m_actionHints = new List<ActionHintData>();

	private List<TrackSelectorListItem> m_trackItems = new List<TrackSelectorListItem>();

	private List<TrackSelectorListItem> m_favTracksResult = new List<TrackSelectorListItem>();

	private Dictionary<SwitcherMode, ulong> m_lastSelectedPerSwicherTracks = new Dictionary<SwitcherMode, ulong>();

	private int m_lastCarId;

	private bool m_subconfigsMode;

	private static List<ulong> m_corruptedMetaTracks = new List<ulong>();

	private bool m_isFavouriteToggleRequested;

	private CancellationTokenSource m_cancellationFetchTokenSource;

	private Coroutine m_setDataCoroutine;

	public GamePrefs prefs => m_prefs;

	public bool isLoadingTracks => m_isLoadingTracks.value;

	public RaceMode? raceMode { get; private set; }

	private void Awake()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		m_model = DependencyInjector.Resolve<BaseModel>();
	}

	private void Update()
	{
		foreach (TrackSelectorListItem item in tracksList.items)
		{
			TrackSubItem[] subItems = item.subItems;
			for (int i = 0; i < subItems.Length; i++)
			{
				subItems[i]?.Update();
			}
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		BringForwardByDepth();
		InitTracksMeta();
		if (nightMode.value != BaseRaceState.NightModeActive)
		{
			BaseRaceState.NightModeActive = nightMode.value;
		}
		m_nonExternalNightModeActive = BaseRaceState.NightModeActive;
		m_lastCarId = m_prefs.globals.lastCarId;
		m_switcherModeType = SwitcherMode.Tracks;
		m_switcherMode.value = (int)m_switcherModeType;
		m_showExernalTrackMode.value = !Overlay.instance.IsUserGeneratedContentRestricted;
		m_showWorkshopSwitcher.value = !Overlay.instance.IsUserGeneratedContentRestricted;
		if (m_prefs.globals.lastTrackId == -1)
		{
			m_prefs.globals.lastTrackId = FindDefaultUnlockedTrackId();
		}
		if (!m_selectedTrackId.HasValue)
		{
			m_selectedTrackId = m_prefs.globals.lastTrackId;
		}
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		if (raceMode.HasValue)
		{
			RefreshTopPanel(raceMode.Value);
		}
		CancelFetchingTracks();
		UpdateBottomPanel();
	}

	protected override void OnDeactivate()
	{
		HideTopPanel();
		HideBottomPanel();
		UnloadResources();
		CancelFetchingTracks();
		ResetPublicationView();
		TrackSubItem.DisposeAllStaticResources();
		SetLoadingScreen(needActivate: false);
		StopSetDataCoroutine();
		base.OnDeactivate();
	}

	private void ResetPublicationView()
	{
		if (!m_subconfigsMode)
		{
			m_baseSharingPublication.ResetView();
		}
	}

	public void OnSwitchMode(bool next)
	{
		if (!m_isLoadingTracks.value)
		{
			int switcherModeType = (int)m_switcherModeType;
			int num = (next ? 1 : (-1));
			switcherModeType += num;
			int length = Enum.GetValues(typeof(SwitcherMode)).Length;
			if (switcherModeType >= 0 && switcherModeType <= length - 1 && (m_showExernalTrackMode.value || switcherModeType != 2))
			{
				SetSwitcherMode(switcherModeType);
			}
		}
	}

	public void SetSwitcherMode(int switcherMode)
	{
		if (m_switcherMode.value != switcherMode && !m_isLoadingTracks.value)
		{
			TrackSubItem selection = GetSelection();
			if (selection != null)
			{
				ulong value = (selection.track.external ? selection.track.externalId : ((ulong)selection.track.id));
				m_lastSelectedPerSwicherTracks[m_switcherModeType] = value;
			}
			m_switcherModeType = (SwitcherMode)switcherMode;
			m_switcherMode.value = switcherMode;
			ResetPublicationView();
			ReloadTracks(needShowLoadingScreen: true).Forget();
			m_needShowLoadingOnEnter = m_switcherModeType == SwitcherMode.WorkshopTracks;
		}
	}

	private void InitTracksMeta()
	{
		m_tracks = m_model.QueryAll<DB.Meta.Track>();
	}

	private void SetLoadingScreen(bool needActivate)
	{
		m_isLoadingTracks.value = needActivate;
		m_trackContainerCanvasGroup.alpha = ((needActivate || m_isLoadingTracksList.value) ? 0f : 1f);
		if (needActivate)
		{
			HideBottomPanel();
		}
	}

	private async UniTask ReloadTracks(bool needShowLoadingScreen)
	{
		UnloadResources();
		m_trackItems.Clear();
		if (needShowLoadingScreen)
		{
			SetLoadingScreen(needActivate: true);
			m_showTrackDetails.value = false;
		}
		switch (m_switcherModeType)
		{
		case SwitcherMode.Favorites:
			m_trackItems = GetItemListFavorite();
			break;
		case SwitcherMode.Tracks:
		case SwitcherMode.WorkshopTracks:
			m_trackItems = GetItemList();
			break;
		}
		tracksList.Clear();
		tracksList.AddRange(m_trackItems);
		tracksList.RecalcList();
		ResetVirtualContentRectOffset();
		ulong trackId = 0uL;
		if (m_lastSelectedPerSwicherTracks.TryGetValue(m_switcherModeType, out var value))
		{
			trackId = value;
		}
		else if (m_switcherModeType == SwitcherMode.Tracks)
		{
			trackId = (ulong)m_prefs.globals.lastTrackId;
		}
		if (needShowLoadingScreen)
		{
			TimeController.AddCallback(0.5f, delegate
			{
				if (!base.isActive)
				{
					UnloadResources();
				}
				else
				{
					SetLoadingScreen(needActivate: false);
					UpdateEmptyMessageVisibility();
					UpdateBottomPanel();
				}
			});
		}
		else
		{
			UpdateEmptyMessageVisibility();
		}
		LoadingResources();
		UpdateSelection(trackId);
	}

	private void UpdateEmptyMessageVisibility()
	{
		m_showEmptyMsg.value = m_trackItems.Count == 0;
		m_showTrackDetails.value = !m_showEmptyMsg.value;
	}

	private void ResetVirtualContentRectOffset()
	{
		RectTransform rectTransform = tracksList.contentContainer as RectTransform;
		if (rectTransform != null)
		{
			Vector2 offsetMin = (rectTransform.offsetMax = Vector2.zero);
			rectTransform.offsetMin = offsetMin;
		}
	}

	private void CancelFetchingTracks()
	{
		if (m_cancellationFetchTokenSource != null && !m_cancellationFetchTokenSource.IsCancellationRequested)
		{
			m_cancellationFetchTokenSource.Cancel();
			m_cancellationFetchTokenSource.Dispose();
		}
	}

	public void SetTrackInfo(DB.Meta.Track track)
	{
		TrackSubItem selection = GetSelection();
		if (selection?.track.id == track.id)
		{
			SetTrackInfo(selection);
		}
	}

	public void UpdateExternalSubItem(ulong externalId)
	{
		TrackSubItem trackSubItem = FindTrackSubItem(externalId);
		if (trackSubItem != null)
		{
			trackSubItem.UpdatePreview();
			TrackSubItemController trackSubItemController = trackSubItem.trackSubItemController;
			if (trackSubItemController != null)
			{
				trackSubItemController.UpdateItem();
			}
		}
	}

	public TrackSubItemController GetExternalTrackSubItemController(ulong externalId)
	{
		return FindTrackSubItem(externalId)?.trackSubItemController;
	}

	public void AddCorruptedTrackList(ulong trackId)
	{
		if (!m_corruptedMetaTracks.Contains(trackId))
		{
			m_corruptedMetaTracks.Add(trackId);
		}
	}

	public static bool IsCorruptedExternalTrack(ulong externalTrackId)
	{
		return m_corruptedMetaTracks.Contains(externalTrackId);
	}

	private void LoadingResources()
	{
		if (tracksList == null)
		{
			return;
		}
		foreach (TrackSelectorListItem trackItem in m_trackItems)
		{
			trackItem.LoadResources();
			trackItem.UpdatePreview();
		}
	}

	private void UpdateSelection(ulong trackId)
	{
		TimeController.StartNow(UpdateSelectionAsync(trackId));
		IEnumerator UpdateSelectionAsync(ulong selection)
		{
			yield return new WaitUntil(() => base.uiBuilded);
			SetSelection(selection);
			SetTrackInfo(GetSelection());
		}
	}

	private List<TrackSelectorListItem> GetItemList(bool fetchFavorites = false)
	{
		List<TrackSelectorListItem> list = new List<TrackSelectorListItem>();
		m_tracks.Sort(DB.Meta.Track.Sort);
		TrackSubItem[] subItems = new TrackSubItem[5];
		int num = 0;
		m_subconfigsMode = false;
		if (raceMode == RaceMode.XDS)
		{
			foreach (DB.Meta.Track track in m_tracks)
			{
				if (track.enabled && !track.hidden && !(track.configuration != "Default") && TrackMetaDB.HaveConfig(track.scene, raceMode.Value))
				{
					m_subconfigsMode = true;
					break;
				}
			}
		}
		foreach (DB.Meta.Track track2 in m_tracks)
		{
			if ((track2.external && m_switcherModeType == SwitcherMode.Tracks) || (!track2.external && m_switcherModeType == SwitcherMode.WorkshopTracks) || !track2.enabled || track2.hidden || (raceMode == RaceMode.Dyno && track2.configuration == "Reversed"))
			{
				continue;
			}
			if (raceMode == RaceMode.HalloweenEvent)
			{
				if (track2.configuration != "Default" || !TrackMetaDB.HaveConfig(track2.scene, raceMode.Value))
				{
					continue;
				}
			}
			else if (m_subconfigsMode && (track2.configuration != "Default" || !TrackMetaDB.HaveConfig(track2.scene, raceMode.Value)))
			{
				continue;
			}
			TrackSubItem trackSubItem = new TrackSubItem(this, track2, m_prefs.playerProfile.level, IsFavoriteTrack(track2));
			if (!fetchFavorites || IsFavoriteTrack(track2))
			{
				subItems[num] = trackSubItem;
				num++;
				if (num > 3)
				{
					list.Add(MakeListItem(ref subItems));
					num = 0;
				}
			}
		}
		if (num > 0)
		{
			list.Add(MakeListItem(ref subItems));
		}
		return list;
	}

	private List<TrackSelectorListItem> GetItemListFavorite()
	{
		m_favTracksResult.Clear();
		m_tracks.Sort(DB.Meta.Track.Sort);
		TrackSubItem[] subItems = new TrackSubItem[5];
		int num = 0;
		m_subconfigsMode = false;
		foreach (DB.Meta.Track track in m_tracks)
		{
			if (!track.enabled || track.hidden || (track.external && Overlay.instance.IsUserGeneratedContentRestricted) || (raceMode == RaceMode.Dyno && track.configuration == "Reversed"))
			{
				continue;
			}
			if (raceMode == RaceMode.HalloweenEvent)
			{
				if (track.configuration != "Default" || !TrackMetaDB.HaveConfig(track.scene, raceMode.Value))
				{
					continue;
				}
			}
			else if (m_subconfigsMode && (track.configuration != "Default" || !TrackMetaDB.HaveConfig(track.scene, raceMode.Value)))
			{
				continue;
			}
			if (IsFavoriteTrack(track))
			{
				TrackSubItem trackSubItem = new TrackSubItem(this, track, m_prefs.playerProfile.level, isFavorite: true);
				subItems[num] = trackSubItem;
				num++;
				if (num > 3)
				{
					m_favTracksResult.Add(MakeListItem(ref subItems));
					num = 0;
				}
			}
		}
		if (num > 0)
		{
			m_favTracksResult.Add(MakeListItem(ref subItems));
		}
		return m_favTracksResult;
	}

	private bool IsFavoriteTrack(DB.Meta.Track track)
	{
		ulong trackId = (track.external ? track.externalId : ((ulong)track.id));
		return m_prefs.favoriteTrack.IsFavorite(trackId);
	}

	private TrackSelectorListItem MakeListItem(ref TrackSubItem[] subItems)
	{
		TrackSelectorListItem trackSelectorListItem = new TrackSelectorListItem();
		for (int i = 0; i < subItems.Length; i++)
		{
			if (subItems[i] != null)
			{
				subItems[i].parentItem = trackSelectorListItem;
			}
			trackSelectorListItem.SetSubItem(i, subItems[i]);
			subItems[i] = null;
		}
		return trackSelectorListItem;
	}

	public int FindDefaultUnlockedTrackId()
	{
		foreach (DB.Meta.Track track in m_tracks)
		{
			if (!track.defaultLocked && !track.hidden)
			{
				return track.id;
			}
		}
		return -1;
	}

	public void SetRaceMode(RaceMode mode)
	{
		raceMode = mode;
		isTimeattack.value = mode == RaceMode.TimeAttack;
		isTraining.value = mode == RaceMode.Freeride || mode == RaceMode.XDS || mode == RaceMode.Dyno;
		m_showSwitcher.value = mode == RaceMode.Freeride;
		isEventMode.value = mode == RaceMode.HalloweenEvent;
		if (isEventMode.value)
		{
			isTraining.value = true;
		}
		bool flag = m_switcherModeType == SwitcherMode.WorkshopTracks;
		ReloadTracks(m_needShowLoadingOnEnter || flag);
		m_needShowLoadingOnEnter = flag;
		ulong num = (ulong)m_prefs.globals.lastTrackId;
		SetTrackInfo(num);
		if (m_trackItems.Count > 0)
		{
			int num2 = 0;
			foreach (TrackSelectorListItem trackItem in m_trackItems)
			{
				num2 += trackItem.subItems.Count((TrackSubItem x) => x != null);
			}
			tracksCountText.value = LocalizationKeys.halloweenEventTracksCount.LocalizedFormat(Mathf.Min(m_prefs.eventsData.halloweenLocksTracks.Count, num2), num2);
			tracksCount.value = (float)m_prefs.eventsData.halloweenLocksTracks.Count / (float)num2;
		}
		if (Singleton<GameplayEvents>.instance.eventSettings != null)
		{
			int maxCountPumpkins = Singleton<GameplayEvents>.instance.eventSettings.maxCountPumpkins;
			pumpkinsCountText.value = LocalizationKeys.halloweenEventPumpkinsCount.LocalizedFormat(Mathf.Min(m_prefs.eventsData.pumpkinsCount, maxCountPumpkins), maxCountPumpkins);
			pumpkinsCount.value = (float)m_prefs.eventsData.pumpkinsCount / (float)maxCountPumpkins;
		}
		UpdateSelection(num);
		UpdateByLockedState();
		RefreshTopPanel(mode);
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_lastCarId);
		PlayerCar byId = m_model.GetById<PlayerCar>(m_lastCarId);
		carTitle.value = byId.name;
		PlayerCarProfile byId2 = m_model.GetById<PlayerCarProfile>(profileIdForCar);
		m_setupIcon.value = m_setupsAtlas.GetSprite(byId2.GetProfileIconName());
		foreach (TrackSelectorListItem item in tracksList.items)
		{
			TrackSubItem[] subItems = item.subItems;
			foreach (TrackSubItem trackSubItem in subItems)
			{
				if (trackSubItem == null)
				{
					continue;
				}
				int trackId = trackSubItem.track.id;
				IRaceNorm raceNorm = null;
				switch (mode)
				{
				case RaceMode.Drift:
					raceNorm = m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == trackId);
					break;
				case RaceMode.TimeAttack:
					raceNorm = m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == trackId && x.carId == m_lastCarId);
					break;
				}
				if (raceNorm == null)
				{
					raceNorm = new EmptyRaceNormMeta();
				}
				trackSubItem.SetCupsInfo(raceNorm);
				RaceCupType raceCupType = RaceCupType.None;
				RaceResult raceResult = UIUtils.GetRaceResult(trackId, mode);
				if (raceResult != null)
				{
					switch (mode)
					{
					case RaceMode.Drift:
						raceCupType = raceNorm.GetCupByValue(raceResult.recordDriftPoints);
						break;
					case RaceMode.TimeAttack:
						raceCupType = raceNorm.GetCupByValue(raceResult.GetBestLapTime());
						break;
					default:
						trackSubItem.SetBestResult(string.Empty);
						break;
					}
				}
				if (raceCupType != RaceCupType.None)
				{
					trackSubItem.SetCupIndex((int)raceCupType);
				}
				switch (mode)
				{
				case RaceMode.Drift:
					trackSubItem.SetBestResult(UIUtils.MakeViewBestDriftPoints(trackId));
					break;
				case RaceMode.TimeAttack:
					trackSubItem.SetBestResult(UIUtils.MakeViewBestTimeAttackPoints(trackId));
					break;
				default:
					trackSubItem.SetBestResult(string.Empty);
					break;
				}
				trackSubItem.UpdatePreview();
			}
		}
	}

	public void RefreshTopPanel(RaceMode mode)
	{
		switch (mode)
		{
		case RaceMode.Drift:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.singlemode_dirft.Localized());
			break;
		case RaceMode.TimeAttack:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.singlemode_timeattack.Localized());
			break;
		case RaceMode.Freeride:
		{
			LocalizationKeys.Key key = (UISetupsSharingContext.isSetupsSharingActive ? LocalizationKeys.nav_hint_goto_testdrive : LocalizationKeys.singlemode_training);
			ShowTopPanel(ETopPanelState.Title, key.Localized());
			break;
		}
		case RaceMode.XDS:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.singlemode_xds.Localized());
			break;
		default:
			ShowTopPanel(ETopPanelState.Title);
			break;
		}
	}

	private TrackSubItem FindTrackSubItem(ulong trackId)
	{
		foreach (TrackSelectorListItem item in tracksList.items)
		{
			TrackSubItem[] subItems = item.subItems;
			foreach (TrackSubItem trackSubItem in subItems)
			{
				if (trackSubItem != null && (trackSubItem.track.external ? ((long)trackSubItem.track.externalId) : ((long)trackSubItem.track.id)) == (long)trackId)
				{
					return trackSubItem;
				}
			}
		}
		return null;
	}

	private void SetSelection(ulong trackId)
	{
		ClearSelection();
		TrackSubItem trackSubItem = FindTrackSubItem(trackId);
		if (trackSubItem != null)
		{
			SetSelectionFromSubItem(trackSubItem);
			return;
		}
		tracksList.selectedIndex = 0;
		if (tracksList.items.Count > 0)
		{
			tracksList.items[0].selectionIndex = 0;
		}
	}

	public TrackSubItem GetSelection()
	{
		if (tracksList.selectedIndex > -1)
		{
			return tracksList.GetSelectedItem()?.GetSelectedSubItem();
		}
		return null;
	}

	private void SetTrackInfo(ulong trackId)
	{
		SetTrackInfo(FindTrackSubItem(trackId));
	}

	public void SetTrackInfo(TrackSubItem trackInfo)
	{
		visibleDetailPanel.value = trackInfo != null;
		if (visibleDetailPanel.value)
		{
			isReverse.value = trackInfo.isReverse;
			trackInfo?.UpdatePreview();
			trackName.value = (trackInfo.isExternal ? LocalizationKeys.freecam_mode_title_description.Localized() : trackInfo.name);
			goldLimit.value = trackInfo.goldLimit;
			isExternalTrackInfo.value = trackInfo.track.external;
			spriteTrack.value = (trackInfo.track.external ? null : ResourcesWrapper.Load<Sprite>("ui/tracks/minimaps/" + trackInfo.track.largeIcon, "temp"));
			silverLimit.value = trackInfo.silverLimit;
			bronzeLimit.value = trackInfo.bronzeLimit;
			bestResult.value = ((string.IsNullOrEmpty(trackInfo.bestResult) || trackInfo.bestResult.Contains("---")) ? "" : trackInfo.bestResult);
			cupsLevel.value = trackInfo.cupIndex;
			if (cupsLevel.value < 1)
			{
				cupsLevel.value = 4;
			}
			ClearSelection();
			SetSelectionFromSubItem(trackInfo);
			StopSetDataCoroutine();
			if (m_showPublicationBlock != null)
			{
				bool isExternal = trackInfo.isExternal;
				m_showPublicationBlock.value = isExternal;
				m_baseSharingPublication.ResetView();
				m_baseSharingPublication.SetIconVisibility(!isExternal);
				if (isExternal)
				{
					UGCItem uGCItem = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)trackInfo.track.ugcServiceId).TryGetInstalledItem(trackInfo.track.externalId);
					if (uGCItem != null)
					{
						m_baseSharingPublication.InitView(uGCItem);
						m_baseSharingPublication.SetOwnerStatus(uGCItem as IUGCOwnerSystem);
						m_baseSharingPublication.SetSubscridedCount(uGCItem as IUGCSubscridable);
						m_baseSharingPublication.SetMetaData(uGCItem as IUGCMeta);
						m_baseSharingPublication.SetScore(uGCItem as IUGCRatable);
					}
				}
			}
			if (trackInfo.isExternal)
			{
				nightMode.value = false;
				BaseRaceState.NightModeActive = false;
			}
			else
			{
				nightMode.value = m_nonExternalNightModeActive;
				BaseRaceState.NightModeActive = m_nonExternalNightModeActive;
			}
		}
		if (base.isActive)
		{
			UpdateBottomPanel();
		}
	}

	private void StopSetDataCoroutine()
	{
		if (m_setDataCoroutine != null)
		{
			TimeController.StopNow(m_setDataCoroutine);
			m_setDataCoroutine = null;
		}
	}

	public void ClearSelection()
	{
		tracksList.selectedIndex = -1;
		foreach (TrackSelectorListItem item in tracksList.items)
		{
			item.selectionIndex = -1;
		}
	}

	public void SetSelectionFromSubItem(TrackSubItem subItem)
	{
		tracksList.selectedIndex = tracksList.items.IndexOf(subItem.parentItem);
		for (int i = 0; i < subItem.parentItem.subItems.Length; i++)
		{
			if (subItem.parentItem.subItems[i] == subItem)
			{
				subItem.parentItem.selectionIndex = i;
				break;
			}
		}
	}

	private void UnloadResources()
	{
		foreach (TrackSelectorListItem trackItem in m_trackItems)
		{
			trackItem?.UnloadResources();
		}
	}

	private void UpdateByLockedState()
	{
		if (m_selectedTrackId.HasValue && !IsLockedTrack(m_selectedTrackId))
		{
			m_prefs.stats.SetLastRaceModeForTrack(m_selectedTrackId.Value, raceMode.Value);
			m_prefs.globals.lastTrackId = (m_selectedTrackId.HasValue ? m_selectedTrackId.Value : (-1));
		}
	}

	private bool IsLockedTrack(int? id)
	{
		if (!id.HasValue)
		{
			return true;
		}
		if (m_model == null || m_prefs == null)
		{
			return true;
		}
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(id.Value);
		if (byId == null)
		{
			return true;
		}
		if (m_prefs.locks.IsTrackUnlockedByDLC(id.Value))
		{
			return false;
		}
		if (Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
		{
			return false;
		}
		if (byId.requiredLevel <= m_prefs.playerProfile.level)
		{
			return m_prefs.locks.GetTrackLockState(id.Value);
		}
		return true;
	}

	public void SetSelectedItemId(int? id)
	{
		m_selectedTrackId = id;
		UpdateByLockedState();
	}

	public void DoLoadRace(int? id)
	{
		SetSelectedItemId(id);
		if (m_subconfigsMode)
		{
			TrackSubItem selection = GetSelection();
			base.owner.HideStackAndPush<UISelectorTrackMetaConfigContext>().Setup(LocalizationService.Localize(selection.track.name), selection.track.scene, raceMode.Value);
			return;
		}
		TrackSubItem selection2 = GetSelection();
		if (selection2 != null && !selection2.track.external)
		{
			TrackMetaConfig config = TrackMetaDB.GetConfig(selection2.track.scene, selection2.track.configuration, raceMode.Value);
			if (config != null)
			{
				m_prefs.globals.lastMetaConfig = config;
				bool flag = TrackMetaDB.IsWinterTrackWithIce(id.Value);
				if (flag)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.tires_changed_to_winter_type.Localized());
				}
				m_prefs.carSettings.SetWinterTiresForcar(m_lastCarId, flag);
				States.State<LoadRaceState>().LoadLocalRace(config.data);
				SoundController.PlayFX(FXSample.RaceStartSound);
				return;
			}
		}
		States.State<LoadRaceState>().LoadLocalRace();
		SoundController.PlayFX(FXSample.RaceStartSound);
	}

	public void OnClickClose()
	{
		base.owner.Hide<UIBackgroundContext>();
		Hide();
	}

	public void OnSelect()
	{
		if (!m_isLoadingTracks.value && tracksList.selectedIndex > -1)
		{
			(tracksList.GetSelectedItem()?.GetSelectedSubItem())?.OnDoubleClick();
		}
	}

	public void OnChangeNightMode()
	{
		if (!GameManager.NightModeInstalled && !nightMode.value)
		{
			Overlay.ShowNoNightModeDLCDialog(base.owner);
			return;
		}
		nightMode.value = !nightMode.value;
		BaseRaceState.NightModeActive = nightMode.value;
		m_nonExternalNightModeActive = nightMode.value;
		UpdateBottomPanel();
	}

	public void UpdateBottomPanel()
	{
		m_actionHints.Clear();
		m_actionHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = OnClickClose
		});
		if (m_isLoadingTracks.value)
		{
			ShowBottomPanel(m_actionHints);
			return;
		}
		TrackSubItem trackSubItem = GetSelectedTrackSubItem();
		if (trackSubItem == null)
		{
			ShowBottomPanel(m_actionHints);
		}
		else
		{
			if (m_trackItems.Count == 0 || m_showEmptyMsg.value)
			{
				return;
			}
			DB.Meta.Track selectedTrack = trackSubItem.track;
			bool external = selectedTrack.external;
			bool flag = m_corruptedMetaTracks.Contains(selectedTrack.externalId);
			bool flag2 = UGCServiceProvider.instance.IsContentInstalled(selectedTrack.externalId);
			if (!trackSubItem.isDownloading)
			{
				string empty = string.Empty;
				empty = ((!(!external || flag2 || flag)) ? LocalizationKeys.nav_download.Localized() : LocalizationKeys.nav_hint_select.Localized());
				m_actionHints.Add(new ActionHintData
				{
					hint = empty,
					type = ActionType.NavSelectBtnA,
					action = OnSelect
				});
			}
			if (!external)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = (nightMode.value ? LocalizationKeys.nav_hint_day.Localized() : LocalizationKeys.nav_hint_night.Localized()),
					type = ActionType.NavAdditionalPrevBtnL2,
					action = OnChangeNightMode
				});
			}
			if (m_showSwitcher.value && !trackSubItem.locked && !trackSubItem.isLockedByDLC && !trackSubItem.isLockedByLevel && trackSubItem.isDownloaded)
			{
				LocalizationKeys.Key key = (trackSubItem.isFavorite ? LocalizationKeys.car_catalog_remove_from_favorites : LocalizationKeys.car_catalog_add_to_favorites);
				m_actionHints.Add(new ActionHintData
				{
					hint = key.Localized(),
					type = ActionType.NavAdditional0BtnX,
					action = OnToggleFavorite
				});
			}
			if (external)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.ugc_manager_show_item_in_workshop.Localized(),
					type = ActionType.NavBtnExpand,
					action = delegate
					{
						ShowItemInWorkshop(selectedTrack);
					}
				});
			}
			ShowBottomPanel(m_actionHints);
		}
		TrackSubItem GetSelectedTrackSubItem()
		{
			return tracksList.GetSelectedItem()?.GetSelectedSubItem();
		}
		async void OnToggleFavorite()
		{
			TrackSubItem selectedTrack2 = GetSelectedTrackSubItem();
			if (selectedTrack2 != null && !m_isLoadingTracks.value && !m_isFavouriteToggleRequested)
			{
				ulong trackId = (selectedTrack2.isExternal ? selectedTrack2.track.externalId : ((ulong)selectedTrack2.track.id));
				if (prefs.favoriteTrack.IsFavorite(trackId))
				{
					m_isFavouriteToggleRequested = true;
					if (!selectedTrack2.isExternal || await SetExternalFavorite(selectedTrack2.track, needAdd: false))
					{
						prefs.favoriteTrack.RemoveTrack(trackId);
						selectedTrack2?.SetFavorite(favorite: false);
						if (m_switcherModeType == SwitcherMode.Favorites && base.isActive)
						{
							ReloadTracks(needShowLoadingScreen: false).Forget();
						}
					}
					m_isFavouriteToggleRequested = false;
				}
				else
				{
					m_isFavouriteToggleRequested = true;
					if (!selectedTrack2.isExternal || await SetExternalFavorite(selectedTrack2.track, needAdd: true))
					{
						prefs.favoriteTrack.AddTrack(trackId);
						selectedTrack2?.SetFavorite(favorite: true);
					}
					m_isFavouriteToggleRequested = false;
				}
				selectedTrack2?.UpdatePreview();
				if (selectedTrack2?.trackSubItemController != null)
				{
					selectedTrack2.trackSubItemController.UpdateItem();
				}
				if (base.isActive)
				{
					UpdateBottomPanel();
				}
			}
		}
		async Task<bool> SetExternalFavorite(DB.Meta.Track track, bool needAdd)
		{
			if (await SetExternalServicesFavourite(track, needAdd))
			{
				return true;
			}
			GUICommonGodVoice.ShowText(LocalizationKeys.error_server_error.Localized(), 2f);
			return false;
		}
	}

	private async Task<bool> SetExternalServicesFavourite(DB.Meta.Track track, bool isFavorite)
	{
		if (!track.external)
		{
			return false;
		}
		if (UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, IUGCManager.ServiceProvider.Steamworkshop) is SteamWorkshopMapManager steamWorkshopMapManager)
		{
			return await steamWorkshopMapManager.SetFavourite(track.externalId, isFavorite);
		}
		return true;
	}

	private void ShowItemInWorkshop(DB.Meta.Track track)
	{
		if (track.external && !m_isLoadingTracks.value)
		{
			IUGCManager managerBy = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)track.ugcServiceId);
			if (managerBy != null && managerBy.TryGetInstalledItem(track.externalId) is IUGCMeta iUGCMeta)
			{
				iUGCMeta.ShowInOverlay();
			}
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		if (!m_showEmptyMsg.value && !m_isLoadingTracks.value)
		{
			SetTrackInfo(tracksList.NavigationUp());
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		if (!m_showEmptyMsg.value && !m_isLoadingTracks.value)
		{
			SetTrackInfo(tracksList.NavigationDown());
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigateLeft()
	{
		if (!m_showEmptyMsg.value && !m_isLoadingTracks.value)
		{
			SetTrackInfo(tracksList.NavigationLeft());
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigateRight()
	{
		if (!m_showEmptyMsg.value && !m_isLoadingTracks.value)
		{
			SetTrackInfo(tracksList.NavigationRight());
		}
	}
}
