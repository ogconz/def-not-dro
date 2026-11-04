using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DB.Meta;
using DI;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using UnityEngine;

public class TrackSubItem
{
	private class PreviewLoadContainer
	{
		public CancellationTokenSource token;

		public Coroutine coroutine;

		public Texture2D texture2D;

		public PreviewLoadContainer()
		{
		}

		public PreviewLoadContainer(Coroutine coroutine)
		{
			this.coroutine = coroutine;
		}

		public PreviewLoadContainer(CancellationTokenSource token)
		{
			this.token = token;
		}
	}

	public static bool allowShowDownloadInfoGodvoice = true;

	private bool m_isPreviewDownload;

	private static Dictionary<string, PreviewLoadContainer> m_staticContainerPreview = new Dictionary<string, PreviewLoadContainer>();

	public TrackSubItemController trackSubItemController { get; set; }

	public UISelectorTrackContext context { get; private set; }

	public TrackSelectorListItem parentItem { get; set; }

	public DB.Meta.Track track { get; private set; }

	public Sprite spriteAsset { get; private set; }

	public string name { get; private set; }

	public string dlcName { get; private set; }

	public string text { get; private set; }

	public string goldLimit { get; private set; }

	public string silverLimit { get; private set; }

	public string bronzeLimit { get; private set; }

	public string bestResult { get; private set; }

	public bool isLockedByLevel { get; private set; }

	public bool locked { get; set; }

	public bool isLockedByDLC { get; set; }

	public bool isUnlocked { get; private set; }

	public int coinsValue { get; private set; }

	public bool isReverse { get; private set; }

	public int cupIndex { get; private set; }

	public bool isExternal { get; private set; }

	public bool isDownloaded { get; private set; }

	public bool isDownloading { get; private set; }

	public bool isLoadingWebIcon { get; private set; }

	public bool isDLC => !string.IsNullOrEmpty(dlcName);

	public bool isCompleteEvent { get; private set; }

	public bool isFavorite { get; private set; }

	public bool isFriendOwner { get; set; }

	public bool isPlayerOwner { get; set; }

	public bool isSelected { get; private set; }

	public bool isMetaCorrupted { get; private set; }

	public bool isContainView { get; private set; }

	public string targetClientVersion { get; private set; }

	public int ugcServiceId { get; private set; }

	public Texture2D iconPreview { get; private set; }

