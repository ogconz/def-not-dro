using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binding;
using Binding.Base;
using CarProfile;
using Customization;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using UnityEngine;
using UnityEngine.UI;

[AudioFXBlocker(false)]
public class UIBodyCustomizationContext : BaseBindingBehaviourTarget
{
	public enum WheelMode
	{
		Forward = 0,
		Rear = 1,
		All = 2
	}

	public enum NavigationMode
	{
		Categories = 0,
		Parts = 1
	}

	public enum CustomizeMode
	{
		Part = 0,
		Wheels = 1,
		Tires = 2
	}

	public class PartVendorsList
	{
		private class PartVendor
		{
			public string name;

			public int revision;

			public BodyPartItem firstPart;

			public bool isEmptyDescriptionAdded;
		}

		private readonly List<PartVendor> m_vendors = new List<PartVendor>();

		public void AddVendor(BodyPartItem partItem)
		{
			CarPart part = partItem.part;
			if (!partItem.isModPart && part.vendor == "")
			{
				part.vendor = "stock";
			}
			foreach (PartVendor vendor in m_vendors)
			{
				if (!(vendor.name == part.vendor))
				{
					continue;
				}
				if (string.IsNullOrEmpty(part.description))
				{
					if (string.IsNullOrEmpty(vendor.firstPart.part.description))
					{
						vendor.firstPart.part.description = GetRevisionString(1);
						vendor.revision++;
					}
					if (vendor.isEmptyDescriptionAdded)
					{
						part.description = GetRevisionString(vendor.revision);
					}
					vendor.isEmptyDescriptionAdded = true;
					vendor.revision++;
				}
				return;
			}
			m_vendors.Add(new PartVendor
			{
				name = part.vendor,
				revision = 1,
				firstPart = partItem,
				isEmptyDescriptionAdded = string.IsNullOrEmpty(part.description)
			});
		}

		private static string GetRevisionString(int revision)
		{
			if (revision > 0)
			{
				return $"V{revision}";
			}
			if (revision == 0)
			{
				return "Empty";
			}
			return string.Empty;
		}

		public static int CompareDescriptions(string desc0, string desc1)
		{
			if (string.IsNullOrEmpty(desc0))
			{
				return -1;
			}
			if (desc0 == "Tune")
			{
				return 1;
			}
			if (desc1 == "Tune")
			{
				return -1;
			}
			return string.Compare(desc0, desc1, StringComparison.Ordinal);
		}
	}

	[SerializeField]
	private BodyPartsList m_partsList;

	[SerializeField]
	private BodyCategoriesList m_categoriesList;

	private CustomizeMode m_customizeMode;

	public readonly Property<int> wheelMode = new IntProperty(2);

	public readonly Property<int> customizeModeProperty = new IntProperty();

	public readonly Property<bool> useKitWheels = new BoolProperty();

	public readonly Property<bool> useKitWheelsVisible = new BoolProperty();

	public readonly Property<bool> isDiskResizable = new BoolProperty();

	public readonly Property<bool> m_showPublicationBlock = new BoolProperty();

	public readonly Property<float> frontSizeCaption = new FloatProperty();

	public readonly Property<float> rearSizeCaption = new FloatProperty();

	private BoolProperty m_isFavoriteRims = new BoolProperty();

	public Slider frontSlider;

	public Slider rearSlider;

	public readonly Property<bool> isPartsMode = new BoolProperty(false);

	public readonly Property<int> cost = new IntProperty(0);

	public readonly Property<string> DLCName = new StringProperty();

	public readonly Property<bool> BuyDLC = new BoolProperty();

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private BaseModel m_db;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	[Dependency]
	private TransactionManager m_transactionManager;

	private CameraRotation m_cameraRotation;

	private BasePartItem m_externalPart;

	private string m_externalPartID = "";

	private BasePartItem m_lastTrackedDLCItem;

	private List<CarPart> m_currentParts = new List<CarPart>();

	private List<CarPart> m_tempItems = new List<CarPart>();

	private readonly List<ActionHintData> m_bottomHintsCache = new List<ActionHintData>();

	private readonly List<CarVisualManager.CustomizationFlags> m_applyVSCommandsBuffer = new List<CarVisualManager.CustomizationFlags>();

	private NavigationMode m_navMode;

	private int m_carId;

	private int m_prevCarId;

	private CarVisualManager.VisualSettings m_previewSettings;

	private CarVisualManager.VisualSettings m_currentSettings;

	private CarPartDatabase.SlotScheme m_slotScheme;

	private ShopInfo m_shopInfo;

	private PlayerCar m_playerCar;

	private ProfilesInfo.Profile m_currentProfile;

	private CarProfileController m_frontWheelSize;

	private CarProfileController m_rearWheelSize;

	private int m_categoryIndexSelected;

	private int m_wheelCategoryIndex;

	private int m_partIndexSelected = -1;

	private BodyCategoryItem m_targetedCategory;

	private Coroutine m_setDataCoroutine;

	private int m_cacheFrontWheelId;

	private int m_cacheRearWheelId;

	private const string FRONT_DISC_DIAM = "front_disc_diam";

	private const string REAR_DISC_DIAM = "rear_disc_diam";

	public const string DLC_VENDOR = "dlc";

	private const string MODDED_PART_VENDOR = "moddedpart";

	private const string EMPTY_PART_VENDOR = "";

	private const string STOCK_VENDOR = "stock";

	private RaceCar m_car;

	private bool m_shineStops;

	private bool m_isActivated;

	[SerializeField]
	private UIBaseSharingPublication m_baseSharingPublication;

	private ApiClient m_apiClient;

	private ulong m_lastUgcId;

	private List<UGCItem> m_ugcItems = new List<UGCItem>();

	[SerializeField]
	private VendorSpritesAsset m_vendorsAsset;

	private readonly string[] m_duplicatedRimGroups = new string[2] { "stock", "d-spec" };

	private Coroutine m_waitLoadingCarEndReturn;

	public CustomizeMode customizeMode
	{
		get
		{
			return m_customizeMode;
		}
		set
		{
			m_customizeMode = value;
			customizeModeProperty.value = (int)value;
		}
	}

	public GamePrefs prefs { get; private set; }

	private List<BodyCategoryItem> m_categories => m_categoriesList.items;

	private List<BasePartItem> m_parts => m_partsList.items;

	public BodyCategoryItem selectedCategory => m_categories[m_categoryIndexSelected];

	public BasePartItem selectedPart
	{
		get
		{
			if (m_parts.Count <= 0 || m_partIndexSelected < 0)
			{
				return null;
			}
			return m_parts[m_partIndexSelected];
		}
	}

