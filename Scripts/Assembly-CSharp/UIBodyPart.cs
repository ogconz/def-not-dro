using Binding;
using Binding.Base;
using GameInput;
using GameOverlay;
using UnityEngine;
using UnityEngine.UI;

public class UIBodyPart : BaseItemController<BasePartItem>
{
	public Property<Sprite> icon = new SpriteProperty();

	public Property<string> title = new StringProperty();

	public Property<bool> locked = new BoolProperty();

	public Property<string> price = new StringProperty();

	public Property<string> description = new StringProperty();

	public BoolProperty freeDlc = new BoolProperty();

	public BoolProperty isFavourite = new BoolProperty();

	public Property<bool> m_isExternalPart = new BoolProperty();

	public Property<bool> equipped = new BoolProperty();

	private Image m_buttonImage;

	protected override void Awake()
	{
		base.Awake();
		Button componentInChildren = GetComponentInChildren<Button>(includeInactive: true);
		if (componentInChildren != null)
		{
			m_buttonImage = componentInChildren.image;
		}
	}

	public override void OnClick()
	{
		if (m_item != null)
		{
			InputManager.PlayClickSound();
			m_item.OnClick();
		}
	}

	private void LoadPartTypeSprite(BasePartItem newItem)
	{
		ResourcesWrapper.LoadAsync("ui/customization/categories/" + UIBodyCustomizationContext.GetSpriteName(newItem.part.type), delegate(Sprite sprite)
		{
			if (icon != null && newItem == base.item && newItem != null)
			{
				base.item.asset = sprite;
				icon.value = sprite;
			}
		}, "customization");
	}

	public override void SetItem(BasePartItem newItem)
	{
		if (newItem == null || (base.item == newItem && !newItem.isDirty))
		{
			return;
		}
		newItem.isDirty = false;
		if (newItem.asset == null)
		{
			if (!TryLoadSprite(newItem))
			{
				LoadPartTypeSprite(newItem);
			}
		}
		else
		{
			icon.value = newItem.asset;
		}
		title.value = newItem.title;
		locked.value = newItem.locked;
		price.value = newItem.formattedPrice;
		description.value = (newItem.isModPart ? newItem.part.id.ToString() : newItem.part.description);
		isFavourite.value = newItem.isFavourite;
		freeDlc.value = CheckCanShowFreeDLC(newItem.part.DLCCode) && newItem.part.type == "chassis";
		m_isExternalPart.value = newItem.isModPart;
		equipped.value = newItem.equipped;
		base.SetItem(newItem);
	}

	private bool TryLoadSprite(BasePartItem partItem)
	{
		bool flag = !string.IsNullOrEmpty(partItem.icon) && partItem.icon.Contains("/rims/");
		bool flag2 = partItem is TirePartItem;
		if (flag || flag2)
		{
			icon.value = null;
			ResourcesWrapper.LoadAsync((flag ? "ui/customization/" : "ui/customization/prefabs/tires/") + partItem.icon, delegate(Sprite sprite)
			{
				if (base.item != null && partItem == base.item)
				{
					if ((bool)sprite)
					{
						partItem.asset = sprite;
						icon.value = partItem.asset;
					}
					else
					{
						LoadPartTypeSprite(partItem);
					}
				}
			}, "customization");
			return true;
		}
		return false;
	}

	private bool CheckCanShowFreeDLC(DLCCode dlcCode)
	{
		if (dlcCode == DLCCode.SouthAmericaStyle)
		{
			return true;
		}
		return false;
	}
}
