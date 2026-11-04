using Customization;

public class BodyPartItem : BasePartItem
{
	public BodyPartItem(UIBodyCustomizationContext context, CarPart part, bool isModPart, bool isFavorite)
		: base(context)
	{
		base.part = part;
		base.locked = false;
		base.price = 0;
		base.isModPart = isModPart;
		isFavourite = isFavorite;
	}
}