	public TrackSubItem(UISelectorTrackContext context, DB.Meta.Track track, int playerLevel, bool isFavorite)
	{
		this.context = context;
		this.track = track;
		isExternal = track.external;
		isCompleteEvent = IsCompleteEvent();
		IUGCManager managerBy = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)track.ugcServiceId);
		if (managerBy != null && managerBy.TryGetInstalledItem(track.externalId) is IUGCMeta meta)
		{
			managerBy.ItemTargetVersionExist(meta, out var targetVersion);
			targetClientVersion = targetVersion;
		}
		name = (track.external ? track.name : LocalizationService.Localize(track.name).ToUpper());
		ugcServiceId = track.ugcServiceId;
		SetLockedState(playerLevel);
		SetFavorite(isFavorite);
		InitPreview();
	}

	public void Update()
	{
		LoadIcon();
	}

	public void InitPreview()
	{
		LoadIcon();
		UpdatePreview();
	}

	public void SetFavorite(bool favorite)
	{
		isFavorite = favorite;
	}

	private void SetLockedState(int playerLevel)
	{
		dlcName = ((track.dlcCode != DLCCode.Unknown) ? Overlay.instance.GetDLCShortName(track.dlcCode) : string.Empty);
		if (isDLC)
		{
			isLockedByLevel = false;
			locked = false;
			isLockedByDLC = !Overlay.instance.IsDLCInstalled(track.dlcCode);
		}
		else
		{
			isLockedByLevel = !Overlay.instance.IsDLCInstalled(DLCCode.FreeContent) && playerLevel < track.requiredLevel;
		}
	}

	private bool IsCompleteEvent()
	{
		if (context.raceMode == RaceMode.HalloweenEvent)
		{
			return context.prefs.eventsData.halloweenLocksTracks.ContainsKey(track.id);
		}
		return false;
	}

	public void UpdatePreview()
	{
		isReverse = this.track.configuration == "Reversed";
		DB.Meta.Track track = UGCSceneBundleManager.TryGetMetaByExternalId(this.track.externalId);
		bool flag = track != null;
		isDownloaded = !this.track.external || flag;
		if (this.track.external && flag)
		{
			this.track = track;
		}
		isCompleteEvent = IsCompleteEvent();
		isDownloading = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)this.track.ugcServiceId).IsPackageRequested(this.track.externalId);
		isMetaCorrupted = UISelectorTrackContext.IsCorruptedExternalTrack(this.track.externalId);
		isUnlocked = true;
		if (isLockedByLevel)
		{
			text = LocalizationKeys.locked_track_info.LocalizedFormat(this.track.requiredLevel.ToString());
			isUnlocked = false;
		}
		else if (isLockedByDLC)
		{
			isUnlocked = false;
		}
		else if (!isExternal)
		{
			locked = context.prefs.locks.GetTrackLockState(this.track.id);
			if (locked)
			{
				isUnlocked = false;
				int cost = this.track.cost;
				text = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(cost));
				coinsValue = cost;
			}
		}
	}

	public void SetSelected(bool selected)
	{
		if (isSelected != selected)
		{
			isSelected = selected;
		}
	}

	public void SetCupsInfo(IRaceNorm raceNorm)
	{
		goldLimit = raceNorm.GetNormAsString(RaceCupType.Gold);
		silverLimit = raceNorm.GetNormAsString(RaceCupType.Silver);
		bronzeLimit = raceNorm.GetNormAsString(RaceCupType.Bronze);
	}

	public void SetBestResult(string result)
	{
		bestResult = result;
	}

	public void SetCupIndex(int index)
	{
		cupIndex = index;
	}

	public void LoadResources()
	{
		_ = track;
	}

	public static void DisposeAllStaticResources()
	{
		foreach (KeyValuePair<string, PreviewLoadContainer> item in m_staticContainerPreview)
		{
			PreviewLoadContainer value = item.Value;
			if (value.texture2D != null)
			{
				Object.Destroy(value.texture2D);
				value.texture2D = null;
			}
			if (value.coroutine != null)
			{
				TimeController.StopNow(value.coroutine);
				value.coroutine = null;
			}
			CancellationTokenSource token = value.token;
			if (token != null && token.Token.CanBeCanceled)
			{
				value.token.Cancel();
				value.token = null;
			}
		}
		m_staticContainerPreview.Clear();
	}

	private async void LoadIcon()
	{
		if (track == null || m_isPreviewDownload || (iconPreview != null && track.external) || spriteAsset != null)
		{
			return;
		}
		if (trackSubItemController != null)
		{
			RectTransform component = trackSubItemController.gameObject.GetComponent<RectTransform>();
			RectTransform component2 = context.gameObject.GetComponent<RectTransform>();
			if (component != null && component2 != null)
			{
				isContainView = component2.rect.Contains(component.rect.min);
			}
		}
		if (track.external)
		{
			if (!m_staticContainerPreview.TryGetValue(track.icon, out var value))
			{
				if (isDownloaded && !isMetaCorrupted && UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)track.ugcServiceId).TryGetInstalledItem(track.externalId) is IUGCIconSystem iUGCIconSystem)
				{
					m_staticContainerPreview.Add(track.icon, new PreviewLoadContainer(new CancellationTokenSource()));
					Task<Texture2D> task = UIUtils.CreateTextureFromPathAsync(track.icon, m_staticContainerPreview[track.icon].token.Token, iUGCIconSystem.iconWidth, iUGCIconSystem.iconHeight);
					await task;
					if (m_staticContainerPreview.ContainsKey(track.icon))
					{
						m_staticContainerPreview[track.icon].texture2D = task.Result;
						iconPreview = m_staticContainerPreview[track.icon].texture2D;
						TryUpdateList();
					}
				}
			}
			else
			{
				iconPreview = value.texture2D;
				TryUpdateList();
			}
		}
		else
		{
			spriteAsset = ResourcesWrapper.Load<Sprite>("ui/tracks/" + track.icon, "temp");
		}
		void TryUpdateList()
		{
			if (context != null && context.tracksList != null)
			{
				context.tracksList.UpdateList();
			}
		}
	}

	public void UnloadResources()
	{
		if (!track.external && spriteAsset != null)
		{
			ResourcesWrapper.UnloadAsset(spriteAsset);
		}
		spriteAsset = null;
	}

	public void OnClick()
	{
		context.SetTrackInfo(this);
	}

	public void OnDoubleClick()
	{
		if (context.isLoadingTracks)
		{
			return;
		}
		context.prefs.globals.lastTrackId = track.id;
		if (isLockedByLevel)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.locked_level_message_track.LocalizedFormat(track.requiredLevel));
		}
		else if (isLockedByDLC)
		{
			UIMessageBox.ShowDLCDialog(LocalizationKeys.no_dlc_track.Localized(), track.dlcCode);
		}
		else if (locked)
		{
			if (context.prefs.money.coins < track.cost)
			{
				UIMessageBox.ShowHint(LocalizationKeys.not_enough_money.Localized(), "", null, context.owner);
			}
			else
			{
				ShowMessageBuyTrack();
			}
		}
		else if (isMetaCorrupted)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.ugc_track_meta_error.Localized(), 4f);
		}
		else
		{
			if (isDownloading)
			{
				return;
			}
			if (!isDownloaded)
			{
				IUGCManager ugcMapManager = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, IUGCManager.ServiceProvider.Steamworkshop);
				ugcMapManager?.RequestPackage(track.externalId, delegate(float progress)
				{
					TrackSubItemController externalTrackSubItemController = context.GetExternalTrackSubItemController(track.externalId);
					if (externalTrackSubItemController != null)
					{
						externalTrackSubItemController.FillDownloadProgress(progress);
					}
				}, delegate(bool success, string errorMessage)
				{
					if (success)
					{
						DB.Meta.Track track = UGCSceneBundleManager.TryGetMetaByExternalId(this.track.externalId);
						if (track != null)
						{
							this.track = track;
							isMetaCorrupted = false;
							if (allowShowDownloadInfoGodvoice)
							{
								GUICommonGodVoice.ShowText(LocalizationKeys.ugc_track_downloaded_info.Localized());
								allowShowDownloadInfoGodvoice = false;
							}
							else if (!context.isActive)
							{
								GUICommonGodVoice.ShowText(LocalizationKeys.network_game_mapname.Localized() + " " + this.track.name + ": " + LocalizationKeys.ugc_item_downloaded.Localized(), 4f);
							}
						}
						else
						{
							ugcMapManager.RemovePackage(this.track.externalId);
							context.AddCorruptedTrackList(this.track.externalId);
							isMetaCorrupted = true;
							GUICommonGodVoice.ShowText(LocalizationKeys.network_game_mapname.Localized() + " " + this.track.name + ": " + LocalizationKeys.ugc_track_meta_error.Localized(), 4f);
						}
					}
					else
					{
						isDownloading = false;
						GUICommonGodVoice.ShowText(errorMessage, 4f);
					}
					LoadIcon();
					if (context.isActive)
					{
						context.UpdateExternalSubItem(this.track.externalId);
						context.UpdateBottomPanel();
						context.SetTrackInfo(this.track);
					}
				});
				UpdatePreview();
				trackSubItemController.UpdateItem();
				context.UpdateBottomPanel();
			}
			else
			{
				if (isExternal && targetClientVersion == "2.21.0")
				{
					GUICommonGodVoice.ShowText("Map is created for an old version, there may be issues");
				}
				context.DoLoadRace(track.id);
			}
		}
	}

	private void ShowMessageBuyTrack()
	{
		List<DialogButtonData> list = new List<DialogButtonData>();
		list.Add(new DialogButtonData
		{
			action = delegate
			{
				locked = false;
				ECurrencyType currencyType = ECurrencyType.Coins;
				if (DependencyInjector.Resolve<TransactionManager>().TryToPerformUnlockTransaction(TransactionManager.UnlockType.Track, track.id, track.cost, currencyType))
				{
					AnalyticsService.LogUnlockTrack(track.name);
					if (trackSubItemController != null)
					{
						trackSubItemController.UpdateItem();
					}
				}
				context.SetSelectedItemId(track.id);
				context.UpdateBottomPanel();
				ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.BuyTrack, LocalizationService.Localize(track.name));
			},
			text = LocalizationKeys.ShopBuy.Localized()
		});
		list.Add(new DialogButtonData
		{
			action = delegate
			{
			},
			text = LocalizationKeys.Cancel.Localized()
		});
		UIMessageBox.ShowDialog(LocalizationKeys.message_buy_track.LocalizedFormat(LocalizationService.Localize(track.name), UIUtils.DigitsWithSeparate(track.cost)), list.Last()?.action, list.ToArray());
	}
}
