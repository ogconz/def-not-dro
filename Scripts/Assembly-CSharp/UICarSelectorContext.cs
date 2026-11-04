using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DB.Meta;
using DI;
using GameInput;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class UICarSelectorContext : BaseBindingBehaviourTarget
{
	public delegate List<PlayerCar> FilterCars();

	private const float TOP_OFFSET = 10f;

	private const float BOTTOM_OFFSET = 19f;

	private const float STOP_OFFSET = 15f;

	private const float FIND_OFFSET = 0f;

	private const int COLUMNS_COUNT = 4;

	private const int INITIAL_CATEGORY = 1;

	private const float CAT_ITEM_NAVIGATION_OFFSET = 8f;

	private readonly StringProperty m_carsCount = new StringProperty();

	private readonly BoolProperty m_noFavoriteCars = new BoolProperty();

	[Header("Categories")]
	[SerializeField]
	private GameObject m_categoryPrefab;

	[SerializeField]
	private RectTransform m_categoriesContainer;

	[SerializeField]
	private ScrollRect m_categoriesScrollRect;

	[Header("Cars and vendors")]
	[SerializeField]
	private GameObject m_vendorItemPrefab;

	[SerializeField]
	private RectTransform m_mainContainer;

	[SerializeField]
	private ScrollRect m_mainScrollRect;

	[SerializeField]
	private float m_interactivitySecondsDelay;

	public FilterCars carsFilter;

	public Action<UICarSelectorCar> onCarSelected;

	public System.Action goBackAction;

	private readonly List<UICarSelectorCategory> m_categories = new List<UICarSelectorCategory>();

	private CarVendorsAsset m_carVendorsAsset;

	private int m_selectedCategoryIndex;

	private int m_selectedCarIndex;

	private int m_selectedVendorIndex;

	private List<ActionHintData> m_actionHints = new List<ActionHintData>();

	private Coroutine m_scrollCoroutine;

	private bool m_needLerpScroll;

	private WaitForEndOfFrame m_endOfFrame = new WaitForEndOfFrame();

	[Dependency]
	private PlayerCarFactory m_playerCarFactory;

	private readonly List<UICarSelectorCar> m_visibleCars = new List<UICarSelectorCar>(120);

	private readonly List<UICarSelectorCar> m_loadedCars = new List<UICarSelectorCar>(120);

	private readonly List<UICarSelectorVendor> m_loadedVendors = new List<UICarSelectorVendor>(32);

	private readonly List<CarVendor> m_activeVendors = new List<CarVendor>(32);

	private readonly List<PlayerCar> m_tempCarsList = new List<PlayerCar>(120);

	private float m_time;

	[field: Dependency]
	public GamePrefs prefs { get; }

	public IReadOnlyList<UICarSelectorCategory> categories => m_categories;

	protected override void OnActivate()
	{
		base.OnActivate();
		if (States.mainCurrent is GarageGUIState garageGUIState)
		{
			m_carVendorsAsset = garageGUIState.carVendorsAsset;
		}
		if (States.mainCurrent is XDSRaceModeState)
		{
			CameraSwitch.instance.rotateMode = false;
		}
		if ((object)m_carVendorsAsset == null)
		{
			m_carVendorsAsset = CarVendorsAsset.LoadAndInitDB();
		}
		m_mainContainer.DestroyChildren();
		UpdateListCar();
		m_time = Time.unscaledTime;
	}

	private void UpdateListCar()
	{
		FillCarsList();
		FillVendorsList();
		FillCategories();
		SelectStartCategory();
		SelectItemInCurrentCategory(prefs.globals.lastCarId);
		ScrollToSelectedItem(interruptable: false);
		RefreshFavoritesText();
		UpdateBottomPanel();
		BringForwardByDepth();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.catalog_title.Localized());
	}

	public bool CanBeInteractable()
	{
		return Time.unscaledTime - m_time > m_interactivitySecondsDelay;
	}

	protected override void OnDeactivate()
	{
		foreach (UICarSelectorCar loadedCar in m_loadedCars)
		{
			m_playerCarFactory.carItemsPool.ReturnToPool(loadedCar);
		}
		StopScrollCoroutine();
		base.OnDeactivate();
	}

	private void FillCategories()
	{
		m_categoriesContainer.DestroyChildren();
		m_categories.Clear();
		AddCategory().InitFavorites(OnCategorySelected);
		AddCategory().InitAllCars(OnCategorySelected);
		AddCategory().InitAllVendors(OnCategorySelected);
		foreach (CarVendor activeVendor in m_activeVendors)
		{
			AddCategory().InitVendor(activeVendor, OnCategorySelected);
		}
		UICarSelectorCategory AddCategory()
		{
			UICarSelectorCategory component = UnityEngine.Object.Instantiate(m_categoryPrefab, m_categoriesContainer).GetComponent<UICarSelectorCategory>();
			m_categories.Add(component);
			return component;
		}
	}

	private void FillVendorsList()
	{
		m_activeVendors.Clear();
		foreach (CarVendor vendor in m_carVendorsAsset.Vendors)
		{
			if (!IsVendorCarListEmpty(vendor.id))
			{
				m_activeVendors.Add(vendor);
			}
		}
		m_loadedVendors.Clear();
		foreach (CarVendor activeVendor in m_activeVendors)
		{
			UICarSelectorVendor component = UnityEngine.Object.Instantiate(m_vendorItemPrefab, m_mainContainer).GetComponent<UICarSelectorVendor>();
			component.Initialize(this, activeVendor);
			m_loadedVendors.Add(component);
		}
	}

	private int SortCarMeta(PlayerCar left, PlayerCar right)
	{
		if (left.sort == right.sort)
		{
			return left.name.CompareTo(right.name);
		}
		return left.sort.CompareTo(right.sort);
	}

	private void FillCarsList()
	{
		CarItemsPool carItemsPool = m_playerCarFactory.carItemsPool;
		List<PlayerCar> obj = ((carsFilter != null) ? carsFilter() : new List<PlayerCar>(carItemsPool.cars));
		obj.Sort(SortCarMeta);
		m_loadedCars.Clear();
		foreach (PlayerCar item in obj)
		{
			if (item.enabled && !item.forcedDisable)
			{
				UICarSelectorCar uICarSelectorCar = carItemsPool.Get(item.id);
				if (!(uICarSelectorCar == null))
				{
					uICarSelectorCar.transform.SetParent(m_mainContainer);
					uICarSelectorCar.Initialize(this);
					m_loadedCars.Add(uICarSelectorCar);
				}
			}
		}
	}

	public void SetupFilter(Predicate<PlayerCar> rule)
	{
		carsFilter = ((rule != null) ? new FilterCars(Filter) : null);
		SelectStartCategory();
		RefreshFavoritesText();
		List<PlayerCar> Filter()
		{
			m_tempCarsList.Clear();
			foreach (PlayerCar car in m_playerCarFactory.carItemsPool.cars)
			{
				if (rule(car))
				{
					m_tempCarsList.Add(car);
				}
			}
			return m_tempCarsList;
		}
	}

	public void GotoSlotsContext()
	{
		if (States.mainCurrent is GarageGUIState)
		{
			Submit();
			base.owner.Hide<UIGarageContext>();
			base.owner.Push<UICarSlotsContext>().Init(GetSelectedCarId());
		}
		else
		{
			base.owner.HideStackAndPush<UICarSlotsContext>().Init(GetSelectedCarId());
		}
	}

	private void OnCategorySelected(UICarSelectorCategory cat)
	{
		OnCategorySelected(cat, force: false);
	}

	private void OnCategorySelected(UICarSelectorCategory cat, bool force)
	{
		if (!(cat == null))
		{
			UICarSelectorCategory selectedCategory = GetSelectedCategory();
			if (force || !(selectedCategory != null) || cat.type != selectedCategory.type || cat.vendorId != selectedCategory.vendorId)
			{
				m_mainScrollRect.ScrollToTop();
				SelectCategoryUiItem(cat.type, cat.vendorId);
				EnsureCatItemVisibility(cat);
				FilterMainList(cat.type, cat.vendorId);
				SelectItemInCurrentCategory();
				ScrollToSelectedItem(cat.type != UICarSelectorCategory.Type.All);
				RefreshFavoritesText();
				RefreshCarsCount();
				UpdateBottomPanel();
			}
		}
	}

	private void FilterMainList(UICarSelectorCategory.Type type, int vendorId)
	{
		bool isVendorCategory = vendorId != -1;
		switch (type)
		{
		case UICarSelectorCategory.Type.AllVendors:
			DisplayVendors();
			break;
		case UICarSelectorCategory.Type.Favorites:
			DisplayCars(FavoritesFilter, isVendorCategory: false);
			break;
		default:
			DisplayCars(VendorFilter, isVendorCategory);
			break;
		}
		bool FavoritesFilter(PlayerCar meta)
		{
			return prefs.favoriteCars.IsFavorite(meta.id);
		}
		bool VendorFilter(PlayerCar meta)
		{
			if (vendorId != -1)
			{
				return meta.vendorId == vendorId;
			}
			return true;
		}
	}

	private void DisplayVendors()
	{
		m_visibleCars.Clear();
		foreach (UICarSelectorCar loadedCar in m_loadedCars)
		{
			loadedCar.gameObject.SetActive(value: false);
		}
		foreach (UICarSelectorVendor loadedVendor in m_loadedVendors)
		{
			loadedVendor.gameObject.SetActive(value: true);
		}
	}

	private void DisplayCars(Func<PlayerCar, bool> filter, bool isVendorCategory)
	{
		m_visibleCars.Clear();
		foreach (UICarSelectorVendor loadedVendor in m_loadedVendors)
		{
			loadedVendor.gameObject.SetActive(value: false);
		}
		m_loadedCars.Sort((UICarSelectorCar c0, UICarSelectorCar c1) => SortCarMeta(c0.meta, c1.meta));
		for (int num = 0; num < m_loadedCars.Count; num++)
		{
			UICarSelectorCar uICarSelectorCar = m_loadedCars[num];
			uICarSelectorCar.transform.SetSiblingIndex(num);
			bool flag = filter?.Invoke(uICarSelectorCar.meta) ?? true;
			if (flag)
			{
				m_visibleCars.Add(uICarSelectorCar);
			}
			uICarSelectorCar.gameObject.SetActive(flag);
		}
	}

	public void OnPrevCat()
	{
		if (m_selectedCategoryIndex - 1 >= 0)
		{
			SelectCategoryByIndex(m_selectedCategoryIndex - 1);
		}
	}

	public void OnNextCat()
	{
		if (m_selectedCategoryIndex + 1 < categories.Count)
		{
			SelectCategoryByIndex(m_selectedCategoryIndex + 1);
		}
	}

	private void GoBack()
	{
		goBackAction.SafeCall();
	}

	private void Submit()
	{
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		if (selectedCategory == null)
		{
			return;
		}
		if (selectedCategory.type == UICarSelectorCategory.Type.AllVendors)
		{
			UICarSelectorVendor selectedVendor = GetSelectedVendor();
			if (selectedVendor != null)
			{
				SubmitVendor(selectedVendor.vendorId);
			}
		}
		else
		{
			UICarSelectorCar selectedCar = GetSelectedCar();
			if (selectedCar != null)
			{
				SubmitCar(selectedCar);
			}
		}
	}

	private void ToggleFavorite()
	{
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		if (selectedCategory == null)
		{
			return;
		}
		UICarSelectorCar selectedCar = GetSelectedCar();
		if (selectedCar == null)
		{
			return;
		}
		if (prefs.favoriteCars.IsFavorite(selectedCar.carId))
		{
			prefs.favoriteCars.RemoveCar(selectedCar.carId);
			selectedCar.SetFavorite(favorite: false);
			if (selectedCategory.type == UICarSelectorCategory.Type.Favorites)
			{
				m_visibleCars.Remove(selectedCar);
				selectedCar.gameObject.SetActive(value: false);
				NavigateThroughItems((m_selectedCarIndex >= m_visibleCars.Count) ? (-1) : 0, out var _);
			}
		}
		else
		{
			prefs.favoriteCars.AddCar(selectedCar.carId);
			selectedCar.SetFavorite(favorite: true);
		}
		RefreshFavoritesText();
		RefreshCarsCount();
		UpdateBottomPanel();
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	protected virtual void NavigationUp()
	{
		if (NavigateThroughItems(-4, out var needPlaySound))
		{
			ScrollToSelectedItem();
			if (needPlaySound)
			{
				SoundController.Play2DUISound(SoundSample.DefaultBtn);
			}
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	protected virtual void NavigationDown()
	{
		if (NavigateThroughItems(4, out var _))
		{
			ScrollToSelectedItem();
			SoundController.Play2DUISound(SoundSample.DefaultBtn);
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	protected virtual void NavigationRight()
	{
		if (NavigateThroughItems(1, out var needPlaySound))
		{
			ScrollToSelectedItem();
			if (needPlaySound)
			{
				SoundController.Play2DUISound(SoundSample.DefaultBtn);
			}
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	protected virtual void NavigationLeft()
	{
		if (NavigateThroughItems(-1, out var needPlaySound))
		{
			ScrollToSelectedItem();
			if (needPlaySound)
			{
				SoundController.Play2DUISound(SoundSample.DefaultBtn);
			}
		}
	}

	private void RefreshFavoritesText()
	{
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		m_noFavoriteCars.value = selectedCategory != null && selectedCategory.type == UICarSelectorCategory.Type.Favorites && prefs.favoriteCars.cars.Count == 0;
	}

	private UICarSelectorCategory GetSelectedCategory()
	{
		if (m_selectedCategoryIndex >= 0 && m_selectedCategoryIndex <= categories.Count - 1)
		{
			return categories[m_selectedCategoryIndex];
		}
		return null;
	}

	private UICarSelectorCar GetSelectedCar()
	{
		if (m_selectedCarIndex < 0 || m_selectedCarIndex >= m_visibleCars.Count)
		{
			return null;
		}
		return m_visibleCars[m_selectedCarIndex];
	}

	private UICarSelectorVendor GetSelectedVendor()
	{
		if (m_selectedVendorIndex < 0 || m_selectedVendorIndex >= m_loadedVendors.Count)
		{
			return null;
		}
		return m_loadedVendors[m_selectedVendorIndex];
	}

	public int GetSelectedCarId()
	{
		UICarSelectorCar selectedCar = GetSelectedCar();
		if (!(selectedCar != null))
		{
			return -1;
		}
		return selectedCar.carId;
	}

	public void SubmitCar(UICarSelectorCar car)
	{
		DependencyInjector.Resolve<CarVisualManager>().ClearVisualIfNeed(car.carId);
		onCarSelected.SafeCall(car);
	}

	public void SubmitVendor(int vendorId)
	{
		foreach (UICarSelectorCategory category in categories)
		{
			if (category.vendorId == vendorId)
			{
				OnCategorySelected(category);
				break;
			}
		}
	}

	public bool EqualSelectedCarId(int carId)
	{
		UICarSelectorCar selectedCar = GetSelectedCar();
		if (selectedCar != null)
		{
			return selectedCar.carId == carId;
		}
		return false;
	}

	public void SelectItemInCurrentCategory(int targetItemId = -1, bool forceScrollTo = false)
	{
		m_selectedCarIndex = -1;
		m_selectedVendorIndex = -1;
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		if (selectedCategory == null)
		{
			return;
		}
		if (selectedCategory.type == UICarSelectorCategory.Type.AllVendors)
		{
			int num = -1;
			for (int i = 0; i < m_loadedVendors.Count; i++)
			{
				UICarSelectorVendor uICarSelectorVendor = m_loadedVendors[i];
				num++;
				if (uICarSelectorVendor.isActiveGo)
				{
					bool selected = ((targetItemId == -1) ? (num == 0) : (uICarSelectorVendor.vendorId == targetItemId));
					uICarSelectorVendor.SetSelected(selected);
					if (uICarSelectorVendor.isSelected)
					{
						m_selectedVendorIndex = i;
					}
				}
			}
		}
		else
		{
			if (targetItemId == -1)
			{
				targetItemId = prefs.globals.lastCarId;
			}
			bool flag = false;
			UICarSelectorCar uICarSelectorCar = null;
			int selectedCarIndex = -1;
			for (int j = 0; j < m_visibleCars.Count; j++)
			{
				UICarSelectorCar uICarSelectorCar2 = m_visibleCars[j];
				if (uICarSelectorCar2.isActiveGo)
				{
					if (uICarSelectorCar == null)
					{
						uICarSelectorCar = uICarSelectorCar2;
						selectedCarIndex = j;
					}
					uICarSelectorCar2.SetSelected(uICarSelectorCar2.carId == targetItemId);
					if (uICarSelectorCar2.isSelected)
					{
						flag = true;
						m_selectedCarIndex = j;
					}
				}
			}
			if (!flag && uICarSelectorCar != null)
			{
				uICarSelectorCar.SetSelected(selected: true);
				m_selectedCarIndex = selectedCarIndex;
			}
		}
		if (forceScrollTo)
		{
			ScrollToSelectedItem();
		}
		UpdateBottomPanel();
	}

	private bool NavigateThroughItems(int direction, out bool needPlaySound)
	{
		needPlaySound = true;
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		if (selectedCategory == null)
		{
			return false;
		}
		int num = ((selectedCategory.type == UICarSelectorCategory.Type.AllVendors) ? m_loadedVendors.Count : m_visibleCars.Count);
		int num2 = ((selectedCategory.type == UICarSelectorCategory.Type.AllVendors) ? m_selectedVendorIndex : m_selectedCarIndex);
		int num3 = num2 + direction;
		if (direction > 1 && Mathf.CeilToInt((float)num / (float)direction) == Mathf.CeilToInt((float)(num2 + 1) / (float)direction) && num3 > num - 1)
		{
			return false;
		}
		if (num3 > num - 1)
		{
			needPlaySound = false;
			num3 = num - 1;
		}
		if (num3 < 0)
		{
			return false;
		}
		int targetItemId;
		if (selectedCategory.type == UICarSelectorCategory.Type.AllVendors)
		{
			m_selectedVendorIndex = num3;
			if (m_loadedVendors.Count == 0)
			{
				return false;
			}
			targetItemId = m_loadedVendors[m_selectedVendorIndex].vendorId;
		}
		else
		{
			m_selectedCarIndex = num3;
			if (m_visibleCars.Count == 0)
			{
				return false;
			}
			targetItemId = m_visibleCars[m_selectedCarIndex].carId;
		}
		SelectItemInCurrentCategory(targetItemId);
		return true;
	}

	private void SelectStartCategory()
	{
		int index = ((prefs.favoriteCars.cars.Count <= 0) ? 1 : 0);
		SelectCategoryByIndex(index, force: true);
	}

	private void SelectCategoryByIndex(int index, bool force = false)
	{
		if (index >= 0 && index <= categories.Count - 1)
		{
			OnCategorySelected(categories[index], force);
		}
	}

	private void SelectCategoryUiItem(UICarSelectorCategory.Type type, int vendorId = -1)
	{
		if (type == UICarSelectorCategory.Type.Vendor && vendorId == -1)
		{
			return;
		}
		m_selectedCategoryIndex = 1;
		for (int i = 0; i < categories.Count; i++)
		{
			UICarSelectorCategory uICarSelectorCategory = categories[i];
			uICarSelectorCategory.selected = ((type == UICarSelectorCategory.Type.Vendor) ? (uICarSelectorCategory.vendorId == vendorId) : (uICarSelectorCategory.type == type));
			if (uICarSelectorCategory.selected)
			{
				m_selectedCategoryIndex = i;
			}
		}
	}

	private void EnsureCatItemVisibility(UICarSelectorCategory cat)
	{
		if (!(cat == null) && !m_categoriesScrollRect.IsScrollItemHorizontallyVisible(cat.rectTransform))
		{
			m_categoriesScrollRect.HorizontalSnapScrollTo(cat.rectTransform, cat.rectTransform.rect.width / 2f + 8f);
		}
	}

	private void ScrollToSelectedItem(bool interruptable = true)
	{
		if (!(base.gameObject == null) && base.gameObject.activeSelf)
		{
			StopScrollCoroutine();
			m_scrollCoroutine = TimeController.StartNow(TryScrollToSelectedItemAsync(interruptable));
		}
	}

	private void StopScrollCoroutine()
	{
		if (m_scrollCoroutine != null)
		{
			TimeController.StopNow(m_scrollCoroutine);
			m_scrollCoroutine = null;
		}
	}

	private IEnumerator TryScrollToSelectedItemAsync(bool interruptable)
	{
		m_mainScrollRect.enabled = interruptable;
		UICarSelectorCategory cat = GetSelectedCategory();
		if (cat == null)
		{
			yield break;
		}
		if (!interruptable)
		{
			yield return null;
		}
		RectTransform target = null;
		if (cat.type == UICarSelectorCategory.Type.AllVendors)
		{
			UICarSelectorVendor selectedVendor = GetSelectedVendor();
			if (selectedVendor != null)
			{
				target = selectedVendor.rectTransform;
			}
		}
		else
		{
			UICarSelectorCar selectedCar = GetSelectedCar();
			if (selectedCar != null)
			{
				target = selectedCar.rectTransform;
			}
		}
		if (!(target == null))
		{
			if (!interruptable)
			{
				yield return null;
			}
			float height = target.GetHeight();
			float yPos = target.anchoredPosition.y + height / 2f;
			yield return ScrollTo(yPos, height, interruptable);
			m_mainScrollRect.enabled = true;
		}
	}

	private IEnumerator ScrollTo(float YPos, float elementHeight = 0f, bool interruptable = true)
	{
		m_needLerpScroll = true;
		while (m_needLerpScroll)
		{
			m_needLerpScroll = !IsScrollWasInterrupted() || !interruptable;
			EnsureVisibleCurrentFocus(YPos, elementHeight, ref m_needLerpScroll);
			yield return m_endOfFrame;
		}
	}

	private void EnsureVisibleCurrentFocus(float topPos, float elementHeight, ref bool needLerpScroll)
	{
		topPos += 10f;
		if (!m_mainScrollRect.IsScrollItemFloatVerticalyVisible(topPos, elementHeight, 19f) & needLerpScroll)
		{
			m_mainScrollRect.SnapScrollToWithLerp(topPos, elementHeight, ref needLerpScroll, 19f, 0f, 15f);
		}
		else
		{
			needLerpScroll = false;
		}
	}

	private bool IsScrollWasInterrupted()
	{
		if (GameInput.InputManager.instance.lastController.type == ControllerType.Mouse)
		{
			if (!Input.GetMouseButton(0))
			{
				return Input.mouseScrollDelta != Vector2.zero;
			}
			return true;
		}
		return false;
	}

	private void RefreshCarsCount()
	{
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		if (!(selectedCategory == null))
		{
			if (selectedCategory.type == UICarSelectorCategory.Type.Favorites)
			{
				m_carsCount.value = $"{LocalizationKeys.player_profile_cars.Localized()} {m_visibleCars.Count}";
				return;
			}
			if (selectedCategory.type == UICarSelectorCategory.Type.AllVendors)
			{
				m_carsCount.value = $"{LocalizationKeys.car_catalog_vendors_all.Localized()} {m_loadedVendors.Count}";
				return;
			}
			int unlockedCarsCount = GetUnlockedCarsCount();
			m_carsCount.value = $"{LocalizationKeys.player_profile_cars.Localized()} {unlockedCarsCount}/{m_visibleCars.Count}";
		}
	}

	private int GetUnlockedCarsCount()
	{
		int num = 0;
		foreach (UICarSelectorCar visibleCar in m_visibleCars)
		{
			if (visibleCar != null && !prefs.locks.GetCarLockState(visibleCar.carId))
			{
				num++;
			}
		}
		return num;
	}

	private bool IsVendorCarListEmpty(int vendorId)
	{
		int num = 0;
		foreach (UICarSelectorCar loadedCar in m_loadedCars)
		{
			if (loadedCar.meta.vendorId == vendorId)
			{
				num++;
			}
		}
		return num == 0;
	}

	protected void ClearBottomPanel()
	{
		m_actionHints.Clear();
	}

	protected void RefreshBottomPanel()
	{
		ShowBottomPanel(m_actionHints);
	}

	protected void AddGoBackAction()
	{
		AddActionInBottomPanel(LocalizationKeys.nav_hint_goto_back.Localized(), ActionType.NavCancelBtnB, GoBack);
	}

	protected void AddSubmitAction()
	{
		AddActionInBottomPanel(LocalizationKeys.nav_hint_select.Localized(), ActionType.NavSelectBtnA, Submit);
	}

	protected void AddSlotChangeAction()
	{
		UICarSelectorCar selectedCar = GetSelectedCar();
		if (!(selectedCar == null))
		{
			bool flag = false;
			if ((!(States.mainCurrent is GarageGUIState)) ? (prefs.carSettings.GetUsedSlots(selectedCar.carId).Count > 1) : selectedCar.isOwned)
			{
				AddActionInBottomPanel(LocalizationKeys.car_slots_change.Localized(), ActionType.NavAdditional1BtnY, GotoSlotsContext);
			}
		}
	}

	protected void AddFollowCarAction()
	{
		UICarSelectorCategory selectedCategory = GetSelectedCategory();
		UICarSelectorCar selectedCar = GetSelectedCar();
		if (selectedCategory != null && selectedCategory.type != UICarSelectorCategory.Type.AllVendors && selectedCar != null && selectedCar.isOwned)
		{
			LocalizationKeys.Key key = (selectedCar.isFavorite ? LocalizationKeys.car_catalog_remove_from_favorites : LocalizationKeys.car_catalog_add_to_favorites);
			m_actionHints.Add(new ActionHintData
			{
				hint = key.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = ToggleFavorite
			});
		}
	}

	protected void AddActionInBottomPanel(string text, ActionType type, System.Action action = null)
	{
		m_actionHints.Add(new ActionHintData
		{
			hint = text,
			type = type,
			action = action
		});
	}

	public virtual void UpdateBottomPanel()
	{
		ClearBottomPanel();
		AddGoBackAction();
		AddSubmitAction();
		AddSlotChangeAction();
		AddFollowCarAction();
		RefreshBottomPanel();
	}
}
