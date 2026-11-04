using Customization;

public class TirePartItem : BasePartItem
{
	private CarVisualManager.TireVisualSettings m_presetSettings;

	public CarVisualManager.TireVisualSettings presetSettings => m_presetSettings;

	public TirePartItem(UIBodyCustomizationContext context, CarVisualManager.TireVisualSettings paramPreset)
		: base(context)
	{
		m_presetSettings = paramPreset;
		base.part = new CarPart
		{
			type = "tires"
		};
		base.locked = false;
		base.price = 0;
	}
}
