using System;
using Binding;
using Binding.Base;
using Binding.Components;
using GameInput;
using UnityEngine;
using UnityEngine.UI;

public class UIBodyCategory : BaseItemController<BodyCategoryItem>
{
	public Property<Sprite> icon = new SpriteProperty();

	public Property<int> categoryType = new IntProperty();

	public Property<string> title = new StringProperty();

	public Property<int> price = new IntProperty();

	public Property<bool> locked = new BoolProperty();

	public Property<bool> targeting = new BoolProperty(false);

	public Property<int> isExternal = new IntProperty();

	public System.Action selectAction;

	private Image m_buttonImage;

	private ColorToggleBinding m_spriteColorSelectBinding;

	protected override void Awake()
	{
		base.Awake();
		Button componentInChildren = GetComponentInChildren<Button>(includeInactive: true);
		if (componentInChildren != null)
		{
			m_buttonImage = componentInChildren.image;
		}
		m_spriteColorSelectBinding = GetComponentInChildren<ColorToggleBinding>(includeInactive: true);
	}

	public override void SetItem(BodyCategoryItem newItem)
	{
		if (newItem != null && base.item != newItem)
		{
			if (!newItem.useSpriteColorBinding)
			{
				m_spriteColorSelectBinding.IgnoreColorBinding(needIgnore: true);
				m_spriteColorSelectBinding.ClearImageColorBinding();
			}
			else
			{
				m_spriteColorSelectBinding.IgnoreColorBinding(needIgnore: false);
				m_spriteColorSelectBinding.RefreshImageColors();
			}
			icon.value = newItem.icon;
			categoryType.value = newItem.categoryType;
			title.value = (newItem.isFavourite ? LocalizationKeys.car_catalog_category_favorites.Localized() : newItem.title);
			price.value = newItem.price;
			locked.value = newItem.locked;
			targeting.value = newItem.targeting;
			bool flag = newItem.icon == null && (newItem.context.customizeMode == UIBodyCustomizationContext.CustomizeMode.Part || newItem.context.customizeMode == UIBodyCustomizationContext.CustomizeMode.Tires);
			bool num = newItem.icon == null && newItem.context.customizeMode == UIBodyCustomizationContext.CustomizeMode.Wheels;
			isExternal.value = 0;
			if (num)
			{
				isExternal.value = 1;
			}
			else if (flag)
			{
				isExternal.value = 2;
			}
			base.SetItem(newItem);
		}
	}

	public override void OnClick()
	{
		if (m_item != null)
		{
			InputManager.instance.PlayClickEffect(null, m_item.OnClick, skipPauseToAction: true);
		}
	}

	public override void OnDoubleClick()
	{
		InputManager.instance.PlayClickEffect(null, base.OnDoubleClick, skipPauseToAction: true);
	}
}
