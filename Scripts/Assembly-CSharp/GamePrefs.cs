using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using Inventory;
using PrefsModels;
using Quests.Data.Models;
using UnityEngine;

[AddComponentMenu("Scripts/Custom/Misc/GamePrefs")]
public class GamePrefs : MonoBehaviour
{
	[SerializeField]
	private string m_encryptionKey = "AbCdEfGhIj";

	private bool m_inited;

	private bool m_ready;

	private PrefsModelWorkshop m_workshopInfo;

	private PrefsEventsData m_eventsData;

	private CarSettingsModel m_legacyCarSettings;

	private PrefsCarSlotsModel m_carSettings;

	private GlobalModel m_globals;

	private PrefsModelCustomCameras m_customCameras;

	private GraphicSettings m_graphics;

	private LocksModel m_locks;

	private MoneyModel m_money;

	private InventoryModel m_inventory;

	private QuestsInventoryModel m_questsInventory;

	private FavoriteCarsModel m_favoriteCars;

	private FavoriteTracksModel m_favoriteTracks;

	private FavoriteRimsModel m_favoriteRims;

	private CompensationModel m_compensationModel;

	private PlayerProfileModel m_playerProfile;

	private PopupLastShownModel m_popupLastShown;

	private StatsModel m_stats;

	private UIMultiplayerModel m_UIMultiplayer;

	private GlobalGameSettings m_gameSettings;

	private PrefsCarSlotLockModel m_slotLock;

	private PrefsCarSlotProgressionModel m_slotProgression;

	public PrefsDlcSubscriptionsRecords m_dlcSubscriptionsRecord;

	public const string InAppProductsCacheKey = "InAppProducts";

	public const string PurchaseServiceVerifyCashName = "verifyPurchaseTransactionData";

	public const string CarsRemoteCacheKey = "CarsRemoteCashKey";

	public const string CarsProfileRentCacheKey = "CarsProfileRentCacheKey";

	public const string TutorialServiceDataKey = "TutorialServiceDataKey";

	public const string DefeatedOpponentsCacheKey = "MultiplayerDefeatedOpponentsCache";

	public const string StatisticsServiceDataKey = "StatisticsServiceData";

	public const string LocalizationServiceDataKey = "LocalizationServiceData";

	public const string LocalizationServiceIsAutoKey = "LocalizationServiceIsAuto";

	public const string WebConfigKey = "WebConfig";

	public const string ScenariosKey = "Scenarios";

	private Dictionary<Type, BasePrefsModel> m_states = new Dictionary<Type, BasePrefsModel>();

	public bool ready => m_ready;

	public PrefsModelWorkshop workshopInfo => m_workshopInfo ?? (m_workshopInfo = Get<PrefsModelWorkshop>());

	public PrefsEventsData eventsData => m_eventsData ?? (m_eventsData = Get<PrefsEventsData>());

	public CarSettingsModel legacyCarSettings => m_legacyCarSettings ?? (m_legacyCarSettings = Get<CarSettingsModel>());

	public PrefsCarSlotsModel carSettings => m_carSettings ?? (m_carSettings = Get<PrefsCarSlotsModel>());

	public GlobalModel globals => m_globals ?? (m_globals = Get<GlobalModel>());

	public PrefsModelCustomCameras customCameras => m_customCameras ?? (m_customCameras = Get<PrefsModelCustomCameras>());

	public GraphicSettings graphics => m_graphics ?? (m_graphics = Get<GraphicSettings>());

	public LocksModel locks => m_locks ?? (m_locks = Get<LocksModel>());

	public MoneyModel money => m_money ?? (m_money = Get<MoneyModel>());

	public InventoryModel inventory => m_inventory ?? (m_inventory = Get<InventoryModel>());

	public QuestsInventoryModel questsInventory => m_questsInventory ?? (m_questsInventory = Get<QuestsInventoryModel>());

	public FavoriteCarsModel favoriteCars => m_favoriteCars ?? (m_favoriteCars = Get<FavoriteCarsModel>());

	public FavoriteTracksModel favoriteTrack => m_favoriteTracks ?? (m_favoriteTracks = Get<FavoriteTracksModel>());

	public FavoriteRimsModel favoriteRims => m_favoriteRims ?? (m_favoriteRims = Get<FavoriteRimsModel>());

	public CompensationModel compensationModel => m_compensationModel ?? (m_compensationModel = Get<CompensationModel>());

	public PlayerProfileModel playerProfile => m_playerProfile ?? (m_playerProfile = Get<PlayerProfileModel>());

	public PopupLastShownModel popupLastShown => m_popupLastShown ?? (m_popupLastShown = Get<PopupLastShownModel>());