	public bool shineStops
	{
		get
		{
			return m_shineStops;
		}
		set
		{
			m_shineStops = value;
			if (!value)
			{
				m_car3dView.GetRaceCar().carX.brake = 0f;
			}
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.CarDesigning);
		if (m_car3dView != null)
		{
			m_car3dView.Set3DModelVisible(value: true);
		}
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		m_cameraRotation = CameraRotation.instance;
		m_cameraRotation.useZoom = true;
		useKitWheelsVisible.OnValueChanged += UpdateBottomPanel;
		isPartsMode.OnValueChanged += UpdateBottomPanel;
		frontSizeCaption.OnValueChanged += OnWheelSizeChange;
		rearSizeCaption.OnValueChanged += OnWheelSizeChange;
		BringForwardByDepth();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.customization_body.Localized());
		InitApiClient();
		prefs = DependencyInjector.Resolve<GamePrefs>();
		if (prefs == null || m_db == null)
		{
			return;
		}
		m_carId = prefs.globals.lastCarId;
		m_playerCar = m_db.GetById<PlayerCar>(m_carId);
		m_shopInfo = m_db.GetById<ShopInfo>(m_carId);
		CarVisualManager.VisualSettings visualForCar = prefs.carSettings.GetVisualForCar(m_carId);
		m_previewSettings = visualForCar.Copy();
		m_currentSettings = visualForCar.Copy();
		m_car = m_car3dView.GetRaceCar();
		List<CarProfileController> controllers = m_car.profileMetaInfo.dynoInfo.controllers;
		m_currentProfile = prefs.carSettings.GetProfileInfo(m_car.profileMetaInfo.id, m_car.carId);
		m_frontWheelSize = null;
		m_rearWheelSize = null;
		foreach (CarProfileController item in controllers)
		{
			if (item.name == "front_disc_diam")
			{
				m_frontWheelSize = item;
			}
			if (item.name == "rear_disc_diam")
			{
				m_rearWheelSize = item;
			}
		}
		isDiskResizable.value = m_frontWheelSize != null && m_rearWheelSize != null && m_frontWheelSize.enabled && m_rearWheelSize.enabled;
		if (isDiskResizable.value)
		{
			if (!m_currentProfile.controllers.TryGetValue("front_disc_diam", out var value))
			{
				value = m_frontWheelSize.changers[0].EvaluatePhase(m_frontWheelSize.changers[0].defaultValue);
			}
			if (!m_currentProfile.controllers.TryGetValue("rear_disc_diam", out var value2))
			{
				value2 = m_rearWheelSize.changers[0].EvaluatePhase(m_rearWheelSize.changers[0].defaultValue);
			}
			SettingsSlider(frontSlider, m_frontWheelSize.changers[0].values);
			SettingsSlider(rearSlider, m_rearWheelSize.changers[0].values);
			rearSizeCaption.value = m_rearWheelSize.changers[0].EvaluateOriginalValue(value2);
			frontSizeCaption.value = m_frontWheelSize.changers[0].EvaluateOriginalValue(value);
		}
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: true);
		m_cacheFrontWheelId = m_currentSettings.frontWheels;
		m_cacheRearWheelId = m_currentSettings.rearWheels;
		UpdateUseKitWheels();
		ClearBodyPartList();
		ClearPartsSelection();
		NavigateCategory(null);
		DLCName.value = "";
		BuyDLC.value = false;
		shineStops = true;
		m_navMode = NavigationMode.Categories;
		m_isActivated = true;
		InitCategorySelection();
		if ((bool)m_car3dView && m_car3dView.currentCar != null)
		{
			useKitWheelsVisible.value = m_carVisualManager.ForceUpdateAndGetKitWheelVisibleFromSettings(m_previewSettings);
		}
	}

	private void ToggleFavoriteRim()
	{
		if (selectedCategory == null || customizeMode != CustomizeMode.Wheels || selectedPart == null)
		{
			return;
		}
		if (prefs.favoriteRims.IsFavorite((selectedPart.part.id, selectedPart.part.packetid)))
		{
			prefs.favoriteRims.RemoveRim((selectedPart.part.id, selectedPart.part.packetid));
		}
		else
		{
			prefs.favoriteRims.AddRim((selectedPart.part.id, selectedPart.part.packetid));
			if (UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Cars, selectedPart.part.ugcServiceProvider).TryGetInstalledItem(selectedPart.part.packetid) is IUGCFavoritable iUGCFavoritable)
			{
				iUGCFavoritable.SetFavorite(value: true).Forget();
			}
		}
		selectedPart.isFavourite = !selectedPart.isFavourite;
		UIBodyPart selectedController = m_partsList.GetSelectedController();
		selectedController.isFavourite.value = selectedController.item.isFavourite;
		UpdateBottomPanel();
	}

	private void RefreshEquippedItems()
	{
		foreach (BasePartItem part in m_parts)
		{
			int index = m_partsList.IndexOf(part);
			UIBodyPart controller = m_partsList.GetController(index);
			if (part is TirePartItem tirePartItem)
			{
				part.equipped = tirePartItem.presetSettings.logo == m_previewSettings.frontTire.logo || tirePartItem.presetSettings.logo == m_previewSettings.rearTire.logo;
			}
			else
			{
				int id = part.part.id;
				part.equipped = m_previewSettings.parts.Contains(id) || id == m_previewSettings.frontWheels || id == m_previewSettings.rearWheels;
			}
			if (controller != null)
			{
				controller.equipped.value = controller.item.equipped;
			}
		}
	}

	protected override void OnDeactivate()
	{
		useKitWheelsVisible.OnValueChanged -= UpdateBottomPanel;
		isPartsMode.OnValueChanged -= UpdateBottomPanel;
		frontSizeCaption.OnValueChanged -= OnWheelSizeChange;
		rearSizeCaption.OnValueChanged -= OnWheelSizeChange;
		m_isActivated = false;
		m_waitLoadingCarEndReturn = null;
		m_cameraRotation.useZoom = false;
		m_lastUgcId = 0uL;
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		StopSetDataCoroutine();
		if (isDiskResizable.value)
		{
			m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: true);
		}
		m_car3dView.ApplyCustomization(CarVisualManager.CustomizationFlags.All, m_currentSettings);
		m_car = null;
		base.OnDeactivate();
		OnPartsDeactivate();
		m_baseSharingPublication.DisposeResource();
		DisposeExternalResources();
		ResourcesWrapper.UnloadResources("customization");
	}

	private void DisposeExternalResources()
	{
		foreach (UGCItem ugcItem in m_ugcItems)
		{
			if (ugcItem is IUGCIconSystem iUGCIconSystem)
			{
				iUGCIconSystem.DisposeIcon();
			}
		}
	}

	private void Update()
	{
		if (m_lastTrackedDLCItem != null && !string.IsNullOrEmpty(DLCName.value) && Overlay.instance.IsDLCInstalled(m_lastTrackedDLCItem.part.DLCCode))
		{
			FillLockedState(m_lastTrackedDLCItem, 0);
			UpdateCarPartBuyState(m_lastTrackedDLCItem);
			m_lastTrackedDLCItem = null;
		}
		if (m_shineStops && m_car != null)
		{
			m_car.carX.brake = Mathf.Sin(Time.time * 5f);
		}
		UpdateLists();
		CustomCameraPresets instance = Singleton<CustomCameraPresets>.instance;
		if (instance != null && instance.currentPreset.stateType != CustomizationCameraPreset.Garage && m_cameraRotation != null)
		{
			if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) && axis != Vector2.zero)
			{
				instance.CameraTo(CustomizationCameraPreset.Garage);
			}
			if (!UIUtils.IsPointerOverGameObject() && (Input.touchCount > 0 || Input.GetMouseButton(0)) && !m_cameraRotation.skipToRotateByMouse)
			{
				instance.CameraTo(CustomizationCameraPreset.Garage);
			}
		}
	}

	private void UpdateLists()
	{
		m_categoriesList.UpdateList();
		m_partsList.UpdateList();
	}

	private void SettingsSlider(Slider slider, List<float> values)
	{
		if ((bool)slider && values.Count > 1)
		{
			slider.minValue = values[0];
			slider.maxValue = values[values.Count - 1];
		}
	}

	public void NextFrontWheelSize()
	{
		frontSlider.value += 1f;
	}

	public void PrevFrontWheelSize()
	{
		frontSlider.value -= 1f;
	}

	public void NextRearWheelSize()
	{
		rearSlider.value += 1f;
	}

	public void PrevRearWheelSize()
	{
		rearSlider.value -= 1f;
	}

	private void ClearCategoryList()
	{
		if (m_categories.Count != 0 || m_categoriesList.GetCount() != 0)
		{
			m_categories.Clear();
			m_categoriesList.Clear();
		}
	}

	public void NavigateCategory(BodyCategoryItem item)
	{
		if (!string.IsNullOrEmpty(DLCName.value))
		{
			DLCName.value = string.Empty;
			BuyDLC.value = false;
		}
		UpdateTopPanelTitleByCategory(item);
		m_slotScheme = new CarPartDatabase.SlotScheme(m_carId, m_previewSettings.parts);
		if (item == null)
		{
			isPartsMode.value = false;
			ClearCategoryList();
			foreach (string category in CarPartDatabase.GetCategories(m_slotScheme))
			{
				if ((!(category == "wheel") || !m_currentSettings.lockChangeWheels) && !(category == "interior"))
				{
					BodyCategoryItem bodyCategoryItem = InstantiateCategory(category, GetSpriteName(category));
					bodyCategoryItem.finalTarget = false;
					bodyCategoryItem.title = LocalizationKeys.car_castomize_category_caption.FormatLocalized(category);
				}
			}
			BodyCategoryItem bodyCategoryItem2 = InstantiateCategory("tires", GetSpriteName("tires"));
			bodyCategoryItem2.finalTarget = false;
			bodyCategoryItem2.title = LocalizationKeys.car_customize_tires.Localized();
			if (m_categories.Count > 0)
			{
				TimeController.StartNow(CheckAndSelectCategory());
			}
			m_categoriesList.RecalcList();
			m_showPublicationBlock.value = false;
		}
		else if (!item.finalTarget)
		{
			m_targetedCategory = item;
			if (string.IsNullOrEmpty(item.target))
			{
				m_categoryIndexSelected = m_wheelCategoryIndex;
				m_navMode = NavigationMode.Categories;
				NavigateCategory(null);
				ClearBodyPartList();
				SetIsFavoriteRims();
				return;
			}
			List<string> groups = CarPartDatabase.GetGroups(m_carId, item.target);
			if (groups.Count == 1 || item.target == "tires")
			{
				BodyCategoryItem item2 = new BodyCategoryItem(this, (item.target == "tires") ? item.target : groups[0], "")
				{
					finalTarget = true
				};
				NavigateCategory(item2);
				return;
			}
			ClearCategoryList();
			InstantiateCategory("", "_back").finalTarget = false;
			InstantiateCategory("favorite", "wheel_favorite_rims", isFavorite: true).finalTarget = true;
			for (int i = 0; i < groups.Count; i++)
			{
				string text = groups[i];
				if (!IsDuplicationRimGroup(text))
				{
					BodyCategoryItem bodyCategoryItem3 = InstantiateCategory(text, item.target + "_category_" + text.ToLower());
					bodyCategoryItem3.useSpriteColorBinding = false;
					bodyCategoryItem3.title = text;
					bodyCategoryItem3.finalTarget = true;
					bodyCategoryItem3.groupId = i;
				}
			}
			m_categoriesList.RecalcList();
			m_categoriesList.ScrollTo(0);
			OnSelectWheelCategory();
		}
		else
		{
			FillBodyPartList(item);
		}
	}

	private void OnSelectWheelCategory()
	{
		string target = m_targetedCategory.target;
		if (target != "wheel")
		{
			return;
		}
		List<string> groups = CarPartDatabase.GetGroups(m_carId, target);
		string wheelTargetName = string.Empty;
		bool flag = wheelMode.value == 1;
		int wheelId = (flag ? m_previewSettings.rearWheels : m_previewSettings.frontWheels);
		m_wheelCategoryIndex = m_categoryIndexSelected;
		if (useKitWheels.value)
		{
			wheelId = (flag ? m_previewSettings.kitRearWheels : m_previewSettings.kitFrontWheels);
		}
		TrySetDefaultWheelId(ref wheelId);
		for (int i = 0; i < groups.Count; i++)
		{
			List<CarPart> parts = CarPartDatabase.GetParts(m_slotScheme, m_targetedCategory.target, groups[i]);
			bool flag2 = false;
			for (int j = 0; j < parts.Count; j++)
			{
				if (parts[j].id == wheelId)
				{
					wheelTargetName = groups[i];
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				break;
			}
		}
		if (string.IsNullOrEmpty(wheelTargetName) || IsDuplicationRimGroup(wheelTargetName))
		{
			m_categoryIndexSelected = 1;
			m_categoriesList.selectedIndex = m_categoryIndexSelected;
		}
		else
		{
			SelectCategory(m_categories.Find((BodyCategoryItem cat) => cat.target == wheelTargetName));
		}
		if (prefs.favoriteRims.favoriteRims.Count > 0)
		{
			SelectCategory(m_categories.Find((BodyCategoryItem cat) => cat.target == "favorite"));
		}
		NavigateCategory(m_categoriesList.GetSelectedItem());
	}

	private void TrySetDefaultWheelId(ref int wheelId)
	{
		if (wheelId == 0)
		{
			wheelId = m_car.metaInfo.defaultWheelId;
		}
	}

	private IEnumerator CheckAndSelectCategory()
	{
		if (m_categoryIndexSelected >= 0 && m_categoryIndexSelected < m_categories.Count)
		{
			yield return null;
			SelectCategory(m_categories[m_categoryIndexSelected]);
		}
		else
		{
			SelectCategory(m_categories[0]);
		}
		ScrollRect scrollRect = m_categoriesList.scrollRect;
		if ((bool)scrollRect)
		{
			scrollRect.horizontalNormalizedPosition = (float)m_categoryIndexSelected / (float)(m_categoriesList.items.Count - 1);
		}
	}

	private BodyCategoryItem InstantiateCategory(string category, string sprite, bool isFavorite = false)
	{
		BodyCategoryItem bodyCategoryItem = new BodyCategoryItem(this, category, sprite, isFavorite);
		m_categoriesList.AddItem(bodyCategoryItem);
		return bodyCategoryItem;
	}

	private bool IsDuplicationRimGroup(string group)
	{
		return m_duplicatedRimGroups.Contains(group.ToLower());
	}

	private void ClearBodyPartList()
	{
		if (m_partsList.GetCount() != 0)
		{
			m_partsList.Clear();
		}
	}

	private void FillBodyPartList(BodyCategoryItem data)
	{
		shineStops = true;
		ClearBodyPartList();
		isPartsMode.value = true;
		if (data.target == "tires")
		{
			customizeMode = CustomizeMode.Tires;
			TirePartItem tirePartItem = new TirePartItem(this, new CarVisualManager.TireVisualSettings
			{
				logo = CarVisualManager.TireLogos.None,
				stripes = CarVisualManager.TireLogoStripes.None,
				visibleLogo = 0f,
				visibleStripes = 0f
			});
			VendorSpritesAsset.VendorSpriteDesc vendorSpriteDesc = m_vendorsAsset.Find("stock");
			if (vendorSpriteDesc != null)
			{
				tirePartItem.asset = vendorSpriteDesc.sprite;
			}
			m_partsList.AddItem(tirePartItem);
			TakePartShot(tirePartItem, "part_tires");
			foreach (object value2 in Enum.GetValues(typeof(CarVisualManager.TireLogos)))
			{
				if ((CarVisualManager.TireLogos)value2 != CarVisualManager.TireLogos.None)
				{
					TirePartItem tirePartItem2 = new TirePartItem(this, new CarVisualManager.TireVisualSettings
					{
						logo = (CarVisualManager.TireLogos)value2,
						stripes = CarVisualManager.TireLogoStripes.None,
						visibleLogo = 1f,
						visibleStripes = 1f
					});
					m_partsList.AddItem(tirePartItem2);
					TakePartShot(tirePartItem2, value2.ToString().ToLower());
				}
			}
		}
		else
		{
			customizeMode = ((m_targetedCategory.target == "wheel") ? CustomizeMode.Wheels : CustomizeMode.Part);
			CarPart partById = CarPartDatabase.GetPartById(m_car3dView.selectedCarData.defaultWheelId);
			m_currentParts.Clear();
			if (selectedCategory.target == "favorite")
			{
				foreach (var favoriteRim in prefs.favoriteRims.favoriteRims)
				{
					m_currentParts.Add(CarPartDatabase.GetPartById(favoriteRim.Item1));
				}
			}
			else
			{
				m_currentParts = CarPartDatabase.GetParts(m_slotScheme, m_targetedCategory.target, data.target);
			}
			SetIsFavoriteRims(m_currentParts.Count > 0);
			PartVendorsList partVendorsList = new PartVendorsList();
			foreach (CarPart currentPart in m_currentParts)
			{
				if (!m_carFactory.SpecialCarCanApplyPart(m_carId, currentPart) || (customizeMode == CustomizeMode.Wheels && (currentPart.id == 0 || IsDuplicationRimGroup(currentPart.group))))
				{
					continue;
				}
				bool isModPart = false;
				if (currentPart.packetid != 0)
				{
					if (!CarPartDatabase.externalDatabases.TryGetValue(currentPart.packetid, out var value) || (!value.isLocal && !UGCServiceProvider.instance.IsContentInstalled(currentPart.packetid)))
					{
						continue;
					}
					isModPart = true;
					currentPart.vendor = "moddedpart";
				}
				bool isFavorite = prefs.favoriteRims.IsFavorite((currentPart.id, currentPart.packetid));
				BodyPartItem bodyPartItem = new BodyPartItem(this, currentPart, isModPart, isFavorite);
				if (bodyPartItem.part.DLCCode != DLCCode.Unknown && bodyPartItem.part.DLCCode != DLCCode.RuntimeDLC && string.IsNullOrEmpty(bodyPartItem.part.vendor) && bodyPartItem.part.type == "chassis")
				{
					bodyPartItem.part.vendor = "dlc";
				}
				m_partsList.AddItem(bodyPartItem);
				if (customizeMode != CustomizeMode.Wheels)
				{
					partVendorsList.AddVendor(bodyPartItem);
				}
				TakePartShot(bodyPartItem, (currentPart.id != 0) ? currentPart.prefab : partById.prefab);
				if (selectedCategory.target == "favorite")
				{
					data = TryGetBodyCategoryItemByPart(currentPart);
				}
				FillLockedState(bodyPartItem, data.groupId);
			}
			if (customizeMode != CustomizeMode.Wheels)
			{
				m_partsList.Sort(delegate(BasePartItem p0, BasePartItem p1)
				{
					VendorSpritesAsset.VendorSpriteDesc vendorSpriteDesc2 = m_vendorsAsset.Find(p0.part.vendor);
					VendorSpritesAsset.VendorSpriteDesc vendorSpriteDesc3 = m_vendorsAsset.Find(p1.part.vendor);
					if (vendorSpriteDesc2 == null || vendorSpriteDesc3 == null)
					{
						return 1;
					}
					if (vendorSpriteDesc2.sortId > vendorSpriteDesc3.sortId)
					{
						return 1;
					}
					return (vendorSpriteDesc2.sortId < vendorSpriteDesc3.sortId) ? (-1) : PartVendorsList.CompareDescriptions(p0.part.description, p1.part.description);
				});
			}
		}
		m_partIndexSelected = GetInstalledPartIndex();
		SelectionPart(m_partIndexSelected);
		m_navMode = ((m_parts.Count > 0) ? NavigationMode.Parts : NavigationMode.Categories);
		m_partsList.RecalcList();
		m_partsList.ScrollTo(m_partIndexSelected);
		UpdateCarPartBuyState(m_partsList.GetSelectedItem());
		RefreshEquippedItems();
		Canvas.ForceUpdateCanvases();
	}

	private BodyCategoryItem TryGetBodyCategoryItemByPart(CarPart part)
	{
		return m_categories.Find((BodyCategoryItem category) => category.target == part.group);
	}

	private void EnsureCategorySelection(string currentTarget)
	{
		if (string.IsNullOrEmpty(currentTarget))
		{
			return;
		}
		for (int i = 0; i < m_categoriesList.GetCount(); i++)
		{
			if (m_categoriesList[i]?.target == currentTarget)
			{
				m_categoriesList.selectedIndex = i;
				m_categoryIndexSelected = i;
				break;
			}
		}
	}

	public void OnSelectBodyPart(BasePartItem item)
	{
		if (m_navMode != NavigationMode.Parts)
		{
			m_navMode = NavigationMode.Parts;
		}
		string currentTarget = m_categoriesList.GetSelectedItem()?.target;
		PreviewCarPart(item);
		if (item is TirePartItem)
		{
			for (int i = 0; i < m_partsList.GetCount(); i++)
			{
				if (m_partsList[i] == item)
				{
					m_partsList.selectedIndex = i;
					m_partIndexSelected = i;
				}
				m_partsList[i].selected = false;
			}
			item.selected = true;
			SaveCurrentVisualTiresParts();
			SaveCurrentVisualSettings();
			EnsureCategorySelection(currentTarget);
			RefreshEquippedItems();
			return;
		}
		SelectionPart(item);
		m_externalPart = null;
		if (item.part.external)
		{
			ulong packetid = item.part.packetid;
			if (CarPartDatabase.externalDatabases[packetid].isLocal)
			{
				string directoryName = Path.GetDirectoryName(item.part.prefab);
				m_externalPartID = "";
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				if (File.Exists(directoryName + directorySeparatorChar + "pid.txt"))
				{
					directorySeparatorChar = Path.DirectorySeparatorChar;
					m_externalPartID = File.ReadAllText(directoryName + directorySeparatorChar + "pid.txt");
					if (m_externalPartID == "0")
					{
						m_externalPart = item;
					}
					else
					{
						UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Cars, item.part.ugcServiceProvider).IsMyPackage(ulong.Parse(m_externalPartID), delegate(bool isMy)
						{
							if (isMy)
							{
								m_externalPart = item;
							}
						});
					}
				}
			}
		}
		EnsureCategorySelection(currentTarget);
		RefreshEquippedItems();
	}

	private void InitApiClient()
	{
		if (m_apiClient == null)
		{
			m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
			{
				GameLog.Print(AlertLevel.Error, "ServerSharingService", "Server sharing: " + error.debugMessage);
			});
		}
	}

	private bool ValidateDlcParts(List<CarPart> parts)
	{
		foreach (CarPart part in parts)
		{
			if (!(part.vendor != "dlc") && !Overlay.instance.IsDLCInstalled(part.DLCCode))
			{
				return false;
			}
		}
		return true;
	}

	public int ConstructPartList()
	{
		m_tempItems.Clear();
		int price = 0;
		if (m_previewSettings.frontWheels != m_currentSettings.frontWheels || m_previewSettings.rearWheels != m_currentSettings.rearWheels)
		{
			CarPart item = BuyWheelIfNeed(m_previewSettings.frontWheels, ref price);
			m_tempItems.Add(item);
			if (wheelMode.value != 2)
			{
				item = BuyWheelIfNeed(m_previewSettings.rearWheels, ref price);
				m_tempItems.Add(item);
			}
		}
		foreach (int part in m_previewSettings.parts)
		{
			if (m_currentSettings.parts.Contains(part))
			{
				continue;
			}
			CarPart item = CarPartDatabase.GetPartById(part);
			if (!(item.type == "core"))
			{
				if (IsCarPartLocked(item))
				{
					price += item.price;
				}
				m_tempItems.Add(item);
			}
		}
		return price;
	}

	private CarPart BuyWheelIfNeed(int id, ref int price)
	{
		List<string> groups = CarPartDatabase.GetGroups(m_playerCar.id, "wheel");
		CarPart partById = CarPartDatabase.GetPartById(id);
		if (IsCarPartLocked(partById))
		{
			price += m_shopInfo.GetWheelPrice(groups.IndexOf(partById.group));
		}
		return partById;
	}

	public void OnWheelSizeChange()
	{
		if (m_isActivated && isDiskResizable.value)
		{
			m_currentProfile.controllers["front_disc_diam"] = m_frontWheelSize.changers[0].EvaluatePhase(frontSizeCaption.value);
			m_currentProfile.controllers["rear_disc_diam"] = m_rearWheelSize.changers[0].EvaluatePhase(rearSizeCaption.value);
			m_car3dView.ApplyWheelSizeUpdate(m_currentProfile);
			m_car3dView.IconGenerate();
		}
	}

	public void ClearCategoriesSelection()
	{
		m_categoriesList.selectedIndex = -1;
	}

	public void SelectCategory(BodyCategoryItem category)
	{
		for (int i = 0; i < m_categories.Count; i++)
		{
			if (m_categories[i] == category)
			{
				m_categoryIndexSelected = i;
				m_categoriesList.selectedIndex = i;
				break;
			}
		}
	}

	private void UpdateTopPanelTitleByCategory(BodyCategoryItem category)
	{
		if (category == null)
		{
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.customization_body.Localized());
		}
		else if (category.target == "wheel")
		{
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.customize_category_wheel.Localized());
		}
	}

	private void ClearPartsSelection()
	{
		if (m_parts.Count > 0)
		{
			foreach (BasePartItem part in m_parts)
			{
				part.selected = false;
			}
		}
		SelectionPart(-1);
	}

	public void BuyDlcPart()
	{
		DLCCode dLCCode = selectedPart.part.DLCCode;
		if (dLCCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(dLCCode))
		{
			Overlay.instance.ShowBuyDLCPage(dLCCode);
		}
	}

	private void OnPartsDeactivate()
	{
		shineStops = false;
	}

	private void FillLockedState(BasePartItem partContainer, int groupId)
	{
		partContainer.locked = IsCarPartLocked(partContainer.part);
		if (m_shopInfo == null)
		{
			partContainer.price = 0;
			return;
		}
		int price = ((customizeMode == CustomizeMode.Wheels) ? m_shopInfo.GetWheelPrice(groupId) : partContainer.part.price);
		partContainer.price = price;
	}

	private void SetSettingsTire(ref CarVisualManager.TireVisualSettings tireVisualSettings, TirePartItem tirePart)
	{
		CarVisualManager.TyreProtectorGripType protectorGrip = tireVisualSettings.ProtectorGrip;
		tireVisualSettings = tirePart.presetSettings.Copy();
		tireVisualSettings.ProtectorGrip = protectorGrip;
	}

	private bool IsCarPartLocked(CarPart part)
	{
		return CarPartDatabase.IsCarPartLocked(m_carId, part, m_playerCar);
	}

	private void PreviewCarPart(BasePartItem partContainer)
	{
		if (partContainer == null || GetInstalledPart() == partContainer)
		{
			return;
		}
		if (partContainer is TirePartItem tirePart)
		{
			switch ((WheelMode)wheelMode.value)
			{
			case WheelMode.Forward:
				SetSettingsTire(ref m_previewSettings.frontTire, tirePart);
				break;
			case WheelMode.Rear:
				SetSettingsTire(ref m_previewSettings.rearTire, tirePart);
				break;
			case WheelMode.All:
				SetSettingsTire(ref m_previewSettings.frontTire, tirePart);
				SetSettingsTire(ref m_previewSettings.rearTire, tirePart);
				break;
			}
			if (m_car == null)
			{
				m_car = m_car3dView.GetRaceCar();
			}
			m_carVisualManager.ApplyAllTires(m_car, m_previewSettings);
			return;
		}
		CarPart part = partContainer.part;
		if (part == null)
		{
			return;
		}
		switch (m_customizeMode)
		{
		case CustomizeMode.Part:
			m_slotScheme.Equip(part.id);
			m_slotScheme.ToPartList(m_previewSettings.parts);
			UpdatePartsNavigation(keepSelection: true);
			break;
		case CustomizeMode.Wheels:
			switch ((WheelMode)wheelMode.value)
			{
			case WheelMode.Forward:
				m_previewSettings.frontWheels = part.id;
				break;
			case WheelMode.Rear:
				m_previewSettings.rearWheels = part.id;
				break;
			case WheelMode.All:
				m_previewSettings.frontWheels = part.id;
				m_previewSettings.rearWheels = part.id;
				break;
			}
			if (m_previewSettings.frontWheels != m_cacheFrontWheelId && m_previewSettings.rearWheels != m_cacheRearWheelId)
			{
				m_cacheFrontWheelId = m_previewSettings.frontWheels;
				m_cacheRearWheelId = m_previewSettings.rearWheels;
			}
			break;
		case CustomizeMode.Tires:
			throw new Exception("Call PreviewCarPart for tires");
		}
		CarVisualManager.CustomizationFlags applyFlags = ((customizeMode == CustomizeMode.Wheels) ? CarVisualManager.CustomizationFlags.Wheels : CarVisualManager.CustomizationFlags.AllBody);
		ApplyPreviewSettings(applyFlags);
		TimeController.StartNow(m_car.swap.Initialize(m_car3dView.GetRaceCar(), prefs, m_db, -1, force: true));
		UpdateCarPartBuyState(partContainer);
		UpdateUseKitWheels();
	}

	private void UpdateCarPartBuyState(BasePartItem partContainer)
	{
		if (partContainer?.part == null)
		{
			return;
		}
		bool flag = partContainer.part.DLCCode != DLCCode.Unknown;
		bool flag2 = customizeMode == CustomizeMode.Wheels;
		m_lastTrackedDLCItem = null;
		string value = "";
		if (partContainer != null && IsCarPartLocked(partContainer.part))
		{
			cost.value = partContainer.price;
			if (flag && !flag2)
			{
				value = Overlay.instance.GetDLCName(partContainer.part.DLCCode);
				if (partContainer.locked)
				{
					m_lastTrackedDLCItem = partContainer;
				}
			}
		}
		else
		{
			cost.value = 0;
			if (partContainer != null && flag && !flag2)
			{
				value = Overlay.instance.GetDLCName(partContainer.part.DLCCode);
			}
		}
		BuyDLC.value = !string.IsNullOrEmpty(value) && !Overlay.instance.IsDLCInstalled(partContainer.part.DLCCode);
		DLCName.value = value;
	}

	private void TakePartShot(BasePartItem partUI, string prefab)
	{
		if (partUI.icon != null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(partUI.part.vendor) && m_vendorsAsset != null)
		{
			VendorSpritesAsset.VendorSpriteDesc vendorSpriteDesc = m_vendorsAsset.Find(partUI.part.vendor);
			if (vendorSpriteDesc != null)
			{
				partUI.asset = vendorSpriteDesc.sprite;
				if (partUI.asset != null)
				{
					return;
				}
			}
		}
		partUI.icon = prefab;
	}

	private void MountCarPart(CarPart part, bool ignoreForceParts = false, bool instant = true)
	{
		if (!(part.type == "wheel"))
		{
			m_slotScheme.Equip(part.id, ignoreForceParts);
			m_slotScheme.ToPartList(m_previewSettings.parts);
			ApplyPreviewSettings(CarVisualManager.CustomizationFlags.AllBody, instant);
		}
	}

	private void UpdatePartsNavigation(bool keepSelection = false)
	{
		int categoryIndexSelected = m_categoryIndexSelected;
		NavigateCategory(null);
		if (keepSelection)
		{
			m_categoryIndexSelected = categoryIndexSelected;
			m_categoriesList.selectedIndex = categoryIndexSelected;
		}
	}

	private void ApplyPreviewSettings(CarVisualManager.CustomizationFlags applyFlags, bool instant = true)
	{
		if (instant)
		{
			if ((bool)m_car3dView)
			{
				m_car3dView.ApplyCustomization(applyFlags, m_previewSettings);
			}
		}
		else
		{
			m_applyVSCommandsBuffer.Add(applyFlags);
		}
	}

	private void DoApplyPreviewSettingsIfAny()
	{
		if (m_applyVSCommandsBuffer.Count != 0 && !(m_car3dView == null))
		{
			CarVisualManager.CustomizationFlags customizationFlags = CarVisualManager.CustomizationFlags.None;
			for (int i = 0; i < m_applyVSCommandsBuffer.Count; i++)
			{
				customizationFlags |= m_applyVSCommandsBuffer[i];
			}
			m_car3dView.ApplyCustomization(customizationFlags, m_previewSettings);
			m_applyVSCommandsBuffer.Clear();
		}
	}

	private void SaveCurrentVisualSettings()
	{
		m_carVisualManager.SaveVisualSettingsForCar(m_carId, m_currentSettings);
		m_car3dView.IconGenerate();
	}

	private void SaveCurrentVisualTiresParts()
	{
		m_currentSettings.frontTire = m_previewSettings.frontTire;
		m_currentSettings.rearTire = m_previewSettings.rearTire;
	}

	private void UpdateUseKitWheels()
	{
		useKitWheelsVisible.value = m_previewSettings.kitFrontWheels != 0 || m_previewSettings.kitRearWheels != 0;
	}

	private void UpdateBottomPanel()
	{
		m_bottomHintsCache.Clear();
		m_bottomHintsCache.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
		if (useKitWheelsVisible.value && !m_previewSettings.lockChangeWheels && m_categoryIndexSelected == 0)
		{
			m_bottomHintsCache.Add(new ActionHintData
			{
				hint = LocalizationKeys.kit_wheels_customize.Localized(),
				type = ActionType.NavBtnExpand,
				action = ToggleUseKitWheels
			});
		}
		m_bottomHintsCache.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_select.Localized(),
			type = ActionType.NavSelectBtnA,
			action = NavigationSubmit
		});
		if (customizeMode == CustomizeMode.Wheels && selectedPart != null && isPartsMode.value)
		{
			LocalizationKeys.Key key = (selectedPart.isFavourite ? LocalizationKeys.car_catalog_remove_from_favorites : LocalizationKeys.car_catalog_add_to_favorites);
			m_bottomHintsCache.Add(new ActionHintData
			{
				hint = key.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = ToggleFavoriteRim
			});
		}
		if (selectedPart != null && selectedPart.part.external)
		{
			m_bottomHintsCache.Add(new ActionHintData
			{
				hint = LocalizationKeys.ugc_manager_show_item_in_workshop.Localized(),
				type = ActionType.NavBtnExpand,
				action = delegate
				{
					OnShowItemInWorkshop(selectedPart);
				}
			});
		}
		ShowBottomPanel(m_bottomHintsCache);
	}

	private void OnShowItemInWorkshop(BasePartItem partItem)
	{
		if (partItem.isModPart)
		{
			IUGCManager managerBy = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Cars, partItem.part.ugcServiceProvider);
			if (managerBy != null && managerBy.TryGetInstalledItem(partItem.part.packetid) is IUGCMeta iUGCMeta)
			{
				iUGCMeta.ShowInOverlay();
			}
		}
	}

	public void ToggleUseKitWheels()
	{
		bool flag = (m_previewSettings.kitFrontWheels != m_previewSettings.frontWheels || m_previewSettings.kitRearWheels != m_previewSettings.rearWheels) && !useKitWheels.value;
		m_previewSettings.frontWheels = (flag ? m_previewSettings.kitFrontWheels : m_cacheFrontWheelId);
		m_previewSettings.rearWheels = (flag ? m_previewSettings.kitRearWheels : m_cacheRearWheelId);
		ApplyPreviewSettings(CarVisualManager.CustomizationFlags.Wheels);
	}

	private int GetInstalledPartIndex()
	{
		if (string.IsNullOrEmpty(m_targetedCategory?.target))
		{
			return 0;
		}
		switch (customizeMode)
		{
		case CustomizeMode.Part:
		{
			for (int j = 0; j < m_parts.Count; j++)
			{
				BasePartItem basePartItem = m_parts[j];
				if (m_previewSettings.parts.Contains(basePartItem.part.id))
				{
					return j;
				}
			}
			break;
		}
		case CustomizeMode.Wheels:
		{
			for (int k = 0; k < m_parts.Count; k++)
			{
				BasePartItem basePartItem2 = m_parts[k];
				int wheelId = ((wheelMode.value == 1) ? m_previewSettings.rearWheels : m_previewSettings.frontWheels);
				if (useKitWheels.value)
				{
					wheelId = ((wheelMode.value == 1) ? m_previewSettings.kitRearWheels : m_previewSettings.kitFrontWheels);
				}
				TrySetDefaultWheelId(ref wheelId);
				if (basePartItem2.part.id == wheelId)
				{
					return k;
				}
			}
			return 0;
		}
		case CustomizeMode.Tires:
		{
			for (int i = 0; i < m_parts.Count; i++)
			{
				CarVisualManager.TireVisualSettings presetSettings = (m_parts[i] as TirePartItem).presetSettings;
				CarVisualManager.TireVisualSettings tireVisualSettings = ((wheelMode.value == 1) ? m_previewSettings.rearTire : m_previewSettings.frontTire);
				if (presetSettings.logo == tireVisualSettings.logo && presetSettings.stripes == tireVisualSettings.stripes)
				{
					return i;
				}
			}
			break;
		}
		}
		return 0;
	}

	private BasePartItem GetInstalledPart()
	{
		if (string.IsNullOrEmpty(m_targetedCategory?.target))
		{
			return null;
		}
		switch (customizeMode)
		{
		case CustomizeMode.Part:
			foreach (BasePartItem part in m_parts)
			{
				if (m_previewSettings.parts.Contains(part.part.id))
				{
					return part;
				}
			}
			break;
		case CustomizeMode.Wheels:
			foreach (BasePartItem part2 in m_parts)
			{
				switch ((WheelMode)wheelMode.value)
				{
				case WheelMode.Forward:
					if (part2.part.id == m_previewSettings.frontWheels)
					{
						return part2;
					}
					break;
				case WheelMode.Rear:
					if (part2.part.id == m_previewSettings.rearWheels)
					{
						return part2;
					}
					break;
				case WheelMode.All:
					if (part2.part.id == m_previewSettings.frontWheels && part2.part.id == m_previewSettings.rearWheels)
					{
						return part2;
					}
					break;
				}
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case CustomizeMode.Tires:
			break;
		}
		return null;
	}

	[GameHotkey(ActionType.NavUp, false, false)]
	public void NavigationUp()
	{
		NavigationMode navMode = m_navMode;
		if (navMode != NavigationMode.Categories && navMode == NavigationMode.Parts)
		{
			m_navMode = NavigationMode.Categories;
			ClearPartsSelection();
		}
	}

	[GameHotkey(ActionType.NavDown, false, false)]
	public void NavigationDown()
	{
		if (m_parts.Count == 0 && selectedCategory.target == "favorite")
		{
			m_navMode = NavigationMode.Categories;
			return;
		}
		if (m_navMode != NavigationMode.Categories)
		{
			_ = 1;
			return;
		}
		selectedCategory?.OnClick();
		if (m_parts.Count > 0)
		{
			m_navMode = NavigationMode.Parts;
		}
	}

	public void SelectionPart(BasePartItem part)
	{
		int index = 0;
		for (int i = 0; i < m_parts.Count; i++)
		{
			if (m_parts[i] == part)
			{
				index = i;
				break;
			}
		}
		SelectionPart(index);
	}

	public void SelectionPart(int index)
	{
		m_partsList.selectedIndex = index;
		m_partIndexSelected = index;
		ShowUGCItemDetailsIfNeeded();
		UpdateBottomPanel();
	}

	private void ShowUGCItemDetailsIfNeeded()
	{
		if (selectedPart == null)
		{
			m_showPublicationBlock.value = false;
			return;
		}
		m_showPublicationBlock.value = selectedPart.part.packetid != 0;
		if (selectedPart.part.packetid == 0L)
		{
			return;
		}
		UGCItem uGCItem = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Cars, selectedPart.part.ugcServiceProvider).TryGetInstalledItem(selectedPart.part.packetid);
		if (uGCItem != null)
		{
			if (!m_ugcItems.Contains(uGCItem))
			{
				m_ugcItems.Add(uGCItem);
			}
			if (m_lastUgcId != uGCItem.id)
			{
				m_lastUgcId = uGCItem.id;
				m_baseSharingPublication.InitView(uGCItem);
				m_baseSharingPublication.SetMetaData(uGCItem as IUGCMeta);
				m_baseSharingPublication.SetOwnerStatus(uGCItem as IUGCOwnerSystem);
				m_baseSharingPublication.SetSubscridedCount(uGCItem as IUGCSubscridable);
				m_baseSharingPublication.SetScore(uGCItem as IUGCRatable);
				m_baseSharingPublication.DisposeResource();
				m_baseSharingPublication.SetIcon(uGCItem as IUGCIconSystem);
				m_baseSharingPublication.SetIconVisibility(visible: true);
			}
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

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		switch (m_navMode)
		{
		case NavigationMode.Categories:
			if (m_categoryIndexSelected >= 0 && m_categoryIndexSelected < m_categories.Count && m_categories.Count > 1)
			{
				m_categoryIndexSelected++;
				if (m_categoryIndexSelected >= m_categories.Count)
				{
					m_categoryIndexSelected = m_categories.Count - 1;
				}
			}
			else
			{
				m_categoryIndexSelected = 0;
			}
			m_categoriesList.selectedIndex = m_categoryIndexSelected;
			break;
		case NavigationMode.Parts:
			SelectionPart(m_partIndexSelected);
			if (m_partIndexSelected >= 0 && m_partIndexSelected < m_parts.Count && m_parts.Count > 1)
			{
				m_partIndexSelected++;
				if (m_partIndexSelected >= m_parts.Count)
				{
					m_partIndexSelected = m_parts.Count - 1;
				}
			}
			else
			{
				m_partIndexSelected = 0;
			}
			if (m_partIndexSelected >= 0 && m_partIndexSelected < m_parts.Count && m_parts.Count > 1)
			{
				SelectionPart(m_partIndexSelected);
			}
			break;
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		switch (m_navMode)
		{
		case NavigationMode.Categories:
			if (m_categoryIndexSelected >= 0 && m_categoryIndexSelected < m_categories.Count && m_categories.Count > 1)
			{
				m_categoryIndexSelected--;
				if (m_categoryIndexSelected < 0)
				{
					m_categoryIndexSelected = 0;
				}
			}
			else
			{
				m_categoryIndexSelected = 0;
			}
			m_categoriesList.selectedIndex = m_categoryIndexSelected;
			break;
		case NavigationMode.Parts:
			if (m_partIndexSelected >= 0 && m_partIndexSelected < m_parts.Count && m_parts.Count > 1)
			{
				m_partIndexSelected--;
				if (m_partIndexSelected < 0)
				{
					m_partIndexSelected = 0;
				}
			}
			else
			{
				m_partIndexSelected = 0;
			}
			if (m_partIndexSelected >= 0 && m_partIndexSelected < m_parts.Count && m_parts.Count > 1)
			{
				SelectionPart(m_partIndexSelected);
			}
			break;
		}
	}

	public void NavigationSubmit()
	{
		switch (m_navMode)
		{
		case NavigationMode.Categories:
			SelectCurrentCategory();
			break;
		case NavigationMode.Parts:
			if (m_partIndexSelected >= 0 && m_partIndexSelected < m_parts.Count)
			{
				m_parts[m_partIndexSelected].OnClick();
			}
			break;
		}
	}

	private void InitCategorySelection()
	{
		if (m_prevCarId != m_carId)
		{
			m_categoryIndexSelected = 0;
			m_prevCarId = m_carId;
		}
		SelectCurrentCategory();
	}

	private void SelectCurrentCategory()
	{
		if (m_categoryIndexSelected >= 0 && m_categoryIndexSelected < m_categories.Count)
		{
			m_categories[m_categoryIndexSelected].OnClick();
		}
	}

	public void GoToBack()
	{
		if (m_waitLoadingCarEndReturn != null)
		{
			return;
		}
		if (m_targetedCategory?.target == "wheel")
		{
			m_navMode = NavigationMode.Categories;
			m_categoryIndexSelected = 0;
			SetIsFavoriteRims();
			NavigationSubmit();
			ShowUGCItemDetailsIfNeeded();
			return;
		}
		int num = ConstructPartList();
		if (m_tempItems.Count == 0 || !ValidateDlcParts(m_tempItems))
		{
			ReturnToGarage();
		}
		else if (num == 0)
		{
			MessageApplyItems();
		}
		else if (num > prefs.money.coins)
		{
			MessageNotEnoughMoney();
		}
		else
		{
			MessageBuyItems(num);
		}
	}

	private void MessageNotEnoughMoney()
	{
		UIMessageBox.ShowDialog(LocalizationKeys.not_enough_money.Localized(), null, new DialogButtonData[2]
		{
			new DialogButtonData
			{
				action = null,
				text = LocalizationKeys.nav_hint_goto_back.Localized()
			},
			new DialogButtonData
			{
				action = ReturnToGarage,
				text = LocalizationKeys.reset.Localized()
			}
		});
	}

	private void MessageBuyItems(int price)
	{
		int[] itemIds = new int[m_tempItems.Count];
		for (int i = 0; i < m_tempItems.Count; i++)
		{
			itemIds[i] = m_tempItems[i].id;
		}
		DialogButtonData[] btns = new DialogButtonData[3]
		{
			new DialogButtonData
			{
				action = delegate
				{
					if (m_transactionManager.TryToPerformUnlockTransactions(TransactionManager.UnlockType.CarPart, itemIds, price, ECurrencyType.Coins))
					{
						EquipItems(m_tempItems);
					}
				},
				text = LocalizationKeys.ShopBuy.Localized()
			},
			new DialogButtonData
			{
				action = ReturnToGarage,
				text = LocalizationKeys.No.Localized()
			},
			new DialogButtonData
			{
				action = null,
				text = LocalizationKeys.Cancel.Localized()
			}
		};
		UIMessageBox.ShowDialog(LocalizationKeys.message_buy.LocalizedFormat(UIUtils.DigitsWithSeparate(price)), null, btns);
	}

	private void MessageApplyItems()
	{
		DialogButtonData[] btns = new DialogButtonData[3]
		{
			new DialogButtonData
			{
				action = delegate
				{
					EquipItems(m_tempItems);
				},
				text = LocalizationKeys.Yes.Localized()
			},
			new DialogButtonData
			{
				action = ReturnToGarage,
				text = LocalizationKeys.No.Localized()
			},
			new DialogButtonData
			{
				action = null,
				text = LocalizationKeys.Cancel.Localized()
			}
		};
		UIMessageBox.ShowDialog(LocalizationKeys.car_profile_window_save_changes_text.Localized(), null, btns);
	}

	private void SetIsFavoriteRims(bool isFavorite = true)
	{
		m_isFavoriteRims.value = isFavorite;
	}

	private void ReturnToGarage()
	{
		if (m_waitLoadingCarEndReturn == null)
		{
			if (!m_car.IsLoadingVisualInProgress)
			{
				HideAndPushGarage();
			}
			else
			{
				m_waitLoadingCarEndReturn = StartCoroutine(WaitCarLoadingAndReturn());
			}
		}
	}

	private IEnumerator WaitCarLoadingAndReturn()
	{
		yield return new WaitWhile(() => m_car.IsLoadingVisualInProgress);
		HideAndPushGarage();
		m_waitLoadingCarEndReturn = null;
	}

	private void HideAndPushGarage()
	{
		Hide();
		base.owner.Push<UIGarageContext>();
	}

	private void EquipItems(IList<CarPart> unlockedItems)
	{
		foreach (CarPart unlockedItem in unlockedItems)
		{
			MountCarPart(unlockedItem, ignoreForceParts: true, instant: false);
		}
		DoApplyPreviewSettingsIfAny();
		ApplyItems();
		ReturnToGarage();
	}

	private void ApplyItems()
	{
		m_currentSettings.kitFrontWheels = m_previewSettings.kitFrontWheels;
		m_currentSettings.kitRearWheels = m_previewSettings.kitRearWheels;
		m_currentSettings.frontWheels = m_previewSettings.frontWheels;
		m_currentSettings.rearWheels = m_previewSettings.rearWheels;
		m_currentSettings.frontTire = m_previewSettings.frontTire;
		m_currentSettings.rearTire = m_previewSettings.rearTire;
		m_currentSettings.parts.Clear();
		foreach (int part in m_previewSettings.parts)
		{
			m_currentSettings.parts.Add(part);
		}
		SaveCurrentVisualSettings();
	}

	public void ToggleWheelMode()
	{
		wheelMode.value++;
		if (wheelMode.value > 2)
		{
			wheelMode.value = 0;
		}
	}

	public static string GetSpriteName(string partType)
	{
		return partType switch
		{
			"wheel" => "part_rims", 
			"wing" => "part_wings", 
			"bumperfront" => "part_bumper_front", 
			"bumperrear" => "part_bumper_rear", 
			"chassis" => "part_body", 
			"lightsfront" => "part_headlights", 
			"lightsrear" => "part_stop", 
			"mirror" => "part_rearviewmirror", 
			"skirts" => "part_skirt", 
			_ => "part_" + partType, 
		};
	}
}
