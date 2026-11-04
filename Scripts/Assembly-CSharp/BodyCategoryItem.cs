using UnityEngine;

public class BodyCategoryItem : BaseListItem
{
	public enum CategoryType
	{
		None = 0,
		Cups = 1,
		Tires = 2
	}

	public CategoryType type;

	public Sprite icon;

	public int categoryType;

	public string title;

	public int price;

	public bool locked;

	public bool targeting;

	public bool isFavourite;

	public string target;

	public bool finalTarget;

	public int groupId;

	public bool useSpriteColorBinding = true;

	public UIBodyCustomizationContext context { get; }

	public BodyCategoryItem(UIBodyCustomizationContext context, string category, string sprite, bool isFavourite = false)
	{
		this.context = context;
		target = category;
		this.isFavourite = isFavourite;
		if (!string.IsNullOrEmpty(sprite))
		{
			icon = ResourcesWrapper.Load<Sprite>((sprite.ToLower().Contains("wheel") && !sprite.ToLower().Contains("steering")) ? ("ui/customization/categories/rimscategories/" + sprite) : ("ui/customization/categories/" + sprite), "customization");
		}
	}

	public override void OnClick()
	{
		if (!locked)
		{
			context.SelectCategory(this);
			context.NavigateCategory(this);
			Singleton<CustomCameraPresets>.instance.CameraTo(target);
			base.OnClick();
		}
	}

	public override void OnDoubleClick()
	{
		if (!locked)
		{
			base.OnDoubleClick();
		}
	}
}