	public StatsModel stats => m_stats ?? (m_stats = Get<StatsModel>());

	public UIMultiplayerModel UIMultiplayer => m_UIMultiplayer ?? (m_UIMultiplayer = Get<UIMultiplayerModel>());

	public GlobalGameSettings gameSettings => m_gameSettings ?? (m_gameSettings = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>());

	public PrefsCarSlotLockModel slotLock => m_slotLock ?? (m_slotLock = Get<PrefsCarSlotLockModel>());

	public PrefsCarSlotProgressionModel slotProgression => m_slotProgression ?? (m_slotProgression = Get<PrefsCarSlotProgressionModel>());

	public PrefsDlcSubscriptionsRecords dlcSubscriptionsRecord => m_dlcSubscriptionsRecord ?? (m_dlcSubscriptionsRecord = Get<PrefsDlcSubscriptionsRecords>());

	public float coinsFactor
	{
		get
		{
			if (Overlay.instance.IsDLCInstalled(DLCCode.Gold))
			{
				return gameSettings.goldMFactor;
			}
			return 1f;
		}
	}

	public float expFactor
	{
		get
		{
			if (Overlay.instance.IsDLCInstalled(DLCCode.Gold))
			{
				return gameSettings.goldEFactor;
			}
			return 1f;
		}
	}

	public void DisableSaveForAll()
	{
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.isSaveAllowed = false;
		}
	}

	public void EnableSaveForAll()
	{
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.isSaveAllowed = true;
		}
	}

	public void SaveAll()
	{
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.Save();
		}
	}

	public void MakeBackup()
	{
		string date = DateTime.Now.ToShortDateString();
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.MakeBackup(date);
		}
	}

	public void MakeBackup(string date)
	{
		if (string.IsNullOrEmpty(date))
		{
			return;
		}
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.MakeBackup(date);
		}
	}

	public void RestoreBackup(string date)
	{
		foreach (BasePrefsModel value in m_states.Values)
		{
			value.Restore(date);
		}
	}

	public string[] GetBackupsArr()
	{
		return BasePrefsModel.reserveSystem.GetListHistory();
	}

	private BasePrefsModel RegisterState(Type state)
	{
		BasePrefsModel basePrefsModel = Activator.CreateInstance(state) as BasePrefsModel;
		m_states.Add(state, basePrefsModel);
		basePrefsModel.Load();
		return basePrefsModel;
	}

	private T Get<T>() where T : BasePrefsModel
	{
		InitIfNeeded();
		if (m_states.TryGetValue(typeof(T), out var value))
		{
			return value as T;
		}
		GameLog.Print(AlertLevel.Error, "Prefs", $"GamePrefs.Get() m_states not contains {typeof(T)}");
		return null;
	}

	public IEnumerator InitCoroutine()
	{
		m_ready = false;
		if (!m_inited)
		{
			if (!Overlay.IsReady())
			{
				throw new Exception("Overlay not ready");
			}
			m_inited = true;
			yield return null;
			TryCatchObscuredPrefs.lockToDevice = ObscuredPrefs.DeviceLockLevel.Strict;
			TryCatchObscuredPrefs.SetNewCryptoKey(m_encryptionKey);
			m_states.Clear();
			bool loadFailed = false;
			foreach (Type item in from t in Assembly.GetAssembly(GetType()).GetTypes()
				where t.BaseType == typeof(BasePrefsModel)
				select t)
			{
				if (item.IsAbstract)
				{
					continue;
				}
				try
				{
					if (RegisterState(item).isLastLoadFailed)
					{
						Debug.LogError($"GamePrefs>RegisterState({item}).isLastLoadFailed == true");
						loadFailed = true;
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					loadFailed = true;
				}
				yield return null;
			}
			if (loadFailed)
			{
				globals.syncKey = string.Empty;
			}
			foreach (BasePrefsModel value in m_states.Values)
			{
				value.PostLoad();
				value.isSaveAllowed = true;
				yield return null;
			}
			foreach (BasePrefsModel value2 in m_states.Values)
			{
				value2.Start();
				yield return null;
			}
			if (loadFailed)
			{
				globals.syncKey = string.Empty;
				if (!BasePrefsModel.storageSystem.SkipClearProfileOnReadError())
				{
					SyncPlayerProfileModel.ClearGamePrefs();
				}
				GameLog.Print(AlertLevel.Error, "System", "GamePrefs load failed");
				GameSync.SetNeedSync();
			}
		}
		m_ready = true;
	}

	private void InitIfNeeded()
	{
		if (!m_inited)
		{
			IEnumerator enumerator = InitCoroutine();
			while (enumerator.MoveNext())
			{
			}
		}
	}
}
