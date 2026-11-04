using Binding;
using Binding.Base;
using UnityEngine;

public class CarUsageStatItemController : BaseItemController<CarUsageStatListItem>
{
	public Property<Sprite> iconCar = new SpriteProperty();

	public Property<string> nameCar = new StringProperty();

	public Property<float> percent = new FloatProperty();

	public Property<string> hours = new StringProperty();

	public override void OnClick()
	{
		base.OnClick();
	}

	public override void SetItem(CarUsageStatListItem listItem)
	{
		if (listItem != null)
		{
			iconCar.value = listItem.GetIcon();
			nameCar.value = listItem.name;
			percent.value = listItem.percent;
			hours.value = listItem.hours;
		}
		base.SetItem(listItem);
	}
}
