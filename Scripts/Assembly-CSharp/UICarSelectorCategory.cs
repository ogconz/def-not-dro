using System;
using Binding;
using Binding.Base;
using UnityEngine;

public class UICarSelectorCategory : BaseBindingBehaviourTarget
{
	public enum Type
	{
		Favorites = 0,
		All = 1,
		AllVendors = 2,
		Vendor = 3
	}

	private readonly BoolProperty m_selected = new BoolProperty(false);

	private readonly BoolProperty m_textCategory = new BoolProperty(startValue: true);

	private readonly StringProperty m_categoryName = new StringProperty();

	private readonly SpriteProperty m_sprite = new SpriteProperty();

	private Action<UICarSelectorCategory> m_onSubmit;

	public bool selected
	{
		get
		{
			return m_selected.value;
		}
		set
		{
			m_selected.value = value;
		}
	}

	public CarVendor vendor { get; private set; }

	public Type type { get; private set; }

	public int vendorId => vendor?.id ?? (-1);

	public RectTransform rectTransform { get; private set; }

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void InitFavorites(Action<UICarSelectorCategory> onSubmit)
	{
		InitInternal(Type.Favorites, null, LocalizationKeys.car_catalog_category_favorites.Localized(), onSubmit);
	}

	public void InitAllCars(Action<UICarSelectorCategory> onSubmit)
	{
		InitInternal(Type.All, null, LocalizationKeys.car_catalog_category_all.Localized(), onSubmit);
	}

	public void InitAllVendors(Action<UICarSelectorCategory> onSubmit)
	{
		InitInternal(Type.AllVendors, null, LocalizationKeys.car_catalog_vendors_all.Localized(), onSubmit);
	}

	public void InitVendor(CarVendor carVendor, Action<UICarSelectorCategory> onSubmit)
	{
		InitInternal(Type.Vendor, carVendor, "", onSubmit);
	}

	private void InitInternal(Type catType, CarVendor carVendor, string catName, Action<UICarSelectorCategory> onSubmit)
	{
		m_onSubmit = onSubmit;
		vendor = carVendor;
		type = catType;
		m_textCategory.value = !string.IsNullOrWhiteSpace(catName);
		m_categoryName.value = catName;
		m_sprite.value = vendor?.sprite;
	}

	public void OnSelect()
	{
		SoundController.Play2DUISound(SoundSample.DefaultBtn);
		m_onSubmit.SafeCall(this);
	}
}
